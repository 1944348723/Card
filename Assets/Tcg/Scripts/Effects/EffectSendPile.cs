using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/SendPile", order = 10)]
    public class EffectSendPile : EffectData
    {
        public PileType pile;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            Player player = logic.GetGameData().GetPlayer(target.player_id);

            if (pile == PileType.Deck)
                logic.MoveCardToZone(player, target, CardZone.Deck, true);
            if (pile == PileType.Hand)
                logic.MoveCardToZone(player, target, CardZone.Hand, true);
            if (pile == PileType.Discard)
                logic.MoveCardToZone(player, target, CardZone.Discard, true);
            if (pile == PileType.Temp)
                logic.MoveCardToZone(player, target, CardZone.Temp, true);
        }
    }

    public enum PileType
    {
        None = 0,
        Board = 10,
        Hand = 20,
        Deck = 30,
        Discard = 40,
        Secret = 50,
        Equipped = 60,
        Temp = 90,
    }
}
