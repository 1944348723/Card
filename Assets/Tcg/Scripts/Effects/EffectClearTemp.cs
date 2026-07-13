using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/ClearTemp ", order = 10)]
    public class EffectClearTemp : EffectData
    {
        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster)
        {
            logic.ClearTemporaryCards(logic.GameData.GetPlayer(caster.player_id));
        }

        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, Card target)
        {
            logic.ClearTemporaryCards(logic.GameData.GetPlayer(caster.player_id));
        }
    }
}
