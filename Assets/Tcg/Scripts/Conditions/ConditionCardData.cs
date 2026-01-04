using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 条件：检查目标是否是指定的卡牌
    /// 继承自 ConditionData，可以用于技能/效果触发判断
    /// </summary>
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/CardData", order = 10)]
    public class ConditionCardData : ConditionData
    {
        [Header("Card is")]
        // 要匹配的卡牌类型
        public CardData card_type;

        // 布尔比较运算符（例如 等于/不等于）
        public ConditionOperatorBool oper;

        /// <summary>
        /// 判断目标是卡牌时是否满足条件
        /// </summary>
        /// <param name="data">游戏数据上下文</param>
        /// <param name="ability">技能/效果数据</param>
        /// <param name="caster">施法者卡牌</param>
        /// <param name="target">目标卡牌</param>
        /// <returns>是否满足条件</returns>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            // 比较目标卡牌的 card_id 是否等于指定的卡牌 id，并根据 oper 判断结果
            return CompareBool(target.card_id == card_type.id, oper);
        }

        /// <summary>
        /// 判断目标是玩家时，卡牌条件不适用
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Player target)
        {
            return false; // 目标不是卡牌，条件不满足
        }

        /// <summary>
        /// 判断目标是卡槽时，卡牌条件不适用
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Slot target)
        {
            return false; // 目标不是卡牌，条件不满足
        }
    }
}