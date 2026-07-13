using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Create", order = 10)]
    public class EffectCreate : EffectData
    {
        public CardZone create_pile;
        public bool create_opponent;

        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, CardData target)
        {
            Player player = logic.GameData.GetPlayer(caster.player_id);
            if (create_opponent)
                player = logic.GameData.GetOpponentPlayer(caster.player_id);

            if (player == null || target == null || !IsCreationZone(create_pile))
                return;

            Card card = Card.Create(target, caster.VariantData, player);
            if (!logic.MoveCardToZone(card, create_pile))
            {
                player.cards_all.Remove(card.uid);
                return;
            }
            logic.GameData.last_summoned = card.uid;
        }

        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, Card target)
        {
            DoEffect(logic, ability, caster, target.CardData);
        }

        private static bool IsCreationZone(CardZone zone)
        {
            return zone == CardZone.Deck || zone == CardZone.Discard
                || zone == CardZone.Hand || zone == CardZone.Temp;
        }
    }
}
