namespace TcgEngine.Gameplay
{
    /// <summary>伤害、治疗及其战斗附带规则。</summary>
    public sealed class DamageResolver
    {
        private readonly GameRuntime runtime;

        public DamageResolver(GameRuntime runtime)
        {
            this.runtime = runtime;
        }

        public void DamagePlayer(Card attacker, Player target, int value, DamageType type)
        {
            if (attacker == null || target == null || value <= 0)
                return;

            DamageResult result = runtime.Health.DamagePlayer(target, value);
            if (!result.resolved)
                return;

            if (type == DamageType.Combat && attacker.HasStatus(StatusType.LifeSteal))
                HealPlayer(runtime.Game.GetPlayer(attacker.player_id), result.effectiveDamage);

            runtime.Engine.onPlayerDamaged?.Invoke(target, result.finalDamage);
        }

        public void DamagePlayer(Player target, int value, DamageType type)
        {
            if (target == null || value <= 0)
                return;

            DamageResult result = runtime.Health.DamagePlayer(target, value);
            if (result.resolved)
                runtime.Engine.onPlayerDamaged?.Invoke(target, result.finalDamage);
        }

        public void HealPlayer(Player target, int value)
        {
            HealResult result = runtime.Health.HealPlayer(target, value);
            if (result.resolved)
                runtime.Engine.onPlayerHealed?.Invoke(target, result.finalValue);
        }

        public void HealCard(Card target, int value)
        {
            HealResult result = runtime.Health.HealCard(target, value);
            if (result.resolved)
                runtime.Engine.onCardHealed?.Invoke(target, result.finalValue);
        }

        public void DamageCard(Card attacker, Card target, int value, DamageType type)
        {
            if (attacker == null || target == null || value <= 0)
                return;

            DamageResult result = runtime.Health.DamageCard(target, value, type);
            if (!result.resolved)
                return;

            bool isCombat = type == DamageType.Combat;
            if (result.finalDamage > 0)
            {
                if (type != DamageType.Status)
                    target.RemoveStatus(StatusType.Sleep);

                Player targetOwner = runtime.Game.GetPlayer(target.player_id);
                if (isCombat && result.excessDamage > 0 && attacker.HasStatus(StatusType.Trample))
                    DamagePlayer(attacker, targetOwner, result.excessDamage, DamageType.Combat);

                if (isCombat && attacker.HasStatus(StatusType.LifeSteal))
                    HealPlayer(runtime.Game.GetPlayer(attacker.player_id), result.effectiveDamage);
            }

            runtime.Engine.onCardDamaged?.Invoke(target, result.finalDamage);
            if (target.GetHP() <= 0)
                runtime.Cards.Kill(attacker, target);
            else if (result.effectiveDamage > 0
                && isCombat
                && attacker.HasStatus(StatusType.Deathtouch)
                && target.CardData.type == CardType.Character)
            {
                runtime.Cards.Kill(attacker, target);
            }
        }

        public void DamageCard(Card target, int value, DamageType type)
        {
            if (target == null || value <= 0)
                return;

            DamageResult result = runtime.Health.DamageCard(target, value, type);
            if (!result.resolved)
                return;

            if (result.finalDamage > 0 && type != DamageType.Status)
                target.RemoveStatus(StatusType.Sleep);

            runtime.Engine.onCardDamaged?.Invoke(target, result.finalDamage);
            if (target.GetHP() <= 0)
                runtime.Cards.Discard(target);
        }
    }
}
