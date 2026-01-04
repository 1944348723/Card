using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 所有能力条件的基类
    /// 子类可重写 IsConditionMet / IsTargetConditionMet 方法定义触发和目标条件
    /// </summary>
    public class ConditionData : ScriptableObject
    {
        /// <summary>
        /// 检查能力的触发条件是否满足（对任意目标）
        /// </summary>
        /// <param name="data">游戏数据</param>
        /// <param name="ability">能力数据</param>
        /// <param name="caster">施放卡牌</param>
        /// <returns>条件是否满足</returns>
        public virtual bool IsTriggerConditionMet(Game data, AbilityData ability, Card caster)
        {
            return true; // 默认返回true，可被子类重写
        }

        /// <summary>
        /// 检查卡牌目标条件是否满足
        /// </summary>
        public virtual bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            return true; // 默认返回true，可被子类重写
        }

        /// <summary>
        /// 检查玩家目标条件是否满足
        /// </summary>
        public virtual bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Player target)
        {
            return true; // 默认返回true，可被子类重写
        }

        /// <summary>
        /// 检查槽位目标条件是否满足
        /// </summary>
        public virtual bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Slot target)
        {
            return true; // 默认返回true，可被子类重写
        }

        /// <summary>
        /// 检查CardData目标条件是否满足（用于生成卡牌的效果）
        /// </summary>
        public virtual bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, CardData target)
        {
            return true; // 默认返回true，可被子类重写
        }

        /// <summary>
        /// 布尔值比较函数
        /// </summary>
        /// <param name="condition">要比较的布尔值</param>
        /// <param name="oper">比较操作符</param>
        /// <returns>比较结果</returns>
        public bool CompareBool(bool condition, ConditionOperatorBool oper)
        {
            if (oper == ConditionOperatorBool.IsFalse)
                return !condition; // 取反
            return condition;     // IsTrue 返回原值
        }

        /// <summary>
        /// 整数比较函数
        /// </summary>
        /// <param name="ival1">第一个整数</param>
        /// <param name="oper">比较操作符</param>
        /// <param name="ival2">第二个整数</param>
        /// <returns>比较结果</returns>
        public bool CompareInt(int ival1, ConditionOperatorInt oper, int ival2)
        {
            if (oper == ConditionOperatorInt.Equal) return ival1 == ival2;             // 等于
            if (oper == ConditionOperatorInt.NotEqual) return ival1 != ival2;         // 不等于
            if (oper == ConditionOperatorInt.GreaterEqual) return ival1 >= ival2;     // 大于等于
            if (oper == ConditionOperatorInt.LessEqual) return ival1 <= ival2;        // 小于等于
            if (oper == ConditionOperatorInt.Greater) return ival1 > ival2;           // 大于
            if (oper == ConditionOperatorInt.Less) return ival1 < ival2;              // 小于
            return false;
        }
    }

    /// <summary>
    /// 整数比较操作符枚举
    /// </summary>
    public enum ConditionOperatorInt
    {
        Equal,        // 等于
        NotEqual,     // 不等于
        GreaterEqual, // 大于等于
        LessEqual,    // 小于等于
        Greater,      // 大于
        Less,         // 小于
    }

    /// <summary>
    /// 布尔比较操作符枚举
    /// </summary>
    public enum ConditionOperatorBool
    {
        IsTrue,  // 为真
        IsFalse, // 为假
    }
}
