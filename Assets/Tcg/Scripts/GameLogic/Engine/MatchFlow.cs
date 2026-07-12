using UnityEngine;

namespace TcgEngine.Gameplay
{
    /// <summary>开局、回合转换、换牌和胜负结算。</summary>
    public sealed class MatchFlow
    {
        private readonly GameRuntime runtime;

        public MatchFlow(GameRuntime runtime)
        {
            this.runtime = runtime;
        }

        public void StartGame()
        {
            if (runtime.Game.state == GameState.GameEnded)
                return;

            runtime.Game.state = GameState.Play;
            runtime.Game.first_player = runtime.Random.NextDouble() < 0.5 ? 0 : 1;
            runtime.Game.current_player = runtime.Game.first_player;
            runtime.Game.turn_count = 1;

            bool shouldMulligan = GameplayData.Get().mulligan;
            LevelData level = runtime.Game.settings.GetLevel();
            if (level != null)
            {
                if (level.first_player == LevelFirst.Player)
                    runtime.Game.first_player = 0;
                if (level.first_player == LevelFirst.AI)
                    runtime.Game.first_player = 1;

                runtime.Game.current_player = runtime.Game.first_player;
                shouldMulligan = level.mulligan;
            }

            foreach (Player player in runtime.Game.players)
            {
                DeckPuzzleData deck = DeckPuzzleData.Get(player.deck);
                player.hp_max = deck != null ? deck.start_hp : GameplayData.Get().hp_start;
                player.hp = player.hp_max;
                player.mana_max = deck != null ? deck.start_mana : GameplayData.Get().mana_start;
                player.mana = player.mana_max;

                int cards = deck != null ? deck.start_cards : GameplayData.Get().cards_start;
                runtime.Engine.DrawCards(player, cards);

                bool isRandomFirstPlayer = level == null || level.first_player == LevelFirst.Random;
                if (isRandomFirstPlayer
                    && player.player_id != runtime.Game.first_player
                    && GameplayData.Get().second_bonus != null)
                {
                    Card bonus = Card.Create(GameplayData.Get().second_bonus, VariantData.GetDefault(), player);
                    player.cards_hand.Add(bonus);
                }
            }

            runtime.Engine.RefreshData();
            runtime.Engine.onGameStart?.Invoke();

            if (shouldMulligan)
                runtime.Engine.BeginMulliganFromEngine();
            else
                StartTurn();
        }

        public void StartTurn()
        {
            if (runtime.Game.state == GameState.GameEnded)
                return;

            ClearTurnData();
            runtime.Game.phase = GamePhase.StartTurn;
            runtime.Engine.RefreshData();
            runtime.Engine.onTurnStart?.Invoke();

            Player player = runtime.Game.GetActivePlayer();
            if (runtime.Game.turn_count > 1 || player.player_id != runtime.Game.first_player)
                runtime.Engine.DrawCards(player, GameplayData.Get().cards_per_turn);

            player.mana_max += GameplayData.Get().mana_per_turn;
            player.mana_max = Mathf.Min(player.mana_max, GameplayData.Get().mana_max);
            player.mana = player.mana_max;
            runtime.Game.turn_timer = GameplayData.Get().turn_duration;
            player.history_list.Clear();

            runtime.Engine.DamagePlayer(player, player.GetStatusValue(StatusType.Poisoned), DamageType.Status);
            player.hero?.Refresh();

            for (int i = player.cards_board.Count - 1; i >= 0; i--)
            {
                Card card = player.cards_board[i];
                if (!card.HasStatus(StatusType.Sleep))
                    card.Refresh();
                if (card.HasStatus(StatusType.Poisoned))
                    runtime.Engine.DamageCard(card, card.GetStatusValue(StatusType.Poisoned), DamageType.Status);
            }

            runtime.Engine.UpdateOngoings();
            runtime.Engine.TriggerPlayerCardsAbilityType(player, AbilityTrigger.StartOfTurn);
            runtime.Engine.TriggerPlayerSecrets(player, AbilityTrigger.StartOfTurn);
            runtime.ResolveQueue.AddCallback(StartMainPhase);
            runtime.ResolveQueue.ResolveAll(0.2f);
        }

        public void StartNextTurn()
        {
            if (runtime.Game.state == GameState.GameEnded)
                return;

            runtime.Game.current_player = (runtime.Game.current_player + 1) % runtime.Game.settings.nb_players;
            if (runtime.Game.current_player == runtime.Game.first_player)
                runtime.Game.turn_count++;

            CheckForWinner();
            StartTurn();
        }

        public void StartMainPhase()
        {
            if (runtime.Game.state == GameState.GameEnded)
                return;

            runtime.Game.phase = GamePhase.Main;
            runtime.Engine.onTurnPlay?.Invoke();
            runtime.Engine.RefreshData();
        }

        public void EndTurn()
        {
            if (runtime.Game.state == GameState.GameEnded || runtime.Game.phase != GamePhase.Main)
                return;

            runtime.Game.selector = SelectorType.None;
            runtime.Game.phase = GamePhase.EndTurn;

            foreach (Player player in runtime.Game.players)
            {
                player.ReduceStatusDurations();
                foreach (Card card in player.cards_board)
                    card.ReduceStatusDurations();
                foreach (Card card in player.cards_equip)
                    card.ReduceStatusDurations();
            }

            Player activePlayer = runtime.Game.GetActivePlayer();
            runtime.Engine.TriggerPlayerCardsAbilityType(activePlayer, AbilityTrigger.EndOfTurn);
            runtime.Engine.onTurnEnd?.Invoke();
            runtime.Engine.RefreshData();
            runtime.ResolveQueue.AddCallback(StartNextTurn);
            runtime.ResolveQueue.ResolveAll(0.2f);
        }

        public void EndGame(int winner)
        {
            if (runtime.Game.state == GameState.GameEnded)
                return;

            runtime.Game.state = GameState.GameEnded;
            runtime.Game.phase = GamePhase.None;
            runtime.Game.selector = SelectorType.None;
            runtime.Game.current_player = winner;
            runtime.ResolveQueue.Clear();
            runtime.Engine.onGameEnd?.Invoke(runtime.Game.GetPlayer(winner));
            runtime.Engine.RefreshData();
        }

        public void NextStep()
        {
            if (runtime.Game.state == GameState.GameEnded)
                return;

            if (runtime.Game.phase == GamePhase.Mulligan)
            {
                StartTurn();
                return;
            }

            runtime.Engine.CancelSelection();
            runtime.ResolveQueue.AddCallback(EndTurn);
            runtime.ResolveQueue.ResolveAll();
        }

        public void CheckForWinner()
        {
            int aliveCount = 0;
            Player lastAlive = null;
            foreach (Player player in runtime.Game.players)
            {
                if (player.IsDead())
                    continue;

                lastAlive = player;
                aliveCount++;
            }

            if (aliveCount == 0)
                EndGame(-1);
            else if (aliveCount == 1)
                EndGame(lastAlive.player_id);
        }

        private void ClearTurnData()
        {
            runtime.Game.selector = SelectorType.None;
            runtime.ResolveQueue.Clear();
            runtime.ClearTargetCaches();
            runtime.Game.last_played = null;
            runtime.Game.last_destroyed = null;
            runtime.Game.last_target = null;
            runtime.Game.last_summoned = null;
            runtime.Game.ability_triggerer = null;
            runtime.Game.selected_value = 0;
            runtime.Game.ability_played.Clear();
            runtime.Game.cards_attacked.Clear();
        }
    }
}
