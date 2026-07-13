using System.Collections.Generic;

namespace TcgEngine.Gameplay
{
    /// <summary>卡牌所在区域。显式数值用于兼容已有 Unity 序列化资源。</summary>
    public enum CardZone
    {
        None = 0,
        Board = 10,
        Hand = 20,
        Deck = 30,
        Discard = 40,
        Secret = 50,
        Equip = 60,
        Temp = 90,
    }

    /// <summary>维护卡牌区域、所有者索引和装备引用的一致性。</summary>
    public sealed class CardZoneManager
    {
        private readonly GameRuntime runtime;

        public CardZoneManager(GameRuntime runtime)
        {
            this.runtime = runtime;
        }

        public bool MoveTo(Card card, CardZone destination)
        {
            Player owner = GetOwner(card);
            List<Card> target = GetPile(owner, destination);
            if (owner == null || target == null || destination == CardZone.Board)
                return false;

            RemoveFromZones(owner, card);
            target.Add(card);
            return true;
        }

        public bool MoveToBoard(Card card, Slot slot)
        {
            Player owner = GetOwner(card);
            if (owner == null || !runtime.Board.Contains(slot) || !slot.BelongsToPlayer(owner.player_id))
                return false;
            if (runtime.Game.HasCardOnSlot(slot) && runtime.Game.GetSlotCard(slot) != card)
                return false;

            RemoveFromZones(owner, card);
            owner.cards_board.Add(card);
            card.slot = slot;
            return true;
        }

        /// <summary>保留卡牌当前区域并原子地转移所有权；无法保持合法状态时不修改。</summary>
        public bool TransferOwnership(Card card, Player newOwner)
        {
            Player oldOwner = GetOwner(card);
            if (oldOwner == null || newOwner == null || oldOwner == newOwner)
                return false;

            CardZone zone = FindZone(oldOwner, card);
            if (zone == CardZone.None || zone == CardZone.Equip)
                return false;

            Slot destination = card.slot;
            Card equipment = null;
            if (zone == CardZone.Board)
            {
                destination = new Slot(card.slot.x, card.slot.y, newOwner.player_id);
                if (!runtime.Board.Contains(destination))
                    return false;

                Card occupant = runtime.Game.GetSlotCard(destination);
                if (occupant != null && occupant != card)
                    return false;

                if (!string.IsNullOrEmpty(card.equipped_uid))
                {
                    equipment = oldOwner.GetEquipCard(card.equipped_uid);
                    if (equipment == null)
                        return false;
                }
            }

            RemoveFromZones(oldOwner, card, detachEquipment: false);
            oldOwner.cards_all.Remove(card.uid);
            card.player_id = newOwner.player_id;
            card.slot = destination;
            newOwner.cards_all[card.uid] = card;
            GetPile(newOwner, zone).Add(card);

            if (equipment != null)
            {
                oldOwner.cards_equip.Remove(equipment);
                oldOwner.cards_all.Remove(equipment.uid);
                equipment.player_id = newOwner.player_id;
                equipment.slot = destination;
                newOwner.cards_all[equipment.uid] = equipment;
                newOwner.cards_equip.Add(equipment);
            }
            return true;
        }

        public void ClearTemporary(Player player)
        {
            if (player == null) return;

            foreach (Card card in player.cards_temp)
                player.cards_all.Remove(card.uid);
            player.cards_temp.Clear();
        }

        public CardZone FindZone(Player player, Card card)
        {
            if (player == null || card == null) return CardZone.None;
            if (player.cards_board.Contains(card)) return CardZone.Board;
            if (player.cards_hand.Contains(card)) return CardZone.Hand;
            if (player.cards_deck.Contains(card)) return CardZone.Deck;
            if (player.cards_discard.Contains(card)) return CardZone.Discard;
            if (player.cards_secret.Contains(card)) return CardZone.Secret;
            if (player.cards_equip.Contains(card)) return CardZone.Equip;
            if (player.cards_temp.Contains(card)) return CardZone.Temp;
            return CardZone.None;
        }

        private Player GetOwner(Card card)
        {
            if (card == null) return null;
            Player owner = runtime.Game.GetPlayer(card.player_id);
            if (owner == null || !owner.cards_all.TryGetValue(card.uid, out Card registered) || registered != card)
                return null;
            return owner;
        }

        private void RemoveFromZones(Player player, Card card, bool detachEquipment = true)
        {
            player.cards_deck.Remove(card);
            player.cards_hand.Remove(card);
            player.cards_board.Remove(card);
            player.cards_equip.Remove(card);
            player.cards_discard.Remove(card);
            player.cards_secret.Remove(card);
            player.cards_temp.Remove(card);

            if (detachEquipment)
            {
                foreach (Card bearer in player.cards_board)
                {
                    if (bearer.equipped_uid == card.uid)
                        bearer.equipped_uid = null;
                }
            }
        }

        private static List<Card> GetPile(Player player, CardZone zone)
        {
            if (player == null) return null;
            return zone switch
            {
                CardZone.Deck => player.cards_deck,
                CardZone.Hand => player.cards_hand,
                CardZone.Equip => player.cards_equip,
                CardZone.Discard => player.cards_discard,
                CardZone.Secret => player.cards_secret,
                CardZone.Temp => player.cards_temp,
                _ => null,
            };
        }
    }
}
