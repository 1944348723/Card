using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 条件：检查卡牌到目标 Slot（格子）之间是否在“坐标轴范围”内
    ///
    /// SlotRange：分别检查 X / Y / P 三个轴的距离是否在允许范围内
    /// - 逐轴独立判断
    /// - 适合棋盘式、方格式、矩阵式布局
    ///
    /// 如果你想判断“真实移动距离”（整体路径距离），
    /// 而不是分开判断轴，可以使用 ConditionSlotDist。
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/SlotRange", order = 11)]
    public class ConditionSlotRange : ConditionData
    {
        [Header("格子范围限制")]
        public int range_x = 1;   // X 方向允许的最大距离
        public int range_y = 1;   // Y 方向允许的最大距离
        public int range_p = 0;   // P 方向（通常表示玩家阵营 / 层级）的最大距离
        
        /// <summary>
        /// 如果目标是卡牌，则取该卡牌所在 Slot 再继续判断
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            return IsTargetConditionMet(data, ability, caster, target.slot);
        }

        /// <summary>
        /// 如果目标是 Slot（格子），则直接检查：
        /// 1计算目标与施法者 Slot 的 X/Y/P 绝对距离
        /// 2判断是否分别 <= 允许范围
        ///
        /// 举例：
        /// range_x = 2, range_y = 1
        /// 那么只允许：
        /// |dx| <= 2 且 |dy| <= 1
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Slot target)
        { 
            Slot cslot = caster.slot;
            int dist_x = Mathf.Abs(cslot.x - target.x);
            int dist_y = Mathf.Abs(cslot.y - target.y);
            int dist_p = Mathf.Abs(cslot.p - target.p);

            return dist_x <= range_x 
                   && dist_y <= range_y 
                   && dist_p <= range_p;
        }
    }
}