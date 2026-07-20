using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace TcgEngine.Server
{
    /// <summary>
    /// 主匹配服务器脚本
    /// 功能：
    /// - 接收玩家匹配请求
    /// - 将玩家匹配到合适的对局
    /// - 发送游戏 UID 和服务器 URL 给玩家
    /// </summary>
    public class ServerMatchmaker : MonoBehaviour
    {
        [Header("Matchmaker")]
        public string[] servers;  // 可用服务器列表，匹配成功时随机分配

        private Dictionary<ulong, ClientData> client_list = new Dictionary<ulong, ClientData>();  
        // 已连接客户端列表，key: client_id

        private Dictionary<string, MatchPlayerData> matchmaking_players = new Dictionary<string, MatchPlayerData>();  
        // 当前在匹配队列中的玩家，约 20 秒清空一次，key: user_id

        private Dictionary<string, MatchData> matched_players = new Dictionary<string, MatchData>();  
        // 已匹配玩家信息，key: user_id -> MatchData

        private List<MatchPlayerData> valid_users = new List<MatchPlayerData>();  
        // 临时存储有效玩家，用于匹配搜索

        private float matchmake_timer = 0f;  // 匹配定时器

        private static ServerMatchmaker _instance;  // 单例实例

        // -------------------- Unity 生命周期 --------------------
        protected virtual void Awake()
        {
            _instance = this;
            Application.runInBackground = true; // 后台运行
        }

        protected virtual void Start()
        {
            TcgNetwork network = TcgNetwork.Get();
            network.onClientJoin += OnClientConnected;
            network.onClientQuit += OnClientDisconnected;

            // 注册匹配相关消息监听
            Messaging.ListenMsg(NetworkMessageName.Matchmaking, ReceiveMatchmaking);          // 玩家请求匹配
            Messaging.ListenMsg(NetworkMessageName.MatchmakingList, ReceiveMatchmakingList); // 请求当前匹配列表
            Messaging.ListenMsg(NetworkMessageName.MatchList, ReceiveMatchList);             // 请求当前已匹配对局列表

            // 如果网络未激活，则启动服务器
            if (!network.IsActive())
            {
                network.StartServer(NetworkData.Get().port);
            }
        }

        protected virtual void Update()
        {
            // 每 20 秒清理一次匹配队列，只保留最新玩家
            matchmake_timer += Time.deltaTime;
            if (matchmake_timer > 20f)
            {
                matchmake_timer = 0f;
                matchmaking_players.Clear();
            }
        }

        // -------------------- 客户端连接/断开 --------------------
        protected virtual void OnClientConnected(ulong client_id)
        {
            ClientData iclient = new ClientData(client_id);
            client_list[client_id] = iclient;
        }

        protected virtual void OnClientDisconnected(ulong client_id)
        {
            if (client_list.ContainsKey(client_id))
            {
                ClientData iclient = client_list[client_id];
                if (iclient.username != null)
                    matchmaking_players.Remove(iclient.user_id);
                client_list.Remove(client_id);
            }
        }

        // -------------------- 匹配逻辑 --------------------
        /// <summary>
        /// 接收玩家匹配请求
        /// </summary>
        protected virtual void ReceiveMatchmaking(ulong client_id, FastBufferReader reader)
        {
            ClientData iclient = GetClient(client_id);
            reader.ReadNetworkSerializable(out MsgMatchmaking msg);

            if (iclient == null || string.IsNullOrWhiteSpace(msg.user_id) || string.IsNullOrWhiteSpace(msg.username))
                return;

            string user_id = msg.user_id;
            bool is_refresh = msg.refresh;

            // 更新客户端信息
            iclient.user_id = msg.user_id;
            iclient.username = msg.username;

            // 如果不是刷新请求，则重置玩家匹配状态
            if (!is_refresh)
                matched_players.Remove(user_id);

            // 检查是否已经匹配成功
            if (matched_players.ContainsKey(user_id))
            {
                MatchData match = matched_players[user_id];
                if (!match.ended)
                {
                    // 已匹配，直接返回已匹配信息
                    SendMatchmakingResponse(iclient, match, msg.group, match.players.Length);
                    return;
                }
            }

            // 准备匹配玩家数据
            MatchPlayerData pdata = new MatchPlayerData();
            pdata.user_id = msg.user_id;
            pdata.username = msg.username;
            pdata.group = msg.group;
            pdata.elo_rank = msg.elo;
            pdata.nb_players = msg.players;

            // 将玩家加入匹配队列
            if (!matchmaking_players.ContainsKey(user_id))
                matchmaking_players.Add(user_id, pdata);

            // -------------------- 匹配搜索逻辑 --------------------
            float wait_max = 20f;
            int variance_max = 2000;

            bool friendly = msg.group.StartsWith("u_");  // 判断是否友谊赛，友谊赛忽略 Elo 限制
            float wait_timer = msg.time;
            float wait_value = Mathf.Clamp01(wait_timer / wait_max);
            int elo_variance = Mathf.RoundToInt(wait_value * variance_max); // Elo 容差随等待时间增加

            valid_users.Clear();
            valid_users.Add(pdata); // 加入自身

            // 搜索其他有效玩家
            foreach (KeyValuePair<string, MatchPlayerData> opair in matchmaking_players)
            {
                string auser_id = opair.Key;
                MatchPlayerData adata = opair.Value;
                int diff = Mathf.Abs(adata.elo_rank - msg.elo);
                bool same_group = adata.group == msg.group;
                bool same_players = adata.nb_players == msg.players;
                bool valid_elo = friendly || diff < elo_variance;

                if (auser_id != user_id && valid_elo && same_group && same_players)
                {
                    valid_users.Add(adata);
                }
            }

            // 玩家数量不足，返回当前队列人数
            if (valid_users.Count < msg.players)
            {
                SendMatchmakingResponse(iclient, null, msg.group, valid_users.Count);
                return;
            }

            // -------------------- 匹配成功 --------------------
            string prefix = msg.group.Length >= 2 ? msg.group.Substring(0, 2) : "";
            string game_code = prefix + GameTool.GenerateRandomID(12, 15); // 生成随机游戏 UID
            string game_url = ""; // 默认使用客户端默认网络 URL
            if (servers.Length > 0)
                game_url = servers[Random.Range(0, servers.Length)];

            int pindex = 0;
            MatchData nmatch = new MatchData(msg.group, game_code, game_url, msg.players);

            foreach (MatchPlayerData vuser in valid_users)
            {
                if (pindex < nmatch.players.Length)
                {
                    matchmaking_players.Remove(vuser.user_id);
                    matched_players[vuser.user_id] = nmatch;
                    nmatch.players[pindex] = vuser.username;
                    pindex++;
                }
            }

            // 发送匹配成功信息给当前玩家
            if (matched_players.ContainsKey(user_id))
            {
                SendMatchmakingResponse(iclient, nmatch, nmatch.group, nmatch.players.Length);
            }
        }

        /// <summary>
        /// 发送匹配响应给客户端
        /// </summary>
        protected virtual void SendMatchmakingResponse(ClientData iclient, MatchData match, string group, int players)
        {
            MatchmakingResult msg_match = new MatchmakingResult();
            msg_match.success = match != null;
            msg_match.players = players;
            msg_match.group = group;
            msg_match.game_uid = match != null ? match.game_uid : "";
            msg_match.server_url = match != null ? match.server_url : "";

            Messaging.SendObject(NetworkMessageName.Matchmaking, iclient.client_id, msg_match, NetworkDelivery.Reliable);
        }

        // -------------------- 获取匹配列表 --------------------
        protected virtual void ReceiveMatchmakingList(ulong client_id, FastBufferReader reader)
        {
            reader.ReadNetworkSerializable(out MsgMatchmakingList msg);

            List<MatchmakingListItem> items = new List<MatchmakingListItem>();

            foreach (KeyValuePair<string, MatchPlayerData> pair in matchmaking_players)
            {
                if (string.IsNullOrEmpty(msg.username) || pair.Key == msg.username)
                {
                    MatchPlayerData pdata = pair.Value;
                    MatchmakingListItem item = new MatchmakingListItem();
                    item.group = pdata.group;
                    item.user_id = pdata.user_id;
                    item.username = pdata.username;
                    items.Add(item);
                }
            }

            MatchmakingList msg_list = new MatchmakingList();
            msg_list.items = items.ToArray();
            Messaging.SendObject(NetworkMessageName.MatchmakingList, client_id, msg_list, NetworkDelivery.Reliable);
        }

        protected virtual void ReceiveMatchList(ulong client_id, FastBufferReader reader)
        {
            reader.ReadNetworkSerializable(out MsgMatchmakingList msg);

            List<MatchListItem> items = new List<MatchListItem>();

            foreach (KeyValuePair<string, MatchData> pair in matched_players)
            {
                if (!pair.Value.ended)
                {
                    if (string.IsNullOrEmpty(msg.username) || Contains(pair.Value.players, msg.username))
                    {
                        MatchData pdata = pair.Value;
                        MatchListItem item = new MatchListItem();
                        item.group = pair.Value.group;
                        item.username = msg.username;
                        item.game_uid = pdata.game_uid;
                        item.game_url = pdata.server_url;
                        items.Add(item);
                    }
                }
            }

            MatchList msg_list = new MatchList();
            msg_list.items = items.ToArray();

            Messaging.SendObject(NetworkMessageName.MatchList, client_id, msg_list, NetworkDelivery.Reliable);
        }

        // -------------------- 工具方法 --------------------
        private bool Contains(string[] users, string user)
        {
            foreach (string auser in users)
            {
                if (auser == user)
                    return true;
            }
            return false;
        }

        public void EndMatch(string uid)
        {
            // 将指定游戏 UID 的匹配标记为已结束
            foreach (KeyValuePair<string, MatchData> pair in matched_players)
            {
                if (pair.Value.game_uid == uid)
                    pair.Value.ended = true;
            }
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

        // -------------------- 属性 & 单例 --------------------
        public ulong ServerID { get { return TcgNetwork.Get().ServerID; } }
        public NetworkMessaging Messaging { get { return TcgNetwork.Get().Messaging; } }

        public static ServerMatchmaker Get()
        {
            return _instance;
        }
    }

    /// <summary>
    /// 匹配玩家数据
    /// </summary>
    public class MatchPlayerData
    {
        public string user_id;
        public string username;
        public string group;      // 玩家所属匹配组
        public int elo_rank;      // 玩家 Elo 积分
        public int nb_players;    // 对局需要玩家数量
    }

    /// <summary>
    /// 匹配成功的游戏数据
    /// </summary>
    public class MatchData
    {
        public string group;       // 匹配组
        public string game_uid;    // 游戏唯一 ID
        public string server_url;  // 分配的服务器 URL
        public bool ended = false; // 游戏是否结束
        public string[] players;   // 玩家用户名列表

        public MatchData(string grp, string uid, string url, int players) 
        { 
            group = grp; 
            game_uid = uid; 
            server_url = url; 
            this.players = new string[players]; 
        }
    }
}
