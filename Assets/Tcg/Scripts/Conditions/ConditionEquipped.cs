using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 条件：检查卡牌是否装备了装备卡
    /// 继承自 ConditionData，可用于技能或效果触发判断
    /// </summary>
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/CardEquipped", order = 10)]
    public class ConditionEquipped : ConditionData
    {
        [Header("Target is equipped")]
        // 布尔比较运算符（等于/不等于）
        public ConditionOperatorBool oper;

        /// <summary>
        /// 判断目标是卡牌实例时是否已装备装备卡
        /// </summary>
        /// <param name="data">游戏数据上下文</param>
        /// <param name="ability">技能/效果数据</param>
        /// <param name="caster">施法者卡牌</param>
        /// <param name="target">目标卡牌</param>
        /// <returns>是否满足装备条件</returns>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            // 当 equipped_uid 不为空表示卡牌已装备
            return CompareBool(target.equipped_uid != null, oper);
        }

        /// <summary>
        /// 判断目标是玩家时，不适用装备条件
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Player target)
        {
            return false; // 玩家无法装备
        }

        /// <summary>
        /// 判断目标是卡槽时，不适用装备条件
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Slot target)
        {
            return false; // 卡槽不适用
        }
    }
}