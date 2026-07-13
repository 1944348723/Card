using System;
using System.Collections.Generic;

namespace TcgEngine.Gameplay
{
    /// <summary>
    /// The deliberately small execution surface exposed to ScriptableObject effects.
    /// It replaces the old GameLogic back-reference and keeps effects inside the rules runtime.
    /// </summary>
    public sealed class EffectContext
    {
        private readonly GameRuntime runtime;

        internal EffectContext(GameRuntime runtime)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

        public Game GameData => runtime.Game;
        public Game GetGameData() => runtime.Game;
        public Random GetRandom() => runtime.Random;

        public void AttackTarget(Card attacker, Card target, bool skipCost = false) => runtime.Combat.AttackCard(attacker, target, skipCost);
        public void AttackPlayer(Card attacker, Player target, bool skipCost = false) => runtime.Combat.AttackPlayer(attacker, target, skipCost);
        public void RedirectAttack(Card attacker, Card target) => runtime.Combat.Redirect(attacker, target);
        public void RedirectAttack(Card attacker, Player target) => runtime.Combat.Redirect(attacker, target);
        public void ShuffleDeck(List<Card> cards) => runtime.Cards.ShuffleDeck(cards, runtime.Random);
        public void DrawCards(Player player, int count = 1)
        {
            int drawn = runtime.Cards.DrawCards(player, count);
            runtime.Events.RaiseCardsDrawn(drawn);
        }
        public void DiscardCardsFromHand(Player player, int count = 1) => runtime.Cards.DiscardCardsFromHand(player, count);
        public Card SummonCard(Player player, CardData data, VariantData variant, Slot slot) => runtime.Cards.Summon(player, data, variant, slot);
        public Card SummonCardHand(Player player, CardData data, VariantData variant) => runtime.Cards.CreateInHand(player, data, variant);
        public Card TransformCard(Card card, CardData transformTo) => runtime.Cards.Transform(card, transformTo);
        public void ChangeOwner(Card card, Player owner) => runtime.Cards.ChangeOwner(card, owner);
        public bool MoveCardToZone(Card card, CardZone zone, bool clearCard = false)
        {
            bool moved = runtime.Zones.MoveTo(card, zone);
            if (moved && clearCard)
                card.Clear();
            return moved;
        }
        public void ClearTemporaryCards(Player player) => runtime.Zones.ClearTemporary(player);
        public void DamagePlayer(Card attacker, Player target, int value, DamageType type) => runtime.Damage.DamagePlayer(attacker, target, value, type);
        public void DamagePlayer(Player target, int value, DamageType type) => runtime.Damage.DamagePlayer(target, value, type);
        public void HealPlayer(Player target, int value) => runtime.Damage.HealPlayer(target, value);
        public void HealCard(Card target, int value) => runtime.Damage.HealCard(target, value);
        public void DamageCard(Card attacker, Card target, int value, DamageType type) => runtime.Damage.DamageCard(attacker, target, value, type);
        public void DamageCard(Card target, int value, DamageType type) => runtime.Damage.DamageCard(target, value, type);
        public void KillCard(Card attacker, Card target) => runtime.Cards.Kill(attacker, target);
        public void DiscardCard(Card card) => runtime.Cards.Discard(card);
        public void PlayCard(Card card, Slot slot, bool skipCost = false) => runtime.Actions.PlayCard(card, slot, skipCost);
        public void TriggerAbilityDelayed(AbilityData ability, Card caster) => runtime.Abilities.TriggerDelayed(ability, caster);
        public void TriggerAbilityDelayed(AbilityData ability, Card caster, Card triggerer) => runtime.Abilities.TriggerDelayed(ability, caster, triggerer);
        public int RollRandomValue(int dice) => RollRandomValue(1, dice + 1);
        public int RollRandomValue(int min, int max)
        {
            runtime.Game.rolled_value = runtime.Random.Next(min, max);
            runtime.Events.RaiseRolled(runtime.Game.rolled_value);
            runtime.ResolveQueue.SetDelay(1f);
            return runtime.Game.rolled_value;
        }
    }
}
