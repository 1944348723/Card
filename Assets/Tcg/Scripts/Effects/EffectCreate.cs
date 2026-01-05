using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 效果说明：
    /// 根据指定的 CardData 创建一张全新的卡牌。
    /// 常用于“发现(Discover)”或“生成卡牌”类效果。
    /// 与 EffectSummon 不同的是，
    /// - 这里是直接基于 CardData 创建一张新卡
    /// - 并把它放入指定的卡堆（牌库 / 手牌 / 弃牌堆 / 临时堆等）
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Create", order = 10)]
    public class EffectCreate : EffectData
    {
        public PileType create_pile;   // 创建后放入的卡堆类型。通常不要设置为 Board，如需上场或放入秘密区，建议用 EffectSummon 或 EffectPlay
        public bool create_opponent;   // 是否把创建的卡牌给对手（true = 给对手，false = 给施法者）

        // 目标为 CardData（直接创建指定卡数据）
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, CardData target)
        {
            // 默认获取施法者所属玩家
            Player player = logic.GameData.GetPlayer(caster.player_id);

            // 如果选择生成给对手，则改为对手玩家
            if (create_opponent)
                player = logic.GameData.GetOpponentPlayer(caster.player_id);

            // 基于目标 CardData 创建一张新卡
            Card card = Card.Create(target, caster.VariantData, player);

            // 记录最近被“召唤/创建”的卡牌
            logic.GameData.last_summoned = card.uid;

            // 根据目标堆类型放入不同卡区
            if (create_pile == PileType.Deck)
                player.cards_deck.Add(card);       // 放入牌库

            if (create_pile == PileType.Discard)
                player.cards_discard.Add(card);    // 放入弃牌堆

            if (create_pile == PileType.Hand)
                player.cards_hand.Add(card);       // 放入手牌

            if (create_pile == PileType.Temp)
                player.cards_temp.Add(card);       // 放入临时卡池
        }

        // 目标为 Card（会复制目标卡的 CardData 再创建）
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            // 调用上面的方法，基于这张卡的 CardData 创建一张“复制卡”
            DoEffect(logic, ability, caster, target.CardData);
        }
    }
}
