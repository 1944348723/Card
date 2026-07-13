using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// Effect to make a card attack a target
    /// 效果说明：让某张卡牌去攻击目标（可以是玩家或另一张卡牌）
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Attack", order = 10)]
    public class EffectAttack : EffectData
    {
        public EffectAttackerType attacker_type;   // 指定攻击者的类型（谁来发动攻击）

        // 对“玩家目标”执行效果：让某个卡牌攻击玩家
        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, Player target)
        {
            Card attacker = GetAttacker(logic.GetGameData(), caster);  // 获取真正的攻击者
            if (attacker != null)
            {
                // 触发攻击玩家逻辑，最后参数 true 表示这是一个强制或立即执行的攻击
                logic.AttackPlayer(attacker, target, true);
            }
        }

        // 对“卡牌目标”执行效果：让某个卡牌攻击另一张卡
        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, Card target)
        {
            Card attack = GetAttacker(logic.GetGameData(), caster);    // 获取真正的攻击者
            if (attack != null)
            {
                // 触发攻击卡牌逻辑
                logic.AttackTarget(attack, target, true);
            }
        }

        // 根据设定的 attacker_type 决定到底哪一张卡是攻击者
        public Card GetAttacker(Game gdata, Card caster)
        {
            // 如果攻击者是“自身”——也就是这个效果所属的卡
            if (attacker_type == EffectAttackerType.Self)
                return caster;

            // 如果攻击者是“触发该能力的卡牌”
            if (attacker_type == EffectAttackerType.AbilityTriggerer)
                return gdata.GetCard(gdata.ability_triggerer);

            // 最近被打出的卡牌作为攻击者
            if (attacker_type == EffectAttackerType.LastPlayed)
                return gdata.GetCard(gdata.last_played);

            // 最近被指定为目标的卡牌作为攻击者
            if (attacker_type == EffectAttackerType.LastTargeted)
                return gdata.GetCard(gdata.last_target);

            return null;   // 找不到攻击者则返回空
        }
    }

    // 攻击者类型枚举
    public enum EffectAttackerType
    {
        Self = 1,                  // 自身（施放这个效果的卡）
        AbilityTriggerer = 25,     // 触发这个技能/效果的卡
        LastPlayed = 70,           // 最近打出的卡
        LastTargeted = 72,         // 最近被选为目标的卡
    }
}
