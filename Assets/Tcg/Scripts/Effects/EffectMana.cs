using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 效果说明：
    /// 增加或减少玩家的法力值（Mana）。
    /// - increase_value：是否增加当前法力值
    /// - increase_max：是否增加最大法力值
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Mana", order = 10)]
    public class EffectMana : EffectData
    {
        public bool increase_value;  // 是否增加当前法力值
        public bool increase_max;    // 是否增加最大法力值

        // 对玩家执行效果
        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, Player target)
        {
            // 增加最大法力值
            if (increase_max)
            {
                target.mana_max += ability.value;

                // 限制最大法力值在 0 ~ 系统允许的最大值之间
                target.mana_max = Mathf.Clamp(target.mana_max, 0, GameplayData.Get().mana_max);
            }
            
            // 增加当前法力值
            if (increase_value)
            {
                target.mana += ability.value;

                // 当前法力值不能低于 0
                target.mana = Mathf.Max(target.mana, 0);
            }
        }

    }
}
