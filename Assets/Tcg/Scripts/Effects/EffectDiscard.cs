using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// Effect to discard cards from hand
    /// 效果说明：
    /// 丢弃卡牌。
    /// - 对玩家目标：从手牌中丢弃指定数量的卡牌（ability.value 张）；
    /// - 对卡牌目标：直接丢弃指定卡牌。
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Discard", order = 10)]
    public class EffectDiscard : EffectData
    {
        // 对玩家执行丢弃效果
        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, Player target)
        {
            // 从玩家手牌中丢弃前 ability.value 张卡
            logic.DiscardCardsFromHand(target, ability.value);
        }

        // 对卡牌执行丢弃效果
        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, Card target)
        {
            // 丢弃指定卡牌
            logic.DiscardCard(target);
        }

    }
}
