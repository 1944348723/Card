using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 枚举：用于指定要比较的“属性类型”
    /// 可选择：
    /// - Attack ：攻击力
    /// - HP     ：生命值
    /// - Mana   ：法力值/资源值
    /// </summary>
    public enum ConditionStatType
    {
        None = 0,
        Attack = 10,
        HP = 20,
        Mana = 30,
    }

    /// <summary>
    /// 条件：比较“卡牌”或“玩家”的基础属性（攻击 / 生命 / 法力）
    ///
    /// 功能用途：
    /// --------------------------------
    /// ✔ 判断一张卡是否攻击 >= 某值
    /// ✔ 判断目标是否生命 <= 某值
    /// ✔ 判断法力是否刚好等于指定数值
    /// ✔ 同时支持：
    ///     - Card 目标
    ///     - Player 目标
    ///
    /// 适用场景：
    /// - “只能选择攻击 >= 5 的单位”
    /// - “只能对生命值 ≤ 3 的单位释放”
    /// - “仅当玩家 Mana ≥ 2 时触发”
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/Stat", order = 10)]
    public class ConditionStat : ConditionData
    {
        [Header("属性判断设置")]
        public ConditionStatType type;   // 要比较的属性类型（攻击 / HP / Mana）
        public ConditionOperatorInt oper; // 比较方式（>, <, >=, == 等）
        public int value;                // 目标数值

        /// <summary>
        /// 当目标是 Card（卡牌单位）时
        /// 根据 type 选择对应的属性进行比较
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            if (type == ConditionStatType.Attack)
            {
                // 比较 卡牌攻击力 vs 目标数值
                return CompareInt(target.GetAttack(), oper, value);
            }

            if (type == ConditionStatType.HP)
            {
                // 比较 卡牌生命值 vs 目标数值
                return CompareInt(target.GetHP(), oper, value);
            }

            if (type == ConditionStatType.Mana)
            {
                // 部分卡可能有“法力属性”（视游戏设计）
                return CompareInt(target.GetMana(), oper, value);
            }

            return false;
        }

        /// <summary>
        /// 当目标是 Player（玩家）时
        /// 只有 HP 与 Mana 有意义
        /// Attack 不适用于玩家
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Player target)
        {
            if (type == ConditionStatType.HP)
            {
                return CompareInt(target.hp, oper, value);
            }

            if (type == ConditionStatType.Mana)
            {
                return CompareInt(target.mana, oper, value);
            }

            return false;
        }
    }
}
