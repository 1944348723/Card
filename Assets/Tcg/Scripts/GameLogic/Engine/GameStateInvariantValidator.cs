using System;
using System.Collections.Generic;

namespace TcgEngine.Gameplay
{
    /// <summary>Validates cross-object state invariants at rule transaction boundaries.</summary>
    public static class GameStateInvariantValidator
    {
        public static List<string> Validate(Game game)
        {
            List<string> errors = new();
            if (game?.players == null)
            {
                errors.Add("Game or players is null.");
                return errors;
            }

            HashSet<string> globalUids = new();
            foreach (Player player in game.players)
            {
                if (player == null)
                {
                    errors.Add("Player entry is null.");
                    continue;
                }

                Dictionary<Card, string> locations = new();
                AddZone(player, player.cards_deck, "Deck", locations, errors);
                AddZone(player, player.cards_hand, "Hand", locations, errors);
                AddZone(player, player.cards_board, "Board", locations, errors);
                AddZone(player, player.cards_equip, "Equip", locations, errors);
                AddZone(player, player.cards_discard, "Discard", locations, errors);
                AddZone(player, player.cards_secret, "Secret", locations, errors);
                AddZone(player, player.cards_temp, "Temp", locations, errors);

                if (player.hero != null)
                    AddLocation(player, player.hero, "Hero", locations, errors);

                foreach (KeyValuePair<string, Card> pair in player.cards_all)
                {
                    Card card = pair.Value;
                    if (card == null)
                    {
                        errors.Add($"Player {player.player_id}: cards_all[{pair.Key}] is null.");
                        continue;
                    }

                    if (pair.Key != card.uid)
                        errors.Add($"Player {player.player_id}: dictionary key {pair.Key} != card uid {card.uid}.");
                    if (!globalUids.Add(card.uid))
                        errors.Add($"Duplicate card uid across players: {card.uid}.");
                    if (!locations.ContainsKey(card))
                        errors.Add($"Player {player.player_id}: card {card.uid} has no location.");
                }

                foreach (KeyValuePair<Card, string> pair in locations)
                {
                    Card card = pair.Key;
                    if (!player.cards_all.TryGetValue(card.uid, out Card indexed) || !ReferenceEquals(indexed, card))
                        errors.Add($"Player {player.player_id}: {pair.Value} card {card.uid} is not the indexed instance.");
                    if (card.player_id != player.player_id)
                        errors.Add($"Player {player.player_id}: {pair.Value} card {card.uid} belongs to {card.player_id}.");
                    if (pair.Value == "Board" && !game.Board.Contains(card.slot))
                        errors.Add($"Player {player.player_id}: board card {card.uid} has invalid slot.");
                    else if (pair.Value == "Board" && card.slot.p != player.player_id)
                        errors.Add($"Player {player.player_id}: board card {card.uid} has slot owner {card.slot.p}.");
                }

                ValidateEquipment(player, errors);
            }

            return errors;
        }

        public static void ThrowIfInvalid(Game game)
        {
            List<string> errors = Validate(game);
            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
        }

        private static void AddZone(Player player, List<Card> cards, string zone,
            Dictionary<Card, string> locations, List<string> errors)
        {
            foreach (Card card in cards)
                AddLocation(player, card, zone, locations, errors);
        }

        private static void AddLocation(Player player, Card card, string location,
            Dictionary<Card, string> locations, List<string> errors)
        {
            if (card == null)
            {
                errors.Add($"Player {player.player_id}: {location} contains null.");
                return;
            }

            if (locations.TryGetValue(card, out string existing))
                errors.Add($"Player {player.player_id}: card {card.uid} is in both {existing} and {location}.");
            else
                locations.Add(card, location);
        }

        private static void ValidateEquipment(Player player, List<string> errors)
        {
            foreach (Card bearer in player.cards_board)
            {
                if (bearer?.equipped_uid == null)
                    continue;

                Card equipment = player.GetEquipCard(bearer.equipped_uid);
                if (equipment == null)
                    errors.Add($"Player {player.player_id}: bearer {bearer.uid} references missing equipment {bearer.equipped_uid}.");
            }

            foreach (Card equipment in player.cards_equip)
            {
                if (equipment != null && player.GetBearerCard(equipment) == null)
                    errors.Add($"Player {player.player_id}: equipment {equipment.uid} has no bearer.");
            }
        }
    }
}
