using UnityEngine.Profiling;

namespace TcgEngine.Gameplay
{
    /// <summary>每次状态变化后重建持续效果。</summary>
    public sealed class OngoingResolver
    {
        private readonly GameRuntime runtime;

        private Game game => runtime.Game;

        public OngoingResolver(GameRuntime runtime)
        {
            this.runtime = runtime;
        }

        public void UpdateOngoings()
        {
            Profiler.BeginSample("Update Ongoing");
            ClearOngoings();
            ApplyOngoingAbilities();
            ApplyDerivedStatusEffects();
            Profiler.EndSample();
        }

        // 清空场上所有临时效果，如特性、状态、能力、属性
        private void ClearOngoings()
        {
            foreach (Player player in game.players)
            {
                player.ClearOngoing();

                foreach (Card card in player.cards_board)
                {
                    card.ClearOngoing();
                }

                foreach (Card card in player.cards_equip)
                {
                    card.ClearOngoing();
                }

                foreach (Card card in player.cards_hand)
                {
                    card.ClearOngoing();
                }
            }
        }

        private void ApplyOngoingAbilities()
        {
            foreach (Player player in game.players)
            {
                UpdateAbilities(player, player.hero);  // 更新英雄持续能力

                foreach (Card card in player.cards_board)
                {
                    UpdateAbilities(player, card);
                }

                foreach (Card card in player.cards_equip)
                {
                    UpdateAbilities(player, card);
                }
            }
        }

        private void UpdateAbilities(Player player, Card card)
        {
            if (card == null || !card.CanDoAbilities()) return;

            foreach (AbilityData ability in card.GetAbilities())
            {
                if (ability == null || ability.trigger != AbilityTrigger.Ongoing) continue;
                if (!ability.AreTriggerConditionsMet(game, card)) continue;

                ResolveOngoingAbility(player, card, ability);
            }
        }

        private void ApplyDerivedStatusEffects()
        {
            foreach (Player player in game.players)
            {
                foreach (Card card in player.cards_board)
                {
                    ApplyProtection(player, card);

                    foreach (CardStatus status in card.status)
                        AddOngoingStatusBonus(card, status);
                    foreach (CardStatus status in card.ongoing_status)
                        AddOngoingStatusBonus(card, status);
                }

                foreach (Card card in player.cards_hand)
                {
                    foreach (CardStatus status in card.status)
                        AddOngoingStatusBonus(card, status);
                    foreach (CardStatus status in card.ongoing_status)
                        AddOngoingStatusBonus(card, status);
                }
            }
        }
        
        // 嘲讽
        private void ApplyProtection(Player player, Card card)
        {
            if (!card.HasStatus(StatusType.Protection) || card.HasStatus(StatusType.Stealth))   return;

            player.AddOngoingStatus(StatusType.Protected, 0);

            foreach (Card target in player.cards_board)
            {
                if (!target.HasStatus(StatusType.Protection) && !target.HasStatus(StatusType.Protected))
                {
                    target.AddOngoingStatus(StatusType.Protected, 0);
                }
            }
        }

        private void AddOngoingStatusBonus(Card card, CardStatus status)
        {
            if (status.type == StatusType.AddAttack)
                card.attack_ongoing += status.value;
            if (status.type == StatusType.AddHP)
                card.hp_ongoing += status.value;
            if (status.type == StatusType.AddManaCost)
                card.mana_ongoing += status.value;
        }

        private void ResolveOngoingAbility(Player player, Card card, AbilityData ability)
        {
            EffectContext context = runtime.Effects;
            
            if (ability.target == AbilityTarget.Self)
            {
                if (ability.AreTargetConditionsMet(game, card, card))
                {
                    ability.DoOngoingEffects(context, card, card);
                }
            }

            if (ability.target == AbilityTarget.PlayerSelf)
            {
                if (ability.AreTargetConditionsMet(game, card, player))
                {
                    ability.DoOngoingEffects(context, card, player);
                }
            }

            if (ability.target == AbilityTarget.AllPlayers || ability.target == AbilityTarget.PlayerOpponent)
            {
                foreach (Player targetPlayer in game.players)
                {
                    if (ability.target == AbilityTarget.PlayerOpponent && targetPlayer.player_id == player.player_id)
                        continue;

                    if (ability.AreTargetConditionsMet(game, card, targetPlayer))
                    {
                        ability.DoOngoingEffects(context, card, targetPlayer);
                    }
                }
            }

            if (ability.target == AbilityTarget.EquippedCard)
            {
                if (card.CardData.IsEquipment())
                {
                    // 获取装备的承载者
                    Card target = player.GetBearerCard(card);
                    if (target != null && ability.AreTargetConditionsMet(game, card, target))
                    {
                        ability.DoOngoingEffects(context, card, target); // 对承载者执行持续效果
                    }
                }
                else if (card.equipped_uid != null)
                {
                    // 获取被装备的卡牌
                    Card target = game.GetCard(card.equipped_uid);
                    if (target != null && ability.AreTargetConditionsMet(game, card, target))
                    {
                        ability.DoOngoingEffects(context, card, target); // 对装备卡牌执行持续效果
                    }
                }
            }

            if (ability.target == AbilityTarget.AllCardsAllPiles
                || ability.target == AbilityTarget.AllCardsHand
                || ability.target == AbilityTarget.AllCardsBoard)
            {
                foreach (Player targetPlayer in game.players)
                {
                    // 手牌卡牌
                    if (ability.target == AbilityTarget.AllCardsAllPiles || ability.target == AbilityTarget.AllCardsHand)
                    {
                        foreach (Card targetCard in targetPlayer.cards_hand)
                        {
                            if (ability.AreTargetConditionsMet(game, card, targetCard))
                            {
                                ability.DoOngoingEffects(context, card, targetCard);
                            }
                        }
                    }

                    // 场上卡牌
                    if (ability.target == AbilityTarget.AllCardsAllPiles || ability.target == AbilityTarget.AllCardsBoard)
                    {
                        foreach (Card targetCard in targetPlayer.cards_board)
                        {
                            if (ability.AreTargetConditionsMet(game, card, targetCard))
                            {
                                ability.DoOngoingEffects(context, card, targetCard);
                            }
                        }
                    }

                    // 装备卡牌
                    if (ability.target == AbilityTarget.AllCardsAllPiles)
                    {
                        foreach (Card targetCard in targetPlayer.cards_equip)
                        {
                            if (ability.AreTargetConditionsMet(game, card, targetCard))
                            {
                                ability.DoOngoingEffects(context, card, targetCard);
                            }
                        }
                    }
                }
            }
        }
    }
}
