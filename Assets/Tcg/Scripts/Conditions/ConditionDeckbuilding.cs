using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 条件：检查卡牌是否为可用于构筑牌组的卡牌（排除召唤生成的Token等）
    /// 继承自 ConditionData，可用于技能或效果触发判断
    /// </summary>
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/CardDeckbuilding", order = 10)]
    public class ConditionDeckbuilding : ConditionData
    {
        [Header("Card is Deckbuilding")]
        // 布尔比较运算符（等于/不等于）
        public ConditionOperatorBool oper;

        /// <summary>
        /// 判断目标是卡牌实例时是否为可构筑卡
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            // 根据卡牌数据中的 deckbuilding 字段判断
            return CompareBool(target.CardData.deckbuilding, oper);
        }

        /// <summary>
        /// 判断目标是卡牌数据（CardData）时是否为可构筑卡
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, CardData target)
        {
            // 根据卡牌数据中的 deckbuilding 字段判断
            return CompareBool(target.deckbuilding, oper);
        }
    }
}