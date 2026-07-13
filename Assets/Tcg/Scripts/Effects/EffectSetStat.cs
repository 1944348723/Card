using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 效果说明：
    /// 将目标卡牌或玩家的基础属性（生命值 HP、攻击力 Attack、法力值 Mana）设置为指定值。
    /// - type：指定要修改的属性类型
    /// - ability.value：设置的具体数值
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/SetStat", order = 10)]
    public class EffectSetStat : EffectData
    {
        public EffectStatType type;  // 要设置的属性类型（HP/Attack/Mana）

        // 对玩家目标执行效果
        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, Player target)
        {
            if (type == EffectStatType.HP)
            {
                target.hp = ability.value; // 设置玩家生命值
            }

            if (type == EffectStatType.Mana)
            {
                target.mana = ability.value;          // 设置玩家法力值
                target.mana = Mathf.Max(target.mana, 0); // 法力值不能低于 0
            }
        }

        // 对卡牌目标执行效果
        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, Card target)
        {
            if (type == EffectStatType.Attack)
                target.attack = ability.value; // 设置卡牌攻击力

            if (type == EffectStatType.Mana)
                target.mana = ability.value;   // 设置卡牌法力值

            if (type == EffectStatType.HP)
            {
                target.hp = ability.value;     // 设置卡牌生命值
                target.damage = 0;             // 重置卡牌伤害
            }
        }

        // 对卡牌执行持续效果（Ongoing）
        public override void DoOngoingEffect(EffectContext logic, AbilityData ability, Card caster, Card target)
        {
            if (type == EffectStatType.Attack)
                target.attack = ability.value; // 设置攻击力

            if (type == EffectStatType.HP)
                target.hp = ability.value;     // 设置生命值

            if (type == EffectStatType.Mana)
                target.mana = ability.value;   // 设置法力值
        }

    }
}
