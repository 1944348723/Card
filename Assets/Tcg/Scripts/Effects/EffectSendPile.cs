using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/SendPile", order = 10)]
    public class EffectSendPile : EffectData
    {
        public CardZone pile;

        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, Card target)
        {
            if (pile == CardZone.Deck || pile == CardZone.Hand
                || pile == CardZone.Discard || pile == CardZone.Temp)
                logic.MoveCardToZone(target, pile, true);
        }
    }
}
