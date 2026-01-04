using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.AI;

namespace TcgEngine
{
    /// <summary>
    /// 条件判断：
    /// 用于比较【技能设计时允许的目标类型】与【当前实际目标类型】是否匹配。
    /// 
    /// 举例：
    /// - 如果技能只允许选择“卡牌”，那么只有当目标是 Card 时条件才成立
    /// - 如果技能要求目标是“玩家”，而你选了卡或格子，则条件失败
    /// 
    /// 常用于：
    /// - 技能逻辑保护
    /// - AI 目标检查
    /// - 限制技能只能作用在正确对象上
    /// </summary>
    
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/Player", order = 10)]
    public class ConditionTarget : ConditionData
    {
        [Header("期望的目标类型")]
        public ConditionTargetType type;   // 希望目标属于哪一类（卡 / 玩家 / 格子）
        public ConditionOperatorBool oper; // 比较方式（是否要求成立）

        /// <summary>
        /// 当目标是 Card（卡牌）时：
        /// 判断：期望类型 是否 == Card
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            return CompareBool(type == ConditionTargetType.Card, oper);  // 是“卡”吗？
        }

        /// <summary>
        /// 当目标是 Player（玩家）时：
        /// 判断：期望类型 是否 == Player
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Player target)
        {
            return CompareBool(type == ConditionTargetType.Player, oper); // 是“玩家”吗？
        }

        /// <summary>
        /// 当目标是 Slot（棋盘格子）时：
        /// 判断：期望类型 是否 == Slot
        /// </summary>
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Slot target)
        {
            return CompareBool(type == ConditionTargetType.Slot, oper);  // 是“格子”吗？
        }
    }

    /// <summary>
    /// 目标类型枚举
    /// 用于区分技能可作用的对象类型
    /// </summary>
    public enum ConditionTargetType
    {
        None = 0,  // 无目标（一般不用）
        Card = 10, // 卡牌目标
        Player = 20, // 玩家目标
        Slot = 30, // 棋盘格子目标
    }
}