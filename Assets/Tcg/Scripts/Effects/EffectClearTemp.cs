using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// Clear temporary array of player's card
    /// 效果说明：
    /// 清空玩家的“临时卡牌列表”（cards_temp），
    /// 通常用于一些暂存卡列表、临时选择卡、过渡效果结束后的清理。
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/ClearTemp ", order = 10)]
    public class EffectClearTemp : EffectData
    {
        // 无目标版本（只和施法者有关）
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster)
        {
            // 根据施法者的 player_id 获取其玩家对象
            Player player = logic.GameData.GetPlayer(caster.player_id);

            // 清空该玩家的临时卡牌列表
            player.cards_temp.Clear();
        }

        // 有“卡牌目标”的版本，但逻辑相同，同样清空施法者玩家的临时卡池
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            Player player = logic.GameData.GetPlayer(caster.player_id);
            player.cards_temp.Clear();
        }
    }
}