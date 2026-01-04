using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 条件：仅由 AI 检查
    /// 防止 AI 在施放法术时错误地作用于自己，但允许真实玩家自由选择目标
    /// 继承自 ConditionData，可用于技能或效果触发判断
    /// </summary>
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/CardOwnerAI", order = 10)]
    public class ConditionOwnerAI : ConditionData
    {
        [Header("AI Only: Target owner is caster owner")]
        // 布尔比较运算符（等于/不等于）
        public ConditionOperatorBool oper;

        /// <summary>
        /// 判断目标是卡牌实例时是否与施法者同属一个玩家（仅 AI 检查）
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            if (!IsAIPlayer(data, caster))
                return true; // 对人类玩家始终返回 true

            bool same_owner = caster.player_id == target.player_id;
            return CompareBool(same_owner, oper);
        }

        /// <summary>
        /// 判断目标是玩家时是否与施法者同属一个玩家（仅 AI 检查）
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Player target)
        {
            if (!IsAIPlayer(data, caster))
                return true; // 对人类玩家始终返回 true

            bool same_owner = caster.player_id == target.player_id;
            return CompareBool(same_owner, oper);
        }

        /// <summary>
        /// 判断目标是卡槽时是否属于施法者同一个玩家（仅 AI 检查）
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Slot target)
        {
            if (!IsAIPlayer(data, caster))
                return true; // 对人类玩家始终返回 true

            bool same_owner = Slot.GetP(caster.player_id) == target.p;
            return CompareBool(same_owner, oper);
        }

        /// <summary>
        /// 判断施法者是否为 AI 玩家
        /// </summary>
        private bool IsAIPlayer(Game data, Card caster)
        {
            Player player = data.GetPlayer(caster.player_id);
            return player.is_ai;
        }
    }
}
