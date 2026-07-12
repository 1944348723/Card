using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Create", order = 10)]
    public class EffectCreate : EffectData
    {
        public PileType create_pile;
        public bool create_opponent;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, CardData target)
        {
            Player player = logic.GameData.GetPlayer(caster.player_id);
            if (create_opponent)
                player = logic.GameData.GetOpponentPlayer(caster.player_id);

            Card card = Card.Create(target, caster.VariantData, player);
            logic.GameData.last_summoned = card.uid;

            if (create_pile == PileType.Deck)
                logic.MoveCardToZone(player, card, CardZone.Deck);
            if (create_pile == PileType.Discard)
                logic.MoveCardToZone(player, card, CardZone.Discard);
            if (create_pile == PileType.Hand)
                logic.MoveCardToZone(player, card, CardZone.Hand);
            if (create_pile == PileType.Temp)
                logic.MoveCardToZone(player, card, CardZone.Temp);
        }

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            DoEffect(logic, ability, caster, target.CardData);
        }
    }
}
