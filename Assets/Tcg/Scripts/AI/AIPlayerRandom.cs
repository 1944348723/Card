using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine.AI
{
    /// <summary>
    /// 完全随机决策的 AI 玩家
    /// 非常弱的 AI，但对于测试卡牌或游戏逻辑非常有用
    /// </summary>
    public class AIPlayerRandom : AIPlayer
    {
        private const float FirstDecisionDelay = 1f;
        private const float DecisionDelay = 0.5f;
        private const int MaxActionsPerTurn = 8;
        private const double EndTurnChance = 0.15;

        private readonly System.Random rand = new();
        private readonly List<AIAction> legalActions = new();

        private bool isRunning = false;
        private int activeTurn = -1;
        private int turnActionCount = 0;

        /// <summary>
        /// 构造函数
        /// </summary>
        public AIPlayerRandom(GameLogic gameplay, int id, int level)
            : base(gameplay, id, level)
        {
        }

        /// <summary>
        /// 每帧更新 AI
        /// </summary>
        public override void Update()
        {
            if (isRunning || gameplay.IsResolving())
                return;

            if (!CanMulligan() && !CanResolveSelection() && !CanTakeAction())
                return;

            isRunning = true;
            TimeTool.StartCoroutine(RunDecision());
        }

        /// <summary>
        /// 每次只处理一个当前决策，完成后由下一帧根据最新游戏状态继续。
        /// </summary>
        private IEnumerator RunDecision()
        {
            try
            {
                yield return new WaitForSeconds(GetDecisionDelay());

                if (gameplay.IsResolving())
                    yield break;

                if (CanMulligan())
                {
                    SelectMulligan();
                    yield break;
                }

                if (CanResolveSelection())
                {
                    ResolveCurrentSelection();
                    yield break;
                }

                if (CanTakeAction())
                    ExecuteNextTurnAction();
            }
            finally
            {
                isRunning = false;
            }
        }

        private float GetDecisionDelay()
        {
            Game gameData = gameplay.GetGameData();
            bool isFirstTurnAction = CanTakeAction() && gameData.turn_count != activeTurn;
            return isFirstTurnAction ? FirstDecisionDelay : DecisionDelay;
        }

        private void ExecuteNextTurnAction()
        {
            Game gameData = gameplay.GetGameData();
            if (activeTurn != gameData.turn_count)
            {
                activeTurn = gameData.turn_count;
                turnActionCount = 0;
            }

            CollectLegalActions(gameData, legalActions);

            bool reachedActionLimit = turnActionCount >= MaxActionsPerTurn;
            bool randomlyEndTurn = turnActionCount > 0 && rand.NextDouble() < EndTurnChance;
            if (legalActions.Count == 0 || reachedActionLimit || randomlyEndTurn)
            {
                EndTurn();
                return;
            }

            AIAction action = legalActions[rand.Next(0, legalActions.Count)];
            if (TryExecuteAction(action))
                turnActionCount++;
        }

        private void ResolveCurrentSelection()
        {
            Game gameData = gameplay.GetGameData();
            var selected = gameData.selector switch
            {
                SelectorType.SelectTarget => TrySelectTarget(),
                SelectorType.SelectorCard => TrySelectCard(),
                SelectorType.SelectorChoice => TrySelectChoice(),
                SelectorType.SelectorCost => TrySelectCost(),
                _ => false,
            };
            if (!selected && CanResolveSelection())
                CancelSelect();
        }

        // ---------- 普通回合动作 ----------

        private void CollectLegalActions(Game gameData, List<AIAction> actions)
        {
            actions.Clear();

            Player player = gameData.GetPlayer(PlayerId);
            if (player == null || !gameplay.Rules.IsPlayerActionTurn(player))
                return;

            foreach (Card card in player.cards_hand)
                AddPlayCardActions(gameData, player, card, actions);

            foreach (Card card in player.cards_board)
            {
                AddAttackActions(gameData, card, actions);
                AddMoveActions(gameData, player, card, actions);
                AddAbilityActions(gameData, card, actions);
            }

            foreach (Card card in player.cards_equip)
                AddAbilityActions(gameData, card, actions);

            if (player.hero != null)
                AddAbilityActions(gameData, player.hero, actions);
        }

        private void AddPlayCardActions(Game gameData, Player player, Card card, List<AIAction> actions)
        {
            if (card.CardData.IsBoardCard() || card.CardData.IsEquipment())
            {
                foreach (Slot slot in gameData.Board.GetAll(player.player_id))
                {
                    if (gameplay.Rules.CanPlayCard(card, slot))
                        actions.Add(CreateAction(GameAction.PlayCard, card, slot));
                }
                return;
            }

            if (card.CardData.IsRequireTargetSpell())
            {
                foreach (Player target in gameData.players)
                {
                    Slot playerSlot = new(target.player_id);
                    if (gameplay.Rules.CanPlayCard(card, playerSlot))
                        actions.Add(CreateAction(GameAction.PlayCard, card, playerSlot));
                }

                foreach (Slot slot in gameData.Board.GetAll())
                {
                    if (gameplay.Rules.CanPlayCard(card, slot))
                        actions.Add(CreateAction(GameAction.PlayCard, card, slot));
                }
                return;
            }

            if (gameplay.Rules.CanPlayCard(card, Slot.None))
                actions.Add(CreateAction(GameAction.PlayCard, card, Slot.None));
        }

        private void AddAttackActions(Game gameData, Card attacker, List<AIAction> actions)
        {
            foreach (Player targetPlayer in gameData.players)
            {
                if (gameplay.Rules.CanAttackTarget(attacker, targetPlayer))
                {
                    AIAction attackPlayer = CreateAction(GameAction.AttackPlayer, attacker, Slot.None);
                    attackPlayer.target_player_id = targetPlayer.player_id;
                    actions.Add(attackPlayer);
                }

                foreach (Card targetCard in targetPlayer.cards_board)
                {
                    if (gameplay.Rules.CanAttackTarget(attacker, targetCard))
                    {
                        AIAction attackCard = CreateAction(GameAction.Attack, attacker, Slot.None);
                        attackCard.target_uid = targetCard.uid;
                        actions.Add(attackCard);
                    }
                }
            }
        }

        private void AddMoveActions(Game gameData, Player player, Card card, List<AIAction> actions)
        {
            foreach (Slot slot in gameData.Board.GetAll(player.player_id))
            {
                if (gameplay.Rules.CanMoveCard(card, slot))
                    actions.Add(CreateAction(GameAction.Move, card, slot));
            }
        }

        private void AddAbilityActions(Game gameData, Card card, List<AIAction> actions)
        {
            foreach (AbilityData ability in card.GetAbilities())
            {
                if (ability != null
                    && gameplay.Rules.CanCastAbility(card, ability)
                    && ability.HasValidSelectTarget(gameData, card))
                {
                    AIAction action = CreateAction(GameAction.CastAbility, card, Slot.None);
                    action.ability_id = ability.id;
                    actions.Add(action);
                }
            }
        }

        private static AIAction CreateAction(ushort type, Card card, Slot slot)
        {
            return new AIAction(type)
            {
                card_uid = card.uid,
                target_player_id = -1,
                slot = slot,
                value = -1,
                valid = true,
            };
        }

        /// <summary>
        /// 随机选择卡牌作为目标
        /// </summary>
        private bool TrySelectCard()
        {
            if (!CanResolveSelection())
                return false;

            Game gameData = gameplay.GetGameData();
            Player player = gameData.GetPlayer(PlayerId);
            AbilityData ability = AbilityData.Get(gameData.selector_ability_id);
            Card caster = gameData.GetCard(gameData.selector_caster_uid);

            if (player != null && ability != null && caster != null)
            {
                List<Card> targets = ability.GetCardTargets(gameData, caster);
                if (targets.Count > 0)
                {
                    Card card = targets[rand.Next(0, targets.Count)];
                    AIAction action = new(GameAction.SelectCard) { target_uid = card.uid };
                    return TryExecuteAction(action);
                }
            }

            return false;
        }

        /// <summary>
        /// 随机选择目标卡牌（玩家操作时）
        /// </summary>
        private bool TrySelectTarget()
        {
            if (!CanResolveSelection())
                return false;

            // 取到卡牌和能力
            Game gameData = gameplay.GetGameData();
            AbilityData ability = AbilityData.Get(gameData.selector_ability_id);
            Card caster = gameData.GetCard(gameData.selector_caster_uid);
            if (ability == null || caster == null)
                return false;

            // 找出可能的目标
            List<Player> playerTargets = new();
            List<Card> cardTargets = new();
            List<Slot> slotTargets = new();

            foreach (Player player in gameData.players)
            {
                if (ability.CanTarget(gameData, caster, player))
                    playerTargets.Add(player);
            }

            foreach (Slot slot in gameData.Board.GetAll())
            {
                Card card = gameData.GetSlotCard(slot);
                if (card != null)
                {
                    if (ability.CanTarget(gameData, caster, card))
                        cardTargets.Add(card);
                }
                else if (ability.CanTarget(gameData, caster, slot))
                {
                    slotTargets.Add(slot);
                }
            }

            int targetCount = playerTargets.Count + cardTargets.Count + slotTargets.Count;
            if (targetCount == 0)
                return false;

            // 随机选择一个目标
            int targetIndex = rand.Next(0, targetCount);
            if (targetIndex < playerTargets.Count)
            {
                AIAction action = new(GameAction.SelectPlayer)
                {
                    target_player_id = playerTargets[targetIndex].player_id,
                };
                return TryExecuteAction(action);
            }

            targetIndex -= playerTargets.Count;
            if (targetIndex < cardTargets.Count)
            {
                AIAction action = new(GameAction.SelectCard)
                {
                    target_uid = cardTargets[targetIndex].uid,
                };
                return TryExecuteAction(action);
            }

            targetIndex -= cardTargets.Count;
            AIAction selectSlot = new(GameAction.SelectSlot)
            {
                slot = slotTargets[targetIndex],
            };
            return TryExecuteAction(selectSlot);
        }

        /// <summary>
        /// 随机选择能力链选项
        /// </summary>
        private bool TrySelectChoice()
        {
            if (!CanResolveSelection())
                return false;

            Game gameData = gameplay.GetGameData();
            AbilityData ability = AbilityData.Get(gameData.selector_ability_id);
            Card caster = gameData.GetCard(gameData.selector_caster_uid);
            if (ability == null || caster == null)
                return false;

            List<int> choices = new();
            for (int i = 0; i < ability.chain_abilities.Length; i++)
            {
                AbilityData choice = ability.chain_abilities[i];
                if (choice != null && gameplay.Rules.CanSelectAbility(caster, choice))
                    choices.Add(i);
            }

            if (choices.Count == 0)
                return false;

            int selectedChoice = choices[rand.Next(0, choices.Count)];
            AIAction action = new(GameAction.SelectChoice) { value = selectedChoice };
            return TryExecuteAction(action);
        }

        /// <summary>
        /// 随机选择支付的法力值
        /// </summary>
        private bool TrySelectCost()
        {
            if (!CanResolveSelection())
                return false;

            Game gameData = gameplay.GetGameData();
            Player player = gameData.GetPlayer(PlayerId);
            Card card = gameData.GetCard(gameData.selector_caster_uid);

            if (player != null && card != null)
            {
                int max = Mathf.Min(player.mana, GameplayData.Get().mana_max - 1);
                if (max < 0)
                    return false;

                int choice = rand.Next(0, max + 1);
                AIAction action = new(GameAction.SelectCost) { value = choice };
                return TryExecuteAction(action);
            }

            return false;
        }

        private void CancelSelect()
        {
            TryExecuteAction(new AIAction(GameAction.CancelSelect));
        }

        /// <summary>
        /// Mulligan 阶段随机选择（不换牌）
        /// </summary>
        private void SelectMulligan()
        {
            TryExecuteAction(new AIAction(GameAction.SelectMulligan));
        }

        private void EndTurn()
        {
            TryExecuteAction(new AIAction(GameAction.EndTurn));
        }
    }

}
