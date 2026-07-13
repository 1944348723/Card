namespace TcgEngine.Gameplay
{
    /// <summary>
    /// Read-only game rule queries bound to the current game state.
    /// This service never mutates state, publishes events, or touches the resolve queue.
    /// </summary>
    public sealed class GameRules
    {
        private Game game;

        public GameRules(Game game)
        {
            SetData(game);
        }

        public void SetData(Game game)
        {
            this.game = game;
        }

        public bool IsPlayerTurn(Player player)
        {
            return IsPlayerActionTurn(player) || IsPlayerSelectorTurn(player);
        }

        public bool IsPlayerActionTurn(Player player)
        {
            return player != null
                && game.current_player == player.player_id
                && game.state == GameState.Play
                && game.phase == GamePhase.Main
                && !game.Selection.IsActive;
        }

        public bool IsPlayerSelectorTurn(Player player)
        {
            return player != null
                && game.Selection.PlayerId == player.player_id
                && game.state == GameState.Play
                && game.phase == GamePhase.Main
                && game.Selection.IsActive;
        }

        public bool IsPlayerMulliganTurn(Player player)
        {
            return game.phase == GamePhase.Mulligan && !player.ready;
        }

        public bool CanPlayCard(Card card, Slot slot, bool skipCost = false)
        {
            if (card == null)
                return false;

            Player player = game.GetPlayer(card.player_id);
            if (!player.HasCardInHand(card))
                return false;
            if (!skipCost && !player.CanPayMana(card))
                return false;
            if (player.is_ai && card.CardData.IsDynamicManaCost() && player.mana == 0)
                return false;

            if (card.CardData.IsBoardCard())
                return game.Board.Contains(slot) && !game.HasCardOnSlot(slot) && slot.BelongsToPlayer(card.player_id);

            if (card.CardData.IsEquipment())
            {
                if (!game.Board.Contains(slot))
                    return false;

                Card target = game.GetSlotCard(slot);
                return target != null
                    && target.CardData.type == CardType.Character
                    && target.player_id == card.player_id;
            }

            if (card.CardData.IsRequireTargetSpell())
                return IsPlayTargetValid(card, slot);
            if (card.CardData.type == CardType.Spell)
                return CanAnyPlayAbilityTrigger(card);

            return true;
        }

        public bool CanMoveCard(Card card, Slot slot, bool skipCost = false)
        {
            if (card == null || !game.Board.Contains(slot))
                return false;
            if (!game.IsOnBoard(card) || !card.CanMove(skipCost))
                return false;
            if (!slot.BelongsToPlayer(card.player_id) || card.slot == slot)
                return false;

            return game.GetSlotCard(slot) == null;
        }

        public bool CanAttackTarget(Card attacker, Player target, bool skipCost = false)
        {
            if (attacker == null || target == null)
                return false;
            if (!attacker.CanAttack(skipCost) || attacker.player_id == target.player_id)
                return false;
            if (!game.IsOnBoard(attacker) || !attacker.CardData.IsCharacter())
                return false;
            if (target.HasStatus(StatusType.Protected) && !attacker.HasStatus(StatusType.Flying))
                return false;

            return true;
        }

        public bool CanAttackTarget(Card attacker, Card target, bool skipCost = false)
        {
            if (attacker == null || target == null)
                return false;
            if (!attacker.CanAttack(skipCost) || attacker.player_id == target.player_id)
                return false;
            if (!game.IsOnBoard(attacker) || !game.IsOnBoard(target))
                return false;
            if (!attacker.CardData.IsCharacter() || !target.CardData.IsBoardCard())
                return false;
            if (target.HasStatus(StatusType.Stealth))
                return false;
            if (target.HasStatus(StatusType.Protected) && !attacker.HasStatus(StatusType.Flying))
                return false;

            return true;
        }

        public bool CanCastAbility(Card card, AbilityData ability)
        {
            if (ability == null || card == null || !card.CanDoActivatedAbilities())
                return false;
            if (ability.trigger != AbilityTrigger.Activate)
                return false;

            Player player = game.GetPlayer(card.player_id);
            return player.CanPayAbility(card, ability) && ability.AreTriggerConditionsMet(game, card);
        }

        public bool CanSelectAbility(Card card, AbilityData ability)
        {
            if (ability == null || card == null || !card.CanDoAbilities())
                return false;

            Player player = game.GetPlayer(card.player_id);
            return player.CanPayAbility(card, ability) && ability.AreTriggerConditionsMet(game, card);
        }

        public bool CanAnyPlayAbilityTrigger(Card card)
        {
            if (card == null)
                return false;
            if (card.CardData.IsDynamicManaCost())
                return true;

            foreach (AbilityData ability in card.GetAbilities())
            {
                if (ability.trigger == AbilityTrigger.OnPlay && ability.AreTriggerConditionsMet(game, card))
                    return true;
            }

            return false;
        }

        public bool IsPlayTargetValid(Card caster, Player target)
        {
            if (caster == null || target == null)
                return false;

            foreach (AbilityData ability in caster.GetAbilities())
            {
                if (ability && ability.trigger == AbilityTrigger.OnPlay && ability.target == AbilityTarget.PlayTarget
                    && !ability.CanTarget(game, caster, target))
                    return false;
            }

            return true;
        }

        public bool IsPlayTargetValid(Card caster, Card target)
        {
            if (caster == null || target == null)
                return false;

            foreach (AbilityData ability in caster.GetAbilities())
            {
                if (ability && ability.trigger == AbilityTrigger.OnPlay && ability.target == AbilityTarget.PlayTarget
                    && !ability.CanTarget(game, caster, target))
                    return false;
            }

            return true;
        }

        public bool IsPlayTargetValid(Card caster, Slot target)
        {
            if (caster == null)
                return false;
            if (target.IsPlayerSlot())
                return IsPlayTargetValid(caster, game.GetPlayer(target.p));

            Card card = game.GetSlotCard(target);
            if (card != null)
                return IsPlayTargetValid(caster, card);

            foreach (AbilityData ability in caster.GetAbilities())
            {
                if (ability && ability.trigger == AbilityTrigger.OnPlay && ability.target == AbilityTarget.PlayTarget
                    && !ability.CanTarget(game, caster, target))
                    return false;
            }

            return true;
        }
    }
}
