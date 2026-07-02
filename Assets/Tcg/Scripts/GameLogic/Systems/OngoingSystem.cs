using System.Collections.Generic;
using UnityEngine.Profiling;

namespace TcgEngine.Gameplay
{
    public sealed class OngoingSystem
    {
        private Game game;

        public OngoingSystem(Game game)
        {
            this.game = game;
        }

        public void SetData(Game game)
        {
            this.game = game;
        }

        // 该函数经常被调用，用于更新受持续能力影响的状态/属性
        // 基本逻辑是先将加成清零（CleanOngoing），再重新计算以确保持续效果存在
        public void Update(GameLogic logic, List<Card> cardsToClear)
        {
            Profiler.BeginSample("Update Ongoing");
            UpdateOngoings(logic);
            UpdateKills(logic, cardsToClear);
            Profiler.EndSample();
        }

        public void UpdateOngoings(GameLogic logic)
        {
            ClearOngoings();
            ApplyOngoingAbilities(logic);
            ApplyDerivedStatusEffects();
        }

        // 杀掉HP为0的卡牌
        public void UpdateKills(GameLogic logic, List<Card> cardsToClear)
        {
            foreach (Player player in game.players)
            {
                for (int i = player.cards_board.Count - 1; i >= 0; i--)
                {
                    if (i < player.cards_board.Count && player.cards_board[i].GetHP() <= 0)
                    {
                        logic.DiscardCard(player.cards_board[i]);
                    }
                }

                for (int i = player.cards_equip.Count - 1; i >= 0; i--)
                {
                    if (i >= player.cards_equip.Count) continue;

                    Card card = player.cards_equip[i];
                    if (card.GetHP() <= 0 || player.GetBearerCard(card) == null)
                    {
                        logic.DiscardCard(card);
                    }
                }
            }

            foreach (Card card in cardsToClear)
            {
                card.Clear();
            }
            cardsToClear.Clear();
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

        private void ApplyOngoingAbilities(GameLogic logic)
        {
            foreach (Player player in game.players)
            {
                UpdateAbilities(logic, player, player.hero);  // 更新英雄持续能力

                foreach (Card card in player.cards_board)
                {
                    UpdateAbilities(logic, player, card);
                }

                foreach (Card card in player.cards_equip)
                {
                    UpdateAbilities(logic, player, card);
                }
            }
        }

        private void UpdateAbilities(GameLogic logic, Player player, Card card)
        {
            if (card == null || !card.CanDoAbilities()) return;

            foreach (AbilityData ability in card.GetAbilities())
            {
                if (ability == null || ability.trigger != AbilityTrigger.Ongoing) continue;
                if (!ability.AreTriggerConditionsMet(game, card)) continue;

                ResolveOngoingAbility(logic, player, card, ability);
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

        private void ResolveOngoingAbility(GameLogic logic, Player player, Card card, AbilityData ability)
        {
            if (ability.target == AbilityTarget.Self)
            {
                if (ability.AreTargetConditionsMet(game, card, card))
                {
                    ability.DoOngoingEffects(logic, card, card);
                }
            }

            if (ability.target == AbilityTarget.PlayerSelf)
            {
                if (ability.AreTargetConditionsMet(game, card, player))
                {
                    ability.DoOngoingEffects(logic, card, player);
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
                        ability.DoOngoingEffects(logic, card, targetPlayer);
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
                        ability.DoOngoingEffects(logic, card, target); // 对承载者执行持续效果
                    }
                }
                else if (card.equipped_uid != null)
                {
                    // 获取被装备的卡牌
                    Card target = game.GetCard(card.equipped_uid);
                    if (target != null && ability.AreTargetConditionsMet(game, card, target))
                    {
                        ability.DoOngoingEffects(logic, card, target); // 对装备卡牌执行持续效果
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
                                ability.DoOngoingEffects(logic, card, targetCard);
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
                                ability.DoOngoingEffects(logic, card, targetCard);
                            }
                        }
                    }

                    // 装备卡牌
                    if (ability.target == AbilityTarget.AllCardsAllPiles)
                    {
                        foreach (Card targetCard in player.cards_equip)
                        {
                            if (ability.AreTargetConditionsMet(game, card, targetCard))
                            {
                                ability.DoOngoingEffects(logic, card, targetCard);
                            }
                        }
                    }
                }
            }
        }
    }
}