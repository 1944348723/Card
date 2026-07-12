using System.Collections.Generic;

namespace TcgEngine.Gameplay
{
    public enum CardZone
    {
        Deck,
        Hand,
        Board,
        Equip,
        Discard,
        Secret,
        Temp,
    }

    /// <summary>所有卡牌区域变更的唯一入口。</summary>
    public sealed class CardZoneManager
    {
        public bool MoveTo(Player player, Card card, CardZone zone)
        {
            if (player == null || card == null) return false;

            player.RemoveCardFromAllGroups(card);
            GetPile(player, zone).Add(card);
            return true;
        }

        public bool MoveToBoard(Player player, Card card, Slot slot)
        {
            if (player == null || card == null || !slot.IsBoardSlot())  return false;

            player.RemoveCardFromAllGroups(card);
            player.cards_board.Add(card);
            card.slot = slot;
            return true;
        }

        public void ClearTemporary(Player player)
        {
            player?.cards_temp.Clear();
        }

        private List<Card> GetPile(Player player, CardZone zone)
        {
            return zone switch
            {
                CardZone.Deck => player.cards_deck,
                CardZone.Hand => player.cards_hand,
                CardZone.Board => player.cards_board,
                CardZone.Equip => player.cards_equip,
                CardZone.Discard => player.cards_discard,
                CardZone.Secret => player.cards_secret,
                CardZone.Temp => player.cards_temp,
                _ => player.cards_temp,
            };
        }
    }
}
