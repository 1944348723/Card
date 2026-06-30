using System.Collections.Generic;
using TcgEngine.AI;
using TcgEngine.Gameplay;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace TcgEngine.Server
{
    /// <summary>
    /// 表示一局服务器上的游戏
    /// 单机时会在本地创建
    /// 联机时服务器上会为每一场对局创建一个 GameServer
    /// 负责接收指令、同步游戏状态、运行 AI
    /// </summary>
    public class GameServer
    {
        public string game_uid; // 游戏唯一ID
        public int nb_players = 2;

        public static float game_expire_time = 30f;   // 当没有任何玩家连接时，游戏在这个时间后被删除
        public static float win_expire_time = 60f;    // 当只剩一个玩家在线时，超过这个时间将自动判定他获胜

        private Game game_data;
        private GameLogic gameplay;
        private float expiration = 0f;
        private float win_expiration = 0f;
        private bool is_dedicated_server = false;

        private List<ClientData> players = new List<ClientData>();            // 只包含玩家（不包含观察者），断线后仍保留在数组中，只有玩家可以发送指令
        private List<ClientData> connected_clients = new List<ClientData>();  // 包含所有已连接客户端（包括观察者），断线会移除，所有人都会接收刷新数据
        private List<AIPlayer> ai_list = new List<AIPlayer>();                // AI 玩家列表
        private Queue<QueuedGameAction> queued_actions = new Queue<QueuedGameAction>(); // 排队等待执行的游戏行为队列
        
        private Dictionary<ushort, CommandEvent> registered_commands = new Dictionary<ushort, CommandEvent>();

        public GameServer(string uid, int players, bool online)
        {
            Init(uid, players, online);
        }

        ~GameServer()
        {
            Clear();
        }

        protected virtual void Init(string uid, int players, bool online)
        {
            game_uid = uid;
            nb_players = Mathf.Max(players, 2);
            is_dedicated_server = online;
            game_data = new Game(uid, nb_players);
            gameplay = new GameLogic(game_data);

            // 注册各种游戏指令
            RegisterAction(GameAction.PlayerSettings, ReceivePlayerSettings);
            RegisterAction(GameAction.PlayerSettingsAI, ReceivePlayerSettingsAI);
            RegisterAction(GameAction.GameSettings, ReceiveGameplaySettings);
            RegisterAction(GameAction.PlayCard, ReceivePlayCard);
            RegisterAction(GameAction.Attack, ReceiveAttackTarget);
            RegisterAction(GameAction.AttackPlayer, ReceiveAttackPlayer);
            RegisterAction(GameAction.Move, ReceiveMove);
            RegisterAction(GameAction.CastAbility, ReceiveCastCardAbility);
            RegisterAction(GameAction.SelectCard, ReceiveSelectCard);
            RegisterAction(GameAction.SelectPlayer, ReceiveSelectPlayer);
            RegisterAction(GameAction.SelectSlot, ReceiveSelectSlot);
            RegisterAction(GameAction.SelectChoice, ReceiveSelectChoice);
            RegisterAction(GameAction.SelectCost, ReceiveSelectCost);
            RegisterAction(GameAction.SelectMulligan, ReceiveSelectMulligan);
            RegisterAction(GameAction.CancelSelect, ReceiveCancelSelection);
            RegisterAction(GameAction.EndTurn, ReceiveEndTurn);
            RegisterAction(GameAction.Resign, ReceiveResign);
            RegisterAction(GameAction.ChatMessage, ReceiveChat);

            // 绑定游戏事件
            gameplay.onGameStart += OnGameStart;
            gameplay.onGameEnd += OnGameEnd;
            gameplay.onTurnStart += OnTurnStart;
            gameplay.onRefresh += RefreshAll;

            gameplay.onCardPlayed += OnCardPlayed;
            gameplay.onCardSummoned += OnCardSummoned;
            gameplay.onCardMoved += OnCardMoved;
            gameplay.onCardTransformed += OnCardTransformed;
            gameplay.onCardDiscarded += OnCardDiscarded;
            gameplay.onCardDrawn += OnCardDraw;
            gameplay.onRollValue += OnValueRolled;

            gameplay.onAbilityStart += OnAbilityStart;
            gameplay.onAbilityTargetCard += OnAbilityTargetCard;
            gameplay.onAbilityTargetPlayer += OnAbilityTargetPlayer;
            gameplay.onAbilityTargetSlot += OnAbilityTargetSlot;
            gameplay.onAbilityEnd += OnAbilityEnd;

            gameplay.onAttackStart += OnAttackStart;
            gameplay.onAttackEnd += OnAttackEnd;
            gameplay.onAttackPlayerStart += OnAttackPlayerStart;
            gameplay.onAttackPlayerEnd += OnAttackPlayerEnd;

            gameplay.onCardDamaged += OnCardDamaged;
            gameplay.onPlayerDamaged += OnPlayerDamaged;
            gameplay.onCardHealed += OnCardHealed;
            gameplay.onPlayerHealed += OnPlayerHealed ;

            gameplay.onSecretTrigger += OnSecretTriggered;
            gameplay.onSecretResolve += OnSecretResolved;
        }

        protected virtual void Clear()
        {
            // 取消所有事件绑定
            gameplay.onGameStart -= OnGameStart;
            gameplay.onGameEnd -= OnGameEnd;
            gameplay.onTurnStart -= OnTurnStart;
            gameplay.onRefresh -= RefreshAll;

            gameplay.onCardPlayed -= OnCardPlayed;
            gameplay.onCardSummoned -= OnCardSummoned;
            gameplay.onCardMoved -= OnCardMoved;
            gameplay.onCardTransformed -= OnCardTransformed;
            gameplay.onCardDiscarded -= OnCardDiscarded;
            gameplay.onCardDrawn -= OnCardDraw;
            gameplay.onRollValue -= OnValueRolled;

            gameplay.onAbilityStart -= OnAbilityStart;
            gameplay.onAbilityTargetCard -= OnAbilityTargetCard;
            gameplay.onAbilityTargetPlayer -= OnAbilityTargetPlayer;
            gameplay.onAbilityTargetSlot -= OnAbilityTargetSlot;
            gameplay.onAbilityEnd -= OnAbilityEnd;

            gameplay.onAttackStart -= OnAttackStart;
            gameplay.onAttackEnd -= OnAttackEnd;
            gameplay.onAttackPlayerStart -= OnAttackPlayerStart;
            gameplay.onAttackPlayerEnd -= OnAttackPlayerEnd;
            gameplay.onCardDamaged -= OnCardDamaged;
            gameplay.onPlayerDamaged -= OnPlayerDamaged;

            gameplay.onSecretTrigger -= OnSecretTriggered;
            gameplay.onSecretResolve -= OnSecretResolved;
        }

        public virtual void Update()
        {
            // 如果无人连接或游戏已结束，开始累计“被删除计时”
            int connected_players = CountConnectedClients();
            if (HasGameEnded() || connected_players == 0)
                expiration += Time.deltaTime;

            // 如果只剩一个玩家连接，开始累计“自动胜利计时”
            if (connected_players == 1 && HasGameStarted() && !HasGameEnded())
                win_expiration += Time.deltaTime;

            // 仅在专用服务器上生效，到达胜利计时则结束游戏
            if (is_dedicated_server && !HasGameEnded() && IsWinExpired())
                EndExpiredGame();

            // 游戏进行中的回合计时
            if (game_data.state == GameState.Play && !gameplay.IsResolving())
            {
                game_data.turn_timer -= Time.deltaTime;
                if (game_data.turn_timer <= 0f)
                {
                    // 回合时间到
                    gameplay.NextStep();
                }
            }

            // 连接阶段，若所有玩家已连接且准备完成则开始游戏
            if (game_data.state == GameState.Connecting)
            {
                bool all_connected = game_data.AreAllPlayersConnected();
                bool all_ready = game_data.AreAllPlayersReady();
                if (all_connected && all_ready)
                {
                    StartGame();
                }
            }

            // 处理排队中的指令
            if (queued_actions.Count > 0 && !gameplay.IsResolving())
            {
                QueuedGameAction action = queued_actions.Dequeue();
                ExecuteAction(action.type, action.client, action.sdata);
            }

            // 更新游戏逻辑
            gameplay.Update(Time.deltaTime);

            // 更新 AI
            foreach (AIPlayer ai in ai_list)
            {
                ai.Update();
            }
        }

        protected virtual void StartGame()
        {
            // 设置并创建 AI
            bool ai_vs_ai = !is_dedicated_server && GameplayData.Get().ai_vs_ai;
            foreach (Player player in game_data.players)
            {
                if (player.is_ai || ai_vs_ai)
                {
                    AIPlayer ai_gameplay = AIPlayer.Create(GameplayData.Get().ai_type, gameplay, player.player_id, player.ai_level);
                    ai_list.Add(ai_gameplay);
                }
            }

            // 开始游戏
            gameplay.StartGame();
        }

        // 当只剩一名玩家在线并达到超时时结束游戏
        protected virtual void EndExpiredGame()
        {
            Game gdata = gameplay.GetGameData();
            foreach (Player player in gdata.players)
            {
                if (player.IsConnected())
                {
                    gameplay.EndGame(player.player_id);
                    return;
                }
            }
        }

        //------ 接收指令 -------

        private void RegisterAction(ushort tag, UnityAction<ClientData, SerializedData> callback)
        {
            CommandEvent cmdevt = new CommandEvent();
            cmdevt.tag = tag;
            cmdevt.callback = callback;
            registered_commands.Add(tag, cmdevt);
        }

        public void ReceiveAction(ulong client_id, FastBufferReader reader)
        {
            ClientData client = GetClient(client_id);
            if (client != null)
            {
                reader.ReadValueSafe(out ushort type);
                SerializedData sdata = new SerializedData(reader);
                if (!gameplay.IsResolving())
                {
                    // 当前无结算，立即执行指令
                    ExecuteAction(type, client, sdata);
                }
                else
                {
                    // 处于结算中，指令进入队列等待执行
                    QueuedGameAction action = new QueuedGameAction();
                    action.type = type;
                    action.client = client;
                    action.sdata = sdata;
                    sdata.PreRead();
                    queued_actions.Enqueue(action);
                }
            }
        }

        public void ExecuteAction(ushort type, ClientData client, SerializedData sdata)
        {
            bool found = registered_commands.TryGetValue(type, out CommandEvent command);
            if(found)
                command.callback.Invoke(client, sdata);
        }

        //-------

        public void ReceivePlayerSettings(ClientData iclient, SerializedData sdata)
        {
            PlayerSettings msg = sdata.Get<PlayerSettings>();
            Player player = GetPlayer(iclient);
            if (player != null && msg != null)
            {
                SetPlayerSettings(player.player_id, msg);
            }
        }

        public void ReceivePlayerSettingsAI(ClientData iclient, SerializedData sdata)
        {
            PlayerSettings msg = sdata.Get<PlayerSettings>();
            Player player = GetPlayer(iclient);
            if (player != null && msg != null)
            {
                SetPlayerSettingsAI(player.player_id, msg);
            }
        }
        // 接收客户端发送的游戏配置（只在连接阶段有效）
        public void ReceiveGameplaySettings(ClientData iclient, SerializedData sdata)
        {
            GameSettings settings = sdata.Get<GameSettings>();
            if (settings != null)
            {
                SetGameSettings(settings);
            }
        }

        // 接收“打出卡牌”请求
        public void ReceivePlayCard(ClientData iclient, SerializedData sdata)
        {
            MsgPlayCard msg = sdata.Get<MsgPlayCard>();
            Player player = GetPlayer(iclient);
            // 必须：玩家存在 + 消息有效 + 当前轮到该玩家行动 + 不能在结算中
            if (player != null && msg != null && game_data.IsPlayerActionTurn(player) && !gameplay.IsResolving())
            {
                Card card = player.GetCard(msg.card_uid);
                // 校验卡牌存在且属于该玩家，防止作弊
                if (card != null && card.player_id == player.player_id)
                    gameplay.PlayCard(card, msg.slot);
            }
        }

        // 接收“攻击目标卡牌”请求
        public void ReceiveAttackTarget(ClientData iclient, SerializedData sdata)
        {
            MsgAttack msg = sdata.Get<MsgAttack>();
            Player player = GetPlayer(iclient);
            if (player != null && msg != null && game_data.IsPlayerActionTurn(player) && !gameplay.IsResolving())
            {
                Card attacker = player.GetCard(msg.attacker_uid);
                Card target = game_data.GetCard(msg.target_uid);
                // 攻击者必须属于该玩家
                if (attacker != null && target != null && attacker.player_id == player.player_id)
                {
                    gameplay.AttackTarget(attacker, target);
                }
            }
        }

        // 接收“攻击玩家”请求
        public void ReceiveAttackPlayer(ClientData iclient, SerializedData sdata)
        {
            MsgAttackPlayer msg = sdata.Get<MsgAttackPlayer>();
            Player player = GetPlayer(iclient);
            if (player != null && msg != null && game_data.IsPlayerActionTurn(player) && !gameplay.IsResolving())
            {
                Card attacker = player.GetCard(msg.attacker_uid);
                Player target = game_data.GetPlayer(msg.target_id);
                if (attacker != null && target != null && attacker.player_id == player.player_id)
                {
                    gameplay.AttackPlayer(attacker, target);
                }
            }
        }

        // 接收“移动卡牌位置”请求
        public void ReceiveMove(ClientData iclient, SerializedData sdata)
        {
            MsgPlayCard msg = sdata.Get<MsgPlayCard>();
            Player player = GetPlayer(iclient);
            if (player != null && msg != null && game_data.IsPlayerActionTurn(player) && !gameplay.IsResolving())
            {
                Card card = player.GetCard(msg.card_uid);
                if (card != null && card.player_id == player.player_id)
                    gameplay.MoveCard(card, msg.slot);
            }
        }

        // 接收“施放卡牌技能”请求
        public void ReceiveCastCardAbility(ClientData iclient, SerializedData sdata)
        {
            MsgCastAbility msg = sdata.Get<MsgCastAbility>();
            Player player = GetPlayer(iclient);
            if (player != null && msg != null && game_data.IsPlayerActionTurn(player) && !gameplay.IsResolving())
            {
                Card card = player.GetCard(msg.caster_uid);
                AbilityData iability = AbilityData.Get(msg.ability_id);
                if (card != null && card.player_id == player.player_id)
                    gameplay.CastAbility(card, iability);
            }
        }

        // 接收“选择卡牌”请求（用于选择阶段）
        public void ReceiveSelectCard(ClientData iclient, SerializedData sdata)
        {
            MsgCard msg = sdata.Get<MsgCard>();
            Player player = GetPlayer(iclient);
            if (player != null && msg != null && game_data.IsPlayerSelectorTurn(player) && !gameplay.IsResolving())
            {
                Card target = game_data.GetCard(msg.card_uid);
                gameplay.SelectCard(target);
            }
        }

        // 接收“选择玩家”请求
        public void ReceiveSelectPlayer(ClientData iclient, SerializedData sdata)
        {
            MsgPlayer msg = sdata.Get<MsgPlayer>();
            Player player = GetPlayer(iclient);
            if (player != null && msg != null && game_data.IsPlayerSelectorTurn(player) && !gameplay.IsResolving())
            {
                Player target = game_data.GetPlayer(msg.player_id);
                gameplay.SelectPlayer(target);
            }
        }

        // 接收“选择格子位置”请求
        public void ReceiveSelectSlot(ClientData iclient, SerializedData sdata)
        {
            Slot slot = sdata.Get<Slot>();
            Player player = GetPlayer(iclient);
            if (player != null && game_data.IsPlayerSelectorTurn(player) && !gameplay.IsResolving())
            {
                // slot 校验有效性
                if(slot != null && slot.IsBoardSlot())
                    gameplay.SelectSlot(slot);
            }
        }

        // 接收“选择某个选项（数字型）”请求
        public void ReceiveSelectChoice(ClientData iclient, SerializedData sdata)
        {
            MsgInt msg = sdata.Get<MsgInt>();
            Player player = GetPlayer(iclient);
            if (player != null && msg != null && game_data.IsPlayerSelectorTurn(player) && !gameplay.IsResolving())
            {
                gameplay.SelectChoice(msg.value);
            }
        }

        // 接收“选择费用”请求
        public void ReceiveSelectCost(ClientData iclient, SerializedData sdata)
        {
            MsgInt msg = sdata.Get<MsgInt>();
            Player player = GetPlayer(iclient);
            if (player != null && msg != null && game_data.IsPlayerSelectorTurn(player) && !gameplay.IsResolving())
            {
                gameplay.SelectCost(msg.value);
            }
        }

        // 接收“取消选择”请求
        public void ReceiveCancelSelection(ClientData iclient, SerializedData sdata)
        {
            Player player = GetPlayer(iclient);
            if (player != null && game_data.IsPlayerSelectorTurn(player) && !gameplay.IsResolving())
            {
                gameplay.CancelSelection();
            }
        }

        // 接收“调度牌（换牌阶段 Mulligan）”请求
        public void ReceiveSelectMulligan(ClientData iclient, SerializedData sdata)
        {
            MsgMulligan msg = sdata.Get<MsgMulligan>();
            Player player = GetPlayer(iclient);
            if (player != null && msg != null && game_data.IsPlayerMulliganTurn(player) && !gameplay.IsResolving())
            {
                gameplay.Mulligan(player, msg.cards);
            }
        }

        // 接收“结束回合”请求
        public void ReceiveEndTurn(ClientData iclient, SerializedData sdata)
        {
            Player player = GetPlayer(iclient);
            if (player != null && game_data.IsPlayerTurn(player))
            {
                gameplay.NextStep();
            }
        }

        // 接收“投降”请求
        public void ReceiveResign(ClientData iclient, SerializedData sdata)
        {
            Player player = GetPlayer(iclient);
            // 游戏必须已开始且未结束
            if (player != null && game_data.state != GameState.Connecting && game_data.state != GameState.GameEnded)
            {
                // 认输则对方直接获胜
                int winner = player.player_id == 0 ? 1 : 0;
                gameplay.EndGame(winner);
            }
        }

        // 接收聊天消息（强制绑定 sender，防止伪造 player_id）
        public void ReceiveChat(ClientData iclient, SerializedData sdata)
        {
            MsgChat msg = sdata.Get<MsgChat>();
            Player player = GetPlayer(iclient);
            if (player != null && msg != null)
            {
                msg.player_id = player.player_id; // 强制覆盖，防止伪造身份
                SendToAll(GameAction.ChatMessage, msg, NetworkDelivery.Reliable);
            }
        }
        
        //--- Setup Commands ------
        /// <summary>
        /// 设置玩家卡组（在连接阶段调用）
        /// 如果是本地游戏，直接使用本地用户数据
        /// 如果是在线模式，则通过 API 去服务器校验并加载玩家卡组
        /// 验证通过 → 设置卡组 → 标记玩家 Ready
        /// 验证失败 → 输出日志
        /// </summary>
        public virtual async void SetPlayerDeck(int player_id, string username, UserDeckData deck)
        {
            Player player = game_data.GetPlayer(player_id);
            
            // 只有在房间还处于 Connecting 阶段时才能设置卡组
            if (player != null && game_data.state == GameState.Connecting)
            {
                // 默认：离线模式，从本地 Authenticator 获取用户数据
                UserData user = Authenticator.Get().UserData;

                // 如果是在线模式，通过 API 拉取真实用户数据（校验）
                if(Authenticator.Get().IsApi())
                    user = await ApiClient.Get().LoadUserData(username);

                // 从用户数据中找到对应 deck（以 deck.tid 作为唯一标识）
                UserDeckData udeck = user?.GetDeck(deck.tid);
                if (user != null && udeck != null)
                {
                    // 校验卡组是否合法
                    if (user.IsDeckValid(udeck))
                    {
                        // 设置真实有效的玩家卡组
                        gameplay.SetPlayerDeck(player, udeck);
                        SendPlayerReady(player);   // 玩家准备完成
                        return;
                    }
                    else
                    {
                        Debug.Log(user.username + " deck is invalid: " + udeck.title);
                        return;
                    }
                }

                // 如果不是 API 卡组，则尝试使用游戏内预设卡组
                DeckData cdeck = DeckData.Get(deck.tid);
                if (cdeck != null)
                    gameplay.SetPlayerDeck(player, cdeck);

                // 测试模式：直接信任客户端传过来的卡组
                else if (Authenticator.Get().IsTest())
                    gameplay.SetPlayerDeck(player, deck);

                // 找不到卡组，输出错误
                else
                    Debug.Log("Player " + player_id + " deck not found: " + deck.tid);

                SendPlayerReady(player);
            }
        }

        /// <summary>
        /// 设置真实玩家（人类玩家）的配置
        /// 只能在 Connecting 阶段执行
        /// 设置头像、卡背、卡组，并标记为 ready
        /// </summary>
        public virtual void SetPlayerSettings(int player_id, PlayerSettings psettings)
        {
            // 游戏已经开始 → 不允许再设置
            if (game_data.state != GameState.Connecting)
                return;

            Player player = game_data.GetPlayer(player_id);
            if (player != null && !player.ready)
            {
                player.avatar = psettings.avatar;       // 玩家头像
                player.cardback = psettings.cardback;   // 卡背皮肤
                player.is_ai = false;                   // 明确标记为真人玩家
                player.ready = true;                    // 玩家准备完毕

                // 设置玩家卡组（内部仍会触发 Ready 校验）
                SetPlayerDeck(player_id, player.username, psettings.deck);

                RefreshAll();   // 通知所有客户端刷新 UI / 数据
            }
        }

        /// <summary>
        /// 设置 AI 玩家配置
        /// 只能在 Connecting 阶段执行
        /// 服务器模式下禁止 AI（只能本地游戏用）
        /// 设置 AI 名字、头像、卡组、难度，并设为 ready
        /// </summary>
        public virtual void SetPlayerSettingsAI(int player_id, PlayerSettings psettings)
        {
            if (game_data.state != GameState.Connecting)
                return; // 游戏已开始，不能设置

            if (is_dedicated_server)
                return; // 专用服务器上不允许 AI

            // AI 作为“对手玩家”加入
            Player player = game_data.GetOpponentPlayer(player_id);
            if (player != null && !player.ready)
            {
                player.username = psettings.username;   // AI 名字
                player.avatar = psettings.avatar;       // AI 头像
                player.cardback = psettings.cardback;   // AI 卡背
                player.is_ai = true;                    // 标记为 AI
                player.ready = true;                    // AI 就绪
                player.ai_level = psettings.ai_level;   // AI 难度

                // 设置 AI 卡组
                SetPlayerDeck(player.player_id, player.username, psettings.deck);

                RefreshAll();   // 通知客户端刷新
            }
        }

        /// <summary>
        /// 设置游戏规则 / 游戏参数（回合时间、生命值等）
        /// 只能在游戏开始前（Connecting 阶段）修改
        /// </summary>
        public virtual void SetGameSettings(GameSettings settings)
        {
            if (game_data.state == GameState.Connecting)
            {
                game_data.settings = settings;  // 更新游戏设置
                RefreshAll();                  // 广播刷新
            }
        }

        
        //-------------  客户端 & 玩家管理  --------------

        // 添加一个客户端到已连接列表（如果还没添加）
        public void AddClient(ClientData client)
        {
            if (!connected_clients.Contains(client))
                connected_clients.Add(client);
        }

        // 从已连接客户端列表中移除一个客户端
        public void RemoveClient(ClientData client)
        {
            connected_clients.Remove(client);

            // 找到该客户端绑定的玩家
            Player player = GetPlayer(client);
            if (player != null && player.connected)
            {
                // 标记该玩家断线
                player.connected = false;

                // 通知所有客户端刷新状态（例如 UI 显示“断线”）
                RefreshAll();
            }
        }

        // 根据 client_id 查找 ClientData
        public ClientData GetClient(ulong client_id)
        {
            foreach (ClientData client in connected_clients)
            {
                if (client.client_id == client_id)
                    return client;
            }
            return null;
        }

        // 将一个客户端绑定为游戏中的 Player，并返回 player_id
        public int AddPlayer(ClientData client)
        {
            // players 不是 Player 列表，而是 ClientData 列表 = “参与游戏的客户端”
            if (!players.Contains(client))
                players.Add(client);

            // 根据 user_id 查找对应 player_id
            int player_id = FindPlayerID(client.user_id);
            Player player = game_data.GetPlayer(player_id);

            // 如果该玩家存在，更新玩家状态（用户名 + 连接状态）
            if (player != null)
            {
                player.username = client.username;
                player.connected = true;
            }

            return player_id;
        }

        // 根据 user_id 找对应的 player 索引（同时也是 player_id）
        public int FindPlayerID(string user_id)
        {
            int index = 0;
            foreach (ClientData player in players)
            {
                if (player.user_id == user_id)
                    return index;
                index++;
            }
            return -1;
        }

        // 通过 ClientData 查 Player
        public Player GetPlayer(ClientData client)
        {
            return GetPlayer(client.user_id);
        }

        // 通过 user_id 查 Player
        public Player GetPlayer(string user_id)
        {
            int player_id = FindPlayerID(user_id);
            return game_data?.GetPlayer(player_id);
        }

        // 判断某个 user_id 是否是玩家
        public bool IsPlayer(string user_id)
        {
            Player player = GetPlayer(user_id);
            return player != null;
        }

        // 判断某个 user_id 是否是“在线玩家”
        public bool IsConnectedPlayer(string user_id)
        {
            Player player = GetPlayer(user_id);
            return player != null && player.connected;
        }

        // 当前加入游戏的玩家数量（不一定在线）
        public int CountPlayers()
        {
            return players.Count;
        }

        // 当前真正在线的玩家数量（检查 Player.connected）
        public int CountConnectedClients()
        {
            int nb = 0;
            Game game = GetGameData();
            foreach (Player player in game.players)
            {
                if (player.IsConnected())
                {
                    nb++;
                }
            }
            return nb;
        }

        // 获取当前 Game 数据（封装一层 gameplay）
        public Game GetGameData()
        {
            return gameplay.GetGameData();
        }

        // 游戏是否已经正式开始
        public virtual bool HasGameStarted()
        {
            return gameplay.IsGameStarted();
        }

        // 游戏是否已经结束
        public virtual bool HasGameEnded()
        {
            return gameplay.IsGameEnded();
        }

        // 游戏是否“整体过期”
        // 代表游戏已无人继续参与或终局状态持续太久，可以销毁
        public virtual bool IsGameExpired()
        {
            return expiration > game_expire_time; 
        }

        // 胜利等待是否超时
        // 代表只剩一名玩家在线，等待另一个过久 → 自动判胜
        public virtual bool IsWinExpired()
        {
            return win_expiration > win_expire_time;
        }

        //---------------- 游戏事件分发（同步给所有客户端） ----------------

        // 游戏开始（通知所有客户端 + 如果是在线服务器则创建比赛记录）
        protected virtual void OnGameStart()
        {
            SendToAll(GameAction.GameStart);

            if (is_dedicated_server && Authenticator.Get().IsApi())
            {
                // 如果接入 Web API，则同步到后端，创建比赛记录
                ApiClient.Get().CreateMatch(game_data);
            }
        }

        // 游戏结束（同步给所有客户端 + 通知后端发放奖励）
        protected virtual void OnGameEnd(Player winner)
        {
            MsgPlayer msg = new MsgPlayer();
            msg.player_id = winner != null ? winner.player_id : -1;
            SendToAll(GameAction.GameEnd, msg, NetworkDelivery.Reliable);

            if (is_dedicated_server && Authenticator.Get().IsApi())
            {
                // 通知服务器比赛结束并结算奖励
                ApiClient.Get().EndMatch(game_data, winner.player_id);
            }
        }

        // 新回合开始（告知所有客户端是谁的回合）
        protected virtual void OnTurnStart()
        {
            MsgPlayer msg = new MsgPlayer();
            msg.player_id = game_data.current_player;
            SendToAll(GameAction.NewTurn, msg, NetworkDelivery.Reliable);
        }

        // 卡牌被“打出”到战场
        protected virtual void OnCardPlayed(Card card, Slot slot)
        {
            MsgPlayCard mdata = new MsgPlayCard();
            mdata.card_uid = card.uid;
            mdata.slot = slot;
            SendToAll(GameAction.CardPlayed, mdata, NetworkDelivery.Reliable);
        }

        // 卡牌被移动（例如战场换位）
        protected virtual void OnCardMoved(Card card, Slot slot)
        {
            MsgPlayCard mdata = new MsgPlayCard();
            mdata.card_uid = card.uid;
            mdata.slot = slot;
            SendToAll(GameAction.CardMoved, mdata, NetworkDelivery.Reliable);
        }

        // 卡牌被召唤（某些效果新生成单位）
        protected virtual void OnCardSummoned(Card card, Slot slot)
        {
            MsgPlayCard mdata = new MsgPlayCard();
            mdata.card_uid = card.uid;
            mdata.slot = slot;
            SendToAll(GameAction.CardSummoned, mdata, NetworkDelivery.Reliable);
        }

        // 卡牌变形（例如进化/替换）
        protected virtual void OnCardTransformed(Card card)
        {
            MsgCard mdata = new MsgCard();
            mdata.card_uid = card.uid;
            SendToAll(GameAction.CardTransformed, mdata, NetworkDelivery.Reliable);
        }

        // 卡牌被丢弃
        protected virtual void OnCardDiscarded(Card card)
        {
            MsgCard mdata = new MsgCard();
            mdata.card_uid = card.uid;
            SendToAll(GameAction.CardDiscarded, mdata, NetworkDelivery.Reliable);
        }

        // 抽牌（只同步抽牌数量，客户端各自展示动画）
        protected virtual void OnCardDraw(int nb)
        {
            MsgInt mdata = new MsgInt();
            mdata.value = nb;
            SendToAll(GameAction.CardDrawn, mdata, NetworkDelivery.Reliable);
        }

        // 掷骰子结果（或随机数结果）
        protected virtual void OnValueRolled(int nb)
        {
            MsgInt mdata = new MsgInt();
            mdata.value = nb;
            SendToAll(GameAction.ValueRolled, mdata, NetworkDelivery.Reliable);
        }

        // 开始攻击卡牌
        protected virtual void OnAttackStart(Card attacker, Card target)
        {
            MsgAttack mdata = new MsgAttack();
            mdata.attacker_uid = attacker.uid;
            mdata.target_uid = target.uid;
            mdata.damage = 0; // 这里只通知开始，不同步伤害
            SendToAll(GameAction.AttackStart, mdata, NetworkDelivery.Reliable);
        }

        // 攻击卡牌结束（伤害结算完毕）
        protected virtual void OnAttackEnd(Card attacker, Card target)
        {
            MsgAttack mdata = new MsgAttack();
            mdata.attacker_uid = attacker.uid;
            mdata.target_uid = target.uid;
            mdata.damage = 0;
            SendToAll(GameAction.AttackEnd, mdata, NetworkDelivery.Reliable);
        }

        // 开始攻击玩家
        protected virtual void OnAttackPlayerStart(Card attacker, Player target)
        {
            MsgAttackPlayer mdata = new MsgAttackPlayer();
            mdata.attacker_uid = attacker.uid;
            mdata.target_id = target.player_id;
            mdata.damage = 0;
            SendToAll(GameAction.AttackPlayerStart, mdata, NetworkDelivery.Reliable);
        }

        // 攻击玩家结束
        protected virtual void OnAttackPlayerEnd(Card attacker, Player target)
        {
            MsgAttackPlayer mdata = new MsgAttackPlayer();
            mdata.attacker_uid = attacker.uid;
            mdata.target_id = target.player_id;
            mdata.damage = 0;
            SendToAll(GameAction.AttackPlayerEnd, mdata, NetworkDelivery.Reliable);
        }

        /// <summary>
        /// 卡牌受到伤害时触发（服务器 → 通知所有客户端）
        /// </summary>
        protected virtual void OnCardDamaged(Card card, int damage)
        {
            MsgCardValue mdata = new MsgCardValue();
            mdata.card_uid = card.uid;      // 受伤卡牌唯一ID
            mdata.value = damage;           // 伤害值
            SendToAll(GameAction.CardDamaged, mdata, NetworkDelivery.Reliable);
        }

        /// <summary>
        /// 玩家受到伤害时触发（服务器 → 通知所有客户端）
        /// </summary>
        protected virtual void OnPlayerDamaged(Player player, int damage)
        {
            MsgPlayerValue mdata = new MsgPlayerValue();
            mdata.player_id = player.player_id; // 玩家ID
            mdata.value = damage;               // 伤害值
            SendToAll(GameAction.PlayerDamaged, mdata, NetworkDelivery.Reliable);
        }

        /// <summary>
        /// 卡牌恢复生命时触发（服务器 → 客户端）
        /// </summary>
        protected virtual void OnCardHealed(Card card, int hp)
        {
            MsgCardValue mdata = new MsgCardValue();
            mdata.card_uid = card.uid;  // 卡牌唯一ID
            mdata.value = hp;           // 恢复量
            SendToAll(GameAction.CardHealed, mdata, NetworkDelivery.Reliable);
        }

        /// <summary>
        /// 玩家恢复生命时触发（服务器 → 客户端）
        /// </summary>
        protected virtual void OnPlayerHealed(Player player, int hp)
        {
            MsgPlayerValue mdata = new MsgPlayerValue();
            mdata.player_id = player.player_id; // 玩家ID
            mdata.value = hp;                   // 恢复量
            SendToAll(GameAction.PlayerHealed, mdata, NetworkDelivery.Reliable);
        }

        /// <summary>
        /// 技能开始施放（但还未选择目标）
        /// </summary>
        protected virtual void OnAbilityStart(AbilityData ability, Card caster)
        {
            MsgCastAbility mdata = new MsgCastAbility();
            mdata.ability_id = ability.id;  // 技能ID
            mdata.caster_uid = caster.uid;  // 施法者卡牌
            mdata.target_uid = "";          // 还没目标
            SendToAll(GameAction.AbilityTrigger, mdata, NetworkDelivery.Reliable);
        }

        /// <summary>
        /// 技能选择目标为卡牌
        /// </summary>
        protected virtual void OnAbilityTargetCard(AbilityData ability, Card caster, Card target)
        {
            MsgCastAbility mdata = new MsgCastAbility();
            mdata.ability_id = ability.id;
            mdata.caster_uid = caster.uid;
            mdata.target_uid = target != null ? target.uid : "";   // 目标卡牌UID（可能为空）
            SendToAll(GameAction.AbilityTargetCard, mdata, NetworkDelivery.Reliable);
        }

        /// <summary>
        /// 技能选择目标为玩家
        /// </summary>
        protected virtual void OnAbilityTargetPlayer(AbilityData ability, Card caster, Player target)
        {
            MsgCastAbilityPlayer mdata = new MsgCastAbilityPlayer();
            mdata.ability_id = ability.id;
            mdata.caster_uid = caster.uid;
            mdata.target_id = target != null ? target.player_id : -1;  // -1 表示无目标
            SendToAll(GameAction.AbilityTargetPlayer, mdata, NetworkDelivery.Reliable);
        }

        /// <summary>
        /// 技能选择目标为场上格子（Slot）
        /// </summary>
        protected virtual void OnAbilityTargetSlot(AbilityData ability, Card caster, Slot target)
        {
            MsgCastAbilitySlot mdata = new MsgCastAbilitySlot();
            mdata.ability_id = ability.id;
            mdata.caster_uid = caster.uid;
            mdata.slot = target;   // 直接同步 Slot 数据
            SendToAll(GameAction.AbilityTargetSlot, mdata, NetworkDelivery.Reliable);
        }

        /// <summary>
        /// 技能执行结束
        /// </summary>
        protected virtual void OnAbilityEnd(AbilityData ability, Card caster)
        {
            MsgCastAbility mdata = new MsgCastAbility();
            mdata.ability_id = ability.id;
            mdata.caster_uid = caster.uid;
            mdata.target_uid = "";     // 结束阶段无目标
            SendToAll(GameAction.AbilityEnd, mdata, NetworkDelivery.Reliable);
        }

        /// <summary>
        /// 秘法触发（被触发事件响应时）
        /// </summary>
        protected virtual void OnSecretTriggered(Card secret, Card trigger)
        {
            MsgSecret mdata = new MsgSecret();
            mdata.secret_uid = secret.uid;                         // 秘密卡 UID
            mdata.triggerer_uid = trigger != null ? trigger.uid : ""; // 触发者
            SendToAll(GameAction.SecretTriggered, mdata, NetworkDelivery.Reliable);
        }

        /// <summary>
        /// 秘法结算完毕
        /// </summary>
        protected virtual void OnSecretResolved(Card secret, Card trigger)
        {
            MsgSecret mdata = new MsgSecret();
            mdata.secret_uid = secret.uid;
            mdata.triggerer_uid = trigger != null ? trigger.uid : "";
            SendToAll(GameAction.SecretResolved, mdata, NetworkDelivery.Reliable);
        }

        /// <summary>
        /// 通知所有客户端：某个玩家已准备
        /// </summary>
        protected virtual void SendPlayerReady(Player player)
        {
            if (player != null && player.IsReady())
            {
                MsgInt mdata = new MsgInt();
                mdata.value = player.player_id;   // 发送玩家ID
                SendToAll(GameAction.PlayerReady, mdata, NetworkDelivery.Reliable);
            }
        }

        /// <summary>
        /// 强制刷新整个游戏状态（同步完整 GameData）
        /// </summary>
        public virtual void RefreshAll()
        {
            MsgRefreshAll mdata = new MsgRefreshAll();
            mdata.game_data = GetGameData();  // 当前完整游戏数据
            SendToAll(GameAction.RefreshAll, mdata, NetworkDelivery.ReliableFragmentedSequenced);
        }

        /// <summary>
        /// 仅发送一个指令 Tag（无额外数据）
        /// </summary>
        public void SendToAll(ushort tag)
        {
            FastBufferWriter writer = new FastBufferWriter(128, Unity.Collections.Allocator.Temp, TcgNetwork.MsgSizeMax);
            writer.WriteValueSafe(tag);   // 写入消息类型
            foreach (ClientData iclient in connected_clients)
            {
                if (iclient != null)
                {
                    // 发送到每个在线客户端
                    Messaging.Send("refresh", iclient.client_id, writer, NetworkDelivery.Reliable);
                }
            }
            writer.Dispose();
        }

        /// <summary>
        /// 发送带网络序列化数据的消息给所有客户端
        /// </summary>
        public void SendToAll(ushort tag, INetworkSerializable data, NetworkDelivery delivery)
        {
            FastBufferWriter writer = new FastBufferWriter(128, Unity.Collections.Allocator.Temp, TcgNetwork.MsgSizeMax);
            writer.WriteValueSafe(tag);          // 写消息类型
            writer.WriteNetworkSerializable(data); // 写数据内容
            foreach (ClientData iclient in connected_clients)
            {
                if (iclient != null)
                {
                    Messaging.Send("refresh", iclient.client_id, writer, delivery);
                }
            }
            writer.Dispose();
        }

        /// <summary>
        /// 服务器唯一ID
        /// </summary>
        public ulong ServerID { get { return TcgNetwork.Get().ServerID; } }

        /// <summary>
        /// 网络消息系统
        /// </summary>
        public NetworkMessaging Messaging { get { return TcgNetwork.Get().Messaging; } }
    }

    /// <summary>
    /// 排队中的游戏指令结构（服务器队列用）
    /// </summary>
    public struct QueuedGameAction
    {
        public ushort type;        // 指令类型
        public ClientData client;  // 发起该指令的客户端
        public SerializedData sdata; // 序列化后的数据
    }

}
