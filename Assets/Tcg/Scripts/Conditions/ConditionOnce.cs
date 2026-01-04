using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 条件：限制技能每回合只能释放一次
    /// 继承自 ConditionData，可用于技能触发条件
    /// </summary>
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/OncePerTurn", order = 10)]
    public class ConditionOnce : ConditionData
    {
        /// <summary>
        /// 判断技能是否可以触发
        /// </summary>
        /// <param name="data">游戏数据上下文</param>
        /// <param name="ability">技能数据</param>
        /// <param name="caster">施法者卡牌</param>
        /// <returns>如果技能本回合尚未释放，返回 true；否则返回 false</returns>
        public override bool IsTriggerConditionMet(Game data, AbilityData ability, Card caster)
        {
            // 如果本回合 ability_played 列表中没有当前技能 ID，则可触发
            return !data.ability_played.Contains(ability.id);
        }
    }
}