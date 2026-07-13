namespace TcgEngine.Gameplay
{
    /// <summary>玩家主动操作：出牌、移动和主动施放能力。</summary>
    public sealed class ActionExecutor
    {
        private readonly GameRuntime runtime;

        public ActionExecutor(GameRuntime runtime)
        {
            this.runtime = runtime;
        }

        public void PlayCard(Card card, Slot slot, bool skipCost)
        {
            if (!runtime.Rules.CanPlayCard(card, slot, skipCost))
                return;

            Player player = runtime.Game.GetPlayer(card.player_id);
            if (!skipCost)
                player.PayMana(card);

            CardData data = card.CardData;
            if (data.IsBoardCard())
            {
                runtime.Zones.MoveToBoard(card, slot);
                card.exhausted = true;
            }
            else if (data.IsEquipment())
            {
                runtime.Cards.EquipAndDiscardExisting(runtime.Game.GetSlotCard(slot), card);
                card.exhausted = true;
            }
            else if (data.IsSecret())
            {
                runtime.Zones.MoveTo(card, CardZone.Secret);
            }
            else
            {
                runtime.Zones.MoveTo(card, CardZone.Discard);
                card.slot = slot;
            }

            if (!runtime.IsAiSimulation && !data.IsSecret())
                player.AddHistory(GameAction.PlayCard, card);

            runtime.Game.last_played = card.uid;
            runtime.UpdateOngoings();

            if (card.CardData.IsDynamicManaCost())
            {
                runtime.Selection.BeginCostSelector(card);
            }
            else
            {
                runtime.Secrets.TriggerSecrets(AbilityTrigger.OnPlayOther, card);
                runtime.Abilities.TriggerType(AbilityTrigger.OnPlay, card);
                runtime.Abilities.TriggerOtherCards(AbilityTrigger.OnPlayOther, card);
            }

            runtime.Events.RaiseRefreshed();
            runtime.Events.RaiseCardPlayed(card, slot);
            runtime.ResolveQueue.ResolveAll(0.3f);
        }

        public void MoveCard(Card card, Slot slot, bool skipCost)
        {
            if (!runtime.Rules.CanMoveCard(card, slot, skipCost))
                return;

            card.slot = slot;
            Card equipment = runtime.Game.GetEquipCard(card.equipped_uid);
            if (equipment != null)
                equipment.slot = slot;

            runtime.UpdateOngoings();
            runtime.Events.RaiseRefreshed();
            runtime.Events.RaiseCardMoved(card, slot);
            runtime.ResolveQueue.ResolveAll(0.2f);
        }

        public void CastAbility(Card card, AbilityData ability)
        {
            if (!runtime.Rules.CanCastAbility(card, ability))
                return;

            Player player = runtime.Game.GetPlayer(card.player_id);
            if (!runtime.IsAiSimulation && ability.target != AbilityTarget.SelectTarget)
                player.AddHistory(GameAction.CastAbility, card, ability);

            card.RemoveStatus(StatusType.Stealth);
            runtime.Abilities.Trigger(ability, card);
            runtime.ResolveQueue.ResolveAll();
        }
    }
}
