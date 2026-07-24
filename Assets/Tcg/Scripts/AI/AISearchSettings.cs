using System;

namespace TcgEngine.AI
{
    /// <summary>
    /// Minimax 搜索的规模限制。配置创建后不可变，可安全地在多次搜索间复用。
    /// </summary>
    public sealed class AISearchSettings
    {
        public static AISearchSettings Default { get; } = new(
            maxTurnDepth: 3,
            wideSearchDepth: 1,
            maxActionsPerTurn: 2,
            wideMaxActionsPerTurn: 3,
            maxBranchesPerAction: 4,
            wideMaxBranchesPerAction: 7);

        public int MaxTurnDepth { get; }
        public int WideSearchDepth { get; }
        public int MaxActionsPerTurn { get; }
        public int WideMaxActionsPerTurn { get; }
        public int MaxBranchesPerAction { get; }
        public int WideMaxBranchesPerAction { get; }

        public AISearchSettings(
            int maxTurnDepth,
            int wideSearchDepth,
            int maxActionsPerTurn,
            int wideMaxActionsPerTurn,
            int maxBranchesPerAction,
            int wideMaxBranchesPerAction)
        {
            if (maxTurnDepth < 1)
                throw new ArgumentOutOfRangeException(nameof(maxTurnDepth));
            if (wideSearchDepth < 0 || wideSearchDepth > maxTurnDepth)
                throw new ArgumentOutOfRangeException(nameof(wideSearchDepth));
            if (maxActionsPerTurn < 1)
                throw new ArgumentOutOfRangeException(nameof(maxActionsPerTurn));
            if (wideMaxActionsPerTurn < 1)
                throw new ArgumentOutOfRangeException(nameof(wideMaxActionsPerTurn));
            if (maxBranchesPerAction < 1)
                throw new ArgumentOutOfRangeException(nameof(maxBranchesPerAction));
            if (wideMaxBranchesPerAction < 1)
                throw new ArgumentOutOfRangeException(nameof(wideMaxBranchesPerAction));

            MaxTurnDepth = maxTurnDepth;
            WideSearchDepth = wideSearchDepth;
            MaxActionsPerTurn = maxActionsPerTurn;
            WideMaxActionsPerTurn = wideMaxActionsPerTurn;
            MaxBranchesPerAction = maxBranchesPerAction;
            WideMaxBranchesPerAction = wideMaxBranchesPerAction;
        }
    }
}
