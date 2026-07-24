using System;
using System.Collections.Generic;
using TcgEngine.Gameplay;

namespace TcgEngine.AI
{
    /// <summary>
    /// 随从牌落位的枚举策略。
    /// Minimax 只取一个随机空位以限制搜索树，Random 则枚举全部合法位置。
    /// </summary>
    internal enum AIPlayPositionMode
    {
        AllLegalSlots,
        SingleRandomBoardSlot,
    }

    /// <summary>
    /// 根据当前游戏状态生成合法 AI 动作。
    /// 该类只负责规则与候选枚举，不决定动作优先级，也不执行动作。
    /// </summary>
    internal sealed class AIActionGenerator
    {
        private readonly GameLogic gameLogic;
        private readonly Func<ushort, Card, AIAction> actionFactory;

        public AIActionGenerator(
            GameLogic gameLogic,
            Func<ushort, Card, AIAction> actionFactory)
        {
            this.gameLogic = gameLogic ?? throw new ArgumentNullException(nameof(gameLogic));
            this.actionFactory = actionFactory ?? throw new ArgumentNullException(nameof(actionFactory));
        }

        public void AddPlayCardActions(
            List<AIAction> actions,
            Game game,
            Card card,
            AIPlayPositionMode positionMode,
            Random random = null,
            List<Slot> slotBuffer = null)
        {
            if (!CanGenerateNormalAction(actions, game, card))
                return;

            Player player = game.GetPlayer(card.player_id);
            if (player == null)
                return;

            if (card.CardData.IsBoardCard())
            {
                if (positionMode == AIPlayPositionMode.SingleRandomBoardSlot)
                {
                    if (random == null)
                        throw new ArgumentNullException(nameof(random));

                    Slot slot = player.GetRandomEmptySlot(game.Board, random, slotBuffer);
                    AddPlayCardActionIfLegal(actions, game, card, slot);
                }
                else
                {
                    foreach (Slot slot in game.Board.GetAll(player.player_id))
                        AddPlayCardActionIfLegal(actions, game, card, slot);
                }

                return;
            }

            if (card.CardData.IsEquipment())
            {
                foreach (Card target in player.cards_board)
                    AddPlayCardActionIfLegal(actions, game, card, target.slot);

                return;
            }

            if (card.CardData.IsRequireTargetSpell())
            {
                foreach (Player target in game.players)
                    AddPlayCardActionIfLegal(actions, game, card, new Slot(target.player_id));

                foreach (Slot slot in game.Board.GetAll())
                    AddPlayCardActionIfLegal(actions, game, card, slot);

                return;
            }

            AddPlayCardActionIfLegal(actions, game, card, Slot.None);
        }

        public void AddAttackActions(List<AIAction> actions, Game game, Card attacker)
        {
            if (!CanGenerateNormalAction(actions, game, attacker))
                return;

            foreach (Player targetPlayer in game.players)
            {
                if (gameLogic.Rules.CanAttackTarget(attacker, targetPlayer))
                {
                    AIAction action = CreateAction(GameAction.AttackPlayer, attacker);
                    action.target_player_id = targetPlayer.player_id;
                    actions.Add(action);
                }

                foreach (Card targetCard in targetPlayer.cards_board)
                {
                    if (!gameLogic.Rules.CanAttackTarget(attacker, targetCard))
                        continue;

                    AIAction action = CreateAction(GameAction.Attack, attacker);
                    action.target_uid = targetCard.uid;
                    actions.Add(action);
                }
            }
        }

        public void AddActivatedAbilityActions(List<AIAction> actions, Game game, Card card)
        {
            if (!CanGenerateNormalAction(actions, game, card))
                return;

            foreach (AbilityData ability in card.GetAbilities())
            {
                if (ability == null
                    || !gameLogic.Rules.CanCastAbility(card, ability)
                    || !ability.HasValidSelectTarget(game, card))
                {
                    continue;
                }

                AIAction action = CreateAction(GameAction.CastAbility, card);
                action.ability_id = ability.id;
                actions.Add(action);
            }
        }

        public void AddMoveActions(List<AIAction> actions, Game game, Card card)
        {
            if (!CanGenerateNormalAction(actions, game, card))
                return;

            Player player = game.GetPlayer(card.player_id);
            if (player == null)
                return;

            foreach (Slot slot in game.Board.GetAll(player.player_id))
            {
                if (!gameLogic.Rules.CanMoveCard(card, slot))
                    continue;

                AIAction action = CreateAction(GameAction.Move, card);
                action.slot = slot;
                actions.Add(action);
            }
        }

        public void AddSelectionActions(
            List<AIAction> actions,
            Game game,
            int minimumCost,
            ListSwap<Card> cardBuffer = null)
        {
            if (actions == null)
                throw new ArgumentNullException(nameof(actions));
            if (game == null || game.selector == SelectorType.None)
                return;

            int initialActionCount = actions.Count;
            Player player = game.GetPlayer(game.selector_player_id);
            Card caster = game.GetCard(game.selector_caster_uid);
            AbilityData ability = AbilityData.Get(game.selector_ability_id);

            if (caster != null)
            {
                if (game.selector == SelectorType.SelectTarget && ability != null)
                    AddTargetSelectionActions(actions, game, caster, ability);
                else if (game.selector == SelectorType.SelectorCard && ability != null)
                    AddCardSelectionActions(actions, game, caster, ability, cardBuffer);
                else if (game.selector == SelectorType.SelectorChoice && ability != null)
                    AddChoiceSelectionActions(actions, caster, ability);
                else if (game.selector == SelectorType.SelectorCost && player != null)
                    AddCostSelectionActions(actions, player, caster, minimumCost);
            }

            if (actions.Count == initialActionCount)
                actions.Add(CreateAction(GameAction.CancelSelect, caster));
        }

        private void AddPlayCardActionIfLegal(
            List<AIAction> actions,
            Game game,
            Card card,
            Slot slot)
        {
            if (!gameLogic.Rules.CanPlayCard(card, slot))
                return;

            AIAction action = CreateAction(GameAction.PlayCard, card);
            action.slot = slot;

            if (slot.IsPlayerSlot())
            {
                action.target_player_id = slot.p;
            }
            else
            {
                Card target = game.GetSlotCard(slot);
                action.target_uid = target?.uid;
            }

            actions.Add(action);
        }

        private void AddTargetSelectionActions(
            List<AIAction> actions,
            Game game,
            Card caster,
            AbilityData ability)
        {
            foreach (Player targetPlayer in game.players)
            {
                if (!ability.CanTarget(game, caster, targetPlayer))
                    continue;

                AIAction action = CreateAction(GameAction.SelectPlayer, caster);
                action.target_player_id = targetPlayer.player_id;
                actions.Add(action);
            }

            foreach (Slot slot in game.Board.GetAll())
            {
                Card targetCard = game.GetSlotCard(slot);
                if (targetCard != null && ability.CanTarget(game, caster, targetCard))
                {
                    AIAction action = CreateAction(GameAction.SelectCard, caster);
                    action.target_uid = targetCard.uid;
                    actions.Add(action);
                }
                else if (targetCard == null && ability.CanTarget(game, caster, slot))
                {
                    AIAction action = CreateAction(GameAction.SelectSlot, caster);
                    action.slot = slot;
                    actions.Add(action);
                }
            }
        }

        private void AddCardSelectionActions(
            List<AIAction> actions,
            Game game,
            Card caster,
            AbilityData ability,
            ListSwap<Card> cardBuffer)
        {
            List<Card> targets = ability.GetCardTargets(game, caster, cardBuffer);
            foreach (Card target in targets)
            {
                AIAction action = CreateAction(GameAction.SelectCard, caster);
                action.target_uid = target.uid;
                actions.Add(action);
            }
        }

        private void AddChoiceSelectionActions(
            List<AIAction> actions,
            Card caster,
            AbilityData ability)
        {
            for (int i = 0; i < ability.chain_abilities.Length; i++)
            {
                AbilityData choice = ability.chain_abilities[i];
                if (choice == null || !gameLogic.Rules.CanSelectAbility(caster, choice))
                    continue;

                AIAction action = CreateAction(GameAction.SelectChoice, caster);
                action.value = i;
                actions.Add(action);
            }
        }

        private void AddCostSelectionActions(
            List<AIAction> actions,
            Player player,
            Card caster,
            int minimumCost)
        {
            int firstCost = Math.Max(0, minimumCost);
            int maximumCost = Math.Min(player.mana, GameplayData.Get().mana_max - 1);
            for (int cost = firstCost; cost <= maximumCost; cost++)
            {
                AIAction action = CreateAction(GameAction.SelectCost, caster);
                action.value = cost;
                actions.Add(action);
            }
        }

        private AIAction CreateAction(ushort type, Card card)
        {
            AIAction action = actionFactory(type, card);
            if (action == null)
                throw new InvalidOperationException("AI action factory returned null.");

            return action;
        }

        private static bool CanGenerateNormalAction(
            List<AIAction> actions,
            Game game,
            Card card)
        {
            if (actions == null)
                throw new ArgumentNullException(nameof(actions));

            return game != null
                && card != null
                && game.selector == SelectorType.None
                && !card.HasStatus(StatusType.Paralysed);
        }
    }
}
