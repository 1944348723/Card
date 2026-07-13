using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 效果说明：
    /// 为卡牌或玩家恢复生命值（HP）。
    /// - 恢复量不会超过原始最大生命值；
    /// - 如果需要超过最大生命值，请使用 AddStats 效果。
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Heal", order = 10)]
    public class EffectHeal : EffectData
    {
        // 对玩家执行治疗
        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, Player target)
        {
            logic.HealPlayer(target, ability.value);  // 恢复目标玩家 HP
        }

        // 对卡牌执行治疗
        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, Card target)
        {
            logic.HealCard(target, ability.value);    // 恢复目标卡牌 HP
        }

    }
}
