using System.Collections.Generic;

namespace TcgEngine.Gameplay
{
    /// <summary>能力触发、目标解析、效果执行和连锁结算。</summary>
    public sealed class AbilityResolver
    {
        private readonly GameRuntime runtime;

        public AbilityResolver(GameRuntime runtime)
        {
            this.runtime = runtime;
        }

        public void TriggerType(AbilityTrigger type, Card caster, Card triggerer = null)
        {
            foreach (AbilityData ability in caster.GetAbilities())
            {
                if (ability && ability.trigger == type)
                    Trigger(ability, caster, triggerer);
            }

            Card equipped = runtime.Game.GetEquipCard(caster.equipped_uid);
            if (equipped != null)
                TriggerType(type, equipped, triggerer);
        }

        public void TriggerType(AbilityTrigger type, Card caster, Player triggerer)
        {
            foreach (AbilityData ability in caster.GetAbilities())
            {
                if (ability && ability.trigger == type)
                    Trigger(ability, caster, triggerer);
            }

            Card equipped = runtime.Game.GetEquipCard(caster.equipped_uid);
            if (equipped != null)
                TriggerType(type, equipped, triggerer);
        }

        public void TriggerOtherCards(AbilityTrigger type, Card triggerer)
        {
            foreach (Player player in runtime.Game.players)
            {
                if (player.hero != null)
                    TriggerType(type, player.hero, triggerer);
                foreach (Card card in player.cards_board)
                    TriggerType(type, card, triggerer);
            }
        }

        public void TriggerPlayerCards(Player player, AbilityTrigger type)
        {
            if (player.hero != null)
                TriggerType(type, player.hero, player.hero);
            foreach (Card card in player.cards_board)
                TriggerType(type, card, card);
        }

        public void Trigger(AbilityData ability, Card caster)
        {
            Trigger(ability, caster, caster);
        }

        public void Trigger(AbilityData ability, Card caster, Card triggerer)
        {
            Card trigger = triggerer ?? caster;
            if (!caster.HasStatus(StatusType.Silenced)
                && ability.AreTriggerConditionsMet(runtime.Game, caster, trigger))
            {
                runtime.ResolveQueue.AddAbility(ability, caster, trigger, Resolve);
            }
        }

        public void Trigger(AbilityData ability, Card caster, Player triggerer)
        {
            if (!caster.HasStatus(StatusType.Silenced)
                && ability.AreTriggerConditionsMet(runtime.Game, caster, triggerer))
            {
                runtime.ResolveQueue.AddAbility(ability, caster, caster, Resolve);
            }
        }

        public void TriggerDelayed(AbilityData ability, Card caster)
        {
            runtime.ResolveQueue.AddAbility(ability, caster, caster, Trigger);
        }

        public void TriggerDelayed(AbilityData ability, Card caster, Card triggerer)
        {
            runtime.ResolveQueue.AddAbility(ability, caster, triggerer ?? caster, Trigger);
        }

        public void Resolve(AbilityData ability, Card caster, Card triggerer)
        {
            if (!caster.CanDoAbilities())
                return;

            runtime.Events.RaiseAbilityStarted(ability, caster);
            runtime.Game.ability_triggerer = triggerer.uid;
            runtime.Game.ability_played.Add(ability.id);

            if (BeginSelector(ability, caster))
                return;

            ResolvePlayTarget(ability, caster);
            ResolvePlayerTargets(ability, caster);
            ResolveCardTargets(ability, caster);
            ResolveSlotTargets(ability, caster);
            ResolveCardDataTargets(ability, caster);
            if (ability.target == AbilityTarget.None)
                ability.DoEffects(runtime.Effects, caster);

            Complete(ability, caster);
        }

        public void ResolveEffect(AbilityData ability, Card caster, Player target)
        {
            ability.DoEffects(runtime.Effects, caster, target);
            runtime.Events.RaiseAbilityTargetedPlayer(ability, caster, target);
        }

        public void ResolveEffect(AbilityData ability, Card caster, Card target)
        {
            ability.DoEffects(runtime.Effects, caster, target);
            runtime.Events.RaiseAbilityTargetedCard(ability, caster, target);
        }

        public void ResolveEffect(AbilityData ability, Card caster, Slot target)
        {
            ability.DoEffects(runtime.Effects, caster, target);
            runtime.Events.RaiseAbilityTargetedSlot(ability, caster, target);
        }

        public void Complete(AbilityData ability, Card caster)
        {
            Player player = runtime.Game.GetPlayer(caster.player_id);
            if (ability.trigger == AbilityTrigger.Activate || ability.trigger == AbilityTrigger.None)
            {
                player.mana -= ability.mana_cost;
                caster.exhausted = caster.exhausted || ability.exhaust;
            }

            runtime.UpdateOngoings();
            runtime.Flow.CheckForWinner();

            if (ability.target != AbilityTarget.ChoiceSelector && runtime.Game.state != GameState.GameEnded)
            {
                foreach (AbilityData chained in ability.chain_abilities)
                {
                    if (chained != null)
                        Trigger(chained, caster);
                }
            }

            runtime.Events.RaiseAbilityEnded(ability, caster);
            runtime.ResolveQueue.ResolveAll(0.5f);
            runtime.Events.RaiseRefreshed();
        }

        private bool BeginSelector(AbilityData ability, Card caster)
        {
            if (ability.target == AbilityTarget.SelectTarget)
            {
                runtime.Selection.BeginSelectTarget(ability, caster);
                return true;
            }
            if (ability.target == AbilityTarget.CardSelector)
            {
                runtime.Selection.BeginCardSelector(ability, caster);
                return true;
            }
            if (ability.target == AbilityTarget.ChoiceSelector)
            {
                runtime.Selection.BeginChoiceSelector(ability, caster);
                return true;
            }

            return false;
        }

        private void ResolvePlayTarget(AbilityData ability, Card caster)
        {
            if (ability.target != AbilityTarget.PlayTarget)
                return;

            Slot slot = caster.slot;
            Card card = runtime.Game.GetSlotCard(slot);
            if (slot.IsPlayerSlot())
            {
                Player player = runtime.Game.GetPlayer(slot.p);
                if (ability.CanTarget(runtime.Game, caster, player))
                    ResolveEffect(ability, caster, player);
            }
            else if (card != null)
            {
                if (ability.CanTarget(runtime.Game, caster, card))
                {
                    runtime.Game.last_target = card.uid;
                    ResolveEffect(ability, caster, card);
                }
            }
            else if (ability.CanTarget(runtime.Game, caster, slot))
            {
                ResolveEffect(ability, caster, slot);
            }
        }

        private void ResolvePlayerTargets(AbilityData ability, Card caster)
        {
            List<Player> targets = ability.GetPlayerTargets(runtime.Game, caster, runtime.PlayerTargets);
            foreach (Player target in targets)
                ResolveEffect(ability, caster, target);
        }

        private void ResolveCardTargets(AbilityData ability, Card caster)
        {
            List<Card> targets = ability.GetCardTargets(runtime.Game, caster, runtime.CardTargets);
            foreach (Card target in targets)
                ResolveEffect(ability, caster, target);
        }

        private void ResolveSlotTargets(AbilityData ability, Card caster)
        {
            List<Slot> targets = ability.GetSlotTargets(runtime.Game, caster, runtime.SlotTargets);
            foreach (Slot target in targets)
                ResolveEffect(ability, caster, target);
        }

        private void ResolveCardDataTargets(AbilityData ability, Card caster)
        {
            List<CardData> targets = ability.GetCardDataTargets(runtime.Game, caster, runtime.CardDataTargets);
            foreach (CardData target in targets)
                ability.DoEffects(runtime.Effects, caster, target);
        }
    }
}
