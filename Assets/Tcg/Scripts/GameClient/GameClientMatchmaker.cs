using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Events;
using TcgEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>
    /// 主客户端匹配器脚本
    /// 用于向服务器发送匹配请求，并在匹配成功或失败时接收响应
    /// </summary>
    public class GameClientMatchmaker : MonoBehaviour
    {
        public UnityAction<MatchmakingResult> onMatchmaking;        // 匹配结果回调
        public UnityAction<MatchmakingList> onMatchmakingList;      // 当前匹配列表回调
        public UnityAction<MatchList> onMatchList;                 // 当前比赛列表回调

        private bool matchmaking = false;        // 是否正在匹配
        private float timer = 0f;                // 匹配请求计时器
        private float match_timer = 0f;          // 总匹配时间计时器
        private string matchmaking_group;        // 匹配组名
        private int matchmaking_players;         // 匹配玩家数量
        private UnityAction<bool> connect_callback; // 连接回调

        private static GameClientMatchmaker _instance; // 单例实例

        void Awake()
        {
            _instance = this; // 设置单例
        }

        private void Start()
        {
            TcgNetwork.Get().onConnect += OnConnect;             // 注册连接事件
            TcgNetwork.Get().onDisconnect += OnDisconnect;       // 注册断开事件
            Messaging.ListenMsg(NetworkMessageName.Matchmaking, ReceiveMatchmaking);        // 监听匹配结果消息
            Messaging.ListenMsg(NetworkMessageName.MatchmakingList, ReceiveMatchmakingList); // 监听匹配列表消息
            Messaging.ListenMsg(NetworkMessageName.MatchList, ReceiveMatchList);             // 监听比赛列表消息
        }

        private void OnDestroy()
        {
            Disconnect(); // 切换场景时断开连接

            if (TcgNetwork.Get() != null)
            {
                TcgNetwork.Get().onConnect -= OnConnect;
                TcgNetwork.Get().onDisconnect -= OnDisconnect;
                Messaging.UnListenMsg(NetworkMessageName.Matchmaking);
                Messaging.UnListenMsg(NetworkMessageName.MatchmakingList);
                Messaging.UnListenMsg(NetworkMessageName.MatchList);
            }
        }

        void Update()
        {
            if (matchmaking)
            {
                timer += Time.deltaTime;
                match_timer += Time.deltaTime;

                // 周期性发送匹配请求
                if (IsConnected() && timer > 2f)
                {
                    timer = 0f;
                    SendMatchRequest(true, matchmaking_group, matchmaking_players);
                }

                // 断开连接，停止匹配
                if (!IsConnected() && !IsConnecting() && timer > 5f)
                {
                    StopMatchmaking();
                }
            }
        }

        /// <summary>
        /// 开始匹配
        /// </summary>
        public void StartMatchmaking(string group, int nb_players)
        {
            if (matchmaking)
                StopMatchmaking();

            Debug.Log("Start Matchmaking!");
            matchmaking_group = group;
            matchmaking_players = nb_players;
            matchmaking = true;
            match_timer = 0f;
            timer = 0f;

            Connect(NetworkData.Get().url, NetworkData.Get().port, (bool success) =>
            {
                if (success)
                {
                    SendMatchRequest(false, group, nb_players); // 第一次请求
                }
                else
                {
                    StopMatchmaking();
                }
            });
        }

        /// <summary>
        /// 停止匹配
        /// </summary>
        public void StopMatchmaking()
        {
            if (matchmaking)
            {
                Debug.Log("Stop Matchmaking!");
                onMatchmaking?.Invoke(null);
                matchmaking_group = "";
                matchmaking_players = 0;
                matchmaking = false;
            }
        }

        /// <summary>
        /// 刷新匹配列表
        /// </summary>
        public void RefreshMatchmakingList()
        {
            Connect(NetworkData.Get().url, NetworkData.Get().port, (bool success) =>
            {
                if(success)
                    SendMatchmakingListRequest();
            });
        }

        /// <summary>
        /// 刷新比赛列表
        /// </summary>
        public void RefreshMatchList(string username)
        {
            Connect(NetworkData.Get().url, NetworkData.Get().port, (bool success) =>
            {
                if (success)
                    SendMatchListRequest(username);
            });
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        public void Connect(string url, ushort port, UnityAction<bool> callback=null)
        {
            // 必须登录API才能连接
            if(!Authenticator.Get().IsSignedIn())
            {
                callback?.Invoke(false);
                return;
            }

            // 已连接或正在连接，直接返回
            if (IsConnected() || IsConnecting())
            {
                callback?.Invoke(IsConnected());
                return;
            }

            connect_callback = callback;
            TcgNetwork.Get().StartClient(url, port);
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            TcgNetwork.Get()?.Disconnect();
        }

        /// <summary>
        /// 连接成功回调
        /// </summary>
        private void OnConnect()
        {
            Debug.Log("Connected to server!");
            connect_callback?.Invoke(true);
            connect_callback = null;
        }

        /// <summary>
        /// 断开连接回调
        /// </summary>
        private void OnDisconnect()
        {
            StopMatchmaking(); // 停止匹配
            connect_callback?.Invoke(false);
            connect_callback = null;
            matchmaking = false;
        }

        /// <summary>
        /// 发送匹配请求
        /// </summary>
        private void SendMatchRequest(bool refresh, string group, int nb_players)
        {
            MsgMatchmaking msg_match = new MsgMatchmaking();
            UserData udata = Authenticator.Get().GetUserData();
            msg_match.user_id = Authenticator.Get().GetUserId();
            msg_match.username = Authenticator.Get().GetUsername();
            msg_match.group = group;
            msg_match.players = nb_players;
            msg_match.elo = udata.elo;
            msg_match.time = match_timer;
            msg_match.refresh = refresh;
            Messaging.SendObject(NetworkMessageName.Matchmaking, ServerID, msg_match, NetworkDelivery.Reliable);
        }

        /// <summary>
        /// 请求匹配列表
        /// </summary>
        private void SendMatchmakingListRequest()
        {
            MsgMatchmakingList msg_match = new MsgMatchmakingList();
            msg_match.username = ""; // 返回所有用户
            Messaging.SendObject(NetworkMessageName.MatchmakingList, ServerID, msg_match, NetworkDelivery.Reliable);
        }

        /// <summary>
        /// 请求比赛列表
        /// </summary>
        private void SendMatchListRequest(string username)
        {
            MsgMatchmakingList msg_match = new MsgMatchmakingList();
            msg_match.username = username;
            Messaging.SendObject(NetworkMessageName.MatchList, ServerID, msg_match, NetworkDelivery.Reliable);
        }

        /// <summary>
        /// 接收匹配结果
        /// </summary>
        private void ReceiveMatchmaking(ulong client_id, FastBufferReader reader)
        {
            reader.ReadNetworkSerializable(out MatchmakingResult msg);

            if (IsConnected() && matchmaking && matchmaking_group == msg.group)
            {
                matchmaking = !msg.success; // 如果匹配成功，停止匹配
                onMatchmaking?.Invoke(msg);
            }
        }

        /// <summary>
        /// 接收匹配列表
        /// </summary>
        private void ReceiveMatchmakingList(ulong client_id, FastBufferReader reader)
        {
            reader.ReadNetworkSerializable(out MatchmakingList list);
            onMatchmakingList?.Invoke(list);
        }

        /// <summary>
        /// 接收比赛列表
        /// </summary>
        private void ReceiveMatchList(ulong client_id, FastBufferReader reader)
        {
            reader.ReadNetworkSerializable(out MatchList list);
            onMatchList?.Invoke(list);
        }

        /// <summary>
        /// 是否正在匹配
        /// </summary>
        public bool IsMatchmaking()
        {
            return matchmaking;
        }

        /// <summary>
        /// 获取匹配组名
        /// </summary>
        public string GetGroup()
        {
            return matchmaking_group;
        }

        /// <summary>
        /// 获取匹配玩家数量
        /// </summary>
        public int GetNbPlayers()
        {
            return matchmaking_players;
        }

        /// <summary>
        /// 获取匹配时间
        /// </summary>
        public float GetTimer()
        {
            return match_timer;
        }

        /// <summary>
        /// 是否已连接
        /// </summary>
        public bool IsConnected()
        {
            return TcgNetwork.Get().IsConnected();
        }

        /// <summary>
        /// 是否正在连接
        /// </summary>
        public bool IsConnecting()
        {
            return TcgNetwork.Get().IsConnecting();
        }

        public ulong ServerID { get { return TcgNetwork.Get().ServerID; } }           // 获取服务器ID
        public NetworkMessaging Messaging { get { return TcgNetwork.Get().Messaging; } } // 获取消息通信对象

        public static GameClientMatchmaker Get()
        {
            return _instance; // 获取单例实例
        }
    }

}
