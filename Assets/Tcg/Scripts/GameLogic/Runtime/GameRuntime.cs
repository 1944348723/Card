using System;
using System.Collections.Generic;

namespace TcgEngine.Gameplay
{
    /// <summary>
    /// 规则执行期间的非同步运行时依赖。
    /// </summary>
    public sealed class GameRuntime
    {
        public Game Game { get; private set; }
        public BoardLayout Board => Game.Board;
        public ResolveQueue ResolveQueue { get; }
        public bool IsAiSimulation { get; }
        public Random Random { get; }

        public CardZoneManager Zones { get; }
        public CardLifecycle Cards { get; }
        public OngoingResolver Ongoings { get; }
        public SecretResolver Secrets { get; }
        public CombatResolver Combat { get; }
        public MatchFlow Flow { get; }
        public SelectionResolver Selection { get; }
        public AbilityResolver Abilities { get; }
        public ActionExecutor Actions { get; }
        public DamageResolver Damage { get; }
        public DeckBuilder Decks { get; }
        public GameRules Rules { get; }
        public GameRuntimeEvents Events { get; } = new();
        public EffectContext Effects { get; }

        public ListSwap<Card> CardTargets { get; } = new();
        public ListSwap<Player> PlayerTargets { get; } = new();
        public ListSwap<Slot> SlotTargets { get; } = new();
        public ListSwap<CardData> CardDataTargets { get; } = new();
        public List<Card> CardsToClear { get; } = new();

        public GameRuntime(Game game, bool isAiSimulation, Random random = null)
        {
            ResolveQueue = new ResolveQueue(game, isAiSimulation);
            IsAiSimulation = isAiSimulation;
            Random = random ?? new Random();
            Rules = new GameRules(game);
            Zones = new CardZoneManager(this);
            Cards = new CardLifecycle(this);
            Ongoings = new OngoingResolver(this);
            Secrets = new SecretResolver(this);
            Combat = new CombatResolver(this);
            Flow = new MatchFlow(this);
            Selection = new SelectionResolver(this);
            Abilities = new AbilityResolver(this);
            Actions = new ActionExecutor(this);
            Damage = new DamageResolver(this);
            Decks = new DeckBuilder(this);
            Effects = new EffectContext(this);
            SetData(game);
        }

        public void SetData(Game game)
        {
            Game = game;
            ResolveQueue.SetData(game);
            Rules.SetData(game);
        }

        public void ClearTargetCaches()
        {
            CardTargets.Clear();
            PlayerTargets.Clear();
            SlotTargets.Clear();
            CardDataTargets.Clear();
        }

        public void UpdateOngoings()
        {
            Ongoings.UpdateOngoings();
            Cards.CleanupInvalidCards(CardsToClear);
        }
    }
}
