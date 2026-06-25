using UnityEngine;

namespace TcgEngine.Gameplay
{
    public class HealthSystem
    {
        public int DamagePlayer(Player target, int value)
        {
            if (target == null || value <= 0) return 0;

            int before = target.hp;
            target.hp = Mathf.Clamp(target.hp - value, 0, target.hp_max);
            return before - target.hp;
        }

        public int HealPlayer(Player target, int value)
        {
            if (target == null || value <= 0) return 0;

            int before = target.hp;
            target.hp = Mathf.Clamp(target.hp + value, 0, target.hp_max);
            return target.hp - before;
        }
    }
}