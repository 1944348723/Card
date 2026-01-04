using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 条件：判断目标 Slot（棋盘格子）的坐标是否满足指定规则
    ///
    /// SlotValue 会分别比较：
    /// - slot.x 与某个值的关系（>, <, = , >= 等）
    /// - slot.y 与某个值的关系
    ///
    /// 举例：
    /// oper_x = GreaterOrEqual
    /// value_x = 3
    /// oper_y = Less
    /// value_y = 5
    ///
    /// 则条件相当于：
    /// slot.x >= 3  &&  slot.y < 5
    ///
    /// 主要用途：
    /// - 限制技能只能作用在某一半区
    /// - 只能选棋盘前排 / 后排
    /// - 只能选某一行 / 某一列
    /// - PVP 双方阵营相同地图时，用坐标限制更灵活
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/SlotValue", order = 11)]
    public class ConditionSlotValue : ConditionData
    {
        [Header("Slot 坐标条件")]

        // X 轴条件
        public ConditionOperatorInt oper_x;   // X 轴比较方式（大于、小于、等于等）
        public int value_x = 0;               // 与 X 比较的目标值

        // Y 轴条件
        public ConditionOperatorInt oper_y;   // Y 轴比较方式
        public int value_y = 0;               // 与 Y 比较的目标值
        
        /// <summary>
        /// 目标是“卡牌”时，
        /// 实际判断的是该卡牌所在的 Slot
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            return IsTargetConditionMet(data, ability, caster, target.slot);
        }

        /// <summary>
        /// 目标就是 Slot（格子）时
        /// 1判断 slot.x 是否符合 oper_x 与 value_x 的比较
        /// 2判断 slot.y 是否符合 oper_y 与 value_y 的比较
        /// 3两者都满足才返回 true
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Slot target)
        {
            bool valid_x = CompareInt(target.x, oper_x, value_x);
            bool valid_y = CompareInt(target.y, oper_y, value_y);
            return valid_x && valid_y;
        }
    }
}