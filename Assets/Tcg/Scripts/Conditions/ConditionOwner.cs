using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 条件：检查目标的拥有者是否与施法者的拥有者相同
    /// 继承自 ConditionData，可用于技能或效果触发判断
    /// </summary>
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/CardOwner", order = 10)]
    public class ConditionOwner : ConditionData
    {
        [Header("Target owner is caster owner")]
        // 布尔比较运算符（等于/不等于）
        public ConditionOperatorBool oper;

        /// <summary>
        /// 判断目标是卡牌实例时，是否与施法者同属一个玩家
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            bool same_owner = caster.player_id == target.player_id;
            return CompareBool(same_owner, oper);
        }

        /// <summary>
        /// 判断目标是玩家时，是否与施法者同属一个玩家（一般用于自我或队友判断）
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Player target)
        {
            bool same_owner = caster.player_id == target.player_id;
            return CompareBool(same_owner, oper);
        }

        /// <summary>
        /// 判断目标是卡槽时，是否属于施法者同一个玩家
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Slot target)
        {
            bool same_owner = target.BelongsToPlayer(caster.player_id);
            return CompareBool(same_owner, oper);
        }
    }
}