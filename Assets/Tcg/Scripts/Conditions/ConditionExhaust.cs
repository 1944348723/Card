using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 条件：检查卡牌是否已疲劳（exhausted）
    /// 继承自 ConditionData，可用于技能或效果触发判断
    /// </summary>
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/CardExhausted", order = 10)]
    public class ConditionExhaust : ConditionData
    {
        [Header("Target is exhausted")]
        // 布尔比较运算符（等于/不等于）
        public ConditionOperatorBool oper;

        /// <summary>
        /// 判断目标是卡牌实例时是否疲劳
        /// </summary>
        /// <param name="data">游戏数据上下文</param>
        /// <param name="ability">技能/效果数据</param>
        /// <param name="caster">施法者卡牌</param>
        /// <param name="target">目标卡牌</param>
        /// <returns>是否满足疲劳条件</returns>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            // 根据卡牌的 exhausted 字段判断是否疲劳
            return CompareBool(target.exhausted, oper);
        }

        /// <summary>
        /// 判断目标是玩家时，不适用疲劳条件
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Player target)
        {
            return CompareBool(false, oper); // 玩家不疲劳
        }

        /// <summary>
        /// 判断目标是卡槽时，不适用疲劳条件
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Slot target)
        {
            return CompareBool(false, oper); // 卡槽不疲劳
        }
    }
}