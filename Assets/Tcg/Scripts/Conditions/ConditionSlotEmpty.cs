using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 条件：判断一个 Slot（格子）是否为空
    /// 用于能力、召唤、移动等逻辑中
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/SlotEmpty", order = 11)]
    public class ConditionSlotEmpty : ConditionData
    {
        [Header("目标格子是否为空")]
        public ConditionOperatorBool oper;   // 条件运算符（等于 / 不等于）

        /// <summary>
        /// 如果目标是卡牌，那一定不是“空格子”
        /// 所以无论 oper 判断什么，都传入 false
        /// （意思是：目标不是空格子）
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            return CompareBool(false, oper); // 卡牌目标不可能为空 Slot
        }

        /// <summary>
        /// 如果目标是玩家，也同样不是“格子”
        /// 所以仍然固定 false
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Player target)
        {
            return CompareBool(false, oper); // 玩家目标不可能为空 Slot
        }

        /// <summary>
        /// 如果目标是 Slot（格子），则真正进行判断
        /// 通过 Game 数据系统获取该 Slot 上是否存在卡牌
        /// - 若返回 null，则表示格子为空
        /// - 若不为 null，则表示格子已被占用
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Slot target)
        { 
            Card slot_card = data.GetSlotCard(target);  
            return CompareBool(slot_card == null, oper);
        }
    }
}