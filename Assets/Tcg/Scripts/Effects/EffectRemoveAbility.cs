using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 效果说明：
    /// 从目标卡牌上移除指定的技能/能力（Ability）。
    /// - remove_ability：要移除的技能数据。
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/RemoveAbility", order = 10)]
    public class EffectRemoveAbility : EffectData
    {
        public AbilityData remove_ability;  // 要移除的技能/能力

        // 对卡牌执行效果
        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, Card target)
        {
            target.RemoveAbility(remove_ability); // 从卡牌上移除指定技能
        }
    }
}
