namespace TcgEngine.Gameplay
{
    public sealed class CardSystem
    {
        private Game game;
        private readonly CardZoneService cardZoneService;

        public CardSystem(Game game, CardZoneService zones)
        {
            this.game = game;
            this.cardZoneService = zones;
        }

        public void SetData(Game game)
        {
            this.game = game;
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
    }
}