using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    // 检查一名玩家或一张卡牌是否拥有某种状态效果（Status）
    
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/CardStatus", order = 10)]
    public class ConditionStatus : ConditionData
    {
        [Header("卡牌/玩家是否拥有某个状态")]
        public StatusType has_status;        // 需要检查的状态类型（如：中毒、冻结、护盾等，取决于游戏设计）
        public int value = 0;                // 要求该状态的数值至少达到多少（如果状态是可叠层或有强度）
        public ConditionOperatorBool oper;   // 布尔比较方式（要求 true / false）

        /// <summary>
        /// 目标是 Card（卡牌单位）时
        /// 条件成立必须满足：
        /// 1卡牌拥有该状态
        /// 2状态数值 >= 配置值
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            bool hstatus = target.HasStatus(has_status) 
                           && target.GetStatusValue(has_status) >= value;

            return CompareBool(hstatus, oper);
        }

        /// <summary>
        /// 目标是 Player（玩家）时
        /// 判断逻辑同卡牌：
        /// 1玩家拥有该状态
        /// 2状态强度 >= value
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Player target)
        {
            bool hstatus = target.HasStatus(has_status) 
                           && target.GetStatusValue(has_status) >= value;

            return CompareBool(hstatus, oper);
        }

        /// <summary>
        /// 目标是 Slot（棋盘/格子）时：
        /// 先检查该格子是否有卡牌
        /// 如果有 → 转为检查该卡牌
        /// 如果没有 → 条件失败（返回 false）
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Slot target)
        {
            Card card = data.GetSlotCard(target);
            if (card != null)
                return IsTargetConditionMet(data, ability, caster, card);

            return false;
        }
    }
}