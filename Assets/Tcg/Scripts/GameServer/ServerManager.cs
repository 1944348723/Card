using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;
using Unity.Netcode;

namespace TcgEngine.Server
{
    /// <summary>
    /// 顶层服务器管理脚本
    /// 功能：
    /// - 管理新的客户端连接
    /// - 为玩家分配到对应的游戏（GameServer）
    /// - 接收玩家动作并转发到对应游戏
    /// - 支持同时管理多个游戏
    /// </summary>
    public class ServerManager : MonoBehaviour
    {
        [Header("API")]
        public string api_username;  // 用于服务器 API 登录的用户名
        public string api_password;  // 用于服务器 API 登录的密码

        // --- 内部数据结构 ---
        private Dictionary<ulong, ClientData> client_list = new Dictionary<ulong, ClientData>();  // 所有客户端列表，key: client_id
        private Dictionary<string, GameServer> game_list = new Dictionary<string, GameServer>(); // 游戏列表，key: game_uid
        private List<string> game_remove_list = new List<string>(); // 待移除的游戏 UID 列表

        private float login_timer = 0f; // API 登录计时器，用于定期重新登录

        // -------------------- Unity 生命周期方法 --------------------

        protected virtual void Awake()
        {
            Application.runInBackground = true; // 后台运行，保证服务器即使最小化也能运行
            Application.targetFrameRate = 200;  // 限制帧率，防止 CPU 占用过高
        }

        protected virtual void Start()
        {
            TcgNetwork network = TcgNetwork.Get();

            // 注册网络客户端连接和断开事件
            network.onClientJoin += OnClientConnected;
            network.onClientQuit += OnClientDisconnected;

            // 注册接收消息回调
            Messaging.ListenMsg("connect", ReceiveConnectPlayer); // 处理客户端请求连接游戏
            Messaging.ListenMsg("action", ReceiveGameAction);     // 处理客户端游戏动作

            // 如果网络服务未启动，则启动服务器
            if (!network.IsActive())
            {
                network.StartServer(NetworkData.Get().port);
            }

            // 尝试登录 API
            Login();
        }

        protected virtual void Update()
        {
            // 更新所有游戏并移除没有玩家的游戏
            foreach (KeyValuePair<string, GameServer> pair in game_list)
            {
                GameServer gserver = pair.Value;
                gserver.Update(); // 更新游戏逻辑

                // 如果游戏过期（无人连接或游戏结束），加入移除列表
                if (gserver.IsGameExpired())
                    game_remove_list.Add(pair.Key);
            }

            // 移除过期游戏
            foreach (string key in game_remove_list)
            {
                game_list.Remove(key);

                // 如果使用匹配系统，结束匹配
                if (ServerMatchmaker.Get())
                    ServerMatchmaker.Get().EndMatch(key);
            }
            game_remove_list.Clear();

            // 定期尝试重新登录 API（每 15 秒）
            login_timer += Time.deltaTime;
            if (login_timer > 15f && !Authenticator.Get().IsConnected())
            {
                login_timer = 0f;
                Login();
            }
        }

        // -------------------- API 登录 --------------------
        protected virtual async void Login()
        {
            await Authenticator.Get().Login(api_username, api_password);

            bool success = Authenticator.Get().IsConnected();
            int permission = Authenticator.Get().GetPermission();
            string api = Authenticator.Get().IsApi() ? "API" : "Local";

            Debug.Log(api + " authentication: " + success + " (" + permission + ")");

            // 如果登录失败，则 5 秒后再次尝试
            if (!success)
            {
                TimeTool.WaitFor(5f, () =>
                {
                    if (!Authenticator.Get().IsConnected())
                    {
                        Login();
                    }
                });
            }
        }

        // -------------------- 客户端连接事件 --------------------
        protected virtual void OnClientConnected(ulong client_id)
        {
            // 新建客户端数据
            ClientData iclient = new ClientData(client_id);
            client_list[client_id] = iclient;
        }

        protected virtual void OnClientDisconnected(ulong client_id)
        {
            // 移除客户端
            ClientData iclient = GetClient(client_id);
            client_list.Remove(client_id);
            ReceiveDisconnectPlayer(iclient);
        }

        // -------------------- 接收客户端消息 --------------------

        /// <summary>
        /// 客户端请求连接到游戏
        /// </summary>
        protected virtual void ReceiveConnectPlayer(ulong client_id, FastBufferReader reader)
        {
            ClientData iclient = GetClient(client_id);
            reader.ReadNetworkSerializable(out MsgPlayerConnect msg);

            if (iclient != null && msg != null)
            {
                if (string.IsNullOrWhiteSpace(msg.username))
                    return;
                if (string.IsNullOrWhiteSpace(msg.game_uid))
                    return;

                Debug.Log("Client " + client_id + " connecting to game: " + msg.game_uid);

                // 根据 observer 标记，连接为玩家或观察者
                if (msg.observer)
                    ConnectObserverToGame(iclient, msg.user_id, msg.username, msg.game_uid);
                else
                    ConnectPlayerToGame(iclient, msg.user_id, msg.username, msg.game_uid, msg.nb_players);

                // 刷新游戏数据给所有客户端
                GameServer gserver = GetGame(msg.game_uid);
                if(gserver != null)
                    gserver.RefreshAll();
            }
        }

        /// <summary>
        /// 客户端断开连接时处理
        /// </summary>
        protected virtual void ReceiveDisconnectPlayer(ClientData iclient)
        {
            if (iclient == null)
                return;

            GameServer gserver = GetGame(iclient.game_uid);
            if (gserver != null)
            {
                gserver.RemoveClient(iclient);
            }
        }

        /// <summary>
        /// 客户端发送游戏动作
        /// </summary>
        protected virtual void ReceiveGameAction(ulong client_id, FastBufferReader reader)
        {
            ClientData client = GetClient(client_id);
            if (client != null)
            {
                GameServer gserver = GetGame(client.game_uid);
                if (gserver != null && gserver.IsConnectedPlayer(client.user_id))
                    gserver.ReceiveAction(client_id, reader);
            }
        }

        // -------------------- 玩家连接游戏 --------------------

        /// <summary>
        /// 玩家请求连接游戏
        /// </summary>
        protected virtual void ConnectPlayerToGame(ClientData client, string user_id, string username, string game_uid, int nb_players)
        {
            GameServer gserver = GetGame(game_uid);

            // 如果游戏不存在，则创建新游戏
            if (gserver == null)
                gserver = CreateGame(game_uid, nb_players);

            bool can_connect = gserver.IsPlayer(user_id) || gserver.CountPlayers() < gserver.nb_players;
            if (gserver != null && can_connect)
            {
                // 设置客户端数据
                client.game_uid = game_uid;
                client.user_id = user_id;
                client.username = username;

                // 将客户端加入游戏
                gserver.AddClient(client);

                // 添加玩家到游戏
                int player_id = gserver.AddPlayer(client);

                // 发送连接成功消息给客户端
                MsgAfterConnected msg_data = new MsgAfterConnected();
                msg_data.success = true;
                msg_data.player_id = player_id;
                msg_data.game_data = gserver.GetGameData();
                SendToClient(client.client_id, GameAction.Connected, msg_data, NetworkDelivery.ReliableFragmentedSequenced);
            }
        }

        /// <summary>
        /// 玩家作为观察者连接游戏
        /// </summary>
        protected virtual void ConnectObserverToGame(ClientData client, string user_id, string username, string game_uid)
        {
            GameServer gserver = GetGame(game_uid);
            if (gserver != null && client != null)
            {
                client.game_uid = game_uid;
                client.user_id = user_id;
                client.username = username;
                gserver.AddClient(client);

                MsgAfterConnected msg_data = new MsgAfterConnected();
                msg_data.success = true;
                msg_data.player_id = -1; // 观察者没有玩家 ID
                msg_data.game_data = gserver.GetGameData();
                SendToClient(client.client_id, GameAction.Connected, msg_data, NetworkDelivery.ReliableFragmentedSequenced);
            }
        }

        // -------------------- 发送消息给客户端 --------------------
        public void SendToClient(ulong client_id, ushort tag, INetworkSerializable data, NetworkDelivery delivery)
        {
            FastBufferWriter writer = new FastBufferWriter(128, Unity.Collections.Allocator.Temp, TcgNetwork.MsgSizeMax);
            writer.WriteValueSafe(tag);
            writer.WriteNetworkSerializable(data);
            Messaging.Send("refresh", client_id, writer, delivery);
            writer.Dispose();
        }

        public void SendMsgToClient(ushort client_id, string msg)
        {
            FastBufferWriter writer = new FastBufferWriter(128, Unity.Collections.Allocator.Temp, TcgNetwork.MsgSizeMax);
            writer.WriteValueSafe(GameAction.ServerMessage);
            writer.WriteValueSafe(msg);
            Messaging.Send("refresh", client_id, writer, NetworkDelivery.Reliable);
            writer.Dispose();
        }

        // -------------------- 游戏管理 --------------------
        public GameServer CreateGame(string uid, int nb_players)
        {
            GameServer game = new GameServer(uid, nb_players, true);
            game_list[game.game_uid] = game;
            return game;
        }

        public void RemoveGame(string game_id)
        {
            game_list.Remove(game_id);
        }

        public GameServer GetGame(string game_uid)
        {
            if (string.IsNullOrEmpty(game_uid))
                return null;
            if (game_list.ContainsKey(game_uid))
                return game_list[game_uid];
            return null;
        }

        // -------------------- 客户端查询 --------------------
        public ClientData GetClient(ulong client_id)
        {
            if (client_list.ContainsKey(client_id))
                return client_list[client_id];
            return null;
        }

        public ClientData GetClientByUser(string username)
        {
            foreach (KeyValuePair<ulong, ClientData> pair in client_list)
            {
                if (pair.Value.username == username)
                    return pair.Value;
            }
            return null;
        }

        // -------------------- 辅助属性 --------------------
        public ulong ServerID { get { return TcgNetwork.Get().ServerID; } }
        public NetworkMessaging Messaging { get { return TcgNetwork.Get().Messaging; } }
    }

    // -------------------- 客户端数据结构 --------------------
    public class ClientData
    {
        public ulong client_id; // 连接索引
        public string user_id;  // 玩家在认证系统中的唯一 ID
        public string username; // 玩家用户名
        public string game_uid; // 玩家所属游戏 UID

        public ClientData(ulong id) { client_id = id; }
    }

    // -------------------- 命令事件 --------------------
    public class CommandEvent
    {
        public ushort tag; // 命令类型标识
        public UnityAction<ClientData, SerializedData> callback; // 命令回调函数
    }
}
