using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Play", order = 10)]
    public class EffectPlay : EffectData
    {
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            Game game = logic.GetGameData();
            Player player = game.GetPlayer(caster.player_id);
            Slot slot = player.GetRandomEmptySlot(logic.GetRandom());

            logic.MoveCardToZone(player, target, CardZone.Hand);
            if (slot != Slot.None)
                logic.PlayCard(target, slot, true);
        }
    }
}
