using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 效果说明：
    /// 从指定的 CardData 召唤一张全新的卡牌（不属于任何玩家的牌库）。
    /// - summon：要召唤的卡牌数据
    /// - 如果目标是玩家，则召唤到手牌
    /// - 如果目标是卡牌或槽位，则召唤到战场的对应槽位
    /// - 与 EffectCreate 不同的是，EffectSummon 更关注卡牌放置的位置，而不是卡牌数据本身
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Summon", order = 10)]
    public class EffectSummon : EffectData
    {
        public CardData summon;  // 要召唤的卡牌数据

        // 对玩家目标执行召唤效果（召唤到手牌）
        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, Player target)
        {
            logic.SummonCardHand(target, summon, caster.VariantData); // 召唤到手牌
        }

        // 对卡牌目标执行召唤效果（召唤到战场）
        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, Card target)
        {
            Player player = logic.GameData.GetPlayer(caster.player_id);
            logic.SummonCard(player, summon, caster.VariantData, target.slot); // 假设目标卡牌已被移除，槽位为空
        }

        // 对槽位目标执行召唤效果（召唤到指定槽位）
        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, Slot target)
        {
            Player player = logic.GameData.GetPlayer(caster.player_id);
            logic.SummonCard(player, summon, caster.VariantData, target); // 召唤到指定槽位
        }

        // 对 CardData 目标执行召唤效果（召唤到手牌）
        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, CardData target)
        {
            Player player = logic.GameData.GetPlayer(caster.player_id);
            logic.SummonCardHand(player, target, caster.VariantData); // 召唤到手牌
        }
    }
}
