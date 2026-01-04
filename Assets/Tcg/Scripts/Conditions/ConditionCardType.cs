using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 条件：检查卡牌的类型、所属队伍和特性
    /// 继承自 ConditionData，可用于技能或效果触发判断
    /// </summary>
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/CardType", order = 10)]
    public class ConditionCardType : ConditionData
    {
        [Header("Card is of type")]
        // 要匹配的卡牌类型
        public CardType has_type;

        // 要匹配的队伍
        public TeamData has_team;

        // 要匹配的特性
        public TraitData has_trait;

        // 布尔比较运算符（等于/不等于）
        public ConditionOperatorBool oper;

        /// <summary>
        /// 判断目标是卡牌实例时是否满足条件
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            // 调用私有方法判断目标卡牌是否符合类型/队伍/特性
            return CompareBool(IsTrait(target), oper);
        }

        /// <summary>
        /// 判断目标是玩家时，条件不适用
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Player target)
        {
            return false; // 目标不是卡牌
        }

        /// <summary>
        /// 判断目标是卡槽时，条件不适用
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Slot target)
        {
            return false; // 目标不是卡牌
        }

        /// <summary>
        /// 判断目标是卡牌数据（CardData）时是否满足类型/队伍/特性
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, CardData target)
        {
            bool is_type = target.type == has_type || has_type == CardType.None;      // 类型匹配或不限制
            bool is_team = target.team == has_team || has_team == null;                // 队伍匹配或不限制
            bool is_trait = target.HasTrait(has_trait) || has_trait == null;          // 特性匹配或不限制
            return (is_type && is_team && is_trait);                                   // 全部匹配返回 true
        }

        /// <summary>
        /// 私有方法：检查卡牌实例是否符合类型、队伍和特性
        /// </summary>
        private bool IsTrait(Card card)
        {
            bool is_type = card.CardData.type == has_type || has_type == CardType.None;
            bool is_team = card.CardData.team == has_team || has_team == null;
            bool is_trait = card.HasTrait(has_trait) || has_trait == null;
            return (is_type && is_team && is_trait);
        }
    }
}
