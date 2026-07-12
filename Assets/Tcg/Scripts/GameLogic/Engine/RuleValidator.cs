namespace TcgEngine.Gameplay
{
    /// <summary>
    /// 纯规则查询：不发送事件、不修改游戏状态、不依赖运行时队列。
    /// </summary>
    public static class RuleValidator
    {
        public static bool IsPlayerTurn(Game game, Player player)
        {
            return IsPlayerActionTurn(game, player) || IsPlayerSelectorTurn(game, player);
        }

        public static bool IsPlayerActionTurn(Game game, Player player)
        {
            return player != null
                && game.current_player == player.player_id
                && game.state == GameState.Play
                && game.phase == GamePhase.Main
                && game.selector == SelectorType.None;
        }

        public static bool IsPlayerSelectorTurn(Game game, Player player)
        {
            return player != null
                && game.selector_player_id == player.player_id
                && game.state == GameState.Play
                && game.phase == GamePhase.Main
                && game.selector != SelectorType.None;
        }

        public static bool IsPlayerMulliganTurn(Game game, Player player)
        {
            return game.phase == GamePhase.Mulligan && !player.ready;
        }

        public static bool CanPlayCard(Game game, Card card, Slot slot, bool skipCost)
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
                return slot.IsBoardSlot() && !game.HasCardOnSlot(slot) && slot.BelongsToPlayer(card.player_id);

            if (card.CardData.IsEquipment())
            {
                if (!slot.IsBoardSlot())
                    return false;

                Card target = game.GetSlotCard(slot);
                return target != null
                    && target.CardData.type == CardType.Character
                    && target.player_id == card.player_id;
            }

            if (card.CardData.IsRequireTargetSpell())
                return IsPlayTargetValid(game, card, slot);
            if (card.CardData.type == CardType.Spell)
                return CanAnyPlayAbilityTrigger(game, card);

            return true;
        }

        public static bool CanMoveCard(Game game, Card card, Slot slot, bool skipCost)
        {
            if (card == null || !slot.IsBoardSlot())
                return false;
            if (!game.IsOnBoard(card) || !card.CanMove(skipCost))
                return false;
            if (!slot.BelongsToPlayer(card.player_id) || card.slot == slot)
                return false;

            return game.GetSlotCard(slot) == null;
        }

        public static bool CanAttackTarget(Game game, Card attacker, Player target, bool skipCost)
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

        public static bool CanAttackTarget(Game game, Card attacker, Card target, bool skipCost)
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

        public static bool CanCastAbility(Game game, Card card, AbilityData ability)
        {
            if (ability == null || card == null || !card.CanDoActivatedAbilities())
                return false;
            if (ability.trigger != AbilityTrigger.Activate)
                return false;

            Player player = game.GetPlayer(card.player_id);
            return player.CanPayAbility(card, ability) && ability.AreTriggerConditionsMet(game, card);
        }

        public static bool CanSelectAbility(Game game, Card card, AbilityData ability)
        {
            if (ability == null || card == null || !card.CanDoAbilities())
                return false;

            Player player = game.GetPlayer(card.player_id);
            return player.CanPayAbility(card, ability) && ability.AreTriggerConditionsMet(game, card);
        }

        public static bool CanAnyPlayAbilityTrigger(Game game, Card card)
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

        public static bool IsPlayTargetValid(Game game, Card caster, Player target)
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

        public static bool IsPlayTargetValid(Game game, Card caster, Card target)
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

        public static bool IsPlayTargetValid(Game game, Card caster, Slot target)
        {
            if (caster == null)
                return false;
            if (target.IsPlayerSlot())
                return IsPlayTargetValid(game, caster, game.GetPlayer(target.p));

            Card card = game.GetSlotCard(target);
            if (card != null)
                return IsPlayTargetValid(game, caster, card);

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
