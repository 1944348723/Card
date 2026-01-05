using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 效果说明：
    /// 抽卡效果。
    /// - 对玩家目标：直接让玩家抽指定数量的卡牌（ability.value 张）；
    /// - 对卡牌目标：获取该卡所属玩家，然后让玩家抽卡。
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Draw", order = 10)]
    public class EffectDraw : EffectData
    {
        // 对玩家执行抽卡
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            logic.DrawCard(target, ability.value);  // 玩家抽 ability.value 张卡
        }

        // 对卡牌执行抽卡（抽卡归该卡所属玩家）
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            Player player = logic.GameData.GetPlayer(target.player_id); // 获取卡牌所属玩家
            logic.DrawCard(player, ability.value);                     // 玩家抽卡
        }

    }
}