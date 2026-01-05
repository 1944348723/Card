using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 效果说明：
    /// 将指定玩家的牌库（Deck）进行随机洗牌。
    /// - target：要洗牌的玩家
    /// - 调用逻辑层的 ShuffleDeck 方法打乱牌库顺序
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Shuffle", order = 10)]
    public class EffectShuffle : EffectData
    {
        // 对玩家执行洗牌效果
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            logic.ShuffleDeck(target.cards_deck); // 洗牌
        }
    }
}