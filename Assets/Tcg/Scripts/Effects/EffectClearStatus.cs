using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// Effect that removes a status,
    /// Will remove all status if the public field is empty
    /// 效果说明：
    /// 用来移除状态效果（Status）。
    /// 如果指定了某个 status，则只移除该状态；
    /// 如果 status 为空，则清除目标的所有状态。
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/ClearStatus", order = 10)]
    public class EffectClearStatus : EffectData
    {
        public StatusData status;   // 要移除的状态（如果为空则移除全部状态）

        // 对“玩家目标”生效
        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, Player target)
        {
            // 如果有指定具体状态 → 移除该状态
            if (status != null)
                target.RemoveStatus(status.effect);
            else
                // 否则清空玩家身上所有状态
                target.status.Clear();
        }

        // 对“卡牌目标”生效
        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, Card target)
        {
            // 如果有指定具体状态 → 移除该状态
            if (status != null)
                target.RemoveStatus(status.effect);
            else
                // 否则清空卡牌所有状态
                target.status.Clear();
        }
    }
}
