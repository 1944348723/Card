using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 判断当前是否为你的回合
    /// </summary>
    
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/Turn", order = 10)]
    public class ConditionTurn : ConditionData
    {
        public ConditionOperatorBool oper;   // 布尔条件运算符（用来判断 true / false 是否符合条件）

        // 判断触发条件是否满足（基于当前是否为施法者所在玩家的回合）
        public override bool IsTriggerConditionMet(Game data, AbilityData ability, Card caster)
        {
            bool yourturn = caster.player_id == data.current_player;  // 判断当前回合是否属于该卡牌所属玩家
            return CompareBool(yourturn, oper);  // 用布尔比较方法进行结果判断
        }
    }
}