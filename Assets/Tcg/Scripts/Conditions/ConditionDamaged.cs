using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 条件：检查卡牌或玩家是否受伤（存在伤害）
    /// 继承自 ConditionData，可用于技能或效果触发判断
    /// </summary>
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/Damaged", order = 10)]
    public class ConditionDamaged : ConditionData
    {
        [Header("Card is damaged")]
        // 布尔比较运算符（等于/不等于）
        public ConditionOperatorBool oper;

        /// <summary>
        /// 判断目标是卡牌时是否受伤
        /// </summary>
        /// <param name="data">游戏数据上下文</param>
        /// <param name="ability">技能/效果数据</param>
        /// <param name="caster">施法者卡牌</param>
        /// <param name="target">目标卡牌</param>
        /// <returns>是否满足受伤条件</returns>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            // 当卡牌伤害值大于 0 时表示受伤
            return CompareBool(target.damage > 0, oper);
        }

        /// <summary>
        /// 判断目标是玩家时是否受伤
        /// </summary>
        /// <param name="target">目标玩家</param>
        /// <returns>是否满足受伤条件</returns>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Player target)
        {
            // 当玩家当前生命小于最大生命值时表示受伤
            return CompareBool(target.hp < target.hp_max, oper);
        }

        /// <summary>
        /// 判断目标是卡槽时，受伤条件不适用
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Slot target)
        {
            return false; // 卡槽不存在生命值，不适用
        }
    }
}