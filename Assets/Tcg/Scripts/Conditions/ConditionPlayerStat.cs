using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 条件：比较玩家的基本属性（如生命值 HP、法力值 Mana）
    /// 继承自 ConditionData，可用于技能或效果触发判断
    /// </summary>
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/PlayerStat", order = 10)]
    public class ConditionPlayerStat : ConditionData
    {
        [Header("Card stat is")]
        // 需要比较的属性类型（HP 或 Mana）
        public ConditionStatType type;

        // 整数比较运算符（大于/小于/等于/不等于等）
        public ConditionOperatorInt oper;

        // 比较的数值
        public int value;

        /// <summary>
        /// 当目标是卡牌时，根据其所属玩家判断属性条件
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            Player ptarget = data.GetPlayer(target.player_id);
            return IsTargetConditionMet(data, ability, caster, ptarget);
        }

        /// <summary>
        /// 当目标是玩家时，判断指定属性是否满足条件
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Player target)
        {
            if (type == ConditionStatType.HP)
            {
                // 比较玩家生命值
                return CompareInt(target.hp, oper, value);
            }

            if (type == ConditionStatType.Mana)
            {
                // 比较玩家法力值
                return CompareInt(target.mana, oper, value);
            }

            return false;
        }
    }
}