using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 比较卡牌或玩家的自定义属性（Trait）
    /// </summary>
    
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/StatCustom", order = 10)]
    public class ConditionTrait : ConditionData
    {
        [Header("卡牌属性判断")]
        public TraitData trait;              // 要检测的自定义属性（Trait）
        public ConditionOperatorInt oper;    // 比较运算符（大于、小于、等于等）
        public int value;                    // 用来比较的目标数值

        // 判断目标“卡牌”是否满足条件
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            // 获取卡牌对应属性值，并与指定数值进行比较
            return CompareInt(target.GetTraitValue(trait.id), oper, value);
        }

        // 判断目标“玩家”是否满足条件
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Player target)
        {
            // 获取玩家对应属性值，并与指定数值进行比较
            return CompareInt(target.GetTraitValue(trait.id), oper, value);
        }
    }
}