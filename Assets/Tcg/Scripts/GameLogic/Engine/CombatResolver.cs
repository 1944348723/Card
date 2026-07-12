namespace TcgEngine.Gameplay
{
    /// <summary>攻击的排队、命中、伤害、反击、疲劳和重定向。</summary>
    public sealed class CombatResolver
    {
        private readonly GameRuntime runtime;

        public CombatResolver(GameRuntime runtime)
        {
            this.runtime = runtime;
        }

        public void AttackCard(Card attacker, Card target, bool skipCost)
        {
            if (!runtime.Game.CanAttackTarget(attacker, target, skipCost))
                return;

            Player player = runtime.Game.GetPlayer(attacker.player_id);
            if (!runtime.IsAiSimulation)
                player.AddHistory(GameAction.Attack, attacker, target);

            runtime.Game.last_target = target.uid;
            runtime.Engine.TriggerCardAbilityType(AbilityTrigger.OnBeforeAttack, attacker, target);
            runtime.Engine.TriggerCardAbilityType(AbilityTrigger.OnBeforeDefend, target, attacker);
            runtime.Engine.TriggerSecrets(AbilityTrigger.OnBeforeAttack, attacker);
            runtime.Engine.TriggerSecrets(AbilityTrigger.OnBeforeDefend, target);

            runtime.ResolveQueue.AddAttack(attacker, target, ResolveAttackCard, skipCost);
            runtime.ResolveQueue.ResolveAll();
        }

        public void AttackPlayer(Card attacker, Player target, bool skipCost)
        {
            if (attacker == null || target == null)
                return;
            if (!runtime.Game.CanAttackTarget(attacker, target, skipCost))
                return;

            Player player = runtime.Game.GetPlayer(attacker.player_id);
            if (!runtime.IsAiSimulation)
                player.AddHistory(GameAction.AttackPlayer, attacker, target);

            runtime.Engine.TriggerSecrets(AbilityTrigger.OnBeforeAttack, attacker);
            runtime.Engine.TriggerCardAbilityType(AbilityTrigger.OnBeforeAttack, attacker, target);

            runtime.ResolveQueue.AddAttack(attacker, target, ResolveAttackPlayer, skipCost);
            runtime.ResolveQueue.ResolveAll();
        }

        public void Exhaust(Card attacker)
        {
            bool attackedBefore = runtime.Game.cards_attacked.Contains(attacker.uid);
            runtime.Game.cards_attacked.Add(attacker.uid);
            bool canAttackAgain = attacker.HasStatus(StatusType.Fury) && !attackedBefore;
            attacker.exhausted = !canAttackAgain;
        }

        public void Redirect(Card attacker, Card newTarget)
        {
            foreach (AttackQueueElement attack in runtime.ResolveQueue.GetAttackQueue())
            {
                if (attack.attacker.uid != attacker.uid)
                    continue;

                attack.target = newTarget;
                attack.ptarget = null;
                attack.callback = ResolveAttackCard;
                attack.pcallback = null;
            }
        }

        public void Redirect(Card attacker, Player newTarget)
        {
            foreach (AttackQueueElement attack in runtime.ResolveQueue.GetAttackQueue())
            {
                if (attack.attacker.uid != attacker.uid)
                    continue;

                attack.ptarget = newTarget;
                attack.target = null;
                attack.pcallback = ResolveAttackPlayer;
                attack.callback = null;
            }
        }

        private void ResolveAttackCard(Card attacker, Card target, bool skipCost)
        {
            if (!runtime.Game.IsOnBoard(attacker) || !runtime.Game.IsOnBoard(target))
                return;

            runtime.Engine.onAttackStart?.Invoke(attacker, target);
            attacker.RemoveStatus(StatusType.Stealth);
            runtime.Engine.UpdateOngoings();

            runtime.ResolveQueue.AddAttack(attacker, target, ResolveAttackCardHit, skipCost);
            runtime.ResolveQueue.ResolveAll(0.3f);
        }

        private void ResolveAttackCardHit(Card attacker, Card target, bool skipCost)
        {
            int attackerDamage = attacker.GetAttack();
            int defenderDamage = target.GetAttack();

            runtime.Engine.DamageCard(attacker, target, attackerDamage, DamageType.Combat);
            if (!attacker.HasStatus(StatusType.Intimidate))
                runtime.Engine.DamageCard(target, attacker, defenderDamage, DamageType.Combat);

            if (!skipCost)
                Exhaust(attacker);

            runtime.Engine.UpdateOngoings();

            bool attackerOnBoard = runtime.Game.IsOnBoard(attacker);
            bool defenderOnBoard = runtime.Game.IsOnBoard(target);
            if (attackerOnBoard)
                runtime.Engine.TriggerCardAbilityType(AbilityTrigger.OnAfterAttack, attacker, target);
            if (defenderOnBoard)
                runtime.Engine.TriggerCardAbilityType(AbilityTrigger.OnAfterDefend, target, attacker);
            if (attackerOnBoard)
                runtime.Engine.TriggerSecrets(AbilityTrigger.OnAfterAttack, attacker);
            if (defenderOnBoard)
                runtime.Engine.TriggerSecrets(AbilityTrigger.OnAfterDefend, target);

            runtime.Engine.onAttackEnd?.Invoke(attacker, target);
            runtime.Engine.RefreshData();
            runtime.Flow.CheckForWinner();
            runtime.ResolveQueue.ResolveAll(0.2f);
        }

        private void ResolveAttackPlayer(Card attacker, Player target, bool skipCost)
        {
            if (!runtime.Game.IsOnBoard(attacker))
                return;

            runtime.Engine.onAttackPlayerStart?.Invoke(attacker, target);
            attacker.RemoveStatus(StatusType.Stealth);
            runtime.Engine.UpdateOngoings();

            runtime.ResolveQueue.AddAttack(attacker, target, ResolveAttackPlayerHit, skipCost);
            runtime.ResolveQueue.ResolveAll(0.3f);
        }

        private void ResolveAttackPlayerHit(Card attacker, Player target, bool skipCost)
        {
            runtime.Engine.DamagePlayer(attacker, target, attacker.GetAttack(), DamageType.Combat);
            if (!skipCost)
                Exhaust(attacker);

            runtime.Engine.UpdateOngoings();
            if (runtime.Game.IsOnBoard(attacker))
                runtime.Engine.TriggerCardAbilityType(AbilityTrigger.OnAfterAttack, attacker, target);
            runtime.Engine.TriggerSecrets(AbilityTrigger.OnAfterAttack, attacker);

            runtime.Engine.onAttackPlayerEnd?.Invoke(attacker, target);
            runtime.Engine.RefreshData();
            runtime.Flow.CheckForWinner();
            runtime.ResolveQueue.ResolveAll(0.2f);
        }
    }
}
