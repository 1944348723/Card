using System;
using System.Collections.Generic;
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

    /// <summary>
    /// 执行并处理游戏规则和逻辑
    /// </summary>

    public class GameLogic
    {
        public UnityAction onGameStart;
        public UnityAction<Player> onGameEnd;              // 参数为获胜玩家

        public UnityAction onTurnStart;
        public UnityAction onTurnPlay;
        public UnityAction onTurnEnd;

        public UnityAction<Card, Slot> onCardPlayed;
        public UnityAction<Card, Slot> onCardSummoned;
        public UnityAction<Card, Slot> onCardMoved;
        public UnityAction<Card> onCardTransformed;
        public UnityAction<Card> onCardDiscarded;
        public UnityAction<int> onCardDrawn;
        public UnityAction<int> onRollValue;

        public UnityAction<AbilityData, Card> onAbilityStart;
        public UnityAction<AbilityData, Card, Card> onAbilityTargetCard;       // 能力作用于卡牌事件（Ability, 施法者, 目标卡）
        public UnityAction<AbilityData, Card, Player> onAbilityTargetPlayer;   // 能力作用于玩家事件
        public UnityAction<AbilityData, Card, Slot> onAbilityTargetSlot;       // 能力作用于槽位事件
        public UnityAction<AbilityData, Card> onAbilityEnd;

        public UnityAction<Card, Card> onAttackStart;        // 攻击开始事件（攻击者, 防御者）
        public UnityAction<Card, Card> onAttackEnd;
        public UnityAction<Card, Player> onAttackPlayerStart;
        public UnityAction<Card, Player> onAttackPlayerEnd;

        public UnityAction<Card, int> onCardDamaged;
        public UnityAction<Card, int> onCardHealed;
        public UnityAction<Player, int> onPlayerDamaged;
        public UnityAction<Player, int> onPlayerHealed;

        public UnityAction<Card, Card> onSecretTrigger;     // 秘密触发事件（秘密卡, 触发者）
        public UnityAction<Card, Card> onSecretResolve;     // 秘密结算事件（秘密卡, 触发者）

        public UnityAction onRefresh;                       // 刷新事件

        private readonly GameRuntime runtime;

        public GameLogic(Game game, bool isAi = false, System.Random random = null)
        {
            runtime = new GameRuntime(game, isAi, random);
            BindRuntimeEvents();
            SetData(game);
        }

        private void BindRuntimeEvents()
        {
            runtime.Events.GameStarted += () => onGameStart?.Invoke();
            runtime.Events.GameEnded += player => onGameEnd?.Invoke(player);
            runtime.Events.TurnStarted += () => onTurnStart?.Invoke();
            runtime.Events.MainPhaseStarted += () => onTurnPlay?.Invoke();
            runtime.Events.TurnEnded += () => onTurnEnd?.Invoke();
            runtime.Events.CardPlayed += (card, slot) => onCardPlayed?.Invoke(card, slot);
            runtime.Events.CardSummoned += (card, slot) => onCardSummoned?.Invoke(card, slot);
            runtime.Events.CardMoved += (card, slot) => onCardMoved?.Invoke(card, slot);
            runtime.Events.CardTransformed += card => onCardTransformed?.Invoke(card);
            runtime.Events.CardDiscarded += card => onCardDiscarded?.Invoke(card);
            runtime.Events.CardsDrawn += count => onCardDrawn?.Invoke(count);
            runtime.Events.Rolled += value => onRollValue?.Invoke(value);
            runtime.Events.AbilityStarted += (ability, caster) => onAbilityStart?.Invoke(ability, caster);
            runtime.Events.AbilityTargetedCard += (ability, caster, target) => onAbilityTargetCard?.Invoke(ability, caster, target);
            runtime.Events.AbilityTargetedPlayer += (ability, caster, target) => onAbilityTargetPlayer?.Invoke(ability, caster, target);
            runtime.Events.AbilityTargetedSlot += (ability, caster, target) => onAbilityTargetSlot?.Invoke(ability, caster, target);
            runtime.Events.AbilityEnded += (ability, caster) => onAbilityEnd?.Invoke(ability, caster);
            runtime.Events.AttackStarted += (attacker, target) => onAttackStart?.Invoke(attacker, target);
            runtime.Events.AttackEnded += (attacker, target) => onAttackEnd?.Invoke(attacker, target);
            runtime.Events.PlayerAttackStarted += (attacker, target) => onAttackPlayerStart?.Invoke(attacker, target);
            runtime.Events.PlayerAttackEnded += (attacker, target) => onAttackPlayerEnd?.Invoke(attacker, target);
            runtime.Events.CardDamaged += (target, value) => onCardDamaged?.Invoke(target, value);
            runtime.Events.CardHealed += (target, value) => onCardHealed?.Invoke(target, value);
            runtime.Events.PlayerDamaged += (target, value) => onPlayerDamaged?.Invoke(target, value);
            runtime.Events.PlayerHealed += (target, value) => onPlayerHealed?.Invoke(target, value);
            runtime.Events.SecretTriggered += (secret, triggerer) => onSecretTrigger?.Invoke(secret, triggerer);
            runtime.Events.SecretResolved += (secret, triggerer) => onSecretResolve?.Invoke(secret, triggerer);
            runtime.Events.Refreshed += () => onRefresh?.Invoke();
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
            runtime.Flow.StartGame();
        }

        public virtual void StartTurn()
        {
            runtime.Flow.StartTurn();
        }

        public virtual void StartNextTurn()
        {
            runtime.Flow.StartNextTurn();
        }

        public virtual void StartMainPhase()
        {
            runtime.Flow.StartMainPhase();
        }

        public virtual void EndTurn()
        {
            runtime.Flow.EndTurn();
        }

        // 游戏结束并指定获胜玩家
        public virtual void EndGame(int winner)
        {
            runtime.Flow.EndGame(winner);
        }

        // 进入下一步或下一阶段
        public virtual void NextStep()
        {
            runtime.Flow.NextStep();
        }

        // 设置玩家卡组（资源中的卡组）
        public virtual void SetPlayerDeck(Player player, DeckData deck)
        {
            runtime.Decks.SetDeck(player, deck);
        }

        // 设置玩家卡组（存档或数据库中的自定义卡组）
        public virtual void SetPlayerDeck(Player player, UserDeckData deck)
        {
            runtime.Decks.SetDeck(player, deck);
        }

        // 出牌操作
        public virtual void PlayCard(Card card, Slot slot, bool skip_cost = false)
        {
            runtime.Actions.PlayCard(card, slot, skip_cost);
        }

        // 移动卡牌操作
        public virtual void MoveCard(Card card, Slot slot, bool skip_cost = false)
        {
            runtime.Actions.MoveCard(card, slot, skip_cost);
        }


        // 施放卡牌能力
        public virtual void CastAbility(Card card, AbilityData iability)
        {
            runtime.Actions.CastAbility(card, iability);
        }

        // 攻击目标卡牌
        public virtual void AttackTarget(Card attacker, Card target, bool skip_cost = false)
        {
            runtime.Combat.AttackCard(attacker, target, skip_cost);
        }

        // 攻击玩家
        public virtual void AttackPlayer(Card attacker, Player target, bool skip_cost = false)
        {
            runtime.Combat.AttackPlayer(attacker, target, skip_cost);
        }

        // 战斗后疲劳
        public virtual void ExhaustBattle(Card attacker)
        {
            runtime.Combat.Exhaust(attacker);
        }

        // 重定向攻击目标（卡牌）
        public virtual void RedirectAttack(Card attacker, Card new_target)
        {
            runtime.Combat.Redirect(attacker, new_target);
        }

        // 重定向攻击目标（玩家）
        public virtual void RedirectAttack(Card attacker, Player new_target)
        {
            runtime.Combat.Redirect(attacker, new_target);
        }

        // 洗牌
        public virtual void ShuffleDeck(List<Card> cards)
        {
            runtime.Cards.ShuffleDeck(cards, runtime.Random);
        }

        public virtual void DrawCards(Player player, int count = 1)
        {
            int drawn = runtime.Cards.DrawCards(player, count);
            runtime.Events.RaiseCardsDrawn(drawn);
        }

        public virtual void DiscardCardsFromHand(Player player, int count = 1)
        {
            runtime.Cards.DiscardCardsFromHand(player, count);
        }

        // 召唤一张新卡牌到场上
        public virtual Card SummonCard(Player player, CardData card, VariantData variant, Slot slot)
        {
            return runtime.Cards.Summon(player, card, variant, slot);
        }

        // 创建一张新卡牌并放入手牌
        public virtual Card SummonCardHand(Player player, CardData card, VariantData variant)
        {
            return runtime.Cards.CreateInHand(player, card, variant);
        }

        // 将卡牌变形为另一张卡牌
        public virtual Card TransformCard(Card card, CardData transform_to)
        {
            return runtime.Cards.Transform(card, transform_to);
        }

        public virtual void EquipCard(Card bearer, Card equipment)
        {
            runtime.Cards.EquipAndDiscardExisting(bearer, equipment);
        }

        // 卸下卡牌上的所有装备
        public virtual void UnequipAll(Card bearer)
        {
            runtime.Cards.UnequipAndDiscard(bearer);
        }

        // 改变卡牌所有者
        public virtual void ChangeOwner(Card card, Player owner)
        {
            runtime.Cards.ChangeOwner(card, owner);
        }

        /// <summary>
        /// 为效果层提供统一的非战场移区入口。
        /// 不触发死亡、出牌或动画事件；这些行为仍由专用规则流程负责。
        /// </summary>
        public virtual bool MoveCardToZone(Card card, CardZone zone, bool clearCard = false)
        {
            bool moved = runtime.Zones.MoveTo(card, zone);
            if (moved && clearCard)
                card.Clear();
            return moved;
        }

        public virtual void ClearTemporaryCards(Player player)
        {
            runtime.Zones.ClearTemporary(player);
        }

        public virtual void DamagePlayer(Card attacker, Player target, int value, DamageType damageType)
        {
            runtime.Damage.DamagePlayer(attacker, target, value, damageType);
        }

        public virtual void DamagePlayer(Player target, int value, DamageType damageType)
        {
            runtime.Damage.DamagePlayer(target, value, damageType);
        }

        public virtual void HealPlayer(Player target, int value)
        {
            runtime.Damage.HealPlayer(target, value);
        }

        public virtual void HealCard(Card target, int value)
        {
            runtime.Damage.HealCard(target, value);
        }

        public virtual void DamageCard(Card attacker, Card target, int value, DamageType damageType)
        {
            runtime.Damage.DamageCard(attacker, target, value, damageType);
        }
        
        public virtual void DamageCard(Card target, int value, DamageType damageType)
        {
            runtime.Damage.DamageCard(target, value, damageType);
        }

        // 一张卡牌击杀另一张卡牌
        public virtual void KillCard(Card attacker, Card target)
        {
            runtime.Cards.Kill(attacker, target);
        }

        // 将卡牌丢入弃牌堆
        public virtual void DiscardCard(Card card)
        {
            runtime.Cards.Discard(card);
        }


        public int RollRandomValue(int dice)
        {
            return RollRandomValue(1, dice + 1);
        }

        public virtual int RollRandomValue(int min, int max)
        {
            runtime.Game.rolled_value = runtime.Random.Next(min, max); // 生成随机值
            runtime.Events.RaiseRolled(runtime.Game.rolled_value); // 触发掷骰事件
            runtime.ResolveQueue.SetDelay(1f);                     // 设置延迟
            return runtime.Game.rolled_value;
        }

        //--- 能力相关 ---

        // 触发卡牌指定类型的能力
        public virtual void TriggerCardAbilityType(AbilityTrigger type, Card caster, Card triggerer = null)
        {
            runtime.Abilities.TriggerType(type, caster, triggerer);
        }

        public virtual void TriggerCardAbilityType(AbilityTrigger type, Card caster, Player triggerer)
        {
            runtime.Abilities.TriggerType(type, caster, triggerer);
        }

        // 触发其他玩家的卡牌能力
        public virtual void TriggerOtherCardsAbilityType(AbilityTrigger type, Card triggerer)
        {
            runtime.Abilities.TriggerOtherCards(type, triggerer);
        }

        // 触发指定玩家的卡牌能力
        public virtual void TriggerPlayerCardsAbilityType(Player player, AbilityTrigger type)
        {
            runtime.Abilities.TriggerPlayerCards(player, type);
        }

        // 触发卡牌能力（默认触发者为自身）
        public virtual void TriggerCardAbility(AbilityData iability, Card caster)
        {
            runtime.Abilities.Trigger(iability, caster);
        }

        // 触发卡牌能力（指定触发者为卡牌）
        public virtual void TriggerCardAbility(AbilityData iability, Card caster, Card triggerer)
        {
            runtime.Abilities.Trigger(iability, caster, triggerer);
        }

        // 触发卡牌能力（指定触发者为玩家）
        public virtual void TriggerCardAbility(AbilityData iability, Card caster, Player triggerer)
        {
            runtime.Abilities.Trigger(iability, caster, triggerer);
        }

        // 延迟触发能力（默认触发者为自身）
        public virtual void TriggerAbilityDelayed(AbilityData iability, Card caster)
        {
            runtime.Abilities.TriggerDelayed(iability, caster);
        }

        // 延迟触发能力（指定触发者）
        public virtual void TriggerAbilityDelayed(AbilityData iability, Card caster, Card triggerer)
        {
            runtime.Abilities.TriggerDelayed(iability, caster, triggerer);
        }

        // 该函数经常被调用，用于更新受持续能力影响的状态/属性
        // 基本逻辑是先将加成清零（CleanOngoing），再重新计算以确保持续效果存在
        public virtual void UpdateOngoings()
        {
            runtime.UpdateOngoings();
        }

       //---- 秘密卡相关 ------------

        // 最多触发一张
        public virtual bool TriggerPlayerSecrets(Player player, AbilityTrigger trigger_type)
        {
            return runtime.Secrets.TriggerPlayerSecrets(player, trigger_type);
        }

        // 最多触发一张
        public virtual bool TriggerSecrets(AbilityTrigger trigger_type, Card triggerer)
        {
            return runtime.Secrets.TriggerSecrets(trigger_type, triggerer);
        }

        //---- 选择器解析相关 -----

        public virtual void SelectCard(Card target)
        {
            runtime.Selection.SelectCard(target);
        }

        public virtual void SelectPlayer(Player target)
        {
            runtime.Selection.SelectPlayer(target);
        }

        public virtual void SelectSlot(Slot target)
        {
            runtime.Selection.SelectSlot(target);
        }

        public virtual void SelectChoice(int choice)
        {
            runtime.Selection.SelectChoice(choice);
        }

        public virtual void SelectCost(int select_cost)
        {
            runtime.Selection.SelectCost(select_cost);
        }

        public virtual void CancelSelection()
        {
            runtime.Selection.Cancel();
        }

        public void CancelPlayCard()
        {
            runtime.Selection.CancelPlayCard();
        }


        public virtual void Mulligan(Player player, string[] cards)
        {
            runtime.Selection.Mulligan(player, cards);
        }

        internal void BeginMulliganFromEngine()
        {
            runtime.Selection.BeginMulligan();
        }

        //------------- 数据刷新与解析队列 -------------

        public virtual void RefreshData()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            GameStateInvariantValidator.ThrowIfInvalid(runtime.Game);
#endif
            runtime.Events.RaiseRefreshed(); // 触发刷新事件
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
        public GameRules Rules => runtime.Rules;
        public Game GameData { get { return runtime.Game; } }
    }
}
