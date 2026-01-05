using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// EffectSendPile 效果说明：
    /// 将目标卡牌发送到指定的牌堆（Deck/Discard/Hand/Temp）。
    /// 注意事项：
    /// - 不要将卡牌发送到 Board（战场），因为上场需要 Slot，若要放到战场请使用 EffectPlay。
    /// - 不要从战场直接发送到弃牌堆（Discard），因为不会触发 OnKill 效果，若要销毁请使用 EffectDestroy。
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/SendPile", order = 10)]
    public class EffectSendPile : EffectData
    {
        public PileType pile;  // 目标牌堆类型（Deck、Hand、Discard、Temp 等）

        // 对卡牌目标执行效果
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            Game data = logic.GetGameData();                // 获取游戏数据
            Player player = data.GetPlayer(target.player_id); // 获取目标卡牌所属玩家

            // 发送到牌库
            if (pile == PileType.Deck)
            {
                player.RemoveCardFromAllGroups(target);  // 从所有区域移除卡牌
                player.cards_deck.Add(target);          // 添加到牌库
                target.Clear();                          // 清除卡牌临时状态
            }

            // 发送到手牌
            if (pile == PileType.Hand)
            {
                player.RemoveCardFromAllGroups(target);
                player.cards_hand.Add(target);
                target.Clear();
            }

            // 发送到弃牌堆
            if (pile == PileType.Discard)
            {
                player.RemoveCardFromAllGroups(target);
                player.cards_discard.Add(target);
                target.Clear();
            }

            // 发送到临时区
            if (pile == PileType.Temp)
            {
                player.RemoveCardFromAllGroups(target);
                player.cards_temp.Add(target);
                target.Clear();
            }
        }
    }

    // 牌堆类型枚举
    public enum PileType
    {
        None = 0,       // 无
        Board = 10,     // 战场
        Hand = 20,      // 手牌
        Deck = 30,      // 牌库
        Discard = 40,   // 弃牌堆
        Secret = 50,    // 秘密区
        Equipped = 60,  // 装备区
        Temp = 90,      // 临时区
    }

}
