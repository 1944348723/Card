using System.Collections.Generic;

namespace TcgEngine.Gameplay
{
    public sealed class CardSystem
    {
        private readonly GameRuntimeContext runtimeContext;
        
        private Game game => runtimeContext.Game;
        private CardZoneService cardZoneService => runtimeContext.CardZoneService;

        public CardSystem(GameRuntimeContext context)
        {
            this.runtimeContext = context;
        }

        public void ShuffleDeck(List<Card> cards, System.Random random)
        {
            if (cards == null || random == null) return;

            for (int i = 0; i < cards.Count; i++)
            {
                int randomIndex = random.Next(i, cards.Count);
                (cards[randomIndex], cards[i]) = (cards[i], cards[randomIndex]);
            }
        }

        public int DrawCards(Player player, int count)
        {
            if (player == null) return 0;

            int drawn = 0;

            for (int i = 0; i < count; i++)
            {
                if (player.cards_deck.Count == 0)   break;
                if (player.cards_hand.Count >= GameplayData.Get().cards_max)    break;

                cardZoneService.MoveTo(player, player.cards_deck[0], CardZone.Hand);
                drawn++;
            }

            return drawn;
        }

        // TODO: variant不确定之前是否可以为null，如果出了什么问题可以看看是不是在这被拦了
        public Card CreateInHand(Player player, CardData data, VariantData variant)
        {
            if (player == null || data == null || variant == null) return null;

            Card card = Card.Create(data, variant, player);
            cardZoneService.MoveTo(player, card, CardZone.Hand);
            game.last_summoned = card.uid;
            return card;
        }

        public void DiscardCardsFromHand(Player player, int count = 1)
        {
            if (player == null) return;

            for (int i = 0; i < count; i++)
            {
                if (player.cards_hand.Count <= 0) break;

                cardZoneService.MoveTo(player, player.cards_hand[0], CardZone.Discard);
            }
        }

        public Card Equip(Card bearer, Card equipment)
        {
            if (bearer == null || equipment == null || bearer.player_id != equipment.player_id) return null;
            if (bearer.CardData.IsEquipment() || !equipment.CardData.IsEquipment()) return null;

            Player player = game.GetPlayer(bearer.player_id);
            Card old = Unequip(bearer);

            cardZoneService.MoveTo(player, equipment, CardZone.Equip);
            bearer.equipped_uid = equipment.uid;
            equipment.slot = bearer.slot;

            return old;
        }

        public Card Unequip(Card bearer)
        {
            if (bearer == null || bearer.equipped_uid == null)  return null;

            // TODO: 卸下装备后这里没有立刻将卡移出装备区，是因为目前逻辑得这样做，外部在DiscardCard中该卡因为其属于装备区可能还要触发什么东西
            // 但是这样感觉Unequip本身并不独立，强依赖于和DiscardCard联用，只调用Unequip的话调用后游戏状态是错误的
            Player player = game.GetPlayer(bearer.player_id);
            Card equipment = player.GetEquipCard(bearer.equipped_uid);
            bearer.equipped_uid = null;

            return equipment;
        }

        public void ChangeOwner(Card card, Player owner)
        {
            if (card == null || owner == null || card.player_id == owner.player_id) return;

            Player oldOwner = game.GetPlayer(card.player_id);
            oldOwner.RemoveCardFromAllGroups(card);
            oldOwner.cards_all.Remove(card.uid);

            owner.cards_all[card.uid] = card;
            card.player_id = owner.player_id;
        }
        
        // 杀掉HP为0的卡牌
        public void CleanupInvalidCards(GameLogic logic, List<Card> cardsToClear)
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

                // 上面清除场上卡牌后，可能剩下装备，也需要清理
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
    }
}