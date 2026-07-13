using System;

namespace TcgEngine.Gameplay
{
    /// <summary>
    /// Rules publish domain notifications here. Presentation and network adapters subscribe
    /// without becoming dependencies of the rules runtime.
    /// </summary>
    public sealed class GameRuntimeEvents
    {
        public event Action GameStarted;
        public event Action<Player> GameEnded;
        public event Action TurnStarted;
        public event Action MainPhaseStarted;
        public event Action TurnEnded;
        public event Action<Card, Slot> CardPlayed;
        public event Action<Card, Slot> CardSummoned;
        public event Action<Card, Slot> CardMoved;
        public event Action<Card> CardTransformed;
        public event Action<Card> CardDiscarded;
        public event Action<int> CardsDrawn;
        public event Action<int> Rolled;
        public event Action<AbilityData, Card> AbilityStarted;
        public event Action<AbilityData, Card, Card> AbilityTargetedCard;
        public event Action<AbilityData, Card, Player> AbilityTargetedPlayer;
        public event Action<AbilityData, Card, Slot> AbilityTargetedSlot;
        public event Action<AbilityData, Card> AbilityEnded;
        public event Action<Card, Card> AttackStarted;
        public event Action<Card, Card> AttackEnded;
        public event Action<Card, Player> PlayerAttackStarted;
        public event Action<Card, Player> PlayerAttackEnded;
        public event Action<Card, int> CardDamaged;
        public event Action<Card, int> CardHealed;
        public event Action<Player, int> PlayerDamaged;
        public event Action<Player, int> PlayerHealed;
        public event Action<Card, Card> SecretTriggered;
        public event Action<Card, Card> SecretResolved;
        public event Action Refreshed;

        public void RaiseGameStarted() => GameStarted?.Invoke();
        public void RaiseGameEnded(Player player) => GameEnded?.Invoke(player);
        public void RaiseTurnStarted() => TurnStarted?.Invoke();
        public void RaiseMainPhaseStarted() => MainPhaseStarted?.Invoke();
        public void RaiseTurnEnded() => TurnEnded?.Invoke();
        public void RaiseCardPlayed(Card card, Slot slot) => CardPlayed?.Invoke(card, slot);
        public void RaiseCardSummoned(Card card, Slot slot) => CardSummoned?.Invoke(card, slot);
        public void RaiseCardMoved(Card card, Slot slot) => CardMoved?.Invoke(card, slot);
        public void RaiseCardTransformed(Card card) => CardTransformed?.Invoke(card);
        public void RaiseCardDiscarded(Card card) => CardDiscarded?.Invoke(card);
        public void RaiseCardsDrawn(int count) => CardsDrawn?.Invoke(count);
        public void RaiseRolled(int value) => Rolled?.Invoke(value);
        public void RaiseAbilityStarted(AbilityData ability, Card caster) => AbilityStarted?.Invoke(ability, caster);
        public void RaiseAbilityTargetedCard(AbilityData ability, Card caster, Card target) => AbilityTargetedCard?.Invoke(ability, caster, target);
        public void RaiseAbilityTargetedPlayer(AbilityData ability, Card caster, Player target) => AbilityTargetedPlayer?.Invoke(ability, caster, target);
        public void RaiseAbilityTargetedSlot(AbilityData ability, Card caster, Slot target) => AbilityTargetedSlot?.Invoke(ability, caster, target);
        public void RaiseAbilityEnded(AbilityData ability, Card caster) => AbilityEnded?.Invoke(ability, caster);
        public void RaiseAttackStarted(Card attacker, Card target) => AttackStarted?.Invoke(attacker, target);
        public void RaiseAttackEnded(Card attacker, Card target) => AttackEnded?.Invoke(attacker, target);
        public void RaisePlayerAttackStarted(Card attacker, Player target) => PlayerAttackStarted?.Invoke(attacker, target);
        public void RaisePlayerAttackEnded(Card attacker, Player target) => PlayerAttackEnded?.Invoke(attacker, target);
        public void RaiseCardDamaged(Card target, int value) => CardDamaged?.Invoke(target, value);
        public void RaiseCardHealed(Card target, int value) => CardHealed?.Invoke(target, value);
        public void RaisePlayerDamaged(Player target, int value) => PlayerDamaged?.Invoke(target, value);
        public void RaisePlayerHealed(Player target, int value) => PlayerHealed?.Invoke(target, value);
        public void RaiseSecretTriggered(Card secret, Card triggerer) => SecretTriggered?.Invoke(secret, triggerer);
        public void RaiseSecretResolved(Card secret, Card triggerer) => SecretResolved?.Invoke(secret, triggerer);
        public void RaiseRefreshed() => Refreshed?.Invoke();
    }
}
