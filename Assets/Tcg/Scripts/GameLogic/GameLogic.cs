using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace TcgEngine.Gameplay
{
    public enum DamageType
    {
        Combat,
        Spell,
        Status,
        Direct
    }

    public sealed class GameRuntimeContext
    {
        public GameLogic Logic { get; private set; }
        public Game Game { get; private set; }
        public ResolveQueue ResolveQueue { get; private set; }
        public bool IsAiPredict { get; private set; }

        public System.Random Random { get; private set; } = new();
        public CardZoneService CardZoneService;
        public CardSystem CardSystem;
        public HealthSystem HealthSystem;
        public OngoingSystem OngoingSystem;

        public ListSwap<Card> CardTargets = new();         // 临时卡牌列表
        public ListSwap<Player> PlayerTargets = new();   // 临时玩家列表
        public ListSwap<Slot> SlotTargets = new();         // 临时槽位列表
        public ListSwap<CardData> CardDataTargets = new(); // 临时卡牌数据列表
        public List<Card> CardsToClear = new();             // 待清理的卡牌列表

        public GameRuntimeContext(GameLogic logic, Game game, bool isAiPredict)
        {
            Logic = logic ?? throw new ArgumentNullException(nameof(logic));
            ResolveQueue = new ResolveQueue(game, isAiPredict);
            IsAiPredict = isAiPredict;
            Random = new System.Random();
        
            CardZoneService = new CardZoneService();
            HealthSystem = new HealthSystem();
            CardSystem = new CardSystem(this);
            OngoingSystem = new OngoingSystem(this);
        }

        public void SetData(Game game)
        {
            Game = game;
            ResolveQueue.SetData(game);
        }

        public void ClearTargetCaches()
        {
            CardTargets.Clear();
            PlayerTargets.Clear();
            SlotTargets.Clear();
            CardDataTargets.Clear();
        }
    }

    /// <summary>
    /// 执行并处理游戏规则和逻辑
    /// </summary>

    public class GameLogic
    {
        public UnityAction onGameStart;                    // 游戏开始事件
        public UnityAction<Player> onGameEnd;              // 游戏结束事件，参数为获胜玩家

        public UnityAction onTurnStart;                    // 回合开始事件
        public UnityAction onTurnPlay;                     // 回合进行事件（主阶段）
        public UnityAction onTurnEnd;                      // 回合结束事件

        public UnityAction<Card, Slot> onCardPlayed;       // 卡牌出牌事件
        public UnityAction<Card, Slot> onCardSummoned;    // 卡牌召唤事件
        public UnityAction<Card, Slot> onCardMoved;       // 卡牌移动事件
        public UnityAction<Card> onCardTransformed;       // 卡牌变形事件
        public UnityAction<Card> onCardDiscarded;         // 卡牌弃牌事件
        public UnityAction<int> onCardDrawn;              // 卡牌抽取事件
        public UnityAction<int> onRollValue;              // 掷骰事件

        public UnityAction<AbilityData, Card> onAbilityStart;                  // 能力开始触发事件
        public UnityAction<AbilityData, Card, Card> onAbilityTargetCard;       // 能力作用于卡牌事件（Ability, 施法者, 目标卡）
        public UnityAction<AbilityData, Card, Player> onAbilityTargetPlayer;   // 能力作用于玩家事件
        public UnityAction<AbilityData, Card, Slot> onAbilityTargetSlot;       // 能力作用于槽位事件
        public UnityAction<AbilityData, Card> onAbilityEnd;                    // 能力结束事件

        public UnityAction<Card, Card> onAttackStart;        // 攻击开始事件（攻击者, 防御者）
        public UnityAction<Card, Card> onAttackEnd;          // 攻击结束事件
        public UnityAction<Card, Player> onAttackPlayerStart;// 攻击玩家开始事件
        public UnityAction<Card, Player> onAttackPlayerEnd;  // 攻击玩家结束事件

        public UnityAction<Card, int> onCardDamaged;        // 卡牌受伤事件（卡牌, 伤害值）
        public UnityAction<Card, int> onCardHealed;         // 卡牌恢复生命事件
        public UnityAction<Player, int> onPlayerDamaged;    // 玩家受伤事件
        public UnityAction<Player, int> onPlayerHealed;     // 玩家恢复生命事件

        public UnityAction<Card, Card> onSecretTrigger;     // 秘密触发事件（秘密卡, 触发者）
        public UnityAction<Card, Card> onSecretResolve;     // 秘密结算事件（秘密卡, 触发者）

        public UnityAction onRefresh;                       // 刷新事件

        private readonly GameRuntimeContext runtime;

        public GameLogic(Game game, bool isAi = false)
        {
            runtime = new GameRuntimeContext(this, game, isAi);
            SetData(game);
        }

        public virtual void SetData(Game game)
        {
            runtime.SetData(game);
        }

        public virtual void Update(float delta)
        {
            runtime.ResolveQueue.Update(delta); // 更新处理队列
        }

        //----- 回合阶段处理 ----------

        public virtual void StartGame()
        {
            if (runtime.Game.state == GameState.GameEnded) return;

            // 选择先手玩家
            runtime.Game.state = GameState.Play;
            runtime.Game.first_player = runtime.Random.NextDouble() < 0.5 ? 0 : 1;
            runtime.Game.current_player = runtime.Game.first_player;
            runtime.Game.turn_count = 1;

            // 副本/冒险模式设置
            bool should_mulligan = GameplayData.Get().mulligan;
            LevelData level = runtime.Game.settings.GetLevel();
            if (level != null)
            {
                if (level != null && level.first_player == LevelFirst.Player)
                    runtime.Game.first_player = 0;
                if (level != null && level.first_player == LevelFirst.AI)
                    runtime.Game.first_player = 1;
                runtime.Game.current_player = runtime.Game.first_player;
                should_mulligan = level.mulligan;
            }

            // 初始化每个玩家
            foreach (Player player in runtime.Game.players)
            {
                // 关卡指定卡组
                DeckPuzzleData pdeck = DeckPuzzleData.Get(player.deck);

                // 生命值/法力值
                player.hp_max = pdeck != null ? pdeck.start_hp : GameplayData.Get().hp_start;
                player.hp = player.hp_max;
                player.mana_max = pdeck != null ? pdeck.start_mana : GameplayData.Get().mana_start;
                player.mana = player.mana_max;

                // 抽取初始手牌
                int cards_count = pdeck != null ? pdeck.start_cards : GameplayData.Get().cards_start;
                DrawCards(player, cards_count);

                // 给第二位玩家添加额外金币卡
                bool is_random = (level == null) || (level.first_player == LevelFirst.Random);
                if (is_random && player.player_id != runtime.Game.first_player && GameplayData.Get().second_bonus != null)
                {
                    Card card = Card.Create(GameplayData.Get().second_bonus, VariantData.GetDefault(), player);
                    player.cards_hand.Add(card);
                }
            }

            // 初始化游戏状态
            RefreshData();
            onGameStart?.Invoke();

            if(should_mulligan)
                GoToMulligan(); // 进入换牌阶段
            else
                StartTurn();    // 开始回合
        }

        public virtual void StartTurn()
        {
            if (runtime.Game.state == GameState.GameEnded) return;

            ClearTurnData();                   // 清理上回合数据
            runtime.Game.phase = GamePhase.StartTurn;
            RefreshData();                     // 刷新游戏状态
            onTurnStart?.Invoke();             // 回合开始事件

            Player player = runtime.Game.GetActivePlayer();

            // 抽牌
            if (runtime.Game.turn_count > 1 || player.player_id != runtime.Game.first_player)
            {
                DrawCards(player, GameplayData.Get().cards_per_turn);
            }

            // 增加法力值
            player.mana_max += GameplayData.Get().mana_per_turn;
            player.mana_max = Mathf.Min(player.mana_max, GameplayData.Get().mana_max);
            player.mana = player.mana_max;

            // 回合计时器和历史清理
            runtime.Game.turn_timer = GameplayData.Get().turn_duration;
            player.history_list.Clear();

            // 中毒处理
            DamagePlayer(player, player.GetStatusValue(StatusType.Poisoned), DamageType.Status);

            player.hero?.Refresh(); // 刷新英雄状态

            // 刷新场上卡牌及状态效果
            for (int i = player.cards_board.Count - 1; i >= 0; i--)
            {
                Card card = player.cards_board[i];

                if (!card.HasStatus(StatusType.Sleep))
                    card.Refresh(); // 刷新卡牌状态

                if (card.HasStatus(StatusType.Poisoned))
                    DamageCard(card, card.GetStatusValue(StatusType.Poisoned), DamageType.Status); // 中毒伤害
            }

            // 持续能力更新
            UpdateOngoings();

            // 回合开始触发的能力
            TriggerPlayerCardsAbilityType(player, AbilityTrigger.StartOfTurn);
            TriggerPlayerSecrets(player, AbilityTrigger.StartOfTurn);

            runtime.ResolveQueue.AddCallback(StartMainPhase);    // 添加主阶段回调
            runtime.ResolveQueue.ResolveAll(0.2f);               // 立即处理队列
        }

        public virtual void StartNextTurn()
        {
            if (runtime.Game.state == GameState.GameEnded)
                return;

            // 切换到下一位玩家
            runtime.Game.current_player = (runtime.Game.current_player + 1) % runtime.Game.settings.nb_players;

            // 回合计数
            if (runtime.Game.current_player == runtime.Game.first_player)
                runtime.Game.turn_count++;

            CheckForWinner(); // 检查胜利条件
            StartTurn();      // 开始下一回合
        }

        public virtual void StartMainPhase()
        {
            if (runtime.Game.state == GameState.GameEnded)
                return;

            runtime.Game.phase = GamePhase.Main; // 设置为主阶段
            onTurnPlay?.Invoke();             // 回合主阶段事件
            RefreshData();                     // 刷新游戏状态
        }

        public virtual void EndTurn()
        {
            if (runtime.Game.state == GameState.GameEnded)
                return;
            if (runtime.Game.phase != GamePhase.Main)
                return;

            runtime.Game.selector = SelectorType.None;
            runtime.Game.phase = GamePhase.EndTurn; // 设置回合结束阶段

            // 减少状态持续时间
            foreach (Player aplayer in runtime.Game.players)
            {
                aplayer.ReduceStatusDurations();           // 玩家状态减少
                foreach (Card card in aplayer.cards_board)
                    card.ReduceStatusDurations();         // 场上卡牌状态减少
                foreach (Card card in aplayer.cards_equip)
                    card.ReduceStatusDurations();         // 装备卡状态减少
            }

            // 回合结束触发能力
            Player player = runtime.Game.GetActivePlayer();
            TriggerPlayerCardsAbilityType(player, AbilityTrigger.EndOfTurn);

            onTurnEnd?.Invoke();   // 回合结束事件
            RefreshData();         // 刷新状态

            runtime.ResolveQueue.AddCallback(StartNextTurn); // 添加下一回合回调
            runtime.ResolveQueue.ResolveAll(0.2f);           // 立即处理
        }

        // 游戏结束并指定获胜玩家
        public virtual void EndGame(int winner)
        {
            if (runtime.Game.state != GameState.GameEnded)
            {
                runtime.Game.state = GameState.GameEnded;
                runtime.Game.phase = GamePhase.None;
                runtime.Game.selector = SelectorType.None;
                runtime.Game.current_player = winner; // 设置获胜玩家
                runtime.ResolveQueue.Clear();             // 清空处理队列
                Player player = runtime.Game.GetPlayer(winner);
                onGameEnd?.Invoke(player);        // 触发游戏结束事件
                RefreshData();                     // 刷新状态
            }
        }

        // 进入下一步或下一阶段
        public virtual void NextStep()
        {
            if (runtime.Game.state == GameState.GameEnded)
                return;

            if (runtime.Game.phase == GamePhase.Mulligan)
            {
                StartTurn(); // 如果在换牌阶段，直接开始回合
                return;
            }

            CancelSelection(); // 取消当前的选择

            // 添加到解析队列，确保当前操作完成后结束回合
            runtime.ResolveQueue.AddCallback(EndTurn);
            runtime.ResolveQueue.ResolveAll(); // 立即处理队列
        }

        // 检查是否有玩家获胜，如果满足条件则结束游戏
        protected virtual void CheckForWinner()
        {
            int count_alive = 0;
            Player alive = null;
            foreach (Player player in runtime.Game.players)
            {
                if (!player.IsDead())
                {
                    alive = player; // 记录存活玩家
                    count_alive++;
                }
            }

            if (count_alive == 0)
            {
                EndGame(-1); // 所有人死亡，判定为平局
            }
            else if (count_alive == 1)
            {
                EndGame(alive.player_id); // 剩下唯一存活玩家，获胜
            }
        }

        // 清理回合数据
        protected virtual void ClearTurnData()
        {
            runtime.Game.selector = SelectorType.None;     // 重置选择器
            runtime.ResolveQueue.Clear();                       // 清空解析队列
            runtime.CardTargets.Clear();                          // 清理临时卡牌列表
            runtime.PlayerTargets.Clear();                        // 清理临时玩家列表
            runtime.SlotTargets.Clear();                          // 清理临时槽位列表
            runtime.CardDataTargets.Clear();                     // 清理临时卡牌数据列表
            runtime.Game.last_played = null;               // 重置最后出牌记录
            runtime.Game.last_destroyed = null;            // 重置最后销毁记录
            runtime.Game.last_target = null;               // 重置最后目标记录
            runtime.Game.last_summoned = null;             // 重置最后召唤记录
            runtime.Game.ability_triggerer = null;         // 重置能力触发者
            runtime.Game.selected_value = 0;               // 重置选择数值
            runtime.Game.ability_played.Clear();           // 清空已触发能力列表
            runtime.Game.cards_attacked.Clear();           // 清空已攻击卡牌列表
        }

        // 设置玩家卡组（资源中的卡组）
        public virtual void SetPlayerDeck(Player player, DeckData deck)
        {
            player.cards_all.Clear();                   // 清空所有卡牌
            player.cards_deck.Clear();                  // 清空牌库
            player.deck = deck.id;                      // 设置卡组ID
            player.hero = null;                         // 重置英雄

            VariantData variant = VariantData.GetDefault();
            if (deck.hero != null)
            {
                player.hero = Card.Create(deck.hero, variant, player); // 创建英雄卡
            }

            foreach (CardData card in deck.cards)
            {
                if (card != null)
                {
                    Card acard = Card.Create(card, variant, player); // 创建卡牌
                    player.cards_deck.Add(acard);                    // 添加到牌库
                }
            }

            DeckPuzzleData puzzle = deck as DeckPuzzleData;

            // 设置场上卡牌
            if (puzzle != null)
            {
                foreach (DeckCardSlot card in puzzle.board_cards)
                {
                    Card acard = Card.Create(card.card, variant, player); // 创建场上卡牌
                    acard.slot = new Slot(card.slot, player.player_id); // 设置卡牌槽位
                    player.cards_board.Add(acard); // 添加到场上
                }
            }

            // 洗牌
            if (puzzle == null || !puzzle.dont_shuffle_deck)
                ShuffleDeck(player.cards_deck); // 洗牌
        }

        // 设置玩家卡组（存档或数据库中的自定义卡组）
        public virtual void SetPlayerDeck(Player player, UserDeckData deck)
        {
            player.cards_all.Clear();                   // 清空所有卡牌
            player.cards_deck.Clear();                  // 清空牌库
            player.deck = deck.tid;                     // 设置卡组ID
            player.hero = null;                         // 重置英雄

            if (deck.hero != null)
            {
                CardData hdata = CardData.Get(deck.hero.tid);
                VariantData hvariant = VariantData.Get(deck.hero.variant);
                if (hdata != null && hvariant != null)
                    player.hero = Card.Create(hdata, hvariant, player); // 创建英雄卡
            }

            foreach (UserCardData card in deck.cards)
            {
                CardData icard = CardData.Get(card.tid);
                VariantData variant = VariantData.Get(card.variant);
                if (icard != null && variant != null)
                {
                    for (int i = 0; i < card.quantity; i++)
                    {
                        Card acard = Card.Create(icard, variant, player); // 创建卡牌
                        player.cards_deck.Add(acard);                      // 添加到牌库
                    }
                }
            }

            // 洗牌
            ShuffleDeck(player.cards_deck); // 洗牌
        }

        // 出牌操作
        public virtual void PlayCard(Card card, Slot slot, bool skip_cost = false)
        {
            if (!runtime.Game.CanPlayCard(card, slot, skip_cost)) return;

            Player player = runtime.Game.GetPlayer(card.player_id);

            // 扣除法力
            if (!skip_cost)
                player.PayMana(card);

            // 放置到目标位置
            CardData icard = card.CardData;
            if (icard.IsBoardCard())
            {
                runtime.CardZoneService.MoveToBoard(player, card, slot);
                card.exhausted = true;        // 本回合不能攻击
            }
            else if (icard.IsEquipment())
            {
                Card bearer = runtime.Game.GetSlotCard(slot);
                EquipCard(bearer, card);      // 装备卡牌
                card.exhausted = true;
            }
            else if (icard.IsSecret())
            {
                runtime.CardZoneService.MoveTo(player, card, CardZone.Secret);
            }
            else
            {
                runtime.CardZoneService.MoveTo(player, card, CardZone.Discard);
                card.slot = slot;               // 保存槽位信息
            }

            // 历史记录
            if (!runtime.IsAiPredict && !icard.IsSecret())
                player.AddHistory(GameAction.PlayCard, card);

            // 更新持续效果
            runtime.Game.last_played = card.uid;
            UpdateOngoings();

            // 触发能力
            if (card.CardData.IsDynamicManaCost())
            {
                GoToSelectorCost(card); // 如果是X费牌，进入选择费用阶段
            }
            else
            {
                TriggerSecrets(AbilityTrigger.OnPlayOther, card); // 出牌后触发其他秘密
                TriggerCardAbilityType(AbilityTrigger.OnPlay, card); // 出牌触发自身能力
                TriggerOtherCardsAbilityType(AbilityTrigger.OnPlayOther, card); // 出牌触发其他卡牌能力
            }

            RefreshData();                  // 刷新游戏状态
            onCardPlayed?.Invoke(card, slot);// 触发出牌事件
            runtime.ResolveQueue.ResolveAll(0.3f);  // 解析队列
        }

        // 移动卡牌操作
        public virtual void MoveCard(Card card, Slot slot, bool skip_cost = false)
        {
            if (runtime.Game.CanMoveCard(card, slot, skip_cost))
            {
                card.slot = slot; // 更新卡牌槽位

                // 移动不会影响其他效果，可无限移动

                // 同时移动装备
                Card equip = runtime.Game.GetEquipCard(card.equipped_uid);
                if (equip != null)
                    equip.slot = slot;

                UpdateOngoings();               // 更新持续效果
                RefreshData();                 // 刷新状态

                onCardMoved?.Invoke(card, slot); // 触发移动事件
                runtime.ResolveQueue.ResolveAll(0.2f);  // 解析队列
            }
        }


        // 施放卡牌能力
        public virtual void CastAbility(Card card, AbilityData iability)
        {
            if (runtime.Game.CanCastAbility(card, iability))
            {
                Player player = runtime.Game.GetPlayer(card.player_id);
                if (!runtime.IsAiPredict && iability.target != AbilityTarget.SelectTarget)
                    player.AddHistory(GameAction.CastAbility, card, iability); // 添加历史记录
                card.RemoveStatus(StatusType.Stealth); // 移除潜行状态
                TriggerCardAbility(iability, card);    // 触发能力
                runtime.ResolveQueue.ResolveAll();            // 解析队列
            }
        }

        // 攻击目标卡牌
        public virtual void AttackTarget(Card attacker, Card target, bool skip_cost = false)
        {
            if (runtime.Game.CanAttackTarget(attacker, target, skip_cost))
            {
                Player player = runtime.Game.GetPlayer(attacker.player_id);
                if (!runtime.IsAiPredict)
                    player.AddHistory(GameAction.Attack, attacker, target); // 添加历史记录

                runtime.Game.last_target = target.uid; // 记录最后攻击目标

                // 攻击前触发能力
                TriggerCardAbilityType(AbilityTrigger.OnBeforeAttack, attacker, target);
                TriggerCardAbilityType(AbilityTrigger.OnBeforeDefend, target, attacker);
                TriggerSecrets(AbilityTrigger.OnBeforeAttack, attacker);
                TriggerSecrets(AbilityTrigger.OnBeforeDefend, target);

                // 添加攻击解析队列
                runtime.ResolveQueue.AddAttack(attacker, target, ResolveAttack, skip_cost);
                runtime.ResolveQueue.ResolveAll();
            }
        }

        // 解析攻击动作
        protected virtual void ResolveAttack(Card attacker, Card target, bool skip_cost)
        {
            if (!runtime.Game.IsOnBoard(attacker) || !runtime.Game.IsOnBoard(target))
                return;

            onAttackStart?.Invoke(attacker, target); // 触发攻击开始事件

            attacker.RemoveStatus(StatusType.Stealth); // 移除潜行状态
            UpdateOngoings();                           // 更新持续效果

            runtime.ResolveQueue.AddAttack(attacker, target, ResolveAttackHit, skip_cost); // 添加伤害解析
            runtime.ResolveQueue.ResolveAll(0.3f);
        }

        // 解析攻击命中
        protected virtual void ResolveAttackHit(Card attacker, Card target, bool skip_cost)
        {
            // 计算攻击力
            int datt1 = attacker.GetAttack();
            int datt2 = target.GetAttack();

            // 伤害卡牌
            DamageCard(attacker, target, datt1, DamageType.Combat);

            // 反击伤害
            if (!attacker.HasStatus(StatusType.Intimidate))
                DamageCard(target, attacker, datt2, DamageType.Combat);

            // 保存攻击并疲劳
            if (!skip_cost)
                ExhaustBattle(attacker);

            // 更新加成
            UpdateOngoings();

            // 触发攻击后的能力
            bool att_board = runtime.Game.IsOnBoard(attacker);
            bool def_board = runtime.Game.IsOnBoard(target);
            if (att_board)
                TriggerCardAbilityType(AbilityTrigger.OnAfterAttack, attacker, target);
            if (def_board)
                TriggerCardAbilityType(AbilityTrigger.OnAfterDefend, target, attacker);
            if (att_board)
                TriggerSecrets(AbilityTrigger.OnAfterAttack, attacker);
            if (def_board)
                TriggerSecrets(AbilityTrigger.OnAfterDefend, target);

            onAttackEnd?.Invoke(attacker, target); // 触发攻击结束事件
            RefreshData();                         // 刷新状态
            CheckForWinner();                      // 检查胜利条件

            runtime.ResolveQueue.ResolveAll(0.2f);
        }

        // 攻击玩家
        public virtual void AttackPlayer(Card attacker, Player target, bool skip_cost = false)
        {
            if (attacker == null || target == null)
                return;

            if (!runtime.Game.CanAttackTarget(attacker, target, skip_cost))
                return;

            Player player = runtime.Game.GetPlayer(attacker.player_id);
            if (!runtime.IsAiPredict)
                player.AddHistory(GameAction.AttackPlayer, attacker, target); // 添加历史记录

            // 攻击前触发能力
            TriggerSecrets(AbilityTrigger.OnBeforeAttack, attacker);
            TriggerCardAbilityType(AbilityTrigger.OnBeforeAttack, attacker, target);

            // 添加攻击解析队列
            runtime.ResolveQueue.AddAttack(attacker, target, ResolveAttackPlayer, skip_cost);
            runtime.ResolveQueue.ResolveAll();
        }

        // 解析攻击玩家动作
        protected virtual void ResolveAttackPlayer(Card attacker, Player target, bool skip_cost)
        {
            if (!runtime.Game.IsOnBoard(attacker))
                return;

            onAttackPlayerStart?.Invoke(attacker, target); // 触发攻击玩家开始事件

            attacker.RemoveStatus(StatusType.Stealth); // 移除潜行状态
            UpdateOngoings();                           // 更新持续效果

            runtime.ResolveQueue.AddAttack(attacker, target, ResolveAttackPlayerHit, skip_cost); // 添加伤害解析
            runtime.ResolveQueue.ResolveAll(0.3f);
        }

        // 攻击玩家命中
        protected virtual void ResolveAttackPlayerHit(Card attacker, Player target, bool skip_cost)
        {
            DamagePlayer(attacker, target, attacker.GetAttack(), DamageType.Combat); // 对玩家造成伤害

            // 保存攻击并疲劳
            if (!skip_cost)
                ExhaustBattle(attacker);

            // 更新加成
            UpdateOngoings();

            if (runtime.Game.IsOnBoard(attacker))
                TriggerCardAbilityType(AbilityTrigger.OnAfterAttack, attacker, target);

            TriggerSecrets(AbilityTrigger.OnAfterAttack, attacker); // 触发秘密效果

            onAttackPlayerEnd?.Invoke(attacker, target); // 触发攻击玩家结束事件
            RefreshData();                               // 刷新状态
            CheckForWinner();                            // 检查胜利条件

            runtime.ResolveQueue.ResolveAll(0.2f);
        }

        // 战斗后疲劳
        public virtual void ExhaustBattle(Card attacker)
        {
            bool attacked_before = runtime.Game.cards_attacked.Contains(attacker.uid);
            runtime.Game.cards_attacked.Add(attacker.uid); // 添加到已攻击列表
            bool attack_again = attacker.HasStatus(StatusType.Fury) && !attacked_before;
            attacker.exhausted = !attack_again;         // 设置疲劳状态
        }

        // 重定向攻击目标（卡牌）
        public virtual void RedirectAttack(Card attacker, Card new_target)
        {
            foreach (AttackQueueElement att in runtime.ResolveQueue.GetAttackQueue())
            {
                if (att.attacker.uid == attacker.uid)
                {
                    att.target = new_target;
                    att.ptarget = null;
                    att.callback = ResolveAttack;
                    att.pcallback = null;
                }
            }
        }

        // 重定向攻击目标（玩家）
        public virtual void RedirectAttack(Card attacker, Player new_target)
        {
            foreach (AttackQueueElement att in runtime.ResolveQueue.GetAttackQueue())
            {
                if (att.attacker.uid == attacker.uid)
                {
                    att.ptarget = new_target;
                    att.target = null;
                    att.pcallback = ResolveAttackPlayer;
                    att.callback = null;
                }
            }
        }

        // 洗牌
        public virtual void ShuffleDeck(List<Card> cards)
        {
            runtime.CardSystem.ShuffleDeck(cards, runtime.Random);
        }

        public virtual void DrawCards(Player player, int count = 1)
        {
            int drawn = runtime.CardSystem.DrawCards(player, count);
            onCardDrawn?.Invoke(drawn);
        }

        public virtual void DiscardCardsFromHand(Player player, int count = 1)
        {
            runtime.CardSystem.DiscardCardsFromHand(player, count);
        }

        // 召唤一张新卡牌到场上
        public virtual Card SummonCard(Player player, CardData card, VariantData variant, Slot slot)
        {
            if (!slot.IsBoardSlot() || runtime.Game.HasCardOnSlot(slot))    return null;

            Card acard = SummonCardHand(player, card, variant);
            PlayCard(acard, slot, true); // 放置到场上，不消耗费用

            onCardSummoned?.Invoke(acard, slot); // 触发召唤事件

            return acard;
        }

        // 创建一张新卡牌并放入手牌
        public virtual Card SummonCardHand(Player player, CardData card, VariantData variant)
        {
            return runtime.CardSystem.CreateInHand(player, card, variant);
        }

        // 将卡牌变形为另一张卡牌
        public virtual Card TransformCard(Card card, CardData transform_to)
        {
            card.SetCard(transform_to, card.VariantData);

            onCardTransformed?.Invoke(card); // 触发卡牌变形事件

            return card;
        }

        public virtual void EquipCard(Card bearer, Card equipment)
        {
            Card oldEquipment = runtime.CardSystem.Equip(bearer, equipment);

            if (oldEquipment != null)
            {
                DiscardCard(oldEquipment);
            }
        }

        // 卸下卡牌上的所有装备
        public virtual void UnequipAll(Card bearer)
        {
            Card equipment = runtime.CardSystem.Unequip(bearer);
            if (equipment != null)
            {
                DiscardCard(equipment); // 卸下装备并丢弃
            }
        }

        // 改变卡牌所有者
        public virtual void ChangeOwner(Card card, Player owner)
        {
            runtime.CardSystem.ChangeOwner(card, owner);
        }

        public virtual void DamagePlayer(Card attacker, Player target, int value, DamageType damageType)
        {
            if (attacker == null || target == null || value <= 0) return;
            DamageResult result = runtime.HealthSystem.DamagePlayer(target, value);
            if (!result.resolved) return;

            // 吸血效果
            if (damageType == DamageType.Combat && attacker.HasStatus(StatusType.LifeSteal))
            {
                Player aplayer = runtime.Game.GetPlayer(attacker.player_id);
                HealPlayer(aplayer, result.effectiveDamage);
            }

            onPlayerDamaged?.Invoke(target, result.finalDamage); // 触发玩家受伤事件
        }

        public virtual void DamagePlayer(Player target, int value, DamageType damageType)
        {
            if (target == null || value <= 0) return;
            DamageResult result = runtime.HealthSystem.DamagePlayer(target, value);
            if (!result.resolved) return;

            onPlayerDamaged?.Invoke(target, result.finalDamage); // 触发玩家受伤事件
        }

        public virtual void HealPlayer(Player target, int value)
        {
            HealResult result = runtime.HealthSystem.HealPlayer(target, value);
            if (!result.resolved) return;

            onPlayerHealed?.Invoke(target, result.finalValue); // 触发玩家治疗事件
        }

        public virtual void HealCard(Card target, int value)
        {
            HealResult result = runtime.HealthSystem.HealCard(target, value);
            if (!result.resolved) return;

            onCardHealed?.Invoke(target, result.finalValue); // 触发卡牌治疗事件
        }

        public virtual void DamageCard(Card attacker, Card target, int value, DamageType damageType)
        {
            if (attacker == null || target == null || value <= 0) return;
            DamageResult result = runtime.HealthSystem.DamageCard(target, value, damageType);
            if (!result.resolved) return;
            
            bool isCombat = damageType == DamageType.Combat;
            if (result.finalDamage > 0)
            {
                // 造成伤害后移除沉睡状态
                if (damageType != DamageType.Status)
                {
                    target.RemoveStatus(StatusType.Sleep);
                }

                // 踩踏效果
                Player tplayer = runtime.Game.GetPlayer(target.player_id);
                if (isCombat && result.excessDamage > 0 && attacker.HasStatus(StatusType.Trample))
                {
                    DamagePlayer(attacker, tplayer, result.excessDamage, DamageType.Combat);
                }

                // 吸血效果
                if (isCombat && attacker.HasStatus(StatusType.LifeSteal))
                {
                    Player player = runtime.Game.GetPlayer(attacker.player_id);
                    HealPlayer(player, result.effectiveDamage);
                }
            }

            // 触发回调
            onCardDamaged?.Invoke(target, result.finalDamage);

            if (target.GetHP() <= 0)
            {
                KillCard(attacker, target);
            }   // 死亡之触
            else if (result.effectiveDamage > 0
                && isCombat
                && attacker.HasStatus(StatusType.Deathtouch)
                && target.CardData.type == CardType.Character)
            {
                KillCard(attacker, target);
            }
        }
        
        public virtual void DamageCard(Card target, int value, DamageType damageType)
        {
            if (target == null || value <= 0) return;
            DamageResult result = runtime.HealthSystem.DamageCard(target, value, damageType);
            if (!result.resolved) return;
            
            // 造成伤害后移除沉睡状态
            if (result.finalDamage > 0 && damageType != DamageType.Status)
            {
                target.RemoveStatus(StatusType.Sleep);
            }

            // 触发回调
            onCardDamaged?.Invoke(target, result.finalDamage);

            if (target.GetHP() <= 0)
            {
                DiscardCard(target);
            }
        }

        // 一张卡牌击杀另一张卡牌
        public virtual void KillCard(Card attacker, Card target)
        {
            if (attacker == null || target == null)
                return;

            if (!runtime.Game.IsOnBoard(target) && !runtime.Game.IsEquipped(target))
                return; // 已经被击杀

            if (target.HasStatus(StatusType.Invincibility))
                return; // 无法被击杀

            Player pattacker = runtime.Game.GetPlayer(attacker.player_id);
            if (attacker.player_id != target.player_id)
                pattacker.kill_count++; // 增加击杀计数

            DiscardCard(target); // 丢弃卡牌

            TriggerCardAbilityType(AbilityTrigger.OnKill, attacker, target); // 触发击杀能力
        }

        // 将卡牌丢入弃牌堆
        public virtual void DiscardCard(Card card)
        {
            if (card == null || runtime.Game.IsInDiscard(card)) return;

            Player player = runtime.Game.GetPlayer(card.player_id);
            bool was_on_board = runtime.Game.IsOnBoard(card) || runtime.Game.IsEquipped(card);

            // 卸下装备
            UnequipAll(card);

            // 从场上移除并加入弃牌堆
            runtime.CardZoneService.MoveTo(player, card, CardZone.Discard);
            runtime.Game.last_destroyed = card.uid;

            // 移除持有者关联
            Card bearer = player.GetBearerCard(card);
            if (bearer != null)
                bearer.equipped_uid = null;

            if (was_on_board)
            {
                // 触发死亡能力
                TriggerCardAbilityType(AbilityTrigger.OnDeath, card);
                TriggerOtherCardsAbilityType(AbilityTrigger.OnDeathOther, card);
                TriggerSecrets(AbilityTrigger.OnDeathOther, card);
                runtime.OngoingSystem.UpdateOngoings();
            }

            runtime.CardsToClear.Add(card); // 在下次 UpdateOngoing 中清理，以处理同时伤害效果
            onCardDiscarded?.Invoke(card); // 触发卡牌丢弃事件
        }


        public int RollRandomValue(int dice)
        {
            return RollRandomValue(1, dice + 1);
        }

        public virtual int RollRandomValue(int min, int max)
        {
            runtime.Game.rolled_value = runtime.Random.Next(min, max); // 生成随机值
            onRollValue?.Invoke(runtime.Game.rolled_value);    // 触发掷骰事件
            runtime.ResolveQueue.SetDelay(1f);                     // 设置延迟
            return runtime.Game.rolled_value;
        }

        //--- 能力相关 ---

        // 触发卡牌指定类型的能力
        public virtual void TriggerCardAbilityType(AbilityTrigger type, Card caster, Card triggerer = null)
        {
            foreach (AbilityData iability in caster.GetAbilities())
            {
                if (iability && iability.trigger == type)
                {
                    TriggerCardAbility(iability, caster, triggerer);
                }
            }

            Card equipped = runtime.Game.GetEquipCard(caster.equipped_uid);
            if (equipped != null)
                TriggerCardAbilityType(type, equipped, triggerer); // 装备卡牌也触发能力
        }

        public virtual void TriggerCardAbilityType(AbilityTrigger type, Card caster, Player triggerer)
        {
            foreach (AbilityData iability in caster.GetAbilities())
            {
                if (iability && iability.trigger == type)
                {
                    TriggerCardAbility(iability, caster, triggerer);
                }
            }

            Card equipped = runtime.Game.GetEquipCard(caster.equipped_uid);
            if (equipped != null)
                TriggerCardAbilityType(type, equipped, triggerer); // 装备卡牌也触发能力
        }

        // 触发其他玩家的卡牌能力
        public virtual void TriggerOtherCardsAbilityType(AbilityTrigger type, Card triggerer)
        {
            foreach (Player oplayer in runtime.Game.players)
            {
                if (oplayer.hero != null)
                    TriggerCardAbilityType(type, oplayer.hero, triggerer);

                foreach (Card card in oplayer.cards_board)
                    TriggerCardAbilityType(type, card, triggerer);
            }
        }

        // 触发指定玩家的卡牌能力
        public virtual void TriggerPlayerCardsAbilityType(Player player, AbilityTrigger type)
        {
            if (player.hero != null)
                TriggerCardAbilityType(type, player.hero, player.hero);

            foreach (Card card in player.cards_board)
                TriggerCardAbilityType(type, card, card);
        }

        // 触发卡牌能力（默认触发者为自身）
        public virtual void TriggerCardAbility(AbilityData iability, Card caster)
        {
            TriggerCardAbility(iability, caster, caster);
        }

        // 触发卡牌能力（指定触发者为卡牌）
        public virtual void TriggerCardAbility(AbilityData iability, Card caster, Card triggerer)
        {
            Card trigger_card = triggerer != null ? triggerer : caster; // 如果未指定触发者，默认触发者为施法者
            if (!caster.HasStatus(StatusType.Silenced) && iability.AreTriggerConditionsMet(runtime.Game, caster, trigger_card))
            {
                runtime.ResolveQueue.AddAbility(iability, caster, trigger_card, ResolveCardAbility); // 添加能力到处理队列
            }
        }

        // 触发卡牌能力（指定触发者为玩家）
        public virtual void TriggerCardAbility(AbilityData iability, Card caster, Player triggerer)
        {
            if (!caster.HasStatus(StatusType.Silenced) && iability.AreTriggerConditionsMet(runtime.Game, caster, triggerer))
            {
                runtime.ResolveQueue.AddAbility(iability, caster, caster, ResolveCardAbility); // 添加能力到处理队列
            }
        }

        // 延迟触发能力（默认触发者为自身）
        public virtual void TriggerAbilityDelayed(AbilityData iability, Card caster)
        {
            runtime.ResolveQueue.AddAbility(iability, caster, caster, TriggerCardAbility);
        }

        // 延迟触发能力（指定触发者）
        public virtual void TriggerAbilityDelayed(AbilityData iability, Card caster, Card triggerer)
        {
            Card trigger_card = triggerer != null ? triggerer : caster; // 如果未指定触发者，默认触发者为施法者
            runtime.ResolveQueue.AddAbility(iability, caster, trigger_card, TriggerCardAbility);
        }

        // 解析卡牌能力，可能会等待玩家选择目标
        protected virtual void ResolveCardAbility(AbilityData iability, Card caster, Card triggerer)
        {
            if (!caster.CanDoAbilities())
                return; // 被沉默的卡牌无法施放能力

            //Debug.Log("Trigger Ability " + iability.id + " : " + caster.card_id);

            onAbilityStart?.Invoke(iability, caster); // 触发能力开始事件
            runtime.Game.ability_triggerer = triggerer.uid; 
            runtime.Game.ability_played.Add(iability.id); // 记录已触发的能力

            bool is_selector = ResolveCardAbilitySelector(iability, caster);
            if (is_selector)
                return; // 等待玩家选择

            ResolveCardAbilityPlayTarget(iability, caster); // 处理指定位置目标
            ResolveCardAbilityPlayers(iability, caster);    // 处理玩家目标
            ResolveCardAbilityCards(iability, caster);      // 处理卡牌目标
            ResolveCardAbilitySlots(iability, caster);      // 处理格子目标
            ResolveCardAbilityCardData(iability, caster);   // 处理卡牌数据目标
            ResolveCardAbilityNoTarget(iability, caster);   // 处理无目标能力
            AfterAbilityResolved(iability, caster);         // 能力解析后处理
        }

        // 解析能力是否需要玩家选择
        protected virtual bool ResolveCardAbilitySelector(AbilityData iability, Card caster)
        {
            if (iability.target == AbilityTarget.SelectTarget)
            {
                // 等待玩家选择目标
                GoToSelectTarget(iability, caster);
                return true;
            }
            else if (iability.target == AbilityTarget.CardSelector)
            {
                GoToSelectorCard(iability, caster); // 等待选择卡牌
                return true;
            }
            else if (iability.target == AbilityTarget.ChoiceSelector)
            {
                GoToSelectorChoice(iability, caster); // 等待选择选项
                return true;
            }
            return false;
        }

        // 解析能力的PlayTarget目标
        protected virtual void ResolveCardAbilityPlayTarget(AbilityData iability, Card caster)
        {
            if (iability.target == AbilityTarget.PlayTarget)
            {
                Slot slot = caster.slot;
                Card slot_card = runtime.Game.GetSlotCard(slot);
                if (slot.IsPlayerSlot())
                {
                    Player tplayer = runtime.Game.GetPlayer(slot.p);
                    if (iability.CanTarget(runtime.Game, caster, tplayer))
                        ResolveEffectTarget(iability, caster, tplayer);
                }
                else if (slot_card != null)
                {
                    if (iability.CanTarget(runtime.Game, caster, slot_card))
                    {
                        runtime.Game.last_target = slot_card.uid;
                        ResolveEffectTarget(iability, caster, slot_card);
                    }
                }
                else
                {
                    if (iability.CanTarget(runtime.Game, caster, slot))
                        ResolveEffectTarget(iability, caster, slot);
                }
            }
        }

        // 解析能力的玩家目标
        protected virtual void ResolveCardAbilityPlayers(AbilityData iability, Card caster)
        {
            // 根据条件获取玩家目标
            List<Player> targets = iability.GetPlayerTargets(runtime.Game, caster, runtime.PlayerTargets);

            // 解析效果
            foreach (Player target in targets)
            {
                ResolveEffectTarget(iability, caster, target);
            }
        }

        // 解析能力的卡牌目标
        protected virtual void ResolveCardAbilityCards(AbilityData iability, Card caster)
        {
            // 根据条件获取卡牌目标
            List<Card> targets = iability.GetCardTargets(runtime.Game, caster, runtime.CardTargets);

            // 解析效果
            foreach (Card target in targets)
            {
                ResolveEffectTarget(iability, caster, target);
            }
        }

        // 解析能力的格子目标
        protected virtual void ResolveCardAbilitySlots(AbilityData iability, Card caster)
        {
            // 根据条件获取格子目标
            List<Slot> targets = iability.GetSlotTargets(runtime.Game, caster, runtime.SlotTargets);

            // 解析效果
            foreach (Slot target in targets)
            {
                ResolveEffectTarget(iability, caster, target);
            }
        }


        protected virtual void ResolveCardAbilityCardData(AbilityData iability, Card caster)
        {
            // 根据条件获取卡牌数据目标
            List<CardData> targets = iability.GetCardDataTargets(runtime.Game, caster, runtime.CardDataTargets);

            // 解析效果
            foreach (CardData target in targets)
            {
                ResolveEffectTarget(iability, caster, target);
            }
        }

        protected virtual void ResolveCardAbilityNoTarget(AbilityData iability, Card caster)
        {
            if (iability.target == AbilityTarget.None)
                iability.DoEffects(this, caster); // 无目标能力直接执行效果
        }

        protected virtual void ResolveEffectTarget(AbilityData iability, Card caster, Player target)
        {
            iability.DoEffects(this, caster, target); // 对玩家目标执行效果

            onAbilityTargetPlayer?.Invoke(iability, caster, target); // 触发事件
        }

        protected virtual void ResolveEffectTarget(AbilityData iability, Card caster, Card target)
        {
            iability.DoEffects(this, caster, target); // 对卡牌目标执行效果

            onAbilityTargetCard?.Invoke(iability, caster, target); // 触发事件
        }

        protected virtual void ResolveEffectTarget(AbilityData iability, Card caster, Slot target)
        {
            iability.DoEffects(this, caster, target); // 对格子目标执行效果

            onAbilityTargetSlot?.Invoke(iability, caster, target); // 触发事件
        }

        protected virtual void ResolveEffectTarget(AbilityData iability, Card caster, CardData target)
        {
            iability.DoEffects(this, caster, target); // 对卡牌数据目标执行效果
        }

        protected virtual void AfterAbilityResolved(AbilityData iability, Card caster)
        {
            Player player = runtime.Game.GetPlayer(caster.player_id);

            // 支付消耗
            if (iability.trigger == AbilityTrigger.Activate || iability.trigger == AbilityTrigger.None)
            {
                player.mana -= iability.mana_cost;
                caster.exhausted = caster.exhausted || iability.exhaust;
            }

            // 重新计算状态并清理
            UpdateOngoings();
            CheckForWinner();

            // 链式能力触发
            if (iability.target != AbilityTarget.ChoiceSelector && runtime.Game.state != GameState.GameEnded)
            {
                foreach (AbilityData chain_ability in iability.chain_abilities)
                {
                    if (chain_ability != null)
                    {
                        TriggerCardAbility(chain_ability, caster);
                    }
                }
            }

            onAbilityEnd?.Invoke(iability, caster); // 触发能力结束事件
            runtime.ResolveQueue.ResolveAll(0.5f);         // 解析队列
            RefreshData();                           // 刷新数据
        }
        
        // 该函数经常被调用，用于更新受持续能力影响的状态/属性
        // 基本逻辑是先将加成清零（CleanOngoing），再重新计算以确保持续效果存在
        public virtual void UpdateOngoings()
        {
            runtime.OngoingSystem.UpdateOngoings();
            runtime.CardSystem.CleanupInvalidCards(runtime.CardsToClear);
        }

       //---- 秘密卡相关 ------------

        // 最多触发一张
        public virtual bool TriggerPlayerSecrets(Player player, AbilityTrigger trigger_type)
        {
            for (int i = player.cards_secret.Count - 1; i >= 0; i--)
            {
                Card card = player.cards_secret[i];
                CardData icard = card.CardData;
                if (icard.type != CardType.Secret || card.exhausted) continue;

                if (card.AreAbilityConditionsMet(trigger_type, runtime.Game, card, card))
                {
                    runtime.ResolveQueue.AddSecret(trigger_type, card, card, ResolveSecret); // 添加秘密卡到解析队列
                    runtime.ResolveQueue.SetDelay(0.5f);
                    card.exhausted = true;

                    onSecretTrigger?.Invoke(card, card); // 触发秘密卡事件

                    return true;
                }
            }
            return false;
        }

        // 最多触发一张
        public virtual bool TriggerSecrets(AbilityTrigger trigger_type, Card triggerer)
        {
            // 法术免疫，不触发秘密
            if (triggerer != null && triggerer.HasStatus(StatusType.SpellImmunity)) return false; 

            for (int p = 0; p < runtime.Game.players.Length; p++)
            {
                if (p != runtime.Game.current_player)
                {
                    Player other_player = runtime.Game.players[p];
                    for (int i = other_player.cards_secret.Count - 1; i >= 0; i--)
                    {
                        Card card = other_player.cards_secret[i];
                        CardData icard = card.CardData;
                        if (icard.type == CardType.Secret && !card.exhausted)
                        {
                            Card trigger = triggerer != null ? triggerer : card;
                            if (card.AreAbilityConditionsMet(trigger_type, runtime.Game, card, trigger))
                            {
                                runtime.ResolveQueue.AddSecret(trigger_type, card, trigger, ResolveSecret); // 添加秘密卡到解析队列
                                runtime.ResolveQueue.SetDelay(0.5f);
                                card.exhausted = true;

                                if (onSecretTrigger != null)
                                    onSecretTrigger.Invoke(card, trigger); // 触发秘密卡事件

                                return true; // 每个触发器只触发一个秘密卡
                            }
                        }
                    }
                }
            }
            return false;
        }

        protected virtual void ResolveSecret(AbilityTrigger secret_trigger, Card secret_card, Card trigger)
        {
            CardData icard = secret_card.CardData;
            Player player = runtime.Game.GetPlayer(secret_card.player_id);
            if (icard.type != CardType.Secret) return;

            Player tplayer = runtime.Game.GetPlayer(trigger.player_id);
            if (!runtime.IsAiPredict)
                tplayer.AddHistory(GameAction.SecretTriggered, secret_card, trigger); // 添加触发秘密的历史记录

            TriggerCardAbilityType(secret_trigger, secret_card, trigger); // 触发秘密卡能力
            DiscardCard(secret_card); // 丢弃秘密卡

            onSecretResolve?.Invoke(secret_card, trigger); // 触发秘密卡解析事件
        }

        //---- 选择器解析相关 -----

        public virtual void SelectCard(Card target)
        {
            if (runtime.Game.selector == SelectorType.None)
                return;

            Card caster = runtime.Game.GetCard(runtime.Game.selector_caster_uid);
            AbilityData ability = AbilityData.Get(runtime.Game.selector_ability_id);

            if (caster == null || target == null || ability == null)
                return;

            if (runtime.Game.selector == SelectorType.SelectTarget)
            {
                if (!ability.CanTarget(runtime.Game, caster, target))
                    return; // 不能选择该目标

                Player player = runtime.Game.GetPlayer(caster.player_id);
                if (!runtime.IsAiPredict)
                    player.AddHistory(GameAction.CastAbility, caster, ability, target); // 添加施放能力历史记录

                runtime.Game.selector = SelectorType.None;
                runtime.Game.last_target = target.uid;
                ResolveEffectTarget(ability, caster, target); // 解析目标效果
                AfterAbilityResolved(ability, caster); // 能力解析完成
                runtime.ResolveQueue.ResolveAll();
            }

            if (runtime.Game.selector == SelectorType.SelectorCard)
            {
                if (!ability.IsCardSelectionValid(runtime.Game, caster, target, runtime.CardTargets))
                    return; // 支持条件和过滤器检查

                runtime.Game.selector = SelectorType.None;
                runtime.Game.last_target = target.uid;
                ResolveEffectTarget(ability, caster, target);
                AfterAbilityResolved(ability, caster);
                runtime.ResolveQueue.ResolveAll();
            }
        }

        public virtual void SelectPlayer(Player target)
        {
            if (runtime.Game.selector == SelectorType.None)
                return;

            Card caster = runtime.Game.GetCard(runtime.Game.selector_caster_uid);
            AbilityData ability = AbilityData.Get(runtime.Game.selector_ability_id);

            if (caster == null || target == null || ability == null)
                return;

            if (runtime.Game.selector == SelectorType.SelectTarget)
            {
                if (!ability.CanTarget(runtime.Game, caster, target))
                    return; // 条件不满足

                Player player = runtime.Game.GetPlayer(caster.player_id);
                if (!runtime.IsAiPredict)
                    player.AddHistory(GameAction.CastAbility, caster, ability, target);

                runtime.Game.selector = SelectorType.None;
                ResolveEffectTarget(ability, caster, target);
                AfterAbilityResolved(ability, caster);
                runtime.ResolveQueue.ResolveAll();
            }
        }

        public virtual void SelectSlot(Slot target)
        {
            if (runtime.Game.selector == SelectorType.None)
                return;

            Card caster = runtime.Game.GetCard(runtime.Game.selector_caster_uid);
            AbilityData ability = AbilityData.Get(runtime.Game.selector_ability_id);

            if (caster == null || ability == null || !target.IsBoardSlot())
                return;

            if (runtime.Game.selector == SelectorType.SelectTarget)
            {
                if (!ability.CanTarget(runtime.Game, caster, target))
                    return; // 条件不满足

                Player player = runtime.Game.GetPlayer(caster.player_id);
                if (!runtime.IsAiPredict)
                    player.AddHistory(GameAction.CastAbility, caster, ability, target);

                runtime.Game.selector = SelectorType.None;
                ResolveEffectTarget(ability, caster, target);
                AfterAbilityResolved(ability, caster);
                runtime.ResolveQueue.ResolveAll();
            }
        }

        public virtual void SelectChoice(int choice)
        {
            if (runtime.Game.selector == SelectorType.None)
                return;

            Card caster = runtime.Game.GetCard(runtime.Game.selector_caster_uid);
            AbilityData ability = AbilityData.Get(runtime.Game.selector_ability_id);

            if (caster == null || ability == null || choice < 0)
                return;

            if (runtime.Game.selector == SelectorType.SelectorChoice && ability.target == AbilityTarget.ChoiceSelector)
            {
                if (choice >= 0 && choice < ability.chain_abilities.Length)
                {
                    AbilityData achoice = ability.chain_abilities[choice];
                    if (achoice != null && runtime.Game.CanSelectAbility(caster, achoice))
                    {
                        runtime.Game.selector = SelectorType.None;
                        AfterAbilityResolved(ability, caster);
                        ResolveCardAbility(achoice, caster, caster); // 解析选定的链式能力
                        runtime.ResolveQueue.ResolveAll();
                    }
                }
            }
        }

        public virtual void SelectCost(int select_cost)
        {
            if (runtime.Game.selector == SelectorType.None)
                return;

            Player player = runtime.Game.GetPlayer(runtime.Game.selector_player_id);
            Card caster = runtime.Game.GetCard(runtime.Game.selector_caster_uid);

            if (player == null || caster == null || select_cost < 0)
                return;

            if (runtime.Game.selector == SelectorType.SelectorCost)
            {
                if (select_cost >= 0 && select_cost < 10 && select_cost <= player.mana)
                {
                    runtime.Game.selector = SelectorType.None;
                    runtime.Game.selected_value = select_cost;
                    player.mana -= select_cost;
                    RefreshData();

                    TriggerSecrets(AbilityTrigger.OnPlayOther, caster);
                    TriggerCardAbilityType(AbilityTrigger.OnPlay, caster);
                    TriggerOtherCardsAbilityType(AbilityTrigger.OnPlayOther, caster);
                    runtime.ResolveQueue.ResolveAll();
                }
            }
        }

        public virtual void CancelSelection()
        {
            if (runtime.Game.selector != SelectorType.None)
            {
                // 如果正在选择消耗，退回卡牌到手牌
                if (runtime.Game.selector == SelectorType.SelectorCost)
                    CancelPlayCard();

                // 结束选择
                runtime.Game.selector = SelectorType.None;
                RefreshData();
            }
        }

        public void CancelPlayCard()
        {
            Card card = runtime.Game.GetCard(runtime.Game.selector_caster_uid);
            if (card != null)
            {
                Player player = runtime.Game.GetPlayer(card.player_id);
                if (card.CardData.IsDynamicManaCost())
                    player.mana += runtime.Game.selected_value; // 退回动态法力消耗
                else
                    player.mana += card.CardData.cost; // 退回固定法力消耗

                runtime.CardZoneService.MoveTo(player, card, CardZone.Hand);
                card.Clear(); // 清理卡牌状态
            }
        }


        public virtual void Mulligan(Player player, string[] cards)
        {
            // 如果当前阶段是 Mulligan（重选手牌）且玩家未准备
            if (runtime.Game.phase == GamePhase.Mulligan && !player.ready)
            {
                int count = 0;
                List<Card> remove_list = new List<Card>();

                // 遍历手牌，找到需要重选的卡牌
                foreach (Card card in player.cards_hand)
                {
                    if (cards.Contains(card.uid))
                    {
                        remove_list.Add(card);
                        count++;
                    }
                }

                // 将重选的卡牌移除并放入弃牌堆
                foreach (Card card in remove_list)
                {
                    runtime.CardZoneService.MoveTo(player, card, CardZone.Discard);
                }

                player.ready = true; // 玩家标记为已准备
                DrawCards(player, count); // 抽取等量卡牌
                RefreshData();

                // 如果所有玩家都准备好，开始回合
                if (runtime.Game.AreAllPlayersReady())
                {
                    StartTurn();
                }
            }
        }

        //----- 选择器触发方法 -----

        protected virtual void GoToSelectTarget(AbilityData iability, Card caster)
        {
            runtime.Game.selector = SelectorType.SelectTarget; // 设置选择器类型为目标选择
            runtime.Game.selector_player_id = caster.player_id; // 设置选择器玩家
            runtime.Game.selector_ability_id = iability.id; // 设置能力 ID
            runtime.Game.selector_caster_uid = caster.uid; // 设置施法者 UID
            RefreshData();
        }

        protected virtual void GoToSelectorCard(AbilityData iability, Card caster)
        {
            runtime.Game.selector = SelectorType.SelectorCard; // 设置选择器类型为卡牌选择
            runtime.Game.selector_player_id = caster.player_id;
            runtime.Game.selector_ability_id = iability.id;
            runtime.Game.selector_caster_uid = caster.uid;
            RefreshData();
        }

        protected virtual void GoToSelectorChoice(AbilityData iability, Card caster)
        {
            runtime.Game.selector = SelectorType.SelectorChoice; // 设置选择器类型为选择链式能力
            runtime.Game.selector_player_id = caster.player_id;
            runtime.Game.selector_ability_id = iability.id;
            runtime.Game.selector_caster_uid = caster.uid;
            RefreshData();
        }

        protected virtual void GoToSelectorCost(Card caster)
        {
            runtime.Game.selector = SelectorType.SelectorCost; // 设置选择器类型为选择法力消耗
            runtime.Game.selector_player_id = caster.player_id;
            runtime.Game.selector_ability_id = "";
            runtime.Game.selector_caster_uid = caster.uid;
            runtime.Game.selected_value = 0;
            RefreshData();
        }

        protected virtual void GoToMulligan()
        {
            runtime.Game.phase = GamePhase.Mulligan; // 设置阶段为 Mulligan
            runtime.Game.turn_timer = GameplayData.Get().turn_duration; // 重置回合计时器
            foreach (Player player in runtime.Game.players)
                player.ready = false; // 所有玩家标记为未准备
            RefreshData();
        }

        //------------- 数据刷新与解析队列 -------------

        public virtual void RefreshData()
        {
            onRefresh?.Invoke(); // 触发刷新事件
        }

        public virtual void ClearResolve()
        {
            runtime.ResolveQueue.Clear(); // 清空解析队列
        }

        public virtual bool IsResolving()
        {
            return runtime.ResolveQueue.IsResolving(); // 是否正在解析能力或效果
        }

        public virtual bool IsGameStarted()
        {
            return runtime.Game.HasStarted(); // 游戏是否开始
        }

        public virtual bool IsGameEnded()
        {
            return runtime.Game.HasEnded(); // 游戏是否结束
        }

        public virtual Game GetGameData()
        {
            return runtime.Game; // 获取游戏数据对象
        }

        public System.Random GetRandom()
        {
            return runtime.Random; // 获取随机数生成器
        }

        // 属性访问器
        public Game GameData { get { return runtime.Game; } }
    }
}