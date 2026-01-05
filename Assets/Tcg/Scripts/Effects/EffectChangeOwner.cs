using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// Change owner of target card to the owner of the caster (or the opponent player)
    /// 效果说明：把目标卡牌的“控制权/所属玩家”改变成施法者所属玩家，或者改为对手玩家
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/ChangeOwner", order = 10)]
    public class EffectChangeOwner : EffectData
    {
        public bool owner_opponent; // 是否将控制权转交给对手？true = 转给对手，false = 转给施法者

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            Game game = logic.GetGameData();  // 获取当前游戏数据

            // 根据 owner_opponent 选择目标玩家：
            // 若为 true → 获取施法者的对手
            // 若为 false → 获取施法者自身玩家
            Player tplayer = owner_opponent ? game.GetOpponentPlayer(caster.player_id) 
                : game.GetPlayer(caster.player_id);

            // 执行改变控制权逻辑，把 target 这张卡转移给 tplayer
            logic.ChangeOwner(target, tplayer);
        }
    }
}