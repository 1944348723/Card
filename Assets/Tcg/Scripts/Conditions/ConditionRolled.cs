using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 条件：检查当前掷骰/随机值是否满足指定条件
    /// 可用于触发技能或效果（例如基于掷骰结果触发）
    /// </summary>
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/RolledValue", order = 10)]
    public class ConditionRolled : ConditionData
    {
        [Header("Value Rolled is")]
        // 整数比较运算符（大于/小于/等于/不等于等）
        public ConditionOperatorInt oper;

        // 用于比较的掷骰值
        public int value;

        /// <summary>
        /// 判断技能触发条件（基于掷骰值）
        /// </summary>
        public override bool IsTriggerConditionMet(Game data, AbilityData ability, Card caster)
        {
            return CompareInt(data.rolled_value, oper, value);
        }

        /// <summary>
        /// 当目标是玩家时，判断掷骰值是否满足条件
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Player target)
        {
            return CompareInt(data.rolled_value, oper, value);
        }

        /// <summary>
        /// 当目标是卡牌时，判断掷骰值是否满足条件
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            return CompareInt(data.rolled_value, oper, value);
        }

        /// <summary>
        /// 当目标是卡槽时，判断掷骰值是否满足条件
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Slot target)
        {
            return CompareInt(data.rolled_value, oper, value);
        }
    }
}