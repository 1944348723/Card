using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using TcgEngine.Client;

namespace TcgEngine.Server
{
    /// <summary>
    /// 本地服务器管理器，用于客户端本地运行的单机模式（对战 AI）
    /// 功能：
    /// - 仅包含一个 GameServer
    /// - 管理本地客户端连接
    /// - 接收本地玩家动作并转发给本地游戏
    /// </summary>
    public class ServerManagerLocal : MonoBehaviour
    {
        private GameServer server;  // 本地游戏服务器实例

        private Dictionary<ulong, ClientData> client_list = new Dictionary<ulong, ClientData>();  
        // 客户端列表，key: client_id。单机模式下通常只有本地玩家和 AI 客户端

        // -------------------- Unity 生命周期 --------------------

        protected virtual void Start()
        {
            // 如果当前客户端为主机（Host），启动本地服务器
            if (GameClient.game_settings.IsHost())
            {
                StartServer(); 
            }
        }

        protected virtual void StartServer()
        {
            TcgNetwork network = TcgNetwork.Get();

            // 注册客户端加入和离开事件
            network.onClientJoin += OnClientJoin;
            network.onClientQuit += OnClientQuit;

            // 注册消息监听
            network.Messaging.ListenMsg("connect", ReceiveConnectPlayer); // 玩家请求连接
            network.Messaging.ListenMsg("action", ReceiveGameAction);     // 玩家发送动作

            // 将本地客户端加入客户端列表
            client_list[network.ServerID] = new ClientData(network.ServerID); 

            // 创建本地游戏服务器实例
            server = new GameServer(GameClient.game_settings.game_uid, GameClient.game_settings.nb_players, false);
        }

        protected virtual void OnDestroy()
        {
            // 注销事件，避免内存泄漏
            TcgNetwork network = TcgNetwork.Get();
            if (network != null)
            {
                network.onClientJoin -= OnClientJoin;
                network.onClientQuit -= OnClientQuit;
                network.Messaging.UnListenMsg("connect");
                network.Messaging.UnListenMsg("action");
            }
        }

        // -------------------- 客户端连接/断开 --------------------

        /// <summary>
        /// 客户端加入事件
        /// </summary>
        protected virtual void OnClientJoin(ulong client_id)
        {
            // 新增客户端数据到列表
            client_list[client_id] = new ClientData(client_id);
        }

        /// <summary>
        /// 客户端离开事件
        /// </summary>
        protected virtual void OnClientQuit(ulong client_id)
        {
            ClientData client = GetClient(client_id);
            // 从本地游戏服务器移除客户端
            server?.RemoveClient(client);
            // 从客户端列表中移除
            client_list.Remove(client_id);
        }

        // -------------------- 游戏更新 --------------------

        protected virtual void Update()
        {
            // 更新游戏逻辑
            if (server != null)
                server.Update();
        }

        // -------------------- 接收客户端消息 --------------------

        /// <summary>
        /// 客户端请求连接到游戏
        /// </summary>
        protected virtual void ReceiveConnectPlayer(ulong client_id, FastBufferReader reader)
        {
            reader.ReadNetworkSerializable(out MsgPlayerConnect msg);

            if (msg != null)
            {
                // 检查用户名和游戏 UID
                if (string.IsNullOrWhiteSpace(msg.username))
                    return;
                if (string.IsNullOrWhiteSpace(msg.game_uid))
                    return;

                ClientData client = GetClient(client_id);
                if (client == null)
                    return;

                // 检查是否可以连接游戏（已存在玩家或未满人数）
                bool can_connect = server.IsPlayer(msg.user_id) || server.CountPlayers() < server.playersCount;
                if (can_connect)
                {
                    // 设置客户端数据
                    client.game_uid = msg.game_uid;
                    client.user_id = msg.user_id;
                    client.username = msg.username;

                    // 将客户端加入服务器
                    server.AddClient(client);

                    // 添加玩家到游戏
                    int player_id = server.AddPlayer(client);

                    // 返回连接结果给客户端
                    MsgAfterConnected msg_data = new MsgAfterConnected();
                    msg_data.success = true;
                    msg_data.player_id = player_id;
                    msg_data.game_data = server.GetGameData();
                    SendToClient(client_id, GameAction.Connected, msg_data, NetworkDelivery.ReliableFragmentedSequenced);
                }
            }
        }

        /// <summary>
        /// 接收客户端游戏动作
        /// </summary>
        protected virtual void ReceiveGameAction(ulong client_id, FastBufferReader reader)
        {
            ClientData client = GetClient(client_id);
            if (client != null)
            {
                // 仅处理已连接玩家的动作
                if (server.IsConnectedPlayer(client.user_id))
                    server.ReceiveCommand(client_id, reader);
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

        // -------------------- 客户端查询 --------------------
        public ClientData GetClient(ulong client_id)
        {
            if (client_list.ContainsKey(client_id))
                return client_list[client_id];
            return null;
        }

        // -------------------- 辅助属性 --------------------
        public ulong ServerID { get { return TcgNetwork.Get().ServerID; } }
        public NetworkMessaging Messaging { get { return TcgNetwork.Get().Messaging; } }
    }
}
