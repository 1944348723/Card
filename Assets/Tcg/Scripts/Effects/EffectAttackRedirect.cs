using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// Effect to redirect an attack (usually triggered with OnBeforeAttack or OnBeforeDefend)
    /// 效果说明：重定向攻击目标（通常在攻击前或防御前触发，用来改变攻击指向）
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/AttackRedirect", order = 10)]
    public class EffectAttackRedirect : EffectData
    {
        public EffectAttackerType attacker_type; // 指定哪一张卡作为“当前攻击者”

        // 对“玩家目标”的效果：把攻击重定向到某个玩家
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            Card attacker = GetAttacker(logic.GetGameData(), caster); // 获取攻击来源卡
            if (attacker != null)
            {
                // 将当前攻击的目标改为这个玩家
                logic.RedirectAttack(attacker, target);
            }
        }

        // 对“卡牌目标”的效果：把攻击重定向到某张卡牌
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            Card attacker = GetAttacker(logic.GetGameData(), caster); // 获取攻击来源卡
            if (attacker != null)
            {
                // 将当前攻击目标改为这张卡
                logic.RedirectAttack(attacker, target);
            }
        }

        // 根据 attacker_type 判断谁是攻击者
        public Card GetAttacker(Game gdata, Card caster)
        {
            // 自身作为攻击者
            if (attacker_type == EffectAttackerType.Self)
                return caster;

            // 触发该能力的卡牌作为攻击者
            if (attacker_type == EffectAttackerType.AbilityTriggerer)
                return gdata.GetCard(gdata.ability_triggerer);

            // 最近被打出的卡牌作为攻击者
            if (attacker_type == EffectAttackerType.LastPlayed)
                return gdata.GetCard(gdata.last_played);

            // 最近被选中的目标卡牌作为攻击者
            if (attacker_type == EffectAttackerType.LastTargeted)
                return gdata.GetCard(gdata.last_target);

            return null; // 未找到则返回 null
        }
    }
}
