using System;
using TcgEngine.Gameplay;

namespace TcgEngine.AI
{
    /// <summary>
    /// AI 玩家基类，其他具体 AI（如随机 AI、Minimax AI）都继承自这个类
    /// </summary>
    public abstract class AIPlayer 
    {
        public int PlayerId { get; }
        public int Level { get; }

        protected readonly GameLogic gameplay;

        protected AIPlayer(GameLogic gameplay, int playerId, int level)
        {
            this.gameplay = gameplay ?? throw new ArgumentNullException(nameof(gameplay));
            PlayerId = playerId;
            Level = level;
        }

        /// <summary>
        /// 每帧或定时调用的更新函数
        /// 子类需要重写此方法实现具体 AI 行为
        /// </summary>
        public abstract void Update();

        /// <summary>
        /// 是否可以执行出牌、攻击或结束回合等普通回合动作。
        /// </summary>
        protected bool CanTakeAction()
        {
            if (gameplay.IsResolving())
                return false;

            return gameplay.Rules.IsPlayerActionTurn(GetPlayer());
        }

        /// <summary>
        /// 是否可以响应当前的目标、卡牌、选项或费用选择器。
        /// </summary>
        protected bool CanResolveSelection()
        {
            if (gameplay.IsResolving())
                return false;

            return gameplay.Rules.IsPlayerSelectorTurn(GetPlayer());
        }

        /// <summary>
        /// 是否可以提交初始换牌结果。
        /// </summary>
        protected bool CanMulligan()
        {
            if (gameplay.IsResolving())
                return false;

            Player player = GetPlayer();
            return player != null && gameplay.Rules.IsPlayerMulliganTurn(player);
        }

        protected Player GetPlayer()
        {
            return gameplay.GetGameData().GetPlayer(PlayerId);
        }

        /// <summary>
        /// 根据 AI 类型创建具体 AI 实例
        /// </summary>
        /// <param name="type">AI 类型（随机 / Minimax）</param>
        /// <param name="gameplay">游戏逻辑对象</param>
        /// <param name="id">玩家 ID</param>
        /// <param name="level">AI 等级，默认 0</param>
        /// <returns>返回创建好的 AIPlayer 对象</returns>
        public static AIPlayer Create(AIType type, GameLogic gameplay, int id, int level = 0)
        {
            return type switch
            {
                AIType.Random => new AIPlayerRandom(gameplay, id, level),
                AIType.MiniMax => new AIPlayerMM(gameplay, id, level),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "不支持的 AI 类型"),
            };
        }
    }

    /// <summary>
    /// AI 类型枚举
    /// </summary>
    public enum AIType
    {
        Random = 0,      // 随机 AI，只做随机操作，适合测试卡牌，不会太强
        MiniMax = 10,    // 使用 Minimax 算法 + Alpha-Beta 剪枝的高级 AI，更聪明
    }
}
