using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Play", order = 10)]
    public class EffectPlay : EffectData
    {
        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, Card target)
        {
            Game game = logic.GetGameData();
            Player player = game.GetPlayer(caster.player_id);
            if (target == null || target.player_id != player.player_id)
                return;
            Slot slot = player.GetRandomEmptySlot(game.Board, logic.GetRandom());

            logic.MoveCardToZone(target, CardZone.Hand);
            if (slot != Slot.None)
                logic.PlayCard(target, slot, true);
        }
    }
}
