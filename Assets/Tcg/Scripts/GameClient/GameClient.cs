using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;
using System.Threading.Tasks;
using TcgEngine.Gameplay;

namespace TcgEngine.Client
{
       /// <summary>
    /// 客户端游戏主控制脚本，只应该存在于游戏场景中
    /// 启动后会先连接服务器，然后使用UID连接到指定游戏房间，并向服务器发送游戏设定
    /// 在游戏过程中，负责发送玩家操作，并接收来自服务器的游戏刷新数据
    /// </summary>

    public class GameClient : MonoBehaviour
    {
        //--- 这些设置会在菜单场景中设置，在进入游戏时会发送给服务器

        public static GameSettings game_settings = GameSettings.Default;        // 游戏设置
        public static PlayerSettings player_settings = PlayerSettings.Default;  // 玩家本地设置
        public static PlayerSettings ai_settings = PlayerSettings.DefaultAI;     // AI 玩家设置
        public static string observe_user = null; // 观战模式下，指定要观看哪个玩家；为 null 时表示不是观战

        //-----

        public UnityAction onConnectServer;      // 连接服务器成功回调
        public UnityAction onConnectGame;        // 成功加入游戏回调
        public UnityAction<int> onPlayerReady;   // 某个玩家准备完毕

        public UnityAction onGameStart;          // 游戏开始
        public UnityAction<int> onGameEnd;       // 游戏结束（参数：胜利玩家 player_id）
        public UnityAction<int> onNewTurn;       // 新回合开始（参数：当前玩家 player_id）

        public UnityAction<Card, Slot> onCardPlayed;       // 卡被打出
        public UnityAction<Card, Slot> onCardMoved;        // 卡被移动
        public UnityAction<Slot> onCardSummoned;           // 卡被召唤
        public UnityAction<Card> onCardTransformed;        // 卡被变形
        public UnityAction<Card> onCardDiscarded;          // 卡被丢弃
        public UnityAction<int> onCardDraw;                // 抽卡
        public UnityAction<int> onValueRolled;             // 骰子/随机数掷值

        public UnityAction<AbilityData, Card> onAbilityStart;                     // 技能开始
        public UnityAction<AbilityData, Card, Card> onAbilityTargetCard;          // 技能目标：卡牌
        public UnityAction<AbilityData, Card, Player> onAbilityTargetPlayer;      // 技能目标：玩家
        public UnityAction<AbilityData, Card, Slot> onAbilityTargetSlot;          // 技能目标：格子
        public UnityAction<AbilityData, Card> onAbilityEnd;                       // 技能结束
        public UnityAction<Card, Card> onSecretTrigger;      // 秘密被触发（秘密卡，触发者）
        public UnityAction<Card, Card> onSecretResolve;      // 秘密结算完毕（秘密卡，触发者）

        public UnityAction<Card, Card> onAttackStart;        // 攻击开始（攻击者，防御者）
        public UnityAction<Card, Card> onAttackEnd;          // 攻击结束
        public UnityAction<Card, Player> onAttackPlayerStart; // 攻击玩家开始
        public UnityAction<Card, Player> onAttackPlayerEnd;   // 攻击玩家结束

        public UnityAction<Card, int> onCardDamaged;         // 卡受到伤害
        public UnityAction<Player, int> onPlayerDamaged;     // 玩家受到伤害
        public UnityAction<Card, int> onCardHealed;          // 卡被治疗
        public UnityAction<Player, int> onPlayerHealed;      // 玩家被治疗

        public UnityAction<int, string> onChatMsg;   // 聊天消息（玩家id，消息）
        public UnityAction<string> onServerMsg;      // 服务器系统消息
        public UnityAction onRefreshAll;             // 刷新整个游戏数据

        private int player_id = 0;        // 当前设备所控制的玩家ID
        private Game game_data;           // 当前游戏数据
        private readonly GameRules rules = new GameRules(null);

        private bool observe_mode = false;  // 是否观战模式
        private int observe_player_id = 0;  // 观战目标
        private float timer = 0f;

        private Dictionary<ushort, RefreshEvent> registered_commands = new Dictionary<ushort, RefreshEvent>();  // 注册的刷新指令

        private static GameClient instance;

        protected virtual void Awake()
        {
            instance = this;
            Application.targetFrameRate = 120;   // 设置目标帧率
        }

        protected virtual void Start()
        {
            // 注册服务器刷新事件与回调
            RegisterRefresh(GameAction.Connected, OnConnectedToGame);
            RegisterRefresh(GameAction.PlayerReady, OnPlayerReady);
            RegisterRefresh(GameAction.GameStart, OnGameStart);
            RegisterRefresh(GameAction.GameEnd, OnGameEnd);
            RegisterRefresh(GameAction.NewTurn, OnNewTurn);
            RegisterRefresh(GameAction.CardPlayed, OnCardPlayed);
            RegisterRefresh(GameAction.CardMoved, OnCardMoved);
            RegisterRefresh(GameAction.CardSummoned, OnCardSummoned);
            RegisterRefresh(GameAction.CardTransformed, OnCardTransformed);
            RegisterRefresh(GameAction.CardDiscarded, OnCardDiscarded);
            RegisterRefresh(GameAction.CardDrawn, OnCardDraw);
            RegisterRefresh(GameAction.ValueRolled, OnValueRolled);

            RegisterRefresh(GameAction.AttackStart, OnAttackStart);
            RegisterRefresh(GameAction.AttackEnd, OnAttackEnd);
            RegisterRefresh(GameAction.AttackPlayerStart, OnAttackPlayerStart);
            RegisterRefresh(GameAction.AttackPlayerEnd, OnAttackPlayerEnd);
            RegisterRefresh(GameAction.CardDamaged, OnCardDamaged);
            RegisterRefresh(GameAction.PlayerDamaged, OnPlayerDamaged);
            RegisterRefresh(GameAction.CardHealed, OnCardHealed);
            RegisterRefresh(GameAction.PlayerHealed, OnPlayerHealed);

            RegisterRefresh(GameAction.AbilityTrigger, OnAbilityTrigger);
            RegisterRefresh(GameAction.AbilityTargetCard, OnAbilityTargetCard);
            RegisterRefresh(GameAction.AbilityTargetPlayer, OnAbilityTargetPlayer);
            RegisterRefresh(GameAction.AbilityTargetSlot, OnAbilityTargetSlot);
            RegisterRefresh(GameAction.AbilityEnd, OnAbilityAfter);

            RegisterRefresh(GameAction.SecretTriggered, OnSecretTrigger);
            RegisterRefresh(GameAction.SecretResolved, OnSecretResolve);

            RegisterRefresh(GameAction.ChatMessage, OnChat);
            RegisterRefresh(GameAction.ServerMessage, OnServerMsg);
            RegisterRefresh(GameAction.RefreshAll, OnRefreshAll);

            TcgNetwork.Get().onConnect += OnConnectedServer;          // 服务器连接回调
            TcgNetwork.Get().Messaging.ListenMsg("refresh", OnReceiveRefresh);  // 监听刷新消息

            ConnectToAPI();     // 连接 API
            ConnectToServer();  // 连接游戏服务器
        }

        protected virtual void OnDestroy()
        {
            TcgNetwork.Get().onConnect -= OnConnectedServer;
            TcgNetwork.Get().Messaging.UnListenMsg("refresh");
        }

        protected virtual void Update()
        {
            bool is_starting = game_data == null || game_data.state == GameState.Connecting;
            bool is_client = !game_settings.IsHost();
            bool is_connecting = TcgNetwork.Get().IsConnecting();
            bool is_connected = TcgNetwork.Get().IsConnected();

            // 如果是客户端并且长时间未连接成功，则返回菜单
            if (is_starting && is_client)
            {
                timer += Time.deltaTime;
                if (timer > 10f)
                {
                    SceneNav.GoTo("Menu");
                }
            }

            // 如果游戏中途断线，则尝试自动重连
            if (!is_starting && !is_connecting && is_client && !is_connected)
            {
                timer += Time.deltaTime;
                if (timer > 5f)
                {
                    timer = 0f;
                    ConnectToServer();
                }
            }
        }

        //--------------------

        public virtual void ConnectToAPI()
        {
            // 理论上应该已经在菜单登录
            // 如果没有登录（例如直接从 Unity 启动游戏场景），则进入测试模式
            if (!Authenticator.Get().IsSignedIn())
            {
                Authenticator.Get().LoginTest("Player");

                if (!player_settings.HasDeck())
                {
                    player_settings.deck = new UserDeckData(GameplayData.Get().test_deck);
                }

                if (!ai_settings.HasDeck())
                {
                    ai_settings.deck = new UserDeckData(GameplayData.Get().test_deck_ai);
                    ai_settings.ai_level = GameplayData.Get().ai_level;
                }
            }

            // 从 API 用户数据同步头像与卡背
            UserData udata = Authenticator.Get().UserData;
            if (udata != null)
            {
                player_settings.avatar = udata.GetAvatar();
                player_settings.cardback = udata.GetCardback();
            }
        }


                public virtual async void ConnectToServer()
        {
            await TimeTool.Delay(100); // 等待初始化完成

            if (TcgNetwork.Get().IsActive())
                return; // 已经连接

            if (game_settings.IsHost() && NetworkData.Get().solo_type == SoloType.Offline)
            {
                TcgNetwork.Get().StartHostOffline();    
                // WebGL 不支持自己开主机，必须加入专用服务器
                // 单机模式下启动离线模式，不使用网络代码
            }
            else if (game_settings.IsHost())
            {
                TcgNetwork.Get().StartHost(NetworkData.Get().port);       
                // 主机游戏，无论单机还是 P2P，单机模式也使用网络代码保持行为一致
            }
            else
            {
                TcgNetwork.Get().StartClient(game_settings.GetUrl(), NetworkData.Get().port);       
                // 连接服务器
            }
        }

        public virtual async void ConnectToGame(string uid)
        {
            await TimeTool.Delay(100); // 等待初始化完成

            if (!TcgNetwork.Get().IsActive())
                return; // 未连接服务器

            Debug.Log("Connect to Game: " + uid);

            MsgPlayerConnect nplayer = new MsgPlayerConnect();
            nplayer.user_id = Authenticator.Get().UserID;   // 玩家ID
            nplayer.username = Authenticator.Get().Username; // 玩家名字
            nplayer.game_uid = uid;                          // 游戏UID
            nplayer.nb_players = game_settings.nb_players;  // 游戏玩家数量
            nplayer.observer = game_settings.game_type == GameType.Observer; // 是否为观战模式

            Messaging.SendObject("connect", ServerID, nplayer, NetworkDelivery.Reliable); // 发送连接请求
        }

        public virtual void SendGameSettings()
        {
            if (game_settings.IsOffline())
            {
                // 单机模式：发送自己的设置和AI设置
                SendGameplaySettings(game_settings);
                SendPlayerSettingsAI(ai_settings);
                SendPlayerSettings(player_settings);
            }
            else
            {
                // 联机模式：只发送自己的设置
                SendGameplaySettings(game_settings);
                SendPlayerSettings(player_settings);
            }
        }

        public virtual void Disconnect()
        {
            TcgNetwork.Get().Disconnect(); // 断开连接
        }

        private void RegisterRefresh(ushort tag, UnityAction<SerializedData> callback)
        {
            RefreshEvent cmdevt = new RefreshEvent();
            cmdevt.tag = tag;          // 刷新事件标识
            cmdevt.callback = callback; // 回调方法
            registered_commands.Add(tag, cmdevt); // 注册刷新事件
        }

        public void OnReceiveRefresh(ulong client_id, FastBufferReader reader)
        {
            reader.ReadValueSafe(out ushort type); // 读取刷新类型
            bool found = registered_commands.TryGetValue(type, out RefreshEvent command);
            if (found)
            {
                command.callback.Invoke(new SerializedData(reader)); // 调用对应回调
            }
        }

        //--------------------------

        public void SendPlayerSettings(PlayerSettings psettings)
        {
            SendAction(GameAction.PlayerSettings, psettings, NetworkDelivery.ReliableFragmentedSequenced);
            // 发送玩家设置
        }

        public void SendPlayerSettingsAI(PlayerSettings psettings)
        {
            SendAction(GameAction.PlayerSettingsAI, psettings, NetworkDelivery.ReliableFragmentedSequenced);
            // 发送AI玩家设置
        }

        public void SendGameplaySettings(GameSettings settings)
        {
            SendAction(GameAction.GameSettings, settings, NetworkDelivery.ReliableFragmentedSequenced);
            // 发送游戏设置
        }

        public void PlayCard(Card card, Slot slot)
        {
            MsgPlayCard mdata = new MsgPlayCard();
            mdata.card_uid = card.uid;
            mdata.slot = slot;
            SendAction(GameAction.PlayCard, mdata); // 打出卡牌
        }

        public void AttackTarget(Card card, Card target)
        {
            MsgAttack mdata = new MsgAttack();
            mdata.attacker_uid = card.uid;
            mdata.target_uid = target.uid;
            SendAction(GameAction.Attack, mdata); // 攻击目标卡牌
        }

        public void AttackPlayer(Card card, Player target)
        {
            MsgAttackPlayer mdata = new MsgAttackPlayer();
            mdata.attacker_uid = card.uid;
            mdata.target_id = target.player_id;
            SendAction(GameAction.AttackPlayer, mdata); // 攻击玩家
        }

        public void Move(Card card, Slot slot)
        {
            MsgPlayCard mdata = new MsgPlayCard();
            mdata.card_uid = card.uid;
            mdata.slot = slot;
            SendAction(GameAction.Move, mdata); // 移动卡牌
        }

        public void CastAbility(Card card, AbilityData ability)
        {
            MsgCastAbility mdata = new MsgCastAbility();
            mdata.caster_uid = card.uid;
            mdata.ability_id = ability.id;
            mdata.target_uid = "";
            SendAction(GameAction.CastAbility, mdata); // 施放技能
        }

        public void SelectCard(Card card)
        {
            MsgCard mdata = new MsgCard();
            mdata.card_uid = card.uid;
            SendAction(GameAction.SelectCard, mdata); // 选择卡牌
        }

        public void SelectPlayer(Player player)
        {
            MsgPlayer mdata = new MsgPlayer();
            mdata.player_id = player.player_id;
            SendAction(GameAction.SelectPlayer, mdata); // 选择玩家
        }

        public void SelectSlot(Slot slot)
        {
            SendAction(GameAction.SelectSlot, slot); // 选择格子
        }

        public void SelectChoice(int c)
        {
            MsgInt choice = new MsgInt();
            choice.value = c;
            SendAction(GameAction.SelectChoice, choice); // 选择选项
        }

        public void SelectCost(int c)
        {
            MsgInt choice = new MsgInt();
            choice.value = c;
            SendAction(GameAction.SelectCost, choice); // 选择费用
        }

        public void Mulligan(string[] cards)
        {
            MsgMulligan mdata = new MsgMulligan();
            mdata.cards = cards;
            SendAction(GameAction.SelectMulligan, mdata); // 选择换牌
        }

        public void CancelSelection()
        {
            SendAction(GameAction.CancelSelect); // 取消选择
        }

        public void SendChatMsg(string msg)
        {
            MsgChat chat = new MsgChat();
            chat.msg = msg;
            chat.player_id = player_id;
            SendAction(GameAction.ChatMessage, chat); // 发送聊天消息
        }

        public void EndTurn()
        {
            SendAction(GameAction.EndTurn); // 结束回合
        }

        public void Resign()
        {
            SendAction(GameAction.Resign); // 投降
        }

        public void SetObserverMode(int player_id)
        {
            observe_mode = true;             // 开启观战模式
            observe_player_id = player_id;   // 设置观战目标玩家
        }


                public void SetObserverMode(string username)
        {
            observe_player_id = 0; // 默认值，未找到指定的观战用户

            Game data = GetGameData();
            foreach (Player player in data.players)
            {
                if (player.username == username)
                {
                    observe_player_id = player.player_id; // 找到对应用户名，设置观战玩家ID
                }
            }
        }

        public void SendAction<T>(ushort type, T data, NetworkDelivery delivery = NetworkDelivery.Reliable) where T : INetworkSerializable
        {
            FastBufferWriter writer = new FastBufferWriter(128, Unity.Collections.Allocator.Temp, TcgNetwork.MsgSizeMax);
            writer.WriteValueSafe(type);           // 写入动作类型
            writer.WriteNetworkSerializable(data); // 写入序列化数据
            Messaging.Send("action", ServerID, writer, delivery); // 发送到服务器
            writer.Dispose();                       // 释放资源
        }

        public void SendAction(ushort type, int data)
        {
            FastBufferWriter writer = new FastBufferWriter(128, Unity.Collections.Allocator.Temp, TcgNetwork.MsgSizeMax);
            writer.WriteValueSafe(type); // 写入动作类型
            writer.WriteValueSafe(data); // 写入整数数据
            Messaging.Send("action", ServerID, writer, NetworkDelivery.Reliable); // 发送到服务器
            writer.Dispose();
        }

        public void SendAction(ushort type)
        {
            FastBufferWriter writer = new FastBufferWriter(128, Unity.Collections.Allocator.Temp, TcgNetwork.MsgSizeMax);
            writer.WriteValueSafe(type); // 写入动作类型
            Messaging.Send("action", ServerID, writer, NetworkDelivery.Reliable); // 发送到服务器
            writer.Dispose();
        }

        //--- 接收刷新事件 ----------------------

        protected virtual void OnConnectedServer()
        {
            ConnectToGame(game_settings.game_uid); // 连接游戏
            onConnectServer?.Invoke();             // 触发连接服务器回调
        }

        protected virtual void OnConnectedToGame(SerializedData sdata)
        {
            MsgAfterConnected msg = sdata.Get<MsgAfterConnected>();
            player_id = msg.player_id;        // 本地玩家ID
            game_data = msg.game_data;        // 游戏数据
            rules.SetData(game_data);
            observe_mode = player_id < 0;     // 如果返回 -1，通常表示是观战模式

            if (observe_mode)
                SetObserverMode(observe_user); // 设置观战目标玩家

            if (onConnectGame != null)
                onConnectGame.Invoke();       // 触发连接游戏回调

            SendGameSettings();                // 发送游戏设置
        }

        protected virtual void OnPlayerReady(SerializedData sdata)
        {
            MsgInt msg = sdata.Get<MsgInt>();
            int pid = msg.value;

            if (onPlayerReady != null)
                onPlayerReady.Invoke(pid); // 触发玩家准备回调
        }

        private void OnGameStart(SerializedData sdata)
        {
            onGameStart?.Invoke(); // 游戏开始回调
        }

        private void OnGameEnd(SerializedData sdata)
        {
            MsgPlayer msg = sdata.Get<MsgPlayer>();
            onGameEnd?.Invoke(msg.player_id); // 游戏结束回调，传输获胜玩家ID
        }

        private void OnNewTurn(SerializedData sdata)
        {
            MsgPlayer msg = sdata.Get<MsgPlayer>();
            onNewTurn?.Invoke(msg.player_id); // 新回合回调，当前玩家ID
        }

        private void OnCardPlayed(SerializedData sdata)
        {
            MsgPlayCard msg = sdata.Get<MsgPlayCard>();
            Card card = game_data.GetCard(msg.card_uid); 
            onCardPlayed?.Invoke(card, msg.slot); // 卡牌打出回调
        }

        private void OnCardSummoned(SerializedData sdata)
        {
            MsgPlayCard msg = sdata.Get<MsgPlayCard>();
            onCardSummoned?.Invoke(msg.slot); // 卡牌召唤回调
        }

        private void OnCardMoved(SerializedData sdata)
        {
            MsgPlayCard msg = sdata.Get<MsgPlayCard>();
            Card card = game_data.GetCard(msg.card_uid);
            onCardMoved?.Invoke(card, msg.slot); // 卡牌移动回调
        }

        private void OnCardTransformed(SerializedData sdata)
        {
            MsgCard msg = sdata.Get<MsgCard>();
            Card card = game_data.GetCard(msg.card_uid);
            onCardTransformed?.Invoke(card); // 卡牌变形回调
        }

        private void OnCardDiscarded(SerializedData sdata)
        {
            MsgCard msg = sdata.Get<MsgCard>();
            Card card = game_data.GetCard(msg.card_uid);
            onCardDiscarded?.Invoke(card); // 卡牌弃置回调
        }

        private void OnCardDraw(SerializedData sdata)
        {
            MsgInt msg = sdata.Get<MsgInt>();
            onCardDraw?.Invoke(msg.value); // 抽牌回调
        }

        private void OnValueRolled(SerializedData sdata)
        {
            MsgInt msg = sdata.Get<MsgInt>();
            onValueRolled?.Invoke(msg.value); // 掷骰子回调
        }

        private void OnAttackStart(SerializedData sdata)
        {
            MsgAttack msg = sdata.Get<MsgAttack>();
            Card attacker = game_data.GetCard(msg.attacker_uid);
            Card target = game_data.GetCard(msg.target_uid);
            onAttackStart?.Invoke(attacker, target); // 攻击开始回调
        }

        private void OnAttackEnd(SerializedData sdata)
        {
            MsgAttack msg = sdata.Get<MsgAttack>();
            Card attacker = game_data.GetCard(msg.attacker_uid);
            Card target = game_data.GetCard(msg.target_uid);
            onAttackEnd?.Invoke(attacker, target); // 攻击结束回调
        }

        private void OnAttackPlayerStart(SerializedData sdata)
        {
            MsgAttackPlayer msg = sdata.Get<MsgAttackPlayer>();
            Card attacker = game_data.GetCard(msg.attacker_uid);
            Player target = game_data.GetPlayer(msg.target_id);
            onAttackPlayerStart?.Invoke(attacker, target); // 攻击玩家开始回调
        }

        private void OnAttackPlayerEnd(SerializedData sdata)
        {
            MsgAttackPlayer msg = sdata.Get<MsgAttackPlayer>();
            Card attacker = game_data.GetCard(msg.attacker_uid);
            Player target = game_data.GetPlayer(msg.target_id);
            onAttackPlayerEnd?.Invoke(attacker, target); // 攻击玩家结束回调
        }

        private void OnCardDamaged(SerializedData sdata)
        {
            MsgCardValue msg = sdata.Get<MsgCardValue>();
            Card card = game_data.GetCard(msg.card_uid);
            onCardDamaged?.Invoke(card, msg.value); // 卡牌受伤回调
        }

        private void OnPlayerDamaged(SerializedData sdata)
        {
            MsgPlayerValue msg = sdata.Get<MsgPlayerValue>();
            Player player = game_data.GetPlayer(msg.player_id);
            onPlayerDamaged?.Invoke(player, msg.value); // 玩家受伤回调
        }

        private void OnCardHealed(SerializedData sdata)
        {
            MsgCardValue msg = sdata.Get<MsgCardValue>();
            Card card = game_data.GetCard(msg.card_uid);
            onCardHealed?.Invoke(card, msg.value); // 卡牌治疗回调
        }

        private void OnPlayerHealed(SerializedData sdata)
        {
            MsgPlayerValue msg = sdata.Get<MsgPlayerValue>();
            Player player = game_data.GetPlayer(msg.player_id);
            onPlayerHealed?.Invoke(player, msg.value); // 玩家治疗回调
        }

        private void OnAbilityTrigger(SerializedData sdata)
        {
            MsgCastAbility msg = sdata.Get<MsgCastAbility>();
            AbilityData ability = AbilityData.Get(msg.ability_id);
            Card caster = game_data.GetCard(msg.caster_uid);
            onAbilityStart?.Invoke(ability, caster); // 技能触发开始回调
        }

        private void OnAbilityTargetCard(SerializedData sdata)
        {
            MsgCastAbility msg = sdata.Get<MsgCastAbility>();
            AbilityData ability = AbilityData.Get(msg.ability_id);
            Card caster = game_data.GetCard(msg.caster_uid);
            Card target = game_data.GetCard(msg.target_uid);
            onAbilityTargetCard?.Invoke(ability, caster, target); // 技能选择目标卡牌回调
        }


                private void OnAbilityTargetPlayer(SerializedData sdata)
        {
            MsgCastAbilityPlayer msg = sdata.Get<MsgCastAbilityPlayer>();
            AbilityData ability = AbilityData.Get(msg.ability_id);
            Card caster = game_data.GetCard(msg.caster_uid);
            Player target = game_data.GetPlayer(msg.target_id);
            onAbilityTargetPlayer?.Invoke(ability, caster, target); // 技能选择目标玩家回调
        }

        private void OnAbilityTargetSlot(SerializedData sdata)
        {
            MsgCastAbilitySlot msg = sdata.Get<MsgCastAbilitySlot>();
            AbilityData ability = AbilityData.Get(msg.ability_id);
            Card caster = game_data.GetCard(msg.caster_uid);
            onAbilityTargetSlot?.Invoke(ability, caster, msg.slot); // 技能选择目标位置回调
        }

        private void OnAbilityAfter(SerializedData sdata)
        {
            MsgCastAbility msg = sdata.Get<MsgCastAbility>();
            AbilityData ability = AbilityData.Get(msg.ability_id);
            Card caster = game_data.GetCard(msg.caster_uid);
            onAbilityEnd?.Invoke(ability, caster); // 技能施放结束回调
        }

        private void OnSecretTrigger(SerializedData sdata)
        {
            MsgSecret msg = sdata.Get<MsgSecret>();
            Card secret = game_data.GetCard(msg.secret_uid);
            Card triggerer = game_data.GetCard(msg.triggerer_uid);
            onSecretTrigger?.Invoke(secret, triggerer); // 秘密触发回调
        }

        private void OnSecretResolve(SerializedData sdata)
        {
            MsgSecret msg = sdata.Get<MsgSecret>();
            Card secret = game_data.GetCard(msg.secret_uid);
            Card triggerer = game_data.GetCard(msg.triggerer_uid);
            onSecretResolve?.Invoke(secret, triggerer); // 秘密结算回调
        }

        private void OnChat(SerializedData sdata)
        {
            MsgChat msg = sdata.Get<MsgChat>();
            onChatMsg?.Invoke(msg.player_id, msg.msg); // 接收聊天消息回调
        }

        private void OnServerMsg(SerializedData sdata)
        {
            string msg = sdata.GetString();
            onServerMsg?.Invoke(msg); // 接收服务器消息回调
        }

        private void OnRefreshAll(SerializedData sdata)
        {
            MsgRefreshAll msg = sdata.Get<MsgRefreshAll>();
            game_data = msg.game_data; // 刷新游戏数据
            rules.SetData(game_data);
            onRefreshAll?.Invoke();    // 触发刷新回调
        }

        //--------------------------

        public virtual bool IsReady()
        {
            return game_data != null && TcgNetwork.Get().IsConnected(); // 判断客户端是否准备好（游戏数据已加载且已连接服务器）
        }

        public Player GetPlayer()
        {
            Game gdata = GetGameData();
            return gdata.GetPlayer(GetPlayerID()); // 获取本地玩家对象
        }

        public Player GetOpponentPlayer()
        {
            Game gdata = GetGameData();
            return gdata.GetPlayer(GetOpponentPlayerID()); // 获取对手玩家对象
        }

        public int GetPlayerID()
        {
            if (observe_mode)
                return observe_player_id; // 如果是观战模式，返回观战玩家ID
            return player_id;             // 否则返回本地玩家ID
        }

        public int GetOpponentPlayerID()
        {
            return GetPlayerID() == 0 ? 1 : 0; // 返回对手玩家ID（假设两名玩家ID分别为0和1）
        }

        public virtual bool IsYourTurn()
        {
            Game game_data = GetGameData();
            Player player = GetPlayer();
            return IsReady() && rules.IsPlayerTurn(player); // 判断是否轮到本地玩家操作
        }

        public bool IsObserveMode()
        {
            return observe_mode; // 判断是否是观战模式
        }

        public Game GetGameData()
        {
            return game_data; // 获取当前游戏数据
        }

        public GameRules Rules => rules;

        public bool HasEnded()
        {
            return game_data != null && game_data.HasEnded(); // 判断游戏是否结束
        }

        private void OnApplicationQuit()
        {
            Resign(); // 应用退出时自动投降（注意：消息可能无法及时发送）
        }

        public bool IsHost { get { return TcgNetwork.Get().IsHost; } }        // 是否为主机
        public ulong ServerID { get { return TcgNetwork.Get().ServerID; } }   // 获取服务器ID
        public NetworkMessaging Messaging { get { return TcgNetwork.Get().Messaging; } } // 获取消息通信对象

        public static GameClient Get()
        {
            return instance; // 获取GameClient单例实例
        }

    }

    public class RefreshEvent
    {
        public ushort tag;                                 // 刷新事件标识
        public UnityAction<SerializedData> callback;       // 回调方法
    }
}
