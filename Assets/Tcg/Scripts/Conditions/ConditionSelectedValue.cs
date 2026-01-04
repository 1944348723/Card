using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 条件：检查卡牌的已选择数值（如卡牌费用、能量消耗等）
    /// 可用于技能或效果触发判断
    /// </summary>
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/SelectedValue", order = 10)]
    public class ConditionSelectedValue : ConditionData
    {
        [Header("Selected Value is")]
        // 整数比较运算符（大于/小于/等于/不等于等）
        public ConditionOperatorInt oper;

        // 用于比较的选中值
        public int value;

        /// <summary>
        /// 判断技能触发条件（基于卡牌选中数值）
        /// </summary>
        public override bool IsTriggerConditionMet(Game data, AbilityData ability, Card caster)
        {
            return CompareInt(data.selected_value, oper, value);
        }
    }
}