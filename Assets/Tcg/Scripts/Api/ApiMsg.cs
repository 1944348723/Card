using System;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// ApiClient 请求和响应使用的数据结构列表
    /// </summary>

    //--------- 请求 -----------

    [Serializable]
    public struct LoginRequest
    {
        public string email;      // 邮箱
        public string username;   // 用户名
        public string password;   // 密码
    }

    [Serializable]
    public struct AutoLoginRequest
    {
        public string refresh_token; // 刷新令牌，用于自动登录
    }

    [Serializable]
    public struct RegisterRequest
    {
        public string email;      // 邮箱
        public string username;   // 用户名
        public string password;   // 密码
        public string avatar;     // 头像
    }

    [Serializable]
    public struct EditUserRequest
    {
        public string avatar;     // 新头像
        public string cardback;   // 新卡背
    }

    [Serializable]
    public struct EditEmailRequest
    {
        public string email;      // 新邮箱
    }

    [Serializable]
    public struct EditPasswordRequest
    {
        public string password_previous; // 旧密码
        public string password_new;      // 新密码
    }

    [Serializable]
    public struct FriendAddRequest
    {
        public string username;   // 要添加的好友用户名
    }

    [Serializable]
    public struct AddMatchRequest
    {
        public string tid;        // 对局ID
        public string[] players;  // 玩家列表
        public string mode;       // 游戏模式
        public bool ranked;       // 是否为排位赛
    }

    [Serializable]
    public struct CompleteMatchRequest
    {
        public string tid;        // 对局ID
        public string winner;     // 胜利者用户名
    }

    [Serializable]
    public struct RewardGainRequest
    {
        public string reward;     // 奖励ID
    }

    [Serializable]
    public struct BuyPackRequest
    {
        public string pack;       // 包ID
        public int quantity;      // 购买数量
    }

    [Serializable]
    public struct BuyCardRequest
    {
        public string card;       // 卡牌ID
        public string variant;    // 卡牌变体
        public int quantity;      // 购买数量
    }

    [Serializable]
    public struct SellDuplicateRequest
    {
        //public string variant; // 可选：变体
        //public string rarity;  // 可选：稀有度
        public int keep;          // 保留数量，其余出售
    }

    [Serializable]
    public struct OpenPackRequest
    {
        public string pack;       // 要开启的卡包ID
    }

    //--------- 响应 -----------

    [Serializable]
    public struct VersionResponse
    {
        public string version;    // 客户端版本
    }

    [Serializable]
    public struct RegisterResponse
    {
        public string id;         // 用户ID
        public string username;   // 用户名
        public string version;    // 版本
        public bool success;      // 是否成功
        public string error;      // 错误信息
    }

    [Serializable]
    public struct LoginResponse
    {
        public string id;             // 用户ID
        public string username;       // 用户名
        public string refresh_token;  // 刷新令牌
        public string access_token;   // 访问令牌
        public int permission_level;  // 权限等级
        public int validation_level;  // 验证等级
        public int duration;          // 登录有效期（秒）
        public string version;        // 客户端版本
        public string error;          // 错误信息
        public bool success;          // 是否成功
    }

    [Serializable]
    public struct UserIdResponse
    {
        public string id;         // 用户ID
        public string username;   // 用户名
        public string error;      // 错误信息
    }

    [Serializable]
    public struct MatchResponse
    {
        public string tid;                // 对局ID
        public string[] players;          // 玩家列表
        public DateTime start;            // 对局开始时间
        public DateTime end;              // 对局结束时间
        public string winner;             // 胜利者用户名
        public bool completed;            // 是否完成
        public MatchDataResponse[] udata; // 玩家对局数据
    }

    [Serializable]
    public struct MatchDataResponse
    {
        public string username;   // 玩家用户名
        public int rank;          // 玩家排名
        public DeckData deck;     // 玩家使用的牌组数据
        public RewardResponse reward; // 玩家获得的奖励
    }

    [Serializable]
    public struct RewardResponse
    {
        public string tid;        // 对局ID
        public int coins;         // 获得金币
        public int elo;           // 获得ELO分
        public int xp;            // 获得经验
        public string[] cards;    // 获得的卡牌
        public string[] decks;    // 获得的牌组
    }

    [Serializable]
    public struct MarketResponse
    {
        public string seller;     // 卖家用户名
        public string card;       // 卡牌ID
        public int price;         // 价格
        public int quantity;      // 数量
    }

    [Serializable]
    public struct FriendResponse
    {
        public string username;           // 用户名
        public string server_time;        // 服务器时间
        public FriendData[] friends;      // 好友列表
        public FriendData[] friends_requests; // 好友请求列表
    }

    [System.Serializable]
    public struct FriendData
    {
        public string username;           // 好友用户名
        public string avatar;             // 好友头像
        public string last_online_time;   // 上次在线时间
    }

}
