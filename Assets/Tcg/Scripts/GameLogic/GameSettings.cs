using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace TcgEngine
{
    // 游戏类型枚举
    [System.Serializable]
    public enum GameType
    {
        Solo = 0,       // 单人模式
        Adventure = 10, // 冒险模式
        Multiplayer = 20, // 多人模式（联网对战）
        HostP2P = 30,     // P2P主机模式
        Observer = 40,    // 观察者模式（仅观看）
    }

    // 游戏模式枚举
    [System.Serializable]
    public enum GameMode
    {
        Casual = 0,    // 休闲模式
        Ranked = 10,   // 排位模式
    }

    /// <summary>
    /// 存储客户端的游戏设置，例如游戏模式、游戏UID和要加载的场景
    /// 这些信息在比赛开始时会发送给服务器
    /// </summary>
    [System.Serializable]
    public class GameSettings : INetworkSerializable
    {
        public string server_url;   // 要连接的服务器地址
        public string game_uid;     // 游戏在服务器上的唯一ID
        public string scene;        // 要加载的场景名称
        public int nb_players;      // 玩家数量，包括AI（UI目前只支持2人）

        public GameType game_type = GameType.Solo;      // 游戏类型（多人/单人/观察者等）
        public GameMode game_mode = GameMode.Casual;    // 游戏模式（排位或休闲）
        public string level;                            // 冒险模式下的关卡ID

        // 是否是主机
        public virtual bool IsHost()
        {
            return game_type == GameType.Solo || game_type == GameType.Adventure || game_type == GameType.HostP2P;
        }

        // 是否是离线模式
        public virtual bool IsOffline()
        {
            return game_type == GameType.Solo || game_type == GameType.Adventure;
        }

        // 是否是在线模式
        public virtual bool IsOnline()
        {
            return game_type == GameType.HostP2P || game_type == GameType.Multiplayer || game_type == GameType.Observer;
        }

        // 是否是在线玩家（排除观察者）
        public virtual bool IsOnlinePlayer()
        {
            return game_type == GameType.HostP2P || game_type == GameType.Multiplayer;
        }

        // 是否是排位模式
        public virtual bool IsRanked()
        {
            return game_mode == GameMode.Ranked;
        }

        // 获取服务器URL
        public virtual string GetUrl()
        {
            if (!string.IsNullOrEmpty(server_url))
                return server_url;
            return NetworkData.Get().url; // 默认网络URL
        }

        // 获取场景名称
        public virtual string GetScene()
        {
            if (!string.IsNullOrEmpty(scene))
                return scene;
            return GameplayData.Get().GetRandomArena(); // 默认随机竞技场
        }

        // 获取游戏模式字符串
        public virtual string GetGameModeId()
        {
            if (game_mode == GameMode.Ranked)
                return "ranked";
            if (game_mode == GameMode.Casual)
                return "casual";
            return "";
        }

        // 获取冒险模式关卡数据
        public virtual LevelData GetLevel()
        {
            if (game_type == GameType.Adventure)
            {
                return LevelData.Get(level);
            }
            return null;
        }

        // 网络序列化（用于发送到服务器）
        public virtual void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref server_url);
            serializer.SerializeValue(ref game_uid);
            serializer.SerializeValue(ref scene);
            serializer.SerializeValue(ref game_type);
            serializer.SerializeValue(ref game_mode);
            serializer.SerializeValue(ref nb_players);
            serializer.SerializeValue(ref level);
        }

        // 静态方法：将GameMode转为字符串
        public static string GetRankModeString(GameMode rank_mode)
        {
            if (rank_mode == GameMode.Ranked)
                return "ranked";
            if (rank_mode == GameMode.Casual)
                return "casual";
            return "";
        }

        // 静态方法：将字符串转为GameMode
        public static GameMode GetRankMode(string rank_id)
        {
            if (rank_id == "ranked")
                return GameMode.Ranked;
            if (rank_id == "casual")
                return GameMode.Casual;
            return GameMode.Casual;
        }

        // 默认游戏设置
        public static GameSettings Default
        {
            get
            {
                GameSettings settings = new GameSettings();
                settings.server_url = "";
                settings.game_uid = "test";
                settings.game_type = GameType.Solo;
                settings.game_mode = GameMode.Casual;
                settings.nb_players = 2;
                settings.scene = "Game";
                settings.level = "";
                return settings;
            }
        }
    }

    /// <summary>
    /// 存储客户端玩家的设置，例如头像、卡背和使用的牌组
    /// 这些信息在比赛开始时会发送给服务器
    /// </summary>
    [System.Serializable]
    public class PlayerSettings : INetworkSerializable
    {
        public string username;  // 玩家用户名
        public string avatar;    // 玩家头像
        public string cardback;  // 卡背
        public int ai_level;     // AI等级
        public UserDeckData deck = UserDeckData.Default; // 玩家牌组

        // 判断是否有有效牌组
        public bool HasDeck()
        {
            return deck != null && !string.IsNullOrEmpty(deck.tid);
        }

        // 网络序列化
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref username);
            serializer.SerializeValue(ref avatar);
            serializer.SerializeValue(ref cardback);
            serializer.SerializeValue(ref ai_level);
            serializer.SerializeValue(ref deck);
        }

        // 默认玩家设置
        public static PlayerSettings Default
        {
            get
            {
                PlayerSettings settings = new PlayerSettings();
                settings.username = "Player";
                settings.avatar = "";
                settings.cardback = "";
                settings.deck = UserDeckData.Default;
                settings.ai_level = 1;
                return settings;
            }
        }

        // 默认AI玩家设置
        public static PlayerSettings DefaultAI
        {
            get
            {
                PlayerSettings settings = new PlayerSettings();
                settings.username = "AI";
                settings.avatar = "";
                settings.cardback = "";
                settings.deck = UserDeckData.Default;
                settings.ai_level = 10;
                return settings;
            }
        }
    }
}
