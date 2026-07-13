using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 效果说明：
    /// 使目标卡牌进入“已用（Exhausted）”或“未用（Unexhausted）”状态。
    /// - 已用状态：卡牌无法执行行动（例如攻击或使用技能）。
    /// - 未用状态：卡牌可以再次执行行动。
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Exhaust", order = 10)]
    public class EffectExhaust : EffectData
    {
        public bool exhausted;  // 设置卡牌是否进入已用状态（true = 已用，false = 解除已用）

        // 对卡牌执行效果
        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, Card target)
        {
            target.exhausted = exhausted;  // 改变目标卡牌的已用状态
        }

    }
}
