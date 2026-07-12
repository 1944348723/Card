using UnityEngine;

namespace TcgEngine.Gameplay
{
    public struct DamageResult
    {
        public bool resolved;       // 请求合法，进行了结算，请求参数无效时为false
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
        public bool resolved;   // 请求合法，进行了结算，请求参数无效时为false
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

    /// <summary>伤害和治疗的基础数值结算，不负责事件与触发器。</summary>
    public sealed class HealthResolver
    {
        public DamageResult DamagePlayer(Player target, int value)
        {
            DamageResult result = new(value);

            if (target == null || value <= 0) return result;
            result.resolved = true;

            result.finalDamage = value;
            int before = target.hp;
            target.hp = Mathf.Clamp(target.hp - result.finalDamage, 0, target.hp_max);
            result.effectiveDamage = before - target.hp;
            
            return result;
        }

        public HealResult HealPlayer(Player target, int value)
        {
            HealResult result = new(value);

            if (target == null || value <= 0) return result;
            result.resolved = true;

            result.finalValue = value;
            int before = target.hp;
            target.hp = Mathf.Clamp(target.hp + result.finalValue, 0, target.hp_max);
            result.effectiveValue = target.hp - before;

            return result;
        }

        public HealResult HealCard(Card target, int value)
        {
            HealResult result = new(value);

            if (target == null || value <= 0) return result;
            result.resolved = true;

            if (target.HasStatus(StatusType.Invincibility)) return result;

            result.finalValue = value;
            int before = target.damage;
            target.damage = Mathf.Max(target.damage - result.finalValue, 0);
            result.effectiveValue = before - target.damage;

            return result;
        }

        public DamageResult DamageCard(Card target, int value, DamageType type)
        {
            DamageResult result = new(value);

            if (target == null || value <= 0) return result;
            result.resolved = true;

            if (target.HasStatus(StatusType.Invincibility))
            {
                result.immune = true;
                return result;
            }

            if (type == DamageType.Spell && target.HasStatus(StatusType.SpellImmunity)){
                result.immune = true;
                return result;
            }

            // 护盾
            if ((type == DamageType.Combat || type == DamageType.Spell) && target.HasStatus(StatusType.Shell))
            {
                target.RemoveStatus(StatusType.Shell);
                result.shieldBlocked = true;
                return result;
            }

            // 护甲减伤
            result.finalDamage = value;
            if (type == DamageType.Combat && target.HasStatus(StatusType.Armor))
            {
                result.finalDamage = Mathf.Max(value - target.GetStatusValue(StatusType.Armor), 0);
            }

            result.effectiveDamage = Mathf.Min(result.finalDamage, target.GetHP());
            target.damage += result.effectiveDamage;

            return result;
        }
    }
}
