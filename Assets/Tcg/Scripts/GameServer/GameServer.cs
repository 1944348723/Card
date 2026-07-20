using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TcgEngine.AI;
using TcgEngine.Gameplay;
using Unity.Netcode;
using UnityEngine;

namespace TcgEngine.Server
{
    /// <summary>
    /// 表示一局服务器上的游戏
    /// 单机时会在本地创建
    /// 联机时服务器上会为每一场对局创建一个 GameServer
    /// 负责接收指令、同步游戏状态、运行 AI
    /// </summary>
    public class GameServer : IDisposable
    {
        public string gameUID; // 游戏唯一ID
        public int playersCount = 2;

        public static float gameExpireTime = 30f;   // 当没有任何玩家连接时，游戏在这个时间后被删除
        public static float winExpireTime = 60f;    // 当只剩一个玩家在线时，超过这个时间将自动判定他获胜

        private Game gameData;
        private GameLogic gameplay;
        private float expiration = 0f;
        private float winExpiration = 0f;
        private bool isDedicatedServer = false;
        private bool isDisposed = false;

        private List<ClientData> players = new();            // 只包含玩家（不包含观察者），断线后仍保留在数组中，只有玩家可以发送指令
        private List<ClientData> connectedClients = new();  // 包含所有已连接客户端（包括观察者），断线会移除，所有人都会接收刷新数据
        private readonly List<ulong> connectedClientIds = new(); // 与 connectedClients 同步，用于无临时分配的多目标广播
        private List<AIPlayer> aiPlayers = new();                // AI 玩家列表
        private Queue<PendingClientCommand> pendingCommands = new();
        private HashSet<int> playersSettingUp = new();         // 正在异步校验配置的玩家，防止重复提交
        
        private Dictionary<ushort, Action<ClientData, SerializedData>> commandHandlers = new();

        public ulong ServerID { get { return TcgNetwork.Get().ServerID; } }
        public NetworkMessaging Messaging { get { return TcgNetwork.Get().Messaging; } }

        public GameServer(string uid, int players, bool online)
        {
            Init(uid, players, online);
        }

        public void Dispose()
        {
            if (isDisposed) return;

            isDisposed = true;
            Clear();
        }

        private void Init(string uid, int players, bool online)
        {
            gameUID = uid;
            playersCount = Mathf.Max(players, 2);
            isDedicatedServer = online;
            gameData = new Game(uid, playersCount);
            gameplay = new GameLogic(gameData);

            // 注册各种游戏指令
            RegisterCommandHandler(GameAction.PlayerSettings, ReceivePlayerSettings);
            RegisterCommandHandler(GameAction.PlayerSettingsForAI, ReceivePlayerSettingsForAI);
            RegisterCommandHandler(GameAction.GameSettings, ReceiveGameplaySettings);
            RegisterCommandHandler(GameAction.PlayCard, ReceivePlayCard);
            RegisterCommandHandler(GameAction.Attack, ReceiveAttackTarget);
            RegisterCommandHandler(GameAction.AttackPlayer, ReceiveAttackPlayer);
            RegisterCommandHandler(GameAction.Move, ReceiveMove);
            RegisterCommandHandler(GameAction.CastAbility, ReceiveCastCardAbility);
            RegisterCommandHandler(GameAction.SelectCard, ReceiveSelectCard);
            RegisterCommandHandler(GameAction.SelectPlayer, ReceiveSelectPlayer);
            RegisterCommandHandler(GameAction.SelectSlot, ReceiveSelectSlot);
            RegisterCommandHandler(GameAction.SelectChoice, ReceiveSelectChoice);
            RegisterCommandHandler(GameAction.SelectCost, ReceiveSelectCost);
            RegisterCommandHandler(GameAction.SelectMulligan, ReceiveSelectMulligan);
            RegisterCommandHandler(GameAction.CancelSelect, ReceiveCancelSelection);
            RegisterCommandHandler(GameAction.EndTurn, ReceiveEndTurn);
            RegisterCommandHandler(GameAction.Resign, ReceiveResign);
            RegisterCommandHandler(GameAction.ChatMessage, ReceiveChat);

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
            gameplay.onCardHealed -= OnCardHealed;
            gameplay.onPlayerHealed -= OnPlayerHealed;

            gameplay.onSecretTrigger -= OnSecretTriggered;
            gameplay.onSecretResolve -= OnSecretResolved;

            pendingCommands.Clear();
            playersSettingUp.Clear();
            aiPlayers.Clear();
            connectedClients.Clear();
            connectedClientIds.Clear();
            players.Clear();
        }

        public void Update()
        {
            if (isDisposed) return;

            StartGameWhenReady();
            UpdateGameLifecycle(Time.deltaTime);
            UpdateTurnTimer(Time.deltaTime);

            // 注意顺序，先处理输入，再游戏逻辑，输入正好会带来需要更新的逻辑。然后再更新AI，让AI感知最新的游戏变化
            ProcessNextPendingCommand();
            gameplay.Update(Time.deltaTime);
            foreach (AIPlayer ai in aiPlayers)
            {
                ai.Update();
            }
        }

        public void ReceiveCommand(ulong client_id, FastBufferReader reader)
        {
            if (isDisposed) return;

            ClientData client = GetClient(client_id);
            if (client == null) return;

            reader.ReadValueSafe(out ushort type);
            SerializedData sdata = new(reader);
            if (!gameplay.IsResolving())
            {
                ExecuteCommand(type, client, sdata);
            }
            else
            {
                sdata.PreRead();
                PendingClientCommand command = new()
                {
                    type = type,
                    client = client,
                    sdata = sdata
                };
                pendingCommands.Enqueue(command);
            }
        }

        public void ExecuteCommand(ushort type, ClientData client, SerializedData sdata)
        {
            if (commandHandlers.TryGetValue(type, out var handler))
            {
                handler.Invoke(client, sdata);
            }
        }

        
        //-------------  客户端 & 玩家管理  --------------

        // 添加一个客户端到已连接列表（如果还没添加）
        public void AddClient(ClientData client)
        {
            if (client == null || connectedClients.Contains(client))
                return;

            connectedClients.Add(client);
            connectedClientIds.Add(client.client_id);
        }

        // 从已连接客户端列表中移除一个客户端
        public void RemoveClient(ClientData client)
        {
            if (client == null)
                return;

            connectedClients.Remove(client);
            connectedClientIds.Remove(client.client_id);

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
            foreach (ClientData client in connectedClients)
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
            Player player = gameData.GetPlayer(player_id);

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
            return gameData?.GetPlayer(player_id);
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

        // 获取当前 Game 数据（封装一层 gameplay）
        public Game GetGameData()
        {
            return gameplay.GetGameData();
        }

        // 游戏是否已经正式开始
        public bool HasGameStarted()
        {
            return gameData.HasStarted();
        }

        // 游戏是否已经结束
        public bool HasGameEnded()
        {
            return gameData.HasEnded();
        }

        // 游戏是否“整体过期”
        // 代表游戏已无人继续参与或终局状态持续太久，可以销毁
        public bool IsGameExpired()
        {
            return expiration > gameExpireTime; 
        }

        //---------------- 游戏事件分发（同步给所有客户端） ----------------

        // 游戏开始（通知所有客户端 + 如果是在线服务器则创建比赛记录）
        protected virtual void OnGameStart()
        {
            SendToAll(GameAction.GameStart);

            if (isDedicatedServer && Authenticator.Get().IsApi())
            {
                // 如果接入 Web API，则同步到后端，创建比赛记录
                ApiClient.Get().CreateMatch(gameData);
            }
        }

        // 游戏结束（同步给所有客户端 + 通知后端发放奖励）
        protected virtual void OnGameEnd(Player winner)
        {
            MsgPlayer msg = new MsgPlayer();
            msg.player_id = winner != null ? winner.player_id : -1;
            SendToAll(GameAction.GameEnd, msg, NetworkDelivery.Reliable);

            if (isDedicatedServer && Authenticator.Get().IsApi())
            {
                // 通知服务器比赛结束并结算奖励
                ApiClient.Get().EndMatch(gameData, winner.player_id);
            }
        }

        // 新回合开始（告知所有客户端是谁的回合）
        protected virtual void OnTurnStart()
        {
            MsgPlayer msg = new MsgPlayer();
            msg.player_id = gameData.current_player;
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
        /// 强制刷新整个游戏状态（同步完整 GameData）
        /// </summary>
        public void RefreshAll()
        {
            MsgRefreshAll mdata = new()
            {
                game_data = GetGameData()  // 当前完整游戏数据
            };
            SendToAll(GameAction.RefreshAll, mdata, NetworkDelivery.ReliableFragmentedSequenced);
        }

        /// <summary>
        /// 通知所有客户端：某个玩家已准备
        /// </summary>
        private void SendPlayerReady(Player player)
        {
            if (player == null || !player.IsReady()) return;

            MsgInt mdata = new()
            {
                value = player.player_id   // 发送玩家ID
            };
            SendToAll(GameAction.PlayerReady, mdata, NetworkDelivery.Reliable);
        }


        /// <summary>
        /// 仅发送一个指令 Tag（无额外数据）
        /// </summary>
        private void SendToAll(ushort tag)
        {
            Messaging.SendTagged("refresh", connectedClientIds, tag, NetworkDelivery.Reliable);
        }

        /// <summary>
        /// 发送带网络序列化数据的消息给所有客户端
        /// </summary>
        private void SendToAll(ushort tag, INetworkSerializable data, NetworkDelivery delivery)
        {
            Messaging.SendTagged("refresh", connectedClientIds, tag, data, delivery);
        }


        private void RegisterCommandHandler(ushort tag, Action<ClientData, SerializedData> handler)
        {
            commandHandlers.Add(tag, handler);
        }

        // 连接阶段，若所有玩家已连接且准备完成则开始游戏
        private void StartGameWhenReady()
        {
            if (gameData.state == GameState.Connecting
                && gameData.AreAllPlayersConnected()
                && gameData.AreAllPlayersReady())
            {
                StartGame();
            }
        }
        
        private void UpdateGameLifecycle(float deltaTime)
        {
            int connectedPlayers = gameData.CountConnectedPlayers();

            if (connectedPlayers == 0 || HasGameEnded())
            {
                expiration += deltaTime;
            } else
            {
                expiration = 0f;
            }

            if (isDedicatedServer && connectedPlayers == 1 && gameData.IsPlaying())
            {
                winExpiration += deltaTime;
            } else
            {
                winExpiration = 0f;
            }

            if (winExpiration >= winExpireTime)
            {
                foreach (Player player in gameData.players)
                {
                    if (player.IsConnected())
                    {
                        gameplay.EndGame(player.player_id);
                        return;
                    }
                }
            }
        }

        // 游戏进行中的回合计时
        private void UpdateTurnTimer(float deltaTime)
        {
            if (gameData.state == GameState.Play && !gameplay.IsResolving())
            {
                gameData.turn_timer -= deltaTime;
                if (gameData.turn_timer <= 0f)
                {
                    gameplay.NextStep();
                }
            }
        }

        private void ProcessNextPendingCommand()
        {
            if (pendingCommands.Count > 0 && !gameplay.IsResolving())
            {
                PendingClientCommand command = pendingCommands.Dequeue();
                ExecuteCommand(command.type, command.client, command.sdata);
            }
        }

        private void StartGame()
        {
            CreateAiForGame();
            gameplay.StartGame();
        }

        private void CreateAiForGame()
        {
            bool ai_vs_ai = !isDedicatedServer && GameplayData.Get().ai_vs_ai;
            foreach (Player player in gameData.players)
            {
                if (player.is_ai || ai_vs_ai)
                {
                    AIPlayer aiPlayer = AIPlayer.Create(
                        GameplayData.Get().ai_type,
                        gameplay,
                        player.player_id,
                        player.ai_level
                    );
                    aiPlayers.Add(aiPlayer);
                }
            }
        }


        #region command handlers
        private async void ReceivePlayerSettings(ClientData iclient, SerializedData sdata)
        {
            PlayerSettings msg = sdata.Get<PlayerSettings>();
            Player player = GetPlayer(iclient);
            if (player == null || msg == null) return;

            Player readyPlayer = await SetPlayerSettingsAsync(player.player_id, msg);
            if (readyPlayer == null) return;

            SendPlayerReady(readyPlayer);
            RefreshAll();   // 通知所有客户端刷新 UI / 数据
        }

        private async void ReceivePlayerSettingsForAI(ClientData iclient, SerializedData sdata)
        {
            PlayerSettings msg = sdata.Get<PlayerSettings>();
            Player player = GetPlayer(iclient);
            if (player != null && msg != null)
            {
                await SetPlayerSettingsAIAsync(player.player_id, msg);
            }
        }

        // 接收客户端发送的游戏配置（只在连接阶段有效）
        private void ReceiveGameplaySettings(ClientData iclient, SerializedData sdata)
        {
            GameSettings settings = sdata.Get<GameSettings>();
            if (settings != null)
            {
                SetGameSettings(settings);
            }
        }

        // 接收“打出卡牌”请求
        private void ReceivePlayCard(ClientData iclient, SerializedData sdata)
        {
            MsgPlayCard msg = sdata.Get<MsgPlayCard>();
            Player player = GetPlayer(iclient);
            // 必须：玩家存在 + 消息有效 + 当前轮到该玩家行动 + 不能在结算中
            if (player != null && msg != null && gameplay.Rules.IsPlayerActionTurn(player) && !gameplay.IsResolving())
            {
                Card card = player.GetCard(msg.card_uid);
                // 校验卡牌存在且属于该玩家，防止作弊
                if (card != null && card.player_id == player.player_id)
                    gameplay.PlayCard(card, msg.slot);
            }
        }

        // 接收“攻击目标卡牌”请求
        private void ReceiveAttackTarget(ClientData iclient, SerializedData sdata)
        {
            MsgAttack msg = sdata.Get<MsgAttack>();
            Player player = GetPlayer(iclient);
            if (player != null && msg != null && gameplay.Rules.IsPlayerActionTurn(player) && !gameplay.IsResolving())
            {
                Card attacker = player.GetCard(msg.attacker_uid);
                Card target = gameData.GetCard(msg.target_uid);
                // 攻击者必须属于该玩家
                if (attacker != null && target != null && attacker.player_id == player.player_id)
                {
                    gameplay.AttackTarget(attacker, target);
                }
            }
        }

        // 接收“攻击玩家”请求
        private void ReceiveAttackPlayer(ClientData iclient, SerializedData sdata)
        {
            MsgAttackPlayer msg = sdata.Get<MsgAttackPlayer>();
            Player player = GetPlayer(iclient);
            if (player != null && msg != null && gameplay.Rules.IsPlayerActionTurn(player) && !gameplay.IsResolving())
            {
                Card attacker = player.GetCard(msg.attacker_uid);
                Player target = gameData.GetPlayer(msg.target_id);
                if (attacker != null && target != null && attacker.player_id == player.player_id)
                {
                    gameplay.AttackPlayer(attacker, target);
                }
            }
        }

        // 接收“移动卡牌位置”请求
        private void ReceiveMove(ClientData iclient, SerializedData sdata)
        {
            MsgPlayCard msg = sdata.Get<MsgPlayCard>();
            Player player = GetPlayer(iclient);
            if (player != null && msg != null && gameplay.Rules.IsPlayerActionTurn(player) && !gameplay.IsResolving())
            {
                Card card = player.GetCard(msg.card_uid);
                if (card != null && card.player_id == player.player_id)
                    gameplay.MoveCard(card, msg.slot);
            }
        }

        // 接收“施放卡牌技能”请求
        private void ReceiveCastCardAbility(ClientData iclient, SerializedData sdata)
        {
            MsgCastAbility msg = sdata.Get<MsgCastAbility>();
            Player player = GetPlayer(iclient);
            if (player != null && msg != null && gameplay.Rules.IsPlayerActionTurn(player) && !gameplay.IsResolving())
            {
                Card card = player.GetCard(msg.caster_uid);
                AbilityData iability = AbilityData.Get(msg.ability_id);
                if (card != null && card.player_id == player.player_id)
                    gameplay.CastAbility(card, iability);
            }
        }

        // 接收“选择卡牌”请求（用于选择阶段）
        private void ReceiveSelectCard(ClientData iclient, SerializedData sdata)
        {
            MsgCard msg = sdata.Get<MsgCard>();
            Player player = GetPlayer(iclient);
            if (player != null && msg != null && gameplay.Rules.IsPlayerSelectorTurn(player) && !gameplay.IsResolving())
            {
                Card target = gameData.GetCard(msg.card_uid);
                gameplay.SelectCard(target);
            }
        }

        // 接收“选择玩家”请求
        private void ReceiveSelectPlayer(ClientData iclient, SerializedData sdata)
        {
            MsgPlayer msg = sdata.Get<MsgPlayer>();
            Player player = GetPlayer(iclient);
            if (player != null && msg != null && gameplay.Rules.IsPlayerSelectorTurn(player) && !gameplay.IsResolving())
            {
                Player target = gameData.GetPlayer(msg.player_id);
                gameplay.SelectPlayer(target);
            }
        }

        // 接收“选择格子位置”请求
        private void ReceiveSelectSlot(ClientData iclient, SerializedData sdata)
        {
            Slot slot = sdata.Get<Slot>();
            Player player = GetPlayer(iclient);
            if (player != null && gameplay.Rules.IsPlayerSelectorTurn(player) && !gameplay.IsResolving())
            {
                // slot 校验有效性
                if(gameData.Board.Contains(slot))
                    gameplay.SelectSlot(slot);
            }
        }

        // 接收“选择某个选项（数字型）”请求
        private void ReceiveSelectChoice(ClientData iclient, SerializedData sdata)
        {
            MsgInt msg = sdata.Get<MsgInt>();
            Player player = GetPlayer(iclient);
            if (player != null && msg != null && gameplay.Rules.IsPlayerSelectorTurn(player) && !gameplay.IsResolving())
            {
                gameplay.SelectChoice(msg.value);
            }
        }

        // 接收“选择费用”请求
        private void ReceiveSelectCost(ClientData iclient, SerializedData sdata)
        {
            MsgInt msg = sdata.Get<MsgInt>();
            Player player = GetPlayer(iclient);
            if (player != null && msg != null && gameplay.Rules.IsPlayerSelectorTurn(player) && !gameplay.IsResolving())
            {
                gameplay.SelectCost(msg.value);
            }
        }

        // 接收“取消选择”请求
        private void ReceiveCancelSelection(ClientData iclient, SerializedData sdata)
        {
            Player player = GetPlayer(iclient);
            if (player != null && gameplay.Rules.IsPlayerSelectorTurn(player) && !gameplay.IsResolving())
            {
                gameplay.CancelSelection();
            }
        }

        // 接收“调度牌（换牌阶段 Mulligan）”请求
        private void ReceiveSelectMulligan(ClientData iclient, SerializedData sdata)
        {
            MsgMulligan msg = sdata.Get<MsgMulligan>();
            Player player = GetPlayer(iclient);
            if (player != null && msg != null && gameplay.Rules.IsPlayerMulliganTurn(player) && !gameplay.IsResolving())
            {
                gameplay.Mulligan(player, msg.cards);
            }
        }

        // 接收“结束回合”请求
        private void ReceiveEndTurn(ClientData iclient, SerializedData sdata)
        {
            Player player = GetPlayer(iclient);
            if (player != null && gameplay.Rules.IsPlayerTurn(player))
            {
                gameplay.NextStep();
            }
        }

        // 接收“投降”请求
        private void ReceiveResign(ClientData iclient, SerializedData sdata)
        {
            Player player = GetPlayer(iclient);
            // 游戏必须已开始且未结束
            if (player != null && gameData.state != GameState.Connecting && gameData.state != GameState.GameEnded)
            {
                // 认输则对方直接获胜
                int winner = player.player_id == 0 ? 1 : 0;
                gameplay.EndGame(winner);
            }
        }

        // 接收聊天消息（强制绑定 sender，防止伪造 player_id）
        private void ReceiveChat(ClientData iclient, SerializedData sdata)
        {
            MsgChat msg = sdata.Get<MsgChat>();
            Player player = GetPlayer(iclient);
            if (player != null && msg != null)
            {
                msg.player_id = player.player_id; // 强制覆盖，防止伪造身份
                SendToAll(GameAction.ChatMessage, msg, NetworkDelivery.Reliable);
            }
        }
        #endregion

        #region 准备操作

        private async Task<bool> SetPlayerDeckAsync(int playerId, string username, UserDeckData deck)
        {
            Player player = gameData.GetPlayer(playerId);
            // 只有在房间还处于 Connecting 阶段时才能设置卡组
            if (isDisposed || player == null || gameData.state != GameState.Connecting)
                return false;

            if (deck == null || string.IsNullOrEmpty(deck.tid))
            {
                Debug.Log("Player " + playerId + " submitted an empty deck");
                return false;
            }

            // 1.测试模式，直接信任客户端传过来的卡组
            if (Authenticator.Get().IsTest())
            {
                gameplay.SetPlayerDeck(player, deck);
                return true;
            }

            // 2.在用户卡组中搜索
            UserData user = await LoadUserDataAsync(username);

            // API 请求返回时，对局可能已经离开 Connecting 阶段，需要再次确认
            player = gameData.GetPlayer(playerId);
            if (isDisposed || player == null || gameData.state != GameState.Connecting)
                return false;

            UserDeckData ownedDeck = user?.GetDeck(deck.tid);
            if (user != null && ownedDeck != null)
                return TryApplyOwnedDeck(player, user, ownedDeck);

            // 3.在游戏预设卡组搜索
            return TryApplyBuiltInDeck(player, deck.tid);
        }

        /// <summary>
        /// 设置真实玩家（人类玩家）的配置
        /// 只能在 Connecting 阶段执行
        /// 设置头像、卡背、卡组，并标记为 ready
        /// </summary>
        private async Task<Player> SetPlayerSettingsAsync(int player_id, PlayerSettings psettings)
        {
            if (isDisposed || gameData.state != GameState.Connecting || psettings == null)
            {
                return null;
            }

            Player player = gameData.GetPlayer(player_id);
            if (player == null || player.ready || !playersSettingUp.Add(player_id))
            {
                return null;
            }

            try
            {
                bool deckSet = await SetPlayerDeckAsync(player_id, player.username, psettings.deck);
                if (isDisposed || !deckSet || gameData.state != GameState.Connecting)
                {
                    return null;
                }
            
                player.avatar = psettings.avatar;       // 玩家头像
                player.cardback = psettings.cardback;   // 卡背皮肤
                player.is_ai = false;                   // 明确标记为真人玩家
                player.ready = true;                    // 卡组校验完成后才允许进入 Ready
                return player;
            }
            finally
            {
                playersSettingUp.Remove(player_id);
            }
        }

        /// <summary>
        /// 设置 AI 玩家配置
        /// 只能在 Connecting 阶段执行
        /// 服务器模式下禁止 AI（只能本地游戏用）
        /// 设置 AI 名字、头像、卡组、难度，并设为 ready
        /// </summary>
        public async Task<bool> SetPlayerSettingsAIAsync(int player_id, PlayerSettings psettings)
        {
            if (isDisposed || gameData.state != GameState.Connecting || psettings == null)
                return false; // 游戏已开始，不能设置

            if (isDedicatedServer)
                return false; // 专用服务器上不允许 AI

            // AI 作为“对手玩家”加入
            Player player = gameData.GetOpponentPlayer(player_id);
            if (player == null || player.ready || !playersSettingUp.Add(player.player_id))
                return false;

            try
            {
                bool deck_set = await SetPlayerDeckAsync(player.player_id, psettings.username, psettings.deck);
                if (isDisposed || !deck_set || gameData.state != GameState.Connecting)
                    return false;

                player.username = psettings.username;   // AI 名字
                player.avatar = psettings.avatar;       // AI 头像
                player.cardback = psettings.cardback;   // AI 卡背
                player.is_ai = true;                    // 标记为 AI
                player.ai_level = psettings.ai_level;   // AI 难度
                player.ready = true;                    // 卡组设置完成后 AI 才就绪

                SendPlayerReady(player);
                RefreshAll();   // 通知客户端刷新
                return true;
            }
            finally
            {
                playersSettingUp.Remove(player.player_id);
            }
        }

        /// <summary>
        /// 设置游戏规则 / 游戏参数（回合时间、生命值等）
        /// 只能在游戏开始前（Connecting 阶段）修改
        /// </summary>
        public void SetGameSettings(GameSettings settings)
        {
            if (gameData.state == GameState.Connecting)
            {
                gameData.settings = settings;  // 更新游戏设置
                RefreshAll();                  // 广播刷新
            }
        }

        private async Task<UserData> LoadUserDataAsync(string username)
        {
            UserData user = null;
            // 默认：离线模式，从本地 Authenticator 获取用户数据
            if (!Authenticator.Get().IsApi())
            {
                user = Authenticator.Get().UserData;
            } else  // 如果是在线模式，通过 API 拉取真实用户数据（校验）
            {
                user = await ApiClient.Get().LoadUserData(username);
            }

            return user;
        }

        private bool TryApplyOwnedDeck(Player player, UserData user, UserDeckData ownedDeck)
        {
            if (user.IsDeckValid(ownedDeck))
            {
                gameplay.SetPlayerDeck(player, ownedDeck);
                return true;
            } else
            {
                Debug.Log(user.username + " deck is invalid: " + ownedDeck.title);
                return false;
            }
        }

        private bool TryApplyBuiltInDeck(Player player, string deckId)
        {
            DeckData deck = DeckData.Get(deckId);
            if (deck != null)
            {
                gameplay.SetPlayerDeck(player, deck);
                return true;
            } else
            {
                Debug.Log("Player " + player.player_id+ " deck not found: " + deckId);
                return false;
            }
        }

        #endregion
    }

    public struct PendingClientCommand
    {
        public ushort type;        // 指令类型
        public ClientData client;  // 发起该指令的客户端
        public SerializedData sdata; // 序列化后的数据
    }
}
