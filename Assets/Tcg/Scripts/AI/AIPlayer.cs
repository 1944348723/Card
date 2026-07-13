using TcgEngine.Gameplay;

namespace TcgEngine.AI
{
    /// <summary>
    /// AI 玩家基类，其他具体 AI（如随机 AI、Minimax AI）都继承自这个类
    /// </summary>
    public abstract class AIPlayer 
    {
        public int player_id;   // AI 所控制的玩家 ID
        public int ai_level;    // AI 等级，可以用来调整难度或搜索深度

        protected GameLogic gameplay;  // 游戏逻辑对象，用于访问和操作游戏状态

        /// <summary>
        /// 每帧或定时调用的更新函数
        /// 子类需要重写此方法实现具体 AI 行为
        /// </summary>
        public virtual void Update()
        {
            // 由游戏服务器调用以更新 AI
            // 重写这个方法来让 AI 执行动作
        }

        /// <summary>
        /// 检查 AI 是否可以执行操作
        /// AI 只能在轮到自己行动或选牌阶段（Mulligan）执行动作，并且游戏当前不在处理动作中
        /// </summary>
        /// <returns>返回 true 表示 AI 可以执行操作</returns>
        public bool CanPlay()
        {
            Game game_data = gameplay.GetGameData();        // 获取当前游戏状态
            Player player = game_data.GetPlayer(player_id); // 获取 AI 控制的玩家对象

            // AI 可以操作的条件：
            // 1. 当前轮到该玩家
            // 2. 或者正在进行初始选牌阶段（Mulligan）
            bool can_play = gameplay.Rules.IsPlayerTurn(player) || gameplay.Rules.IsPlayerMulliganTurn(player);

            // 同时要求游戏当前不在解决其他动作中
            return can_play && !gameplay.IsResolving();
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
            if (type == AIType.Random)
                return new AIPlayerRandom(gameplay, id, level); // 随机 AI
            if (type == AIType.MiniMax)
                return new AIPlayerMM(gameplay, id, level);     // 使用 Minimax + alpha-beta 剪枝的 AI
            return null; // 未知类型返回 null
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
