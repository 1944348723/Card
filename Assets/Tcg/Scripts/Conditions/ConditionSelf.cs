using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 条件类：判断目标是否是施法者自身
    /// </summary>
    
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/CardSelf", order = 10)]
    public class ConditionSelf : ConditionData
    {
        [Header("目标是否为施法者自身")]
        public ConditionOperatorBool oper;   // 用于比较布尔条件的操作符（等于 / 不等于）

        /// <summary>
        /// 判断目标卡牌是否就是施法者卡牌
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            // caster == target 表示是否是同一张卡
            return CompareBool(caster == target, oper);
        }

        /// <summary>
        /// 判断目标玩家是否与施法者属于同一玩家
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Player target)
        {
            // 通过 player_id 判断是否为同一玩家
            bool same_owner = caster.player_id == target.player_id;
            return CompareBool(same_owner, oper);
        }

        /// <summary>
        /// 判断目标格子是否是施法者所在的格子
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Slot target)
        {
            // 判断卡牌绑定的 slot 是否就是目标 slot
            return CompareBool(caster.slot == target, oper);
        }
    }
}