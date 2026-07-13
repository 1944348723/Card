namespace TcgEngine.Gameplay
{
    /// <summary>将资源或存档牌组装配为玩家的初始卡牌状态。</summary>
    public sealed class DeckBuilder
    {
        private readonly GameRuntime runtime;

        public DeckBuilder(GameRuntime runtime)
        {
            this.runtime = runtime;
        }

        public void SetDeck(Player player, DeckData deck)
        {
            ClearCards(player);
            player.deck = deck.id;
            player.hero = null;

            VariantData variant = VariantData.GetDefault();
            if (deck.hero != null)
                player.hero = Card.Create(deck.hero, variant, player);

            foreach (CardData card in deck.cards)
            {
                if (card != null)
                    player.cards_deck.Add(Card.Create(card, variant, player));
            }

            DeckPuzzleData puzzle = deck as DeckPuzzleData;
            if (puzzle != null)
            {
                foreach (DeckCardSlot boardCard in puzzle.board_cards)
                {
                    Card card = Card.Create(boardCard.card, variant, player);
                    card.slot = new Slot(boardCard.slot, player.player_id);
                    player.cards_board.Add(card);
                }
            }

            if (puzzle == null || !puzzle.dont_shuffle_deck)
                runtime.Cards.ShuffleDeck(player.cards_deck, runtime.Random);
        }

        public void SetDeck(Player player, UserDeckData deck)
        {
            ClearCards(player);
            player.deck = deck.tid;
            player.hero = null;

            if (deck.hero != null)
            {
                CardData data = CardData.Get(deck.hero.tid);
                VariantData variant = VariantData.Get(deck.hero.variant);
                if (data != null && variant != null)
                    player.hero = Card.Create(data, variant, player);
            }

            foreach (UserCardData userCard in deck.cards)
            {
                CardData data = CardData.Get(userCard.tid);
                VariantData variant = VariantData.Get(userCard.variant);
                if (data == null || variant == null)
                    continue;

                for (int i = 0; i < userCard.quantity; i++)
                    player.cards_deck.Add(Card.Create(data, variant, player));
            }

            runtime.Cards.ShuffleDeck(player.cards_deck, runtime.Random);
        }

        private static void ClearCards(Player player)
        {
            player.cards_all.Clear();
            player.cards_deck.Clear();
            player.cards_hand.Clear();
            player.cards_board.Clear();
            player.cards_equip.Clear();
            player.cards_discard.Clear();
            player.cards_secret.Clear();
            player.cards_temp.Clear();
            player.hero = null;
        }
    }
}
