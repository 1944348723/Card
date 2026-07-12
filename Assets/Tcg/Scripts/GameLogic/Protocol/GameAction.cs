
namespace TcgEngine
{
    /// <summary>
    /// 网络协议中的游戏动作与刷新操作。
    /// 这些动作可以由玩家执行，或者由服务器发送给客户端
    /// </summary>
    public static class GameAction
    {
        public const ushort None = 0; // 无动作

        // ----- 客户端发送到服务器的命令 -----
        public const ushort PlayCard = 1000;       // 出牌
        public const ushort Attack = 1010;         // 攻击其他卡牌
        public const ushort AttackPlayer = 1012;   // 攻击玩家
        public const ushort Move = 1015;           // 移动卡牌
        public const ushort CastAbility = 1020;    // 施放能力
        public const ushort SelectCard = 1030;     // 选择卡牌
        public const ushort SelectPlayer = 1032;   // 选择玩家
        public const ushort SelectSlot = 1034;     // 选择槽位
        public const ushort SelectChoice = 1036;   // 选择选项
        public const ushort SelectCost = 1037;     // 选择消耗
        public const ushort SelectMulligan = 1038; // 握手/换牌阶段选择
        public const ushort CancelSelect = 1039;   // 取消选择
        public const ushort EndTurn = 1040;        // 结束回合
        public const ushort Resign = 1050;         // 投降
        public const ushort ChatMessage = 1090;    // 聊天信息

        public const ushort PlayerSettings = 1100;   // 连接后发送玩家数据
        public const ushort PlayerSettingsAI = 1102; // AI玩家数据
        public const ushort GameSettings = 1105;     // 连接后发送游戏设置

        // ----- 服务器发送到客户端的刷新 -----
        public const ushort Connected = 2000;      // 已连接
        public const ushort PlayerReady = 2001;    // 玩家准备完成

        public const ushort GameStart = 2010;      // 游戏开始
        public const ushort GameEnd = 2012;        // 游戏结束
        public const ushort NewTurn = 2015;        // 新回合开始

        public const ushort CardPlayed = 2020;       // 卡牌已出
        public const ushort CardSummoned = 2022;     // 卡牌召唤
        public const ushort CardTransformed = 2023;  // 卡牌变形
        public const ushort CardDiscarded = 2025;    // 卡牌弃掉
        public const ushort CardDrawn = 2026;        // 卡牌抽取
        public const ushort CardMoved = 2027;        // 卡牌移动

        public const ushort AttackStart = 2030;          // 攻击开始
        public const ushort AttackEnd = 2031;            // 攻击结束
        public const ushort AttackPlayerStart = 2032;   // 攻击玩家开始
        public const ushort AttackPlayerEnd = 2033;     // 攻击玩家结束
        public const ushort CardDamaged = 2036;         // 卡牌受伤
        public const ushort PlayerDamaged = 2037;       // 玩家受伤
        public const ushort CardHealed = 2038;          // 卡牌恢复生命
        public const ushort PlayerHealed = 2039;        // 玩家恢复生命

        public const ushort AbilityTrigger = 2040;      // 能力触发
        public const ushort AbilityTargetCard = 2042;   // 能力作用于卡牌
        public const ushort AbilityTargetPlayer = 2043; // 能力作用于玩家
        public const ushort AbilityTargetSlot = 2044;   // 能力作用于槽位
        public const ushort AbilityEnd = 2048;          // 能力结束

        public const ushort SecretTriggered = 2060;     // 秘密触发
        public const ushort SecretResolved = 2061;      // 秘密结算
        public const ushort ValueRolled = 2070;         // 掷骰结果

        public const ushort ServerMessage = 2190;       // 服务器提示消息
        public const ushort RefreshAll = 2100;          // 刷新所有数据

        /// <summary>
        /// 将动作类型转换为字符串，用于日志或网络传输
        /// </summary>
        /// <param name="type">动作类型</param>
        /// <returns>动作名称字符串</returns>
        public static string GetString(ushort type)
        {
            if (type == GameAction.PlayCard)
                return "play";          // 出牌
            if (type == GameAction.Move)
                return "move";          // 移动
            if (type == GameAction.Attack)
                return "attack";        // 攻击卡牌
            if (type == GameAction.AttackPlayer)
                return "attack_player"; // 攻击玩家
            if (type == GameAction.CastAbility)
                return "cast_ability";  // 施放能力
            if (type == GameAction.EndTurn)
                return "end_turn";      // 结束回合
            if (type == GameAction.SelectCard)
                return "select_card";   // 选择卡牌
            if (type == GameAction.SelectPlayer)
                return "select_player"; // 选择玩家
            if (type == GameAction.SelectChoice)
                return "select_choice"; // 选择选项
            if (type == GameAction.SelectCost)
                return "select_cost";   // 选择消耗
            if (type == GameAction.SelectSlot)
                return "select_slot";   // 选择槽位
            if (type == GameAction.CancelSelect)
                return "cancel_select"; // 取消选择
            if (type == GameAction.Resign)
                return "resign";        // 投降
            if (type == GameAction.ChatMessage)
                return "chat";          // 聊天
            return type.ToString();       // 其他类型直接返回数字
        }
    }
}
