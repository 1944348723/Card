using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 条件：检查卡牌所在的牌堆类型（手牌/牌组/弃牌堆/场上/装备/秘密/临时）
    /// 继承自 ConditionData，可用于技能或效果触发判断
    /// </summary>
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/CardPile", order = 10)]
    public class ConditionCardPile : ConditionData
    {
        [Header("Card is in pile")]
        // 要匹配的牌堆类型
        public PileType type;

        // 布尔比较运算符（等于/不等于）
        public ConditionOperatorBool oper;

        /// <summary>
        /// 判断目标是卡牌时是否满足所在牌堆条件
        /// </summary>
        /// <param name="data">游戏数据上下文</param>
        /// <param name="ability">技能/效果数据</param>
        /// <param name="caster">施法者卡牌</param>
        /// <param name="target">目标卡牌</param>
        /// <returns>是否满足条件</returns>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            if (target == null)
                return false; // 目标为空，不满足条件

            // 根据指定牌堆类型检查卡牌位置
            if (type == PileType.Hand)
            {
                return CompareBool(data.IsInHand(target), oper);
            }

            if (type == PileType.Board)
            {
                return CompareBool(data.IsOnBoard(target), oper);
            }

            if (type == PileType.Equipped)
            {
                return CompareBool(data.IsEquipped(target), oper);
            }

            if (type == PileType.Deck)
            {
                return CompareBool(data.IsInDeck(target), oper);
            }

            if (type == PileType.Discard)
            {
                return CompareBool(data.IsInDiscard(target), oper);
            }

            if (type == PileType.Secret)
            {
                return CompareBool(data.IsInSecret(target), oper);
            }

            if (type == PileType.Temp)
            {
                return CompareBool(data.IsInTemp(target), oper);
            }

            return false; // 不在任何牌堆中
        }

        /// <summary>
        /// 判断目标是玩家时，不适用牌堆条件
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Player target)
        {
            return false; // 玩家无法在牌堆中
        }

        /// <summary>
        /// 判断目标是卡槽时
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Slot target)
        {
            // 卡槽总是在场上，因此只有匹配 Board 时返回 true
            return type == PileType.Board && target != Slot.None;
        }
    }
}
