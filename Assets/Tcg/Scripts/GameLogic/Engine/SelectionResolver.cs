using System.Collections.Generic;
using System.Linq;

namespace TcgEngine.Gameplay
{
    /// <summary>能力目标、费用、选项与换牌阶段的状态机。</summary>
    public sealed class SelectionResolver
    {
        private readonly GameRuntime runtime;

        public SelectionResolver(GameRuntime runtime)
        {
            this.runtime = runtime;
        }

        public void SelectCard(Card target)
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
                    return;

                AddSelectedAbilityHistory(caster, ability, target);
                runtime.Game.selector = SelectorType.None;
                runtime.Game.last_target = target.uid;
                runtime.Abilities.ResolveEffect(ability, caster, target);
                runtime.Abilities.Complete(ability, caster);
                runtime.ResolveQueue.ResolveAll();
            }

            if (runtime.Game.selector == SelectorType.SelectorCard)
            {
                if (!ability.IsCardSelectionValid(runtime.Game, caster, target, runtime.CardTargets))
                    return;

                runtime.Game.selector = SelectorType.None;
                runtime.Game.last_target = target.uid;
                runtime.Abilities.ResolveEffect(ability, caster, target);
                runtime.Abilities.Complete(ability, caster);
                runtime.ResolveQueue.ResolveAll();
            }
        }

        public void SelectPlayer(Player target)
        {
            if (runtime.Game.selector == SelectorType.None)
                return;

            Card caster = runtime.Game.GetCard(runtime.Game.selector_caster_uid);
            AbilityData ability = AbilityData.Get(runtime.Game.selector_ability_id);
            if (caster == null || target == null || ability == null)
                return;
            if (runtime.Game.selector != SelectorType.SelectTarget || !ability.CanTarget(runtime.Game, caster, target))
                return;

            AddSelectedAbilityHistory(caster, ability, target);
            runtime.Game.selector = SelectorType.None;
            runtime.Abilities.ResolveEffect(ability, caster, target);
            runtime.Abilities.Complete(ability, caster);
            runtime.ResolveQueue.ResolveAll();
        }

        public void SelectSlot(Slot target)
        {
            if (runtime.Game.selector == SelectorType.None)
                return;

            Card caster = runtime.Game.GetCard(runtime.Game.selector_caster_uid);
            AbilityData ability = AbilityData.Get(runtime.Game.selector_ability_id);
            if (caster == null || ability == null || !target.IsBoardSlot())
                return;
            if (runtime.Game.selector != SelectorType.SelectTarget || !ability.CanTarget(runtime.Game, caster, target))
                return;

            AddSelectedAbilityHistory(caster, ability, target);
            runtime.Game.selector = SelectorType.None;
            runtime.Abilities.ResolveEffect(ability, caster, target);
            runtime.Abilities.Complete(ability, caster);
            runtime.ResolveQueue.ResolveAll();
        }

        public void SelectChoice(int choice)
        {
            if (runtime.Game.selector == SelectorType.None)
                return;

            Card caster = runtime.Game.GetCard(runtime.Game.selector_caster_uid);
            AbilityData ability = AbilityData.Get(runtime.Game.selector_ability_id);
            if (caster == null || ability == null || choice < 0)
                return;
            if (runtime.Game.selector != SelectorType.SelectorChoice || ability.target != AbilityTarget.ChoiceSelector)
                return;
            if (choice >= ability.chain_abilities.Length)
                return;

            AbilityData selected = ability.chain_abilities[choice];
            if (selected == null || !runtime.Game.CanSelectAbility(caster, selected))
                return;

            runtime.Game.selector = SelectorType.None;
            runtime.Abilities.Complete(ability, caster);
            runtime.Abilities.Resolve(selected, caster, caster);
            runtime.ResolveQueue.ResolveAll();
        }

        public void SelectCost(int selectedCost)
        {
            if (runtime.Game.selector == SelectorType.None)
                return;

            Player player = runtime.Game.GetPlayer(runtime.Game.selector_player_id);
            Card caster = runtime.Game.GetCard(runtime.Game.selector_caster_uid);
            if (player == null || caster == null || selectedCost < 0)
                return;
            if (runtime.Game.selector != SelectorType.SelectorCost)
                return;
            if (selectedCost >= 10 || selectedCost > player.mana)
                return;

            runtime.Game.selector = SelectorType.None;
            runtime.Game.selected_value = selectedCost;
            player.mana -= selectedCost;
            runtime.Engine.RefreshData();

            runtime.Engine.TriggerSecrets(AbilityTrigger.OnPlayOther, caster);
            runtime.Engine.TriggerCardAbilityType(AbilityTrigger.OnPlay, caster);
            runtime.Engine.TriggerOtherCardsAbilityType(AbilityTrigger.OnPlayOther, caster);
            runtime.ResolveQueue.ResolveAll();
        }

        public void Cancel()
        {
            if (runtime.Game.selector == SelectorType.None)
                return;

            if (runtime.Game.selector == SelectorType.SelectorCost)
                CancelPlayCard();

            runtime.Game.selector = SelectorType.None;
            runtime.Engine.RefreshData();
        }

        public void CancelPlayCard()
        {
            Card card = runtime.Game.GetCard(runtime.Game.selector_caster_uid);
            if (card == null)
                return;

            Player player = runtime.Game.GetPlayer(card.player_id);
            if (card.CardData.IsDynamicManaCost())
                player.mana += runtime.Game.selected_value;
            else
                player.mana += card.CardData.cost;

            runtime.Zones.MoveTo(player, card, CardZone.Hand);
            card.Clear();
        }

        public void Mulligan(Player player, string[] cards)
        {
            if (runtime.Game.phase != GamePhase.Mulligan || player.ready)
                return;

            int count = 0;
            List<Card> removed = new();
            foreach (Card card in player.cards_hand)
            {
                if (cards.Contains(card.uid))
                {
                    removed.Add(card);
                    count++;
                }
            }

            foreach (Card card in removed)
                runtime.Zones.MoveTo(player, card, CardZone.Discard);

            player.ready = true;
            runtime.Engine.DrawCards(player, count);
            runtime.Engine.RefreshData();
            if (runtime.Game.AreAllPlayersReady())
                runtime.Engine.StartTurn();
        }

        public void BeginSelectTarget(AbilityData ability, Card caster)
        {
            Begin(SelectorType.SelectTarget, ability.id, caster);
        }

        public void BeginCardSelector(AbilityData ability, Card caster)
        {
            Begin(SelectorType.SelectorCard, ability.id, caster);
        }

        public void BeginChoiceSelector(AbilityData ability, Card caster)
        {
            Begin(SelectorType.SelectorChoice, ability.id, caster);
        }

        public void BeginCostSelector(Card caster)
        {
            Begin(SelectorType.SelectorCost, string.Empty, caster);
            runtime.Game.selected_value = 0;
        }

        public void BeginMulligan()
        {
            runtime.Game.phase = GamePhase.Mulligan;
            runtime.Game.turn_timer = GameplayData.Get().turn_duration;
            foreach (Player player in runtime.Game.players)
                player.ready = false;
            runtime.Engine.RefreshData();
        }

        private void Begin(SelectorType type, string abilityId, Card caster)
        {
            runtime.Game.selector = type;
            runtime.Game.selector_player_id = caster.player_id;
            runtime.Game.selector_ability_id = abilityId;
            runtime.Game.selector_caster_uid = caster.uid;
            runtime.Engine.RefreshData();
        }

        private void AddSelectedAbilityHistory(Card caster, AbilityData ability, Card target)
        {
            if (!runtime.IsAiSimulation)
                runtime.Game.GetPlayer(caster.player_id).AddHistory(GameAction.CastAbility, caster, ability, target);
        }

        private void AddSelectedAbilityHistory(Card caster, AbilityData ability, Player target)
        {
            if (!runtime.IsAiSimulation)
                runtime.Game.GetPlayer(caster.player_id).AddHistory(GameAction.CastAbility, caster, ability, target);
        }

        private void AddSelectedAbilityHistory(Card caster, AbilityData ability, Slot target)
        {
            if (!runtime.IsAiSimulation)
                runtime.Game.GetPlayer(caster.player_id).AddHistory(GameAction.CastAbility, caster, ability, target);
        }
    }
}
