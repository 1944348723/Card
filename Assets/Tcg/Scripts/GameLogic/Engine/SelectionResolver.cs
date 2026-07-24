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
            if (!runtime.Game.Selection.IsActive)
                return;

            Card caster = runtime.Game.GetCard(runtime.Game.Selection.CasterUid);
            AbilityData ability = AbilityData.Get(runtime.Game.Selection.AbilityId);
            if (caster == null || target == null || ability == null)
                return;

            if (runtime.Game.Selection.Type == SelectorType.SelectTarget)
            {
                if (!ability.CanTarget(runtime.Game, caster, target))
                    return;

                AddSelectedAbilityHistory(caster, ability, target);
                runtime.Game.EndSelection();
                runtime.Game.last_target = target.uid;
                runtime.Abilities.ResolveEffect(ability, caster, target);
                runtime.Abilities.Complete(ability, caster);
                runtime.ResolveQueue.ResolveAll();
            }

            if (runtime.Game.Selection.Type == SelectorType.SelectorCard)
            {
                if (!ability.IsCardSelectionValid(runtime.Game, caster, target, runtime.CardTargets))
                    return;

                runtime.Game.EndSelection();
                runtime.Game.last_target = target.uid;
                runtime.Abilities.ResolveEffect(ability, caster, target);
                runtime.Abilities.Complete(ability, caster);
                runtime.ResolveQueue.ResolveAll();
            }
        }

        public void SelectPlayer(Player target)
        {
            if (!runtime.Game.Selection.IsActive)
                return;

            Card caster = runtime.Game.GetCard(runtime.Game.Selection.CasterUid);
            AbilityData ability = AbilityData.Get(runtime.Game.Selection.AbilityId);
            if (caster == null || target == null || ability == null)
                return;
            if (runtime.Game.Selection.Type != SelectorType.SelectTarget || !ability.CanTarget(runtime.Game, caster, target))
                return;

            AddSelectedAbilityHistory(caster, ability, target);
            runtime.Game.EndSelection();
            runtime.Abilities.ResolveEffect(ability, caster, target);
            runtime.Abilities.Complete(ability, caster);
            runtime.ResolveQueue.ResolveAll();
        }

        public void SelectSlot(Slot target)
        {
            if (!runtime.Game.Selection.IsActive)
                return;

            Card caster = runtime.Game.GetCard(runtime.Game.Selection.CasterUid);
            AbilityData ability = AbilityData.Get(runtime.Game.Selection.AbilityId);
            if (caster == null || ability == null || !runtime.Board.Contains(target))
                return;
            if (runtime.Game.Selection.Type != SelectorType.SelectTarget || !ability.CanTarget(runtime.Game, caster, target))
                return;

            AddSelectedAbilityHistory(caster, ability, target);
            runtime.Game.EndSelection();
            runtime.Abilities.ResolveEffect(ability, caster, target);
            runtime.Abilities.Complete(ability, caster);
            runtime.ResolveQueue.ResolveAll();
        }

        public void SelectChoice(int choice)
        {
            if (!runtime.Game.Selection.IsActive)
                return;

            Card caster = runtime.Game.GetCard(runtime.Game.Selection.CasterUid);
            AbilityData ability = AbilityData.Get(runtime.Game.Selection.AbilityId);
            if (caster == null || ability == null || choice < 0)
                return;
            if (runtime.Game.Selection.Type != SelectorType.SelectorChoice || ability.target != AbilityTarget.ChoiceSelector)
                return;
            if (choice >= ability.chain_abilities.Length)
                return;

            AbilityData selected = ability.chain_abilities[choice];
            if (selected == null || !runtime.Rules.CanSelectAbility(caster, selected))
                return;

            runtime.Game.EndSelection();
            runtime.Abilities.Complete(ability, caster);
            runtime.Abilities.Resolve(selected, caster, caster);
            runtime.ResolveQueue.ResolveAll();
        }

        public void SelectCost(int selectedCost)
        {
            if (!runtime.Game.Selection.IsActive)
                return;

            Player player = runtime.Game.GetPlayer(runtime.Game.Selection.PlayerId);
            Card caster = runtime.Game.GetCard(runtime.Game.Selection.CasterUid);
            if (player == null || caster == null || selectedCost < 0)
                return;
            if (runtime.Game.Selection.Type != SelectorType.SelectorCost)
                return;
            if (selectedCost >= GameplayData.Get().mana_max || selectedCost > player.mana)
                return;

            runtime.Game.EndSelection();
            runtime.Game.SetSelectedValue(selectedCost);
            player.mana -= selectedCost;
            runtime.Events.RaiseRefreshed();

            runtime.Secrets.TriggerSecrets(AbilityTrigger.OnPlayOther, caster);
            runtime.Abilities.TriggerType(AbilityTrigger.OnPlay, caster);
            runtime.Abilities.TriggerOtherCards(AbilityTrigger.OnPlayOther, caster);
            runtime.ResolveQueue.ResolveAll();
        }

        public void Cancel()
        {
            if (!runtime.Game.Selection.IsActive)
                return;

            if (runtime.Game.Selection.Type == SelectorType.SelectorCost)
                CancelPlayCard();

            runtime.Game.EndSelection();
            runtime.Events.RaiseRefreshed();
        }

        public void CancelPlayCard()
        {
            Card card = runtime.Game.GetCard(runtime.Game.Selection.CasterUid);
            if (card == null)
                return;

            Player player = runtime.Game.GetPlayer(card.player_id);
            if (card.CardData.IsDynamicManaCost())
                player.mana += runtime.Game.Selection.SelectedValue;
            else
                player.mana += card.CardData.cost;

            runtime.Zones.MoveTo(card, CardZone.Hand);
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
                runtime.Zones.MoveTo(card, CardZone.Discard);

            player.ready = true;
            runtime.Cards.DrawCards(player, count);
            runtime.Events.RaiseRefreshed();
            // TODO: 经常有这样的在某个操作后面接着另一个操作，流程隐藏在了调用链中
            if (runtime.Game.AreAllPlayersReady())
                runtime.Flow.StartTurn();
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
            runtime.Game.SetSelectedValue(0);
            Begin(SelectorType.SelectorCost, string.Empty, caster);
        }

        public void BeginMulligan()
        {
            runtime.Game.phase = GamePhase.Mulligan;
            runtime.Game.turn_timer = GameplayData.Get().turn_duration;
            foreach (Player player in runtime.Game.players)
                player.ready = false;
            runtime.Events.RaiseRefreshed();
        }

        private void Begin(SelectorType type, string abilityId, Card caster)
        {
            runtime.Game.BeginSelection(type, caster.player_id, abilityId, caster.uid);
            runtime.Events.RaiseRefreshed();
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
