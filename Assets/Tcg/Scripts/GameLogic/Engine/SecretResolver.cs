namespace TcgEngine.Gameplay
{
    /// <summary>秘密的发现、入队与结算。</summary>
    public sealed class SecretResolver
    {
        private readonly GameRuntime runtime;

        public SecretResolver(GameRuntime runtime)
        {
            this.runtime = runtime;
        }

        // Each trigger may queue at most one secret for this player.
        public bool TriggerPlayerSecrets(Player player, AbilityTrigger triggerType)
        {
            for (int i = player.cards_secret.Count - 1; i >= 0; i--)
            {
                Card secret = player.cards_secret[i];
                if (secret.CardData.type != CardType.Secret || secret.exhausted)
                    continue;

                if (!secret.AreAbilityConditionsMet(triggerType, runtime.Game, secret, secret))
                    continue;

                runtime.ResolveQueue.AddSecret(triggerType, secret, secret, ResolveSecret);
                runtime.ResolveQueue.SetDelay(0.5f);
                secret.exhausted = true;
                runtime.Engine.onSecretTrigger?.Invoke(secret, secret);
                return true;
            }

            return false;
        }

        // A trigger from the active player may queue at most one opposing secret.
        public bool TriggerSecrets(AbilityTrigger triggerType, Card triggerer)
        {
            if (triggerer != null && triggerer.HasStatus(StatusType.SpellImmunity))
                return false;

            for (int playerId = 0; playerId < runtime.Game.players.Length; playerId++)
            {
                if (playerId == runtime.Game.current_player)
                    continue;

                Player owner = runtime.Game.players[playerId];
                for (int i = owner.cards_secret.Count - 1; i >= 0; i--)
                {
                    Card secret = owner.cards_secret[i];
                    if (secret.CardData.type != CardType.Secret || secret.exhausted)
                        continue;

                    Card trigger = triggerer ?? secret;
                    if (!secret.AreAbilityConditionsMet(triggerType, runtime.Game, secret, trigger))
                        continue;

                    runtime.ResolveQueue.AddSecret(triggerType, secret, trigger, ResolveSecret);
                    runtime.ResolveQueue.SetDelay(0.5f);
                    secret.exhausted = true;
                    runtime.Engine.onSecretTrigger?.Invoke(secret, trigger);
                    return true;
                }
            }

            return false;
        }

        private void ResolveSecret(AbilityTrigger triggerType, Card secret, Card triggerer)
        {
            if (secret.CardData.type != CardType.Secret)
                return;

            if (!runtime.IsAiSimulation)
            {
                Player triggerOwner = runtime.Game.GetPlayer(triggerer.player_id);
                triggerOwner.AddHistory(GameAction.SecretTriggered, secret, triggerer);
            }

            runtime.Engine.TriggerCardAbilityType(triggerType, secret, triggerer);
            runtime.Engine.DiscardCard(secret);
            runtime.Engine.onSecretResolve?.Invoke(secret, triggerer);
        }
    }
}
