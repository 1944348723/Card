
using Unity.Netcode;
using UnityEngine.Events;

namespace TcgEngine
{
    /// <summary>
    /// Netcode Named Message 的协议名称。
    /// 客户端与服务器必须使用同一名称进行注册和发送。
    /// </summary>
    public static class NetworkMessageName
    {
        public const string Connect = "connect";
        public const string Action = "action";
        public const string Refresh = "refresh";

        public const string Matchmaking = "matchmaking";
        public const string MatchmakingList = "matchmaking_list";
        public const string MatchList = "match_list";
    }

    //-------- 连接相关 --------

    // 玩家连接消息
    public class MsgPlayerConnect : INetworkSerializable
    {
        public string user_id;     // 用户 ID
        public string username;    // 用户名
        public string game_uid;    // 游戏唯一 ID
        public int nb_players;     // 当前玩家数量
        public bool observer;      // 是否以观众身份加入

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref user_id);
            serializer.SerializeValue(ref username);
            serializer.SerializeValue(ref game_uid);
            serializer.SerializeValue(ref nb_players);
            serializer.SerializeValue(ref observer);
        }
    }

    // 玩家连接后返回消息
    public class MsgAfterConnected : INetworkSerializable
    {
        public bool success;       // 是否成功
        public int player_id;      // 玩家 ID
        public Game game_data;     // 游戏数据

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref success);
            serializer.SerializeValue(ref player_id);

            if (serializer.IsReader)
            {
                int size = 0;
                serializer.SerializeValue(ref size);
                if (size > 0)
                {
                    byte[] bytes = new byte[size];
                    serializer.SerializeValue(ref bytes);
                    game_data = NetworkTool.Deserialize<Game>(bytes);
                }
            }

            if (serializer.IsWriter)
            {
                byte[] bytes = NetworkTool.Serialize(game_data);
                int size = bytes.Length;
                serializer.SerializeValue(ref size);
                if(size > 0)
                    serializer.SerializeValue(ref bytes);
            }
        }
    }

    //-------- 匹配相关 --------

    // 匹配请求消息
    public class MsgMatchmaking : INetworkSerializable
    {
        public string user_id;     // 用户 ID
        public string username;    // 用户名
        public string group;       // 分组
        public int players;        // 玩家数量
        public int elo;            // Elo 值
        public bool refresh;       // 是否刷新
        public float time;         // 时间

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref user_id);
            serializer.SerializeValue(ref username);
            serializer.SerializeValue(ref group);
            serializer.SerializeValue(ref players);
            serializer.SerializeValue(ref elo);
            serializer.SerializeValue(ref refresh);
            serializer.SerializeValue(ref time);
        }
    }

    // 匹配结果消息
    public class MatchmakingResult : INetworkSerializable
    {
        public bool success;       // 是否成功
        public int players;        // 玩家数量
        public string group;       // 分组
        public string server_url;  // 服务器 URL
        public string game_uid;    // 游戏唯一 ID

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref success);
            serializer.SerializeValue(ref players);
            serializer.SerializeValue(ref group);
            serializer.SerializeValue(ref server_url);
            serializer.SerializeValue(ref game_uid);
        }
    }

    // 匹配列表请求消息
    public class MsgMatchmakingList : INetworkSerializable
    {
        public string username;    // 用户名

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref username);
        }
    }

    [System.Serializable]
    public struct MatchmakingListItem : INetworkSerializable
    {
        public string group;       // 分组
        public string user_id;     // 用户 ID
        public string username;    // 用户名

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref group);
            serializer.SerializeValue(ref user_id);
            serializer.SerializeValue(ref username);
        }
    }

    // 匹配列表
    public class MatchmakingList : INetworkSerializable
    {
        public MatchmakingListItem[] items;  // 列表项

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            NetworkTool.NetSerializeArray(serializer, ref items);
        }
    }

    [System.Serializable]
    public class MatchListItem : INetworkSerializable
    {
        public string group;       // 分组
        public string username;    // 用户名
        public string game_uid;    // 游戏唯一 ID
        public string game_url;    // 游戏 URL

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref group);
            serializer.SerializeValue(ref username);
            serializer.SerializeValue(ref game_uid);
            serializer.SerializeValue(ref game_url);
        }
    }

    // 匹配列表集合
    public class MatchList : INetworkSerializable
    {
        public MatchListItem[] items;  // 列表项数组

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            NetworkTool.NetSerializeArray(serializer, ref items);
        }
    }

    //-------- 游戏内消息 --------

    // 玩家出牌消息
    public class MsgPlayCard : INetworkSerializable
    {
        public string card_uid;    // 卡牌唯一 ID
        public Slot slot;          // 卡槽信息

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref card_uid);
            serializer.SerializeNetworkSerializable(ref slot);
        }
    }

    // 卡牌消息
    public class MsgCard : INetworkSerializable
    {
        public string card_uid;    // 卡牌唯一 ID

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref card_uid);
        }
    }

    // 卡牌数值消息
    public class MsgCardValue : INetworkSerializable
    {
        public string card_uid;    // 卡牌唯一 ID
        public int value;          // 数值

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref card_uid);
            serializer.SerializeValue(ref value);
        }
    }

    // 玩家消息
    public class MsgPlayer : INetworkSerializable
    {
        public int player_id;      // 玩家 ID

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref player_id);
        }
    }

    // 玩家数值消息
    public class MsgPlayerValue : INetworkSerializable
    {
        public int player_id;      // 玩家 ID
        public int value;          // 数值

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref player_id);
            serializer.SerializeValue(ref value);
        }
    }

    // 攻击消息
    public class MsgAttack : INetworkSerializable
    {
        public string attacker_uid;  // 攻击者卡牌 UID
        public string target_uid;    // 目标卡牌 UID
        public int damage;           // 伤害值

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref attacker_uid);
            serializer.SerializeValue(ref target_uid);
            serializer.SerializeValue(ref damage);
        }
    }
    
    // 玩家攻击消息
    public class MsgAttackPlayer : INetworkSerializable
    {
        public string attacker_uid;  // 攻击者卡牌 UID
        public int target_id;        // 目标玩家 ID
        public int damage;           // 伤害值

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref attacker_uid);
            serializer.SerializeValue(ref target_id);
            serializer.SerializeValue(ref damage);
        }
    }

    // 释放技能消息（目标为卡牌）
    public class MsgCastAbility : INetworkSerializable
    {
        public string ability_id;    // 技能 ID
        public string caster_uid;    // 施法者卡牌 UID
        public string target_uid;    // 目标卡牌 UID

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ability_id);
            serializer.SerializeValue(ref caster_uid);
            serializer.SerializeValue(ref target_uid);
        }
    }

    // 释放技能消息（目标为玩家）
    public class MsgCastAbilityPlayer : INetworkSerializable
    {
        public string ability_id;    // 技能 ID
        public string caster_uid;    // 施法者卡牌 UID
        public int target_id;        // 目标玩家 ID

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ability_id);
            serializer.SerializeValue(ref caster_uid);
            serializer.SerializeValue(ref target_id);
        }
    }

    // 释放技能消息（目标为卡槽）
    public class MsgCastAbilitySlot : INetworkSerializable
    {
        public string ability_id;    // 技能 ID
        public string caster_uid;    // 施法者卡牌 UID
        public Slot slot;            // 目标卡槽

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ability_id);
            serializer.SerializeValue(ref caster_uid);
            serializer.SerializeNetworkSerializable(ref slot);
        }
    }

    // 秘密触发消息
    public class MsgSecret : INetworkSerializable
    {
        public string secret_uid;    // 秘密卡牌 UID
        public string triggerer_uid; // 触发者卡牌 UID

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref secret_uid);
            serializer.SerializeValue(ref triggerer_uid);
        }
    }

    // 換牌（Mulligan）消息
    public class MsgMulligan : INetworkSerializable
    {
        public string[] cards;       // 选择的卡牌 UID 数组

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            NetworkTool.NetSerializeArray(serializer, ref cards);
        }
    }

    // 整数消息
    public class MsgInt : INetworkSerializable
    {
        public int value;            // 整数值

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref value);
        }
    }

    // 聊天消息
    public class MsgChat : INetworkSerializable
    {
        public int player_id;        // 玩家 ID
        public string msg;           // 消息内容

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref player_id);
            serializer.SerializeValue(ref msg);
        }
    }

    // 刷新全局游戏数据消息
    public class MsgRefreshAll : INetworkSerializable
    {
        public Game game_data;       // 游戏数据

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                int size = 0;
                serializer.SerializeValue(ref size);
                if (size > 0)
                {
                    byte[] bytes = new byte[size];
                    serializer.SerializeValue(ref bytes);
                    game_data = NetworkTool.Deserialize<Game>(bytes);
                }
            }

            if (serializer.IsWriter)
            {
                byte[] bytes = NetworkTool.Serialize(game_data);
                int size = bytes.Length;
                serializer.SerializeValue(ref size);
                if (size > 0)
                    serializer.SerializeValue(ref bytes);
            }
        }
    }
    
}
