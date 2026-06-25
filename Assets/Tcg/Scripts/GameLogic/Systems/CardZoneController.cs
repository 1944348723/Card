using System.Collections.Generic;
using System.Diagnostics;

namespace TcgEngine.Gameplay
{
    public class CardZoneController
    {
        public int DrawCards(Player player, int count = 1)
        {
            int drawn = 0;
            for (int i = 0; i < count; i++)
            {
                if (player.cards_deck.Count > 0 && player.cards_hand.Count < GameplayData.Get().cards_max)
                {
                    Card card = player.cards_deck[0];
                    player.cards_deck.RemoveAt(0);
                    player.cards_hand.Add(card);
                    ++drawn;
                }
            }
            return drawn;
        }

        public void DiscardCardsFromHand(Player player, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                if (player.cards_hand.Count > 0)
                {
                    Card card = player.cards_hand[0];
                    player.cards_hand.RemoveAt(0);
                    player.cards_discard.Add(card);
                }
            }
        }

        // 返回原来的装备
        public Card EquipCard(Player player, Card bearer, Card equipment)
        {
            if (player == null || bearer == null || equipment == null || bearer.player_id != equipment.player_id) return null;
            if (bearer.CardData.IsEquipment() || !equipment.CardData.IsEquipment()) return null;

            Card oldEquipment = UnequipCard(player, bearer);

            MoveToEquip(player, equipment);
            bearer.equipped_uid = equipment.uid;
            equipment.slot = bearer.slot;         // 装备位置与卡牌一致

            return oldEquipment;
        }

        public Card UnequipCard(Player player, Card bearer)
        {
            if (player == null || bearer == null || bearer.equipped_uid == null) return null;

            Card equipment = player.GetEquipCard(bearer.equipped_uid);
            bearer.equipped_uid = null;
            return equipment;
        }

        public void MoveToDeck(Player player, Card card)
        {
            MoveToPile(player, card, player.cards_deck);
        }

        public void MoveToHand(Player player, Card card)
        {
            MoveToPile(player, card, player.cards_hand);
        }

        public void MoveToSecret(Player player, Card card)
        {
            MoveToPile(player, card, player.cards_secret);
        }

        public void MoveToEquip(Player player, Card card)
        {
            MoveToPile(player, card, player.cards_equip);
        }

        public void MoveToDiscard(Player player, Card card)
        {
            MoveToPile(player, card, player.cards_discard);
        }

        public void MoveToBoard(Player player, Card card, Slot slot)
        {
            MoveToPile(player, card, player.cards_board);
            card.slot = slot;
        }

        private void MoveToPile(Player player, Card card, List<Card> pile)
        {
            Debug.Assert(IsInMoreThanOneZone(player, card), $"Card zone stats invalid: {card.uid}");
            player.RemoveCardFromAllGroups(card);
            pile.Add(card);
        }

        private bool IsInMoreThanOneZone(Player player, Card card)
        {
            int count = 0;
            if (player.cards_deck.Contains(card)) count++;
            if (player.cards_hand.Contains(card)) count++;
            if (player.cards_board.Contains(card)) count++;
            if (player.cards_equip.Contains(card)) count++;
            if (player.cards_discard.Contains(card)) count++;
            if (player.cards_secret.Contains(card)) count++;
            if (player.cards_temp.Contains(card)) count++;
            return count > 1;
        }
    }
}