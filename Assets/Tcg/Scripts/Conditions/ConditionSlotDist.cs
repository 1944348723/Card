using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 条件：判断目标与施法者之间的“格子距离”
    /// SlotDist 表示从施法者到目标的实际移动距离
    /// 不同于 SlotRange（分别判断 X / Y / 队伍等独立维度）
    /// SlotDist 是一个真正的空间距离概念
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/SlotDist", order = 11)]
    public class ConditionSlotDist : ConditionData
    {
        [Header("格子距离判断")]
        public int distance = 1;     // 最大允许的格子距离
        public bool diagonals;       // 是否允许斜向 (对角线) 计算距离

        /// <summary>
        /// 如果目标是卡牌，则转成卡牌所在 Slot 再进行判断
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            return IsTargetConditionMet(data, ability, caster, target.slot);
        }

        /// <summary>
        /// 判断目标格子与施法者格子的距离是否在允许范围内
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Slot target)
        {
            Slot cslot = caster.slot;

            // 允许对角线，则使用更宽松的距离算法
            if (diagonals)
                return cslot.IsInDistance(target, distance);

            // 仅直线（上下左右），不允许斜方向
            return cslot.IsInDistanceStraight(target, distance);
        }
    }
}