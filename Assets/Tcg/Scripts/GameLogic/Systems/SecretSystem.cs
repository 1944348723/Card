namespace TcgEngine.Gameplay
{
    public sealed class SecretSystem
    {
        private readonly GameRuntimeContext runtime;

        public SecretSystem(GameRuntimeContext runtime)
        {
            this.runtime = runtime;
        }

        // 最多触发一张
        public bool TriggerPlayerSecrets(Player player, AbilityTrigger trigger_type)
        {
            for (int i = player.cards_secret.Count - 1; i >= 0; i--)
            {
                Card card = player.cards_secret[i];
                CardData icard = card.CardData;
                if (icard.type != CardType.Secret || card.exhausted) continue;

                if (card.AreAbilityConditionsMet(trigger_type, runtime.Game, card, card))
                {
                    runtime.ResolveQueue.AddSecret(trigger_type, card, card, ResolveSecret); // 添加秘密卡到解析队列
                    runtime.ResolveQueue.SetDelay(0.5f);
                    card.exhausted = true;

                    runtime.Logic.onSecretTrigger?.Invoke(card, card); // 触发秘密卡事件

                    return true;
                }
            }
            return false;
        }

        // 最多触发一张
        public bool TriggerSecrets(AbilityTrigger trigger_type, Card triggerer)
        {
            // 法术免疫，不触发秘密
            if (triggerer != null && triggerer.HasStatus(StatusType.SpellImmunity)) return false; 

            for (int p = 0; p < runtime.Game.players.Length; p++)
            {
                if (p != runtime.Game.current_player)
                {
                    Player other_player = runtime.Game.players[p];
                    for (int i = other_player.cards_secret.Count - 1; i >= 0; i--)
                    {
                        Card card = other_player.cards_secret[i];
                        CardData icard = card.CardData;
                        if (icard.type == CardType.Secret && !card.exhausted)
                        {
                            Card trigger = triggerer != null ? triggerer : card;
                            if (card.AreAbilityConditionsMet(trigger_type, runtime.Game, card, trigger))
                            {
                                runtime.ResolveQueue.AddSecret(trigger_type, card, trigger, ResolveSecret); // 添加秘密卡到解析队列
                                runtime.ResolveQueue.SetDelay(0.5f);
                                card.exhausted = true;

                                runtime.Logic.onSecretTrigger?.Invoke(card, trigger); // 触发秘密卡事件

                                return true; // 每个触发器只触发一个秘密卡
                            }
                        }
                    }
                }
            }
            return false;
        }

        private void ResolveSecret(AbilityTrigger secret_trigger, Card secret_card, Card trigger)
        {
            CardData icard = secret_card.CardData;
            Player player = runtime.Game.GetPlayer(secret_card.player_id);
            if (icard.type != CardType.Secret) return;

            Player tplayer = runtime.Game.GetPlayer(trigger.player_id);
            if (!runtime.IsAiPredict)
                tplayer.AddHistory(GameAction.SecretTriggered, secret_card, trigger); // 添加触发秘密的历史记录

            runtime.Logic.TriggerCardAbilityType(secret_trigger, secret_card, trigger); // 触发秘密卡能力
            runtime.Logic.DiscardCard(secret_card); // 丢弃秘密卡

            runtime.Logic.onSecretResolve?.Invoke(secret_card, trigger); // 触发秘密卡解析事件
        }
    }
}