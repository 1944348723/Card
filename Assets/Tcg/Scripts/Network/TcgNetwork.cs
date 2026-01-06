using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace TcgEngine
{
    // 主网络管理脚本，处理服务器与客户端的连接
    // 这是该资源包中少数需要挂在 DontDestroyOnLoad 对象上的脚本之一
    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(NetworkManager))]
    [RequireComponent(typeof(TcgTransport))]
    public class TcgNetwork : MonoBehaviour
    {
        public NetworkData data; // 网络配置数据

        // 服务器和客户端事件
        public UnityAction onTick;        // 每个网络帧触发
        public UnityAction onConnect;     // 自身连接成功时触发，发生在 onReady 之前，发送任何数据之前
        public UnityAction onDisconnect;  // 自身断开连接时触发

        // 仅服务器事件
        public UnityAction<ulong> onClientJoin;   // 客户端连接服务器时触发
        public UnityAction<ulong> onClientQuit;   // 客户端断开连接时触发
        public UnityAction<ulong> onClientReady;  // 客户端就绪时触发

        // 客户端连接验证事件
        public delegate bool ApprovalEvent(ulong client_id, ConnectionData connect_data);
        public ApprovalEvent checkApproval; // 客户端连接时的额外验证逻辑

        // 内部组件
        private NetworkManager network;
        private TcgTransport transport;
        private NetworkMessaging messaging;
        private Authenticator auth;
        private ConnectionData connection;

        [System.NonSerialized]
        private static bool inited = false; // 是否已初始化
        private static TcgNetwork instance;  // 单例

        private const int msg_size = 1024 * 1024;
        private bool offline_mode = false;  // 是否为离线模式
        private bool connected = false;     // 是否已连接

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject); // 已有实例，销毁新对象
                return;
            }

            Init();
            DontDestroyOnLoad(gameObject); // 保持在场景切换中不销毁
        }

        // 初始化网络组件
        public void Init()
        {
            if (!inited || transport == null)
            {
                instance = this;
                inited = true;
                network = GetComponent<NetworkManager>();
                transport = GetComponent<TcgTransport>();
                messaging = new NetworkMessaging(this);
                connection = new ConnectionData();
                transport.Init();

                network.ConnectionApprovalCallback += ApprovalCheck;
                network.OnClientConnectedCallback += OnClientConnect;
                network.OnClientDisconnectCallback += OnClientDisconnect;

                InitAuth();
            }
        }

        void Update()
        {
            // 这里可扩展定时逻辑
        }

        // 启动主机（客户端+服务器）
        public void StartHost(ushort port)
        {
            Debug.Log("Host Server Port " + port);
            transport.SetServer(port);
            connection.user_id = auth.UserID;
            connection.username = auth.Username;
            network.NetworkConfig.ConnectionData = NetworkTool.NetSerialize(connection);
            offline_mode = false;
            network.StartHost();
            AfterConnected();
        }

        // 启动独立服务器
        public void StartServer(ushort port)
        {
            Debug.Log("Start Server Port " + port);
            transport.SetServer(port);
            connection.user_id = "";
            connection.username = "";
            network.NetworkConfig.ConnectionData = NetworkTool.NetSerialize(connection);
            offline_mode = false;
            network.StartServer();
            AfterConnected();
        }

        // 启动客户端并连接服务器
        public void StartClient(string server_url, ushort port)
        {
            Debug.Log("Join Server: " + server_url + " " + port);
            transport.SetClient(server_url, port);
            connection.user_id = auth.UserID;
            connection.username = auth.Username;
            network.NetworkConfig.ConnectionData = NetworkTool.NetSerialize(connection);
            offline_mode = false;
            network.StartClient();
        }

        // 启动离线主机，所有网络功能关闭
        public void StartHostOffline()
        {
            Debug.Log("Host Offline");
            Disconnect();
            offline_mode = true;
            AfterConnected();
        }

        // 断开连接
        public void Disconnect()
        {
            if (!IsClient && !IsServer)
                return;

            Debug.Log("Disconnect");
            network.Shutdown();
            AfterDisconnected();
        }

        // 设置连接额外数据（字节数组）
        public void SetConnectionExtraData(byte[] bytes)
        {
            connection.extra = bytes;
        }

        // 设置连接额外数据（字符串）
        public void SetConnectionExtraData(string data)
        {
            connection.extra = NetworkTool.SerializeString(data);
        }

        // 设置连接额外数据（可序列化对象）
        public void SetConnectionExtraData<T>(T data) where T : INetworkSerializable, new()
        {
            connection.extra = NetworkTool.NetSerialize(data);
        }

        // 初始化身份认证
        private async void InitAuth()
        {
            auth = Authenticator.Create(data.auth_type);
            await auth.Initialize();
        }

        // 连接成功后调用
        private void AfterConnected()
        {
            if (connected)
                return;

            if (network.NetworkTickSystem != null)
                network.NetworkTickSystem.Tick += OnTick;

            connected = true;
            onConnect?.Invoke();
        }

        // 断开连接后调用
        private void AfterDisconnected()
        {
            if (!connected)
                return;

            if (network.NetworkTickSystem != null)
                network.NetworkTickSystem.Tick -= OnTick;

            offline_mode = false;
            connected = false;
            onDisconnect?.Invoke();
        }

        // 客户端连接回调
        private void OnClientConnect(ulong client_id)
        {
            if (IsServer && client_id != ServerID)
            {
                Debug.Log("Client Connected: " + client_id);
                onClientJoin?.Invoke(client_id);
            }

            if (!IsServer)
                AfterConnected(); // 客户端连接成功后的处理
        }

        // 客户端断开回调
        private void OnClientDisconnect(ulong client_id)
        {
            if (IsServer && client_id != ServerID)
            {
                Debug.Log("Client Disconnected: " + client_id);
                onClientQuit?.Invoke(client_id);
            }

            if (ClientID == client_id || client_id == ServerID)
                AfterDisconnected();
        }

        // 网络帧更新回调
        private void OnTick()
        {
            onTick?.Invoke();
        }

        // 客户端连接审批回调
        private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest req, NetworkManager.ConnectionApprovalResponse res)
        {
            ConnectionData connect = NetworkTool.NetDeserialize<ConnectionData>(req.Payload);
            bool approved = ApproveClient(req.ClientNetworkId, connect);
            res.Approved = approved;
        }
        
        // 审核客户端连接请求
        private bool ApproveClient(ulong client_id, ConnectionData connect)
        {
            if (client_id == ServerID)
                return true; // 服务器总是通过自身连接

            if (offline_mode)
                return false; // 离线模式不允许其他客户端连接

            if (connect == null)
                return false; // 无效连接数据

            if (string.IsNullOrEmpty(connect.username) || string.IsNullOrEmpty(connect.user_id))
                return false; // 用户名或用户ID无效

            if (checkApproval != null && !checkApproval.Invoke(client_id, connect))
                return false; // 自定义审核未通过

            return true; // 新客户端审核通过
        }

        // 获取当前所有客户端ID
        public IReadOnlyList<ulong> GetClientsIds()
        {
            return network.ConnectedClientsIds;
        }

        // 统计当前客户端数量
        public int CountClients()
        {
            if (offline_mode)
                return 1; // 离线模式下视为1个客户端
            if (IsServer && IsConnected())
                return network.ConnectedClientsIds.Count;
            return 0;
        }

        // 是否正在尝试连接（已激活但未连接）
        public bool IsConnecting()
        {
            return IsActive() && !IsConnected();
        }

        // 是否已连接
        public bool IsConnected()
        {
            return offline_mode || network.IsServer || network.IsConnectedClient;
        }

        // 网络是否已激活（客户端或服务器处于活动状态）
        public bool IsActive()
        {
            return offline_mode || network.IsServer || network.IsClient;
        }

        // 当前网络地址
        public string Address
        {
            get { return transport.GetAddress(); }
        }

        // 当前端口
        public ushort Port
        {
            get { return transport.GetPort(); }
        }

        // 当前客户端ID（如果是主机，与ServerID相同，每次重连可能变化）
        public ulong ClientID { get { return offline_mode ? ServerID : network.LocalClientId; } }

        // 服务器ID
        public ulong ServerID { get { return NetworkManager.ServerClientId; } }

        // 是否为服务器
        public bool IsServer { get { return offline_mode || network.IsServer; } }

        // 是否为客户端
        public bool IsClient { get { return offline_mode || network.IsClient; } }

        // 是否为主机（既是客户端又是服务器）
        public bool IsHost { get { return IsClient && IsServer; } }

        // 是否在线
        public bool IsOnline { get { return !offline_mode && IsActive(); } }

        // 本地网络时间
        public NetworkTime LocalTime { get { return network.LocalTime; } }

        // 服务器网络时间
        public NetworkTime ServerTime { get { return network.ServerTime; } }

        // 每帧间隔（Tick）
        public float DeltaTick { get { return 1f / network.NetworkTickSystem.TickRate; } }

        // 获取NetworkManager
        public NetworkManager NetworkManager { get { return network; } }

        // 获取TcgTransport
        public TcgTransport Transport { get { return transport; } }

        // 获取NetworkMessaging
        public NetworkMessaging Messaging { get { return messaging; } }

        // 获取身份认证器
        public Authenticator Auth { get { return auth; } }

        // 消息最大长度
        public static int MsgSizeMax { get { return msg_size; } }
        public static int MsgSize => MsgSizeMax; // 旧名称

        // 获取单例实例
        public static TcgNetwork Get()
        {
            if (instance == null)
            {
                TcgNetwork net = FindObjectOfType<TcgNetwork>();
                net?.Init();
            }
            return instance;
        }

    }

    [System.Serializable]
    public class ConnectionData : INetworkSerializable
    {
        // 用户ID
        public string user_id = "";

        // 用户名
        public string username = "";

        // 额外数据
        public byte[] extra = new byte[0];

        // 获取额外数据的字符串形式
        public string GetExtraString()
        {
            return NetworkTool.DeserializeString(extra);
        }

        // 获取额外数据的对象形式
        public T GetExtraData<T>() where T : INetworkSerializable, new()
        {
            return NetworkTool.NetDeserialize<T>(extra);
        }

        // 网络序列化
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref user_id);
            serializer.SerializeValue(ref username);
            serializer.SerializeValue(ref extra);
        }
    }

    public class SerializedData
    {
        // 网络读取器
        private FastBufferReader reader;

        // 序列化的数据对象
        private INetworkSerializable data;

        // 预读取的字节数据
        private byte[] bytes;

        // 使用读取器初始化
        public SerializedData(FastBufferReader r) { reader = r; data = null; }

        // 使用数据对象初始化
        public SerializedData(INetworkSerializable d) { data = d; }

        // 获取字符串数据
        public string GetString()
        {
            reader.ReadValueSafe(out string msg);
            return msg;
        }

        // 获取泛型对象
        public T Get<T>() where T : INetworkSerializable, new()
        {
            if (data != null)
            {
                return (T)data;
            }
            else if (bytes != null)
            {
                data = NetworkTool.NetDeserialize<T>(bytes);
                return (T)data;
            }
            else
            {
                reader.ReadNetworkSerializable(out T val);
                data = val;
                return val;
            }
        }

        // 预读取数据到字节数组（提前读取，防止 FastBufferReader 被 Netcode 释放）
        public void PreRead()
        {
            int size = reader.Length - reader.Position;
            bytes = new byte[size];
            reader.ReadBytesSafe(ref bytes, size);
        }
    }

}
