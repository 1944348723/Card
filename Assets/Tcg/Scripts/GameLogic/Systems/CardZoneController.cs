namespace TcgEngine.Gameplay
{
    public class CardZoneController
    {
        public virtual void DrawCards(Player player, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                if (player.cards_deck.Count > 0 && player.cards_hand.Count < GameplayData.Get().cards_max)
                {
                    Card card = player.cards_deck[0];
                    player.cards_deck.RemoveAt(0);
                    player.cards_hand.Add(card);
                }
            }
        }
    }
}