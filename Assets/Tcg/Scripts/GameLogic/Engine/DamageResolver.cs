using UnityEngine;

namespace TcgEngine.Gameplay
{
    public struct DamageResult
    {
        public bool resolved;
        public int damage;
        public int finalDamage;
        public int effectiveDamage;
        public int excessDamage => finalDamage - effectiveDamage;
        public bool immune;
        public bool shieldBlocked;

        public DamageResult(int damage)
        {
            this.damage = damage;
            resolved = false;
            finalDamage = 0;
            effectiveDamage = 0;
            immune = false;
            shieldBlocked = false;
        }
    }

    public struct HealResult
    {
        public bool resolved;
        public int value;
        public int finalValue;
        public int effectiveValue;

        public HealResult(int value)
        {
            this.value = value;
            resolved = false;
            finalValue = 0;
            effectiveValue = 0;
        }
    }

    /// <summary>
    /// 伤害和治疗的唯一结算入口，负责数值修改、伤害关键词、死亡处理与事件通知。
    /// </summary>
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

            DamageResult result = ApplyPlayerDamage(target, value);
            if (!result.resolved)
                return;

            if (type == DamageType.Combat && attacker.HasStatus(StatusType.LifeSteal))
                HealPlayer(runtime.Game.GetPlayer(attacker.player_id), result.effectiveDamage);

            runtime.Events.RaisePlayerDamaged(target, result.finalDamage);
        }

        public void DamagePlayer(Player target, int value, DamageType type)
        {
            if (target == null || value <= 0)
                return;

            DamageResult result = ApplyPlayerDamage(target, value);
            if (result.resolved)
                runtime.Events.RaisePlayerDamaged(target, result.finalDamage);
        }

        public void HealPlayer(Player target, int value)
        {
            HealResult result = ApplyPlayerHealing(target, value);
            if (result.resolved)
                runtime.Events.RaisePlayerHealed(target, result.finalValue);
        }

        public void HealCard(Card target, int value)
        {
            HealResult result = ApplyCardHealing(target, value);
            if (result.resolved)
                runtime.Events.RaiseCardHealed(target, result.finalValue);
        }

        public void DamageCard(Card attacker, Card target, int value, DamageType type)
        {
            if (attacker == null || target == null || value <= 0)
                return;

            DamageResult result = ApplyCardDamage(target, value, type);
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

            runtime.Events.RaiseCardDamaged(target, result.finalDamage);
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

            DamageResult result = ApplyCardDamage(target, value, type);
            if (!result.resolved)
                return;

            if (result.finalDamage > 0 && type != DamageType.Status)
                target.RemoveStatus(StatusType.Sleep);

            runtime.Events.RaiseCardDamaged(target, result.finalDamage);
            if (target.GetHP() <= 0)
                runtime.Cards.Discard(target);
        }

        private static DamageResult ApplyPlayerDamage(Player target, int value)
        {
            DamageResult result = new(value);
            if (target == null || value <= 0)
                return result;

            result.resolved = true;
            result.finalDamage = value;
            int before = target.hp;
            target.hp = Mathf.Clamp(target.hp - result.finalDamage, 0, target.hp_max);
            result.effectiveDamage = before - target.hp;
            return result;
        }

        private static HealResult ApplyPlayerHealing(Player target, int value)
        {
            HealResult result = new(value);
            if (target == null || value <= 0)
                return result;

            result.resolved = true;
            result.finalValue = value;
            int before = target.hp;
            target.hp = Mathf.Clamp(target.hp + result.finalValue, 0, target.hp_max);
            result.effectiveValue = target.hp - before;
            return result;
        }

        private static HealResult ApplyCardHealing(Card target, int value)
        {
            HealResult result = new(value);
            if (target == null || value <= 0)
                return result;

            result.resolved = true;
            if (target.HasStatus(StatusType.Invincibility))
                return result;

            result.finalValue = value;
            int before = target.damage;
            target.damage = Mathf.Max(target.damage - result.finalValue, 0);
            result.effectiveValue = before - target.damage;
            return result;
        }

        private static DamageResult ApplyCardDamage(Card target, int value, DamageType type)
        {
            DamageResult result = new(value);
            if (target == null || value <= 0)
                return result;

            result.resolved = true;
            if (target.HasStatus(StatusType.Invincibility))
            {
                result.immune = true;
                return result;
            }

            if (type == DamageType.Spell && target.HasStatus(StatusType.SpellImmunity))
            {
                result.immune = true;
                return result;
            }

            if ((type == DamageType.Combat || type == DamageType.Spell) && target.HasStatus(StatusType.Shell))
            {
                target.RemoveStatus(StatusType.Shell);
                result.shieldBlocked = true;
                return result;
            }

            result.finalDamage = value;
            if (type == DamageType.Combat && target.HasStatus(StatusType.Armor))
                result.finalDamage = Mathf.Max(value - target.GetStatusValue(StatusType.Armor), 0);

            result.effectiveDamage = Mathf.Min(result.finalDamage, target.GetHP());
            target.damage += result.effectiveDamage;
            return result;
        }
    }
}
