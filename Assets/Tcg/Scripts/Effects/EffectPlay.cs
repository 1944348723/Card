using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 效果说明：
    /// 从手牌中免费使用一张卡牌。
    /// - 自动将卡牌从其他区域移回手牌（如果存在）
    /// - 随机选择一个空位上场（如果有空位）
    /// - 最后执行“上场”逻辑，free = true 表示免费使用，不消耗法力
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Play", order = 10)]
    public class EffectPlay : EffectData
    {
        // 对卡牌执行效果
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            Game game = logic.GetGameData();                   // 获取当前游戏数据
            Player player = game.GetPlayer(caster.player_id);  // 获取施法者所属玩家
            Slot slot = player.GetRandomEmptySlot(logic.GetRandom()); // 随机获取一个空位（Slot.None 表示无空位）

            // 确保卡牌从所有区域移除，避免重复存在
            player.RemoveCardFromAllGroups(target);

            // 加入手牌
            player.cards_hand.Add(target);

            // 如果有空位则上场
            if (slot != Slot.None)
            {
                // free = true 表示免费使用，不消耗法力
                logic.PlayCard(target, slot, true);
            }
        }
    }
}