using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Profiling;

namespace TcgEngine.Gameplay
{
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

        private Game game_data;                              // 游戏数据引用

        private ResolveQueue resolve_queue;                  // 处理队列
        private bool is_ai_predict = false;                 // 是否用于AI预测

        private System.Random random = new System.Random(); // 随机数生成器

        private ListSwap<Card> card_array = new ListSwap<Card>();         // 临时卡牌列表
        private ListSwap<Player> player_array = new ListSwap<Player>();   // 临时玩家列表
        private ListSwap<Slot> slot_array = new ListSwap<Slot>();         // 临时槽位列表
        private ListSwap<CardData> card_data_array = new ListSwap<CardData>(); // 临时卡牌数据列表
        private List<Card> cards_to_clear = new List<Card>();             // 待清理的卡牌列表

        public GameLogic(bool is_ai)
        {
            // is_instant 忽略所有游戏延迟，立即处理所有操作，用于AI预测
            resolve_queue = new ResolveQueue(null, is_ai);
            is_ai_predict = is_ai;
        }

        public GameLogic(Game game)
        {
            game_data = game;
            resolve_queue = new ResolveQueue(game, false);
        }

        public virtual void SetData(Game game)
        {
            game_data = game;
            resolve_queue.SetData(game);
        }

        public virtual void Update(float delta)
        {
            resolve_queue.Update(delta); // 更新处理队列
        }

        //----- 回合阶段处理 ----------

        public virtual void StartGame()
        {
            if (game_data.state == GameState.GameEnded)
                return;

            // 选择先手玩家
            game_data.state = GameState.Play;
            game_data.first_player = random.NextDouble() < 0.5 ? 0 : 1;
            game_data.current_player = game_data.first_player;
            game_data.turn_count = 1;

            // 副本/冒险模式设置
            bool should_mulligan = GameplayData.Get().mulligan;
            LevelData level = game_data.settings.GetLevel();
            if (level != null)
            {
                if (level != null && level.first_player == LevelFirst.Player)
                    game_data.first_player = 0;
                if (level != null && level.first_player == LevelFirst.AI)
                    game_data.first_player = 1;
                game_data.current_player = game_data.first_player;
                should_mulligan = level.mulligan;
            }

            // 初始化每个玩家
            foreach (Player player in game_data.players)
            {
                // 关卡指定卡组
                DeckPuzzleData pdeck = DeckPuzzleData.Get(player.deck);

                // 生命值/法力值
                player.hp_max = pdeck != null ? pdeck.start_hp : GameplayData.Get().hp_start;
                player.hp = player.hp_max;
                player.mana_max = pdeck != null ? pdeck.start_mana : GameplayData.Get().mana_start;
                player.mana = player.mana_max;

                // 抽取初始手牌
                int dcards = pdeck != null ? pdeck.start_cards : GameplayData.Get().cards_start;
                DrawCard(player, dcards);

                // 给第二位玩家添加额外金币卡
                bool is_random = level == null || level.first_player == LevelFirst.Random;
                if (is_random && player.player_id != game_data.first_player && GameplayData.Get().second_bonus != null)
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
            if (game_data.state == GameState.GameEnded)
                return;

            ClearTurnData();                   // 清理上回合数据
            game_data.phase = GamePhase.StartTurn;
            RefreshData();                     // 刷新游戏状态
            onTurnStart?.Invoke();             // 回合开始事件

            Player player = game_data.GetActivePlayer();

            // 抽牌
            if (game_data.turn_count > 1 || player.player_id != game_data.first_player)
            {
                DrawCard(player, GameplayData.Get().cards_per_turn);
            }

            // 增加法力值
            player.mana_max += GameplayData.Get().mana_per_turn;
            player.mana_max = Mathf.Min(player.mana_max, GameplayData.Get().mana_max);
            player.mana = player.mana_max;

            // 回合计时器和历史清理
            game_data.turn_timer = GameplayData.Get().turn_duration;
            player.history_list.Clear();

            // 中毒处理
            if (player.HasStatus(StatusType.Poisoned))
                player.hp -= player.GetStatusValue(StatusType.Poisoned);

            if (player.hero != null)
                player.hero.Refresh(); // 刷新英雄状态

            // 刷新场上卡牌及状态效果
            for (int i = player.cards_board.Count - 1; i >= 0; i--)
            {
                Card card = player.cards_board[i];

                if (!card.HasStatus(StatusType.Sleep))
                    card.Refresh(); // 刷新卡牌状态

                if (card.HasStatus(StatusType.Poisoned))
                    DamageCard(card, card.GetStatusValue(StatusType.Poisoned)); // 中毒伤害
            }

            // 持续能力更新
            UpdateOngoing();

            // 回合开始触发的能力
            TriggerPlayerCardsAbilityType(player, AbilityTrigger.StartOfTurn);
            TriggerPlayerSecrets(player, AbilityTrigger.StartOfTurn);

            resolve_queue.AddCallback(StartMainPhase);    // 添加主阶段回调
            resolve_queue.ResolveAll(0.2f);               // 立即处理队列
        }

        public virtual void StartNextTurn()
        {
            if (game_data.state == GameState.GameEnded)
                return;

            // 切换到下一位玩家
            game_data.current_player = (game_data.current_player + 1) % game_data.settings.nb_players;

            // 回合计数
            if (game_data.current_player == game_data.first_player)
                game_data.turn_count++;

            CheckForWinner(); // 检查胜利条件
            StartTurn();      // 开始下一回合
        }

        public virtual void StartMainPhase()
        {
            if (game_data.state == GameState.GameEnded)
                return;

            game_data.phase = GamePhase.Main; // 设置为主阶段
            onTurnPlay?.Invoke();             // 回合主阶段事件
            RefreshData();                     // 刷新游戏状态
        }

        public virtual void EndTurn()
        {
            if (game_data.state == GameState.GameEnded)
                return;
            if (game_data.phase != GamePhase.Main)
                return;

            game_data.selector = SelectorType.None;
            game_data.phase = GamePhase.EndTurn; // 设置回合结束阶段

            // 减少状态持续时间
            foreach (Player aplayer in game_data.players)
            {
                aplayer.ReduceStatusDurations();           // 玩家状态减少
                foreach (Card card in aplayer.cards_board)
                    card.ReduceStatusDurations();         // 场上卡牌状态减少
                foreach (Card card in aplayer.cards_equip)
                    card.ReduceStatusDurations();         // 装备卡状态减少
            }

            // 回合结束触发能力
            Player player = game_data.GetActivePlayer();
            TriggerPlayerCardsAbilityType(player, AbilityTrigger.EndOfTurn);

            onTurnEnd?.Invoke();   // 回合结束事件
            RefreshData();         // 刷新状态

            resolve_queue.AddCallback(StartNextTurn); // 添加下一回合回调
            resolve_queue.ResolveAll(0.2f);           // 立即处理
        }

        // 游戏结束并指定获胜玩家
        public virtual void EndGame(int winner)
        {
            if (game_data.state != GameState.GameEnded)
            {
                game_data.state = GameState.GameEnded;
                game_data.phase = GamePhase.None;
                game_data.selector = SelectorType.None;
                game_data.current_player = winner; // 设置获胜玩家
                resolve_queue.Clear();             // 清空处理队列
                Player player = game_data.GetPlayer(winner);
                onGameEnd?.Invoke(player);        // 触发游戏结束事件
                RefreshData();                     // 刷新状态
            }
        }

        // 进入下一步或下一阶段
        public virtual void NextStep()
        {
            if (game_data.state == GameState.GameEnded)
                return;

            if (game_data.phase == GamePhase.Mulligan)
            {
                StartTurn(); // 如果在换牌阶段，直接开始回合
                return;
            }

            CancelSelection(); // 取消当前的选择

            // 添加到解析队列，确保当前操作完成后结束回合
            resolve_queue.AddCallback(EndTurn);
            resolve_queue.ResolveAll(); // 立即处理队列
        }

        // 检查是否有玩家获胜，如果满足条件则结束游戏
        protected virtual void CheckForWinner()
        {
            int count_alive = 0;
            Player alive = null;
            foreach (Player player in game_data.players)
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
            game_data.selector = SelectorType.None;     // 重置选择器
            resolve_queue.Clear();                       // 清空解析队列
            card_array.Clear();                          // 清理临时卡牌列表
            player_array.Clear();                        // 清理临时玩家列表
            slot_array.Clear();                          // 清理临时槽位列表
            card_data_array.Clear();                     // 清理临时卡牌数据列表
            game_data.last_played = null;               // 重置最后出牌记录
            game_data.last_destroyed = null;            // 重置最后销毁记录
            game_data.last_target = null;               // 重置最后目标记录
            game_data.last_summoned = null;             // 重置最后召唤记录
            game_data.ability_triggerer = null;         // 重置能力触发者
            game_data.selected_value = 0;               // 重置选择数值
            game_data.ability_played.Clear();           // 清空已触发能力列表
            game_data.cards_attacked.Clear();           // 清空已攻击卡牌列表
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
                    acard.slot = new Slot(card.slot, Slot.GetP(player.player_id)); // 设置卡牌槽位
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
            if (game_data.CanPlayCard(card, slot, skip_cost))
            {
                Player player = game_data.GetPlayer(card.player_id);

                // 扣除法力
                if (!skip_cost)
                    player.PayMana(card);

                // 从所有区域移除该卡牌
                player.RemoveCardFromAllGroups(card);

                // 放置到目标位置
                CardData icard = card.CardData;
                if (icard.IsBoardCard())
                {
                    player.cards_board.Add(card); // 添加到场上
                    card.slot = slot;             // 设置槽位
                    card.exhausted = true;        // 本回合不能攻击
                }
                else if (icard.IsEquipment())
                {
                    Card bearer = game_data.GetSlotCard(slot);
                    EquipCard(bearer, card);      // 装备卡牌
                    card.exhausted = true;
                }
                else if (icard.IsSecret())
                {
                    player.cards_secret.Add(card); // 添加到秘密区
                }
                else
                {
                    player.cards_discard.Add(card); // 添加到弃牌堆
                    card.slot = slot;               // 保存槽位信息
                }

                // 历史记录
                if (!is_ai_predict && !icard.IsSecret())
                    player.AddHistory(GameAction.PlayCard, card);

                // 更新持续效果
                game_data.last_played = card.uid;
                UpdateOngoing();

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
                resolve_queue.ResolveAll(0.3f);  // 解析队列
            }
        }

        // 移动卡牌操作
        public virtual void MoveCard(Card card, Slot slot, bool skip_cost = false)
        {
            if (game_data.CanMoveCard(card, slot, skip_cost))
            {
                card.slot = slot; // 更新卡牌槽位

                // 移动不会影响其他效果，可无限移动

                // 同时移动装备
                Card equip = game_data.GetEquipCard(card.equipped_uid);
                if (equip != null)
                    equip.slot = slot;

                UpdateOngoing();               // 更新持续效果
                RefreshData();                 // 刷新状态

                onCardMoved?.Invoke(card, slot); // 触发移动事件
                resolve_queue.ResolveAll(0.2f);  // 解析队列
            }
        }


        // 施放卡牌能力
        public virtual void CastAbility(Card card, AbilityData iability)
        {
            if (game_data.CanCastAbility(card, iability))
            {
                Player player = game_data.GetPlayer(card.player_id);
                if (!is_ai_predict && iability.target != AbilityTarget.SelectTarget)
                    player.AddHistory(GameAction.CastAbility, card, iability); // 添加历史记录
                card.RemoveStatus(StatusType.Stealth); // 移除潜行状态
                TriggerCardAbility(iability, card);    // 触发能力
                resolve_queue.ResolveAll();            // 解析队列
            }
        }

        // 攻击目标卡牌
        public virtual void AttackTarget(Card attacker, Card target, bool skip_cost = false)
        {
            if (game_data.CanAttackTarget(attacker, target, skip_cost))
            {
                Player player = game_data.GetPlayer(attacker.player_id);
                if (!is_ai_predict)
                    player.AddHistory(GameAction.Attack, attacker, target); // 添加历史记录

                game_data.last_target = target.uid; // 记录最后攻击目标

                // 攻击前触发能力
                TriggerCardAbilityType(AbilityTrigger.OnBeforeAttack, attacker, target);
                TriggerCardAbilityType(AbilityTrigger.OnBeforeDefend, target, attacker);
                TriggerSecrets(AbilityTrigger.OnBeforeAttack, attacker);
                TriggerSecrets(AbilityTrigger.OnBeforeDefend, target);

                // 添加攻击解析队列
                resolve_queue.AddAttack(attacker, target, ResolveAttack, skip_cost);
                resolve_queue.ResolveAll();
            }
        }

        // 解析攻击动作
        protected virtual void ResolveAttack(Card attacker, Card target, bool skip_cost)
        {
            if (!game_data.IsOnBoard(attacker) || !game_data.IsOnBoard(target))
                return;

            onAttackStart?.Invoke(attacker, target); // 触发攻击开始事件

            attacker.RemoveStatus(StatusType.Stealth); // 移除潜行状态
            UpdateOngoing();                           // 更新持续效果

            resolve_queue.AddAttack(attacker, target, ResolveAttackHit, skip_cost); // 添加伤害解析
            resolve_queue.ResolveAll(0.3f);
        }

        // 解析攻击命中
        protected virtual void ResolveAttackHit(Card attacker, Card target, bool skip_cost)
        {
            // 计算攻击力
            int datt1 = attacker.GetAttack();
            int datt2 = target.GetAttack();

            // 伤害卡牌
            DamageCard(attacker, target, datt1);

            // 反击伤害
            if (!attacker.HasStatus(StatusType.Intimidate))
                DamageCard(target, attacker, datt2);

            // 保存攻击并疲劳
            if (!skip_cost)
                ExhaustBattle(attacker);

            // 更新加成
            UpdateOngoing();

            // 触发攻击后的能力
            bool att_board = game_data.IsOnBoard(attacker);
            bool def_board = game_data.IsOnBoard(target);
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

            resolve_queue.ResolveAll(0.2f);
        }

        // 攻击玩家
        public virtual void AttackPlayer(Card attacker, Player target, bool skip_cost = false)
        {
            if (attacker == null || target == null)
                return;

            if (!game_data.CanAttackTarget(attacker, target, skip_cost))
                return;

            Player player = game_data.GetPlayer(attacker.player_id);
            if (!is_ai_predict)
                player.AddHistory(GameAction.AttackPlayer, attacker, target); // 添加历史记录

            // 攻击前触发能力
            TriggerSecrets(AbilityTrigger.OnBeforeAttack, attacker);
            TriggerCardAbilityType(AbilityTrigger.OnBeforeAttack, attacker, target);

            // 添加攻击解析队列
            resolve_queue.AddAttack(attacker, target, ResolveAttackPlayer, skip_cost);
            resolve_queue.ResolveAll();
        }

        // 解析攻击玩家动作
        protected virtual void ResolveAttackPlayer(Card attacker, Player target, bool skip_cost)
        {
            if (!game_data.IsOnBoard(attacker))
                return;

            onAttackPlayerStart?.Invoke(attacker, target); // 触发攻击玩家开始事件

            attacker.RemoveStatus(StatusType.Stealth); // 移除潜行状态
            UpdateOngoing();                           // 更新持续效果

            resolve_queue.AddAttack(attacker, target, ResolveAttackPlayerHit, skip_cost); // 添加伤害解析
            resolve_queue.ResolveAll(0.3f);
        }

        // 攻击玩家命中
        protected virtual void ResolveAttackPlayerHit(Card attacker, Player target, bool skip_cost)
        {
            DamagePlayer(attacker, target, attacker.GetAttack()); // 对玩家造成伤害

            // 保存攻击并疲劳
            if (!skip_cost)
                ExhaustBattle(attacker);

            // 更新加成
            UpdateOngoing();

            if (game_data.IsOnBoard(attacker))
                TriggerCardAbilityType(AbilityTrigger.OnAfterAttack, attacker, target);

            TriggerSecrets(AbilityTrigger.OnAfterAttack, attacker); // 触发秘密效果

            onAttackPlayerEnd?.Invoke(attacker, target); // 触发攻击玩家结束事件
            RefreshData();                               // 刷新状态
            CheckForWinner();                            // 检查胜利条件

            resolve_queue.ResolveAll(0.2f);
        }

        // 战斗后疲劳
        public virtual void ExhaustBattle(Card attacker)
        {
            bool attacked_before = game_data.cards_attacked.Contains(attacker.uid);
            game_data.cards_attacked.Add(attacker.uid); // 添加到已攻击列表
            bool attack_again = attacker.HasStatus(StatusType.Fury) && !attacked_before;
            attacker.exhausted = !attack_again;         // 设置疲劳状态
        }

        // 重定向攻击目标（卡牌）
        public virtual void RedirectAttack(Card attacker, Card new_target)
        {
            foreach (AttackQueueElement att in resolve_queue.GetAttackQueue())
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
            foreach (AttackQueueElement att in resolve_queue.GetAttackQueue())
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
            for (int i = 0; i < cards.Count; i++)
            {
                Card temp = cards[i];
                int randomIndex = random.Next(i, cards.Count);
                cards[i] = cards[randomIndex];
                cards[randomIndex] = temp;
            }
        }

        // 抽牌
        public virtual void DrawCard(Player player, int nb = 1)
        {
            for (int i = 0; i < nb; i++)
            {
                if (player.cards_deck.Count > 0 && player.cards_hand.Count < GameplayData.Get().cards_max)
                {
                    Card card = player.cards_deck[0];
                    player.cards_deck.RemoveAt(0);
                    player.cards_hand.Add(card);
                }
            }

            onCardDrawn?.Invoke(nb); // 触发抽牌事件
        }

        // 从牌库直接放入弃牌堆
        public virtual void DrawDiscardCard(Player player, int nb = 1)
        {
            for (int i = 0; i < nb; i++)
            {
                if (player.cards_deck.Count > 0)
                {
                    Card card = player.cards_deck[0];
                    player.cards_deck.RemoveAt(0);
                    player.cards_discard.Add(card);
                }
            }
        }

        // 召唤一张卡牌的复制
        public virtual Card SummonCopy(Player player, Card copy, Slot slot)
        {
            CardData icard = copy.CardData;
            return SummonCard(player, icard, copy.VariantData, slot);
        }

        // 召唤一张卡牌的复制到手牌
        public virtual Card SummonCopyHand(Player player, Card copy)
        {
            CardData icard = copy.CardData;
            return SummonCardHand(player, icard, copy.VariantData);
        }

        // 召唤一张新卡牌到场上
        public virtual Card SummonCard(Player player, CardData card, VariantData variant, Slot slot)
        {
            if (!slot.IsValid())
                return null;

            if (game_data.GetSlotCard(slot) != null)
                return null;

            Card acard = SummonCardHand(player, card, variant);
            PlayCard(acard, slot, true); // 放置到场上，不消耗费用

            onCardSummoned?.Invoke(acard, slot); // 触发召唤事件

            return acard;
        }


        // 创建一张新卡牌并放入手牌
        public virtual Card SummonCardHand(Player player, CardData card, VariantData variant)
        {
            Card acard = Card.Create(card, variant, player);
            player.cards_hand.Add(acard);
            game_data.last_summoned = acard.uid; // 记录最后召唤的卡牌
            return acard;
        }

        // 将卡牌变形为另一张卡牌
        public virtual Card TransformCard(Card card, CardData transform_to)
        {
            card.SetCard(transform_to, card.VariantData);

            onCardTransformed?.Invoke(card); // 触发卡牌变形事件

            return card;
        }

        // 装备卡牌
        public virtual void EquipCard(Card card, Card equipment)
        {
            if (card != null && equipment != null && card.player_id == equipment.player_id)
            {
                if (!card.CardData.IsEquipment() && equipment.CardData.IsEquipment())
                {
                    UnequipAll(card); // 卸下之前装备，每次只允许一件装备

                    Player player = game_data.GetPlayer(card.player_id);
                    player.RemoveCardFromAllGroups(equipment);
                    player.cards_equip.Add(equipment);
                    card.equipped_uid = equipment.uid; // 设置装备关联
                    equipment.slot = card.slot;         // 装备位置与卡牌一致
                }
            }
        }

        // 卸下卡牌上的所有装备
        public virtual void UnequipAll(Card card)
        {
            if (card != null && card.equipped_uid != null)
            {
                Player player = game_data.GetPlayer(card.player_id);
                Card equip = player.GetEquipCard(card.equipped_uid);
                if (equip != null)
                {
                    card.equipped_uid = null;
                    DiscardCard(equip); // 卸下装备并丢弃
                }
            }
        }

        // 改变卡牌所有者
        public virtual void ChangeOwner(Card card, Player owner)
        {
            if (card.player_id != owner.player_id)
            {
                Player powner = game_data.GetPlayer(card.player_id);
                powner.RemoveCardFromAllGroups(card);
                powner.cards_all.Remove(card.uid);
                owner.cards_all[card.uid] = card;
                card.player_id = owner.player_id; // 更新卡牌所属玩家
            }
        }

        // 对玩家造成伤害
        public virtual void DamagePlayer(Card attacker, Player target, int value)
        {
            target.hp -= value; // 减少玩家生命值
            target.hp = Mathf.Clamp(target.hp, 0, target.hp_max);

            // 吸血效果
            Player aplayer = game_data.GetPlayer(attacker.player_id);
            if (attacker.HasStatus(StatusType.LifeSteal))
                aplayer.hp += value;

            onPlayerDamaged?.Invoke(target, value); // 触发玩家受伤事件
        }

        // 治疗卡牌
        public virtual void HealCard(Card target, int value)
        {
            if (target == null)
                return;

            if (target.HasStatus(StatusType.Invincibility))
                return; // 无敌状态无法治疗

            target.damage -= value; // 减少伤害值
            target.damage = Mathf.Max(target.damage, 0);

            onCardHealed?.Invoke(target, value); // 触发卡牌治疗事件
        }

        // 治疗玩家
        public virtual void HealPlayer(Player target, int value)
        {
            if (target == null)
                return;

            target.hp += value;
            target.hp = Mathf.Clamp(target.hp, 0, target.hp_max);

            onPlayerHealed?.Invoke(target, value); // 触发玩家治疗事件
        }

        // 通用伤害（非来自其他卡牌）
        public virtual void DamageCard(Card target, int value)
        {
            if (target == null)
                return;

            if (target.HasStatus(StatusType.Invincibility))
                return; // 无敌状态

            if (target.HasStatus(StatusType.SpellImmunity))
                return; // 法术免疫

            target.damage += value;

            onCardDamaged?.Invoke(target, value); // 触发卡牌受伤事件

            if (target.GetHP() <= 0)
                DiscardCard(target); // 卡牌生命值归零则丢弃
        }

        // 由攻击者/施法者对卡牌造成伤害
        public virtual void DamageCard(Card attacker, Card target, int value, bool spell_damage = false)
        {
            if (attacker == null || target == null)
                return;

            if (target.HasStatus(StatusType.Invincibility))
                return; // 无敌状态

            if (target.HasStatus(StatusType.SpellImmunity) && attacker.CardData.type != CardType.Character)
                return; // 非角色卡牌免疫法术

            // 反弹护盾
            bool doublelife = target.HasStatus(StatusType.Shell);
            if (doublelife && value > 0)
            {
                target.RemoveStatus(StatusType.Shell);
                return;
            }

            // 护甲减伤
            if (!spell_damage && target.HasStatus(StatusType.Armor))
                value = Mathf.Max(value - target.GetStatusValue(StatusType.Armor), 0);

            // 伤害
            int damage_max = Mathf.Min(value, target.GetHP());
            int extra = value - target.GetHP();
            target.damage += value;

            // 踩踏效果
            Player tplayer = game_data.GetPlayer(target.player_id);
            if (!spell_damage && extra > 0 && attacker.player_id == game_data.current_player && attacker.HasStatus(StatusType.Trample))
                tplayer.hp -= extra;

            // 吸血效果
            Player player = game_data.GetPlayer(attacker.player_id);
            if (!spell_damage && attacker.HasStatus(StatusType.LifeSteal))
                player.hp += damage_max;

            // 造成伤害后移除沉睡状态
            target.RemoveStatus(StatusType.Sleep);

            // 触发回调
            onCardDamaged?.Invoke(target, value);

            // 死亡之触
            if (value > 0 && attacker.HasStatus(StatusType.Deathtouch) && target.CardData.type == CardType.Character)
                KillCard(attacker, target);

            // 生命值为0则击杀卡牌
            if (target.GetHP() <= 0)
                KillCard(attacker, target);
        }

        // 一张卡牌击杀另一张卡牌
        public virtual void KillCard(Card attacker, Card target)
        {
            if (attacker == null || target == null)
                return;

            if (!game_data.IsOnBoard(target) && !game_data.IsEquipped(target))
                return; // 已经被击杀

            if (target.HasStatus(StatusType.Invincibility))
                return; // 无法被击杀

            Player pattacker = game_data.GetPlayer(attacker.player_id);
            if (attacker.player_id != target.player_id)
                pattacker.kill_count++; // 增加击杀计数

            DiscardCard(target); // 丢弃卡牌

            TriggerCardAbilityType(AbilityTrigger.OnKill, attacker, target); // 触发击杀能力
        }

        // 将卡牌丢入弃牌堆
        public virtual void DiscardCard(Card card)
        {
            if (card == null)
                return;

            if (game_data.IsInDiscard(card))
                return; // 已经在弃牌堆

            CardData icard = card.CardData;
            Player player = game_data.GetPlayer(card.player_id);
            bool was_on_board = game_data.IsOnBoard(card) || game_data.IsEquipped(card);

            // 卸下装备
            UnequipAll(card);

            // 从场上移除并加入弃牌堆
            player.RemoveCardFromAllGroups(card);
            player.cards_discard.Add(card);
            game_data.last_destroyed = card.uid;

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
                UpdateOngoingCards(); // 避免在 UpdateOngoingKills 中递归调用
            }

            cards_to_clear.Add(card); // 在下次 UpdateOngoing 中清理，以处理同时伤害效果
            onCardDiscarded?.Invoke(card); // 触发卡牌丢弃事件
        }


        public int RollRandomValue(int dice)
        {
            return RollRandomValue(1, dice + 1);
        }

        public virtual int RollRandomValue(int min, int max)
        {
            game_data.rolled_value = random.Next(min, max); // 生成随机值
            onRollValue?.Invoke(game_data.rolled_value);    // 触发掷骰事件
            resolve_queue.SetDelay(1f);                     // 设置延迟
            return game_data.rolled_value;
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

            Card equipped = game_data.GetEquipCard(caster.equipped_uid);
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

            Card equipped = game_data.GetEquipCard(caster.equipped_uid);
            if (equipped != null)
                TriggerCardAbilityType(type, equipped, triggerer); // 装备卡牌也触发能力
        }

        // 触发其他玩家的卡牌能力
        public virtual void TriggerOtherCardsAbilityType(AbilityTrigger type, Card triggerer)
        {
            foreach (Player oplayer in game_data.players)
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
            if (!caster.HasStatus(StatusType.Silenced) && iability.AreTriggerConditionsMet(game_data, caster, trigger_card))
            {
                resolve_queue.AddAbility(iability, caster, trigger_card, ResolveCardAbility); // 添加能力到处理队列
            }
        }

        // 触发卡牌能力（指定触发者为玩家）
        public virtual void TriggerCardAbility(AbilityData iability, Card caster, Player triggerer)
        {
            if (!caster.HasStatus(StatusType.Silenced) && iability.AreTriggerConditionsMet(game_data, caster, triggerer))
            {
                resolve_queue.AddAbility(iability, caster, caster, ResolveCardAbility); // 添加能力到处理队列
            }
        }

        // 延迟触发能力（默认触发者为自身）
        public virtual void TriggerAbilityDelayed(AbilityData iability, Card caster)
        {
            resolve_queue.AddAbility(iability, caster, caster, TriggerCardAbility);
        }

        // 延迟触发能力（指定触发者）
        public virtual void TriggerAbilityDelayed(AbilityData iability, Card caster, Card triggerer)
        {
            Card trigger_card = triggerer != null ? triggerer : caster; // 如果未指定触发者，默认触发者为施法者
            resolve_queue.AddAbility(iability, caster, trigger_card, TriggerCardAbility);
        }

        // 解析卡牌能力，可能会等待玩家选择目标
        protected virtual void ResolveCardAbility(AbilityData iability, Card caster, Card triggerer)
        {
            if (!caster.CanDoAbilities())
                return; // 被沉默的卡牌无法施放能力

            //Debug.Log("Trigger Ability " + iability.id + " : " + caster.card_id);

            onAbilityStart?.Invoke(iability, caster); // 触发能力开始事件
            game_data.ability_triggerer = triggerer.uid; 
            game_data.ability_played.Add(iability.id); // 记录已触发的能力

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
                Card slot_card = game_data.GetSlotCard(slot);
                if (slot.IsPlayerSlot())
                {
                    Player tplayer = game_data.GetPlayer(slot.p);
                    if (iability.CanTarget(game_data, caster, tplayer))
                        ResolveEffectTarget(iability, caster, tplayer);
                }
                else if (slot_card != null)
                {
                    if (iability.CanTarget(game_data, caster, slot_card))
                    {
                        game_data.last_target = slot_card.uid;
                        ResolveEffectTarget(iability, caster, slot_card);
                    }
                }
                else
                {
                    if (iability.CanTarget(game_data, caster, slot))
                        ResolveEffectTarget(iability, caster, slot);
                }
            }
        }

        // 解析能力的玩家目标
        protected virtual void ResolveCardAbilityPlayers(AbilityData iability, Card caster)
        {
            // 根据条件获取玩家目标
            List<Player> targets = iability.GetPlayerTargets(game_data, caster, player_array);

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
            List<Card> targets = iability.GetCardTargets(game_data, caster, card_array);

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
            List<Slot> targets = iability.GetSlotTargets(game_data, caster, slot_array);

            // 解析效果
            foreach (Slot target in targets)
            {
                ResolveEffectTarget(iability, caster, target);
            }
        }


        protected virtual void ResolveCardAbilityCardData(AbilityData iability, Card caster)
        {
            // 根据条件获取卡牌数据目标
            List<CardData> targets = iability.GetCardDataTargets(game_data, caster, card_data_array);

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
            Player player = game_data.GetPlayer(caster.player_id);

            // 支付消耗
            if (iability.trigger == AbilityTrigger.Activate || iability.trigger == AbilityTrigger.None)
            {
                player.mana -= iability.mana_cost;
                caster.exhausted = caster.exhausted || iability.exhaust;
            }

            // 重新计算状态并清理
            UpdateOngoing();
            CheckForWinner();

            // 链式能力触发
            if (iability.target != AbilityTarget.ChoiceSelector && game_data.state != GameState.GameEnded)
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
            resolve_queue.ResolveAll(0.5f);         // 解析队列
            RefreshData();                           // 刷新数据
        }

        // 该函数经常被调用，用于更新受持续能力影响的状态/属性
        // 基本逻辑是先将加成清零（CleanOngoing），再重新计算以确保持续效果存在
        // 仅更新手牌和场上卡牌
        public virtual void UpdateOngoing()
        {
            Profiler.BeginSample("Update Ongoing");
            UpdateOngoingCards(); // 更新卡牌状态和属性
            UpdateOngoingKills(); // 杀掉HP为0的卡牌
            Profiler.EndSample();
        }

        protected virtual void UpdateOngoingCards()
        {
            for (int p = 0; p < game_data.players.Length; p++)
            {
                Player player = game_data.players[p];
                player.ClearOngoing(); // 清理玩家持续状态

                for (int c = 0; c < player.cards_board.Count; c++)
                    player.cards_board[c].ClearOngoing(); // 清理场上卡牌状态

                for (int c = 0; c < player.cards_equip.Count; c++)
                    player.cards_equip[c].ClearOngoing(); // 清理装备卡状态

                for (int c = 0; c < player.cards_hand.Count; c++)
                    player.cards_hand[c].ClearOngoing(); // 清理手牌状态
            }

            for (int p = 0; p < game_data.players.Length; p++)
            {
                Player player = game_data.players[p];
                UpdateOngoingAbilities(player, player.hero);  // 更新英雄持续能力

                for (int c = 0; c < player.cards_board.Count; c++)
                {
                    Card card = player.cards_board[c];
                    UpdateOngoingAbilities(player, card); // 更新场上卡牌持续能力
                }

                for (int c = 0; c < player.cards_equip.Count; c++)
                {
                    Card card = player.cards_equip[c];
                    UpdateOngoingAbilities(player, card); // 更新装备卡持续能力
                }
            }

            // 属性加成处理
            for (int p = 0; p < game_data.players.Length; p++)
            {
                Player player = game_data.players[p];
                for (int c = 0; c < player.cards_board.Count; c++)
                {
                    Card card = player.cards_board[c];

                    // 嘲讽效果
                    if (card.HasStatus(StatusType.Protection) && !card.HasStatus(StatusType.Stealth))
                    {
                        player.AddOngoingStatus(StatusType.Protected, 0); // 玩家获得保护状态

                        for (int tc = 0; tc < player.cards_board.Count; tc++)
                        {
                            Card tcard = player.cards_board[tc];
                            if (!tcard.HasStatus(StatusType.Protection) && !tcard.HasStatus(StatusType.Protected))
                            {
                                tcard.AddOngoingStatus(StatusType.Protected, 0); // 其他卡牌获得保护状态
                            }
                        }
                    }

                    // 状态加成
                    foreach (CardStatus status in card.status)
                        AddOngoingStatusBonus(card, status);
                    foreach (CardStatus status in card.ongoing_status)
                        AddOngoingStatusBonus(card, status);
                }

                for (int c = 0; c < player.cards_hand.Count; c++)
                {
                    Card card = player.cards_hand[c];
                    // 状态加成
                    foreach (CardStatus status in card.status)
                        AddOngoingStatusBonus(card, status);
                    foreach (CardStatus status in card.ongoing_status)
                        AddOngoingStatusBonus(card, status);
                }
            }
        }

        protected virtual void UpdateOngoingKills()
        {
            // 杀死HP为0的卡牌
            for (int p = 0; p < game_data.players.Length; p++)
            {
                Player player = game_data.players[p];
                for (int i = player.cards_board.Count - 1; i >= 0; i--)
                {
                    if (i < player.cards_board.Count)
                    {
                        Card card = player.cards_board[i];
                        if (card.GetHP() <= 0)
                            DiscardCard(card);
                    }
                }
                for (int i = player.cards_equip.Count - 1; i >= 0; i--)
                {
                    if (i < player.cards_equip.Count)
                    {
                        Card card = player.cards_equip[i];
                        if (card.GetHP() <= 0)
                            DiscardCard(card);
                        Card bearer = player.GetBearerCard(card);
                        if (bearer == null)
                            DiscardCard(card);
                    }
                }
            }

            // 清理卡牌
            for (int c = 0; c < cards_to_clear.Count; c++)
                cards_to_clear[c].Clear();
            cards_to_clear.Clear();
        }

        protected virtual void UpdateOngoingAbilities(Player player, Card card)
        {
            if (card == null || !card.CanDoAbilities())
                return;

            List<AbilityData> cabilities = card.GetAbilities();
            for (int a = 0; a < cabilities.Count; a++)
            {
                AbilityData ability = cabilities[a];
                if (ability != null && ability.trigger == AbilityTrigger.Ongoing && ability.AreTriggerConditionsMet(game_data, card))
                {
                    if (ability.target == AbilityTarget.Self)
                    {
                        if (ability.AreTargetConditionsMet(game_data, card, card))
                        {
                            ability.DoOngoingEffects(this, card, card); // 对自身执行持续效果
                        }
                    }

                    if (ability.target == AbilityTarget.PlayerSelf)
                    {
                        if (ability.AreTargetConditionsMet(game_data, card, player))
                        {
                            ability.DoOngoingEffects(this, card, player); // 对自身玩家执行持续效果
                        }
                    }

                    if (ability.target == AbilityTarget.AllPlayers || ability.target == AbilityTarget.PlayerOpponent)
                    {
                        for (int tp = 0; tp < game_data.players.Length; tp++)
                        {
                            if (ability.target == AbilityTarget.AllPlayers || tp != player.player_id)
                            {
                                Player oplayer = game_data.players[tp];
                                if (ability.AreTargetConditionsMet(game_data, card, oplayer))
                                {
                                    ability.DoOngoingEffects(this, card, oplayer); // 对目标玩家执行持续效果
                                }
                            }
                        }
                    }

                    if (ability.target == AbilityTarget.EquippedCard)
                    {
                        if (card.CardData.IsEquipment())
                        {
                            // 获取装备的承载者
                            Card target = player.GetBearerCard(card);
                            if (target != null && ability.AreTargetConditionsMet(game_data, card, target))
                            {
                                ability.DoOngoingEffects(this, card, target); // 对承载者执行持续效果
                            }
                        }
                        else if (card.equipped_uid != null)
                        {
                            // 获取被装备的卡牌
                            Card target = game_data.GetCard(card.equipped_uid);
                            if (target != null && ability.AreTargetConditionsMet(game_data, card, target))
                            {
                                ability.DoOngoingEffects(this, card, target); // 对装备卡牌执行持续效果
                            }
                        }
                    }

                    if (ability.target == AbilityTarget.AllCardsAllPiles || ability.target == AbilityTarget.AllCardsHand || ability.target == AbilityTarget.AllCardsBoard)
                    {
                        for (int tp = 0; tp < game_data.players.Length; tp++)
                        {
                            Player tplayer = game_data.players[tp];

                            // 手牌卡牌
                            if (ability.target == AbilityTarget.AllCardsAllPiles || ability.target == AbilityTarget.AllCardsHand)
                            {
                                for (int tc = 0; tc < tplayer.cards_hand.Count; tc++)
                                {
                                    Card tcard = tplayer.cards_hand[tc];
                                    if (ability.AreTargetConditionsMet(game_data, card, tcard))
                                    {
                                        ability.DoOngoingEffects(this, card, tcard);
                                    }
                                }
                            }

                            // 场上卡牌
                            if (ability.target == AbilityTarget.AllCardsAllPiles || ability.target == AbilityTarget.AllCardsBoard)
                            {
                                for (int tc = 0; tc < tplayer.cards_board.Count; tc++)
                                {
                                    Card tcard = tplayer.cards_board[tc];
                                    if (ability.AreTargetConditionsMet(game_data, card, tcard))
                                    {
                                        ability.DoOngoingEffects(this, card, tcard);
                                    }
                                }
                            }

                            // 装备卡牌
                            if (ability.target == AbilityTarget.AllCardsAllPiles)
                            {
                                for (int tc = 0; tc < tplayer.cards_equip.Count; tc++)
                                {
                                    Card tcard = tplayer.cards_equip[tc];
                                    if (ability.AreTargetConditionsMet(game_data, card, tcard))
                                    {
                                        ability.DoOngoingEffects(this, card, tcard);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        protected virtual void AddOngoingStatusBonus(Card card, CardStatus status)
        {
            if (status.type == StatusType.AddAttack)
                card.attack_ongoing += status.value; // 攻击力加成
            if (status.type == StatusType.AddHP)
                card.hp_ongoing += status.value; // 生命值加成
            if (status.type == StatusType.AddManaCost)
                card.mana_ongoing += status.value; // 法力消耗加成
        }


       //---- 秘密卡相关 ------------

        public virtual bool TriggerPlayerSecrets(Player player, AbilityTrigger secret_trigger)
        {
            for (int i = player.cards_secret.Count - 1; i >= 0; i--)
            {
                Card card = player.cards_secret[i];
                CardData icard = card.CardData;
                if (icard.type == CardType.Secret && !card.exhausted)
                {
                    if (card.AreAbilityConditionsMet(secret_trigger, game_data, card, card))
                    {
                        resolve_queue.AddSecret(secret_trigger, card, card, ResolveSecret); // 添加秘密卡到解析队列
                        resolve_queue.SetDelay(0.5f);
                        card.exhausted = true;

                        if (onSecretTrigger != null)
                            onSecretTrigger.Invoke(card, card); // 触发秘密卡事件

                        return true; // 每个触发器只触发一个秘密卡
                    }
                }
            }
            return false;
        }

        public virtual bool TriggerSecrets(AbilityTrigger secret_trigger, Card trigger_card)
        {
            if (trigger_card != null && trigger_card.HasStatus(StatusType.SpellImmunity))
                return false; // 法术免疫，不触发秘密，触发者为触发陷阱的卡牌

            for (int p = 0; p < game_data.players.Length; p++)
            {
                if (p != game_data.current_player)
                {
                    Player other_player = game_data.players[p];
                    for (int i = other_player.cards_secret.Count - 1; i >= 0; i--)
                    {
                        Card card = other_player.cards_secret[i];
                        CardData icard = card.CardData;
                        if (icard.type == CardType.Secret && !card.exhausted)
                        {
                            Card trigger = trigger_card != null ? trigger_card : card;
                            if (card.AreAbilityConditionsMet(secret_trigger, game_data, card, trigger))
                            {
                                resolve_queue.AddSecret(secret_trigger, card, trigger, ResolveSecret); // 添加秘密卡到解析队列
                                resolve_queue.SetDelay(0.5f);
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
            Player player = game_data.GetPlayer(secret_card.player_id);
            if (icard.type == CardType.Secret)
            {
                Player tplayer = game_data.GetPlayer(trigger.player_id);
                if (!is_ai_predict)
                    tplayer.AddHistory(GameAction.SecretTriggered, secret_card, trigger); // 添加触发秘密的历史记录

                TriggerCardAbilityType(secret_trigger, secret_card, trigger); // 触发秘密卡能力
                DiscardCard(secret_card); // 丢弃秘密卡

                if (onSecretResolve != null)
                    onSecretResolve.Invoke(secret_card, trigger); // 触发秘密卡解析事件
            }
        }

        //---- 选择器解析相关 -----

        public virtual void SelectCard(Card target)
        {
            if (game_data.selector == SelectorType.None)
                return;

            Card caster = game_data.GetCard(game_data.selector_caster_uid);
            AbilityData ability = AbilityData.Get(game_data.selector_ability_id);

            if (caster == null || target == null || ability == null)
                return;

            if (game_data.selector == SelectorType.SelectTarget)
            {
                if (!ability.CanTarget(game_data, caster, target))
                    return; // 不能选择该目标

                Player player = game_data.GetPlayer(caster.player_id);
                if (!is_ai_predict)
                    player.AddHistory(GameAction.CastAbility, caster, ability, target); // 添加施放能力历史记录

                game_data.selector = SelectorType.None;
                game_data.last_target = target.uid;
                ResolveEffectTarget(ability, caster, target); // 解析目标效果
                AfterAbilityResolved(ability, caster); // 能力解析完成
                resolve_queue.ResolveAll();
            }

            if (game_data.selector == SelectorType.SelectorCard)
            {
                if (!ability.IsCardSelectionValid(game_data, caster, target, card_array))
                    return; // 支持条件和过滤器检查

                game_data.selector = SelectorType.None;
                game_data.last_target = target.uid;
                ResolveEffectTarget(ability, caster, target);
                AfterAbilityResolved(ability, caster);
                resolve_queue.ResolveAll();
            }
        }

        public virtual void SelectPlayer(Player target)
        {
            if (game_data.selector == SelectorType.None)
                return;

            Card caster = game_data.GetCard(game_data.selector_caster_uid);
            AbilityData ability = AbilityData.Get(game_data.selector_ability_id);

            if (caster == null || target == null || ability == null)
                return;

            if (game_data.selector == SelectorType.SelectTarget)
            {
                if (!ability.CanTarget(game_data, caster, target))
                    return; // 条件不满足

                Player player = game_data.GetPlayer(caster.player_id);
                if (!is_ai_predict)
                    player.AddHistory(GameAction.CastAbility, caster, ability, target);

                game_data.selector = SelectorType.None;
                ResolveEffectTarget(ability, caster, target);
                AfterAbilityResolved(ability, caster);
                resolve_queue.ResolveAll();
            }
        }

        public virtual void SelectSlot(Slot target)
        {
            if (game_data.selector == SelectorType.None)
                return;

            Card caster = game_data.GetCard(game_data.selector_caster_uid);
            AbilityData ability = AbilityData.Get(game_data.selector_ability_id);

            if (caster == null || ability == null || !target.IsValid())
                return;

            if (game_data.selector == SelectorType.SelectTarget)
            {
                if (!ability.CanTarget(game_data, caster, target))
                    return; // 条件不满足

                Player player = game_data.GetPlayer(caster.player_id);
                if (!is_ai_predict)
                    player.AddHistory(GameAction.CastAbility, caster, ability, target);

                game_data.selector = SelectorType.None;
                ResolveEffectTarget(ability, caster, target);
                AfterAbilityResolved(ability, caster);
                resolve_queue.ResolveAll();
            }
        }

        public virtual void SelectChoice(int choice)
        {
            if (game_data.selector == SelectorType.None)
                return;

            Card caster = game_data.GetCard(game_data.selector_caster_uid);
            AbilityData ability = AbilityData.Get(game_data.selector_ability_id);

            if (caster == null || ability == null || choice < 0)
                return;

            if (game_data.selector == SelectorType.SelectorChoice && ability.target == AbilityTarget.ChoiceSelector)
            {
                if (choice >= 0 && choice < ability.chain_abilities.Length)
                {
                    AbilityData achoice = ability.chain_abilities[choice];
                    if (achoice != null && game_data.CanSelectAbility(caster, achoice))
                    {
                        game_data.selector = SelectorType.None;
                        AfterAbilityResolved(ability, caster);
                        ResolveCardAbility(achoice, caster, caster); // 解析选定的链式能力
                        resolve_queue.ResolveAll();
                    }
                }
            }
        }

        public virtual void SelectCost(int select_cost)
        {
            if (game_data.selector == SelectorType.None)
                return;

            Player player = game_data.GetPlayer(game_data.selector_player_id);
            Card caster = game_data.GetCard(game_data.selector_caster_uid);

            if (player == null || caster == null || select_cost < 0)
                return;

            if (game_data.selector == SelectorType.SelectorCost)
            {
                if (select_cost >= 0 && select_cost < 10 && select_cost <= player.mana)
                {
                    game_data.selector = SelectorType.None;
                    game_data.selected_value = select_cost;
                    player.mana -= select_cost;
                    RefreshData();

                    TriggerSecrets(AbilityTrigger.OnPlayOther, caster);
                    TriggerCardAbilityType(AbilityTrigger.OnPlay, caster);
                    TriggerOtherCardsAbilityType(AbilityTrigger.OnPlayOther, caster);
                    resolve_queue.ResolveAll();
                }
            }
        }

        public virtual void CancelSelection()
        {
            if (game_data.selector != SelectorType.None)
            {
                // 如果正在选择消耗，退回卡牌到手牌
                if (game_data.selector == SelectorType.SelectorCost)
                    CancelPlayCard();

                // 结束选择
                game_data.selector = SelectorType.None;
                RefreshData();
            }
        }

        public void CancelPlayCard()
        {
            Card card = game_data.GetCard(game_data.selector_caster_uid);
            if (card != null)
            {
                Player player = game_data.GetPlayer(card.player_id);
                if (card.CardData.IsDynamicManaCost())
                    player.mana += game_data.selected_value; // 退回动态法力消耗
                else
                    player.mana += card.CardData.cost; // 退回固定法力消耗

                player.RemoveCardFromAllGroups(card);
                player.AddCard(player.cards_hand, card); // 将卡牌放回手牌
                card.Clear(); // 清理卡牌状态
            }
        }


        public virtual void Mulligan(Player player, string[] cards)
        {
            // 如果当前阶段是 Mulligan（重选手牌）且玩家未准备
            if (game_data.phase == GamePhase.Mulligan && !player.ready)
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
                    player.RemoveCardFromAllGroups(card);
                    player.cards_discard.Add(card);
                }

                player.ready = true; // 玩家标记为已准备
                DrawCard(player, count); // 抽取等量卡牌
                RefreshData();

                // 如果所有玩家都准备好，开始回合
                if (game_data.AreAllPlayersReady())
                {
                    StartTurn();
                }
            }
        }

        //----- 选择器触发方法 -----

        protected virtual void GoToSelectTarget(AbilityData iability, Card caster)
        {
            game_data.selector = SelectorType.SelectTarget; // 设置选择器类型为目标选择
            game_data.selector_player_id = caster.player_id; // 设置选择器玩家
            game_data.selector_ability_id = iability.id; // 设置能力 ID
            game_data.selector_caster_uid = caster.uid; // 设置施法者 UID
            RefreshData();
        }

        protected virtual void GoToSelectorCard(AbilityData iability, Card caster)
        {
            game_data.selector = SelectorType.SelectorCard; // 设置选择器类型为卡牌选择
            game_data.selector_player_id = caster.player_id;
            game_data.selector_ability_id = iability.id;
            game_data.selector_caster_uid = caster.uid;
            RefreshData();
        }

        protected virtual void GoToSelectorChoice(AbilityData iability, Card caster)
        {
            game_data.selector = SelectorType.SelectorChoice; // 设置选择器类型为选择链式能力
            game_data.selector_player_id = caster.player_id;
            game_data.selector_ability_id = iability.id;
            game_data.selector_caster_uid = caster.uid;
            RefreshData();
        }

        protected virtual void GoToSelectorCost(Card caster)
        {
            game_data.selector = SelectorType.SelectorCost; // 设置选择器类型为选择法力消耗
            game_data.selector_player_id = caster.player_id;
            game_data.selector_ability_id = "";
            game_data.selector_caster_uid = caster.uid;
            game_data.selected_value = 0;
            RefreshData();
        }

        protected virtual void GoToMulligan()
        {
            game_data.phase = GamePhase.Mulligan; // 设置阶段为 Mulligan
            game_data.turn_timer = GameplayData.Get().turn_duration; // 重置回合计时器
            foreach (Player player in game_data.players)
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
            resolve_queue.Clear(); // 清空解析队列
        }

        public virtual bool IsResolving()
        {
            return resolve_queue.IsResolving(); // 是否正在解析能力或效果
        }

        public virtual bool IsGameStarted()
        {
            return game_data.HasStarted(); // 游戏是否开始
        }

        public virtual bool IsGameEnded()
        {
            return game_data.HasEnded(); // 游戏是否结束
        }

        public virtual Game GetGameData()
        {
            return game_data; // 获取游戏数据对象
        }

        public System.Random GetRandom()
        {
            return random; // 获取随机数生成器
        }

        // 属性访问器
        public Game GameData { get { return game_data; } }
        public ResolveQueue ResolveQueue { get { return resolve_queue; } }

    }
}