using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 从 Web API 数据库获取的用户数据
    /// </summary>
    [System.Serializable]
    public class UserData
    {
        public string id;                 // 用户ID
        public string username;           // 用户名

        public string email;              // 邮箱
        public string avatar;             // 头像ID
        public string cardback;           // 卡背ID
        public int permission_level = 1;  // 权限等级
        public int validation_level = 1;  // 验证等级

        public int coins;                 // 金币数量
        public int xp;                    // 经验值
        public int elo;                   // ELO分

        public int matches;               // 对局总数
        public int victories;             // 胜利数
        public int defeats;               // 失败数

        public UserCardData[] cards;      // 拥有的卡牌列表
        public UserCardData[] packs;      // 拥有的卡包列表
        public UserDeckData[] decks;      // 拥有的牌组列表
        public string[] rewards;          // 获得的奖励ID列表
        public string[] avatars;          // 拥有的头像ID列表
        public string[] cardbacks;        // 拥有的卡背ID列表
        public string[] friends;          // 好友用户名列表

        public UserData()
        {
            cards = new UserCardData[0];
            packs = new UserCardData[0];
            decks = new UserDeckData[0];
            rewards = new string[0];
            avatars = new string[0];
            cardbacks = new string[0];
            friends = new string[0];
            permission_level = 1;
            coins = 10000;
            elo = 1000;
        }

        /// <summary>
        /// 获取用户等级（每1000经验值升一级）
        /// </summary>
        public int GetLevel()
        {
            return Mathf.FloorToInt(xp / 1000) + 1;
        }

        /// <summary>
        /// 获取头像ID
        /// </summary>
        public string GetAvatar()
        {
            if (avatar != null)
                return avatar;
            return "";
        }

        /// <summary>
        /// 获取卡背ID
        /// </summary>
        public string GetCardback()
        {
            if (cardback != null)
                return cardback;
            return "";
        }

        /// <summary>
        /// 更新或添加牌组
        /// </summary>
        public void SetDeck(UserDeckData deck)
        {
            for(int i=0; i<decks.Length; i++)
            {
                if (decks[i].tid == deck.tid)
                {
                    decks[i] = deck;
                    return;
                }
            }

            // 若未找到，添加新牌组
            List<UserDeckData> ldecks = new List<UserDeckData>(decks);
            ldecks.Add(deck);
            this.decks = ldecks.ToArray();
        }

        /// <summary>
        /// 获取指定牌组
        /// </summary>
        public UserDeckData GetDeck(string tid)
        {
            foreach (UserDeckData deck in decks)
            {
                if (deck.tid == tid)
                    return deck;
            }
            return null;
        }

        /// <summary>
        /// 获取指定卡牌
        /// </summary>
        public UserCardData GetCard(string tid, string variant)
        {
            foreach (UserCardData card in cards)
            {
                if (card.tid == tid && card.variant == variant)
                    return card;
            }
            return null;
        }

        /// <summary>
        /// 获取指定卡牌数量
        /// </summary>
        public int GetCardQuantity(CardData card, VariantData variant)
        {
            return GetCardQuantity(card.id, variant.id, variant.is_default);
        }

        /// <summary>
        /// 获取指定卡牌数量（通过ID和变体）
        /// </summary>
        public int GetCardQuantity(string tid, string variant, bool default_variant = false)
        {
            if (cards == null)
                return 0;

            foreach (UserCardData card in cards)
            {
                if (card.tid == tid && card.variant == variant)
                    return card.quantity;
                if (card.tid == tid && card.variant == "" && default_variant)
                    return card.quantity;
            }
            return 0;
        }

        /// <summary>
        /// 获取指定卡包
        /// </summary>
        public UserCardData GetPack(string tid)
        {
            foreach (UserCardData pack in packs)
            {
                if (pack.tid == tid)
                    return pack;
            }
            return null;
        }

        /// <summary>
        /// 获取指定卡包数量
        /// </summary>
        public int GetPackQuantity(string tid)
        {
            if (packs == null)
                return 0;

            foreach (UserCardData pack in packs)
            {
                if (pack.tid == tid)
                    return pack.quantity;
            }
            return 0;
        }

        /// <summary>
        /// 统计拥有的唯一卡牌数量
        /// </summary>
        public int CountUniqueCards()
        {
            if (cards == null)
                return 0;

            HashSet<string> unique_cards = new HashSet<string>();
            foreach (UserCardData card in cards)
            {
                if (!unique_cards.Contains(card.tid))
                    unique_cards.Add(card.tid);
            }
            return unique_cards.Count;
        }

        /// <summary>
        /// 统计拥有的某种卡牌变体数量
        /// </summary>
        public int CountCardType(VariantData variant)
        {
            int value = 0;
            foreach (UserCardData card in cards)
            {
                if (card.variant == variant.id)
                    value += 1;
            }
            return value;
        }

        /// <summary>
        /// 检查牌组中所有卡牌是否都拥有
        /// </summary>
        public bool HasDeckCards(UserDeckData deck)
        {
            foreach (UserCardData card in deck.cards)
            {
                bool default_variant = true; // "" 变体也算有效（兼容老版本）
                if (GetCardQuantity(card.tid, card.variant, default_variant) < card.quantity)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 检查牌组是否有效
        /// </summary>
        public bool IsDeckValid(UserDeckData deck)
        {
            if (Authenticator.Get().IsApi())
                return HasDeckCards(deck) && deck.IsValid();
            return deck.IsValid();
        }

        /// <summary>
        /// 添加新牌组并自动增加卡牌数量
        /// </summary>
        public void AddDeck(UserDeckData deck)
        {
            List<UserDeckData> udecks = new List<UserDeckData>(decks);
            udecks.Add(deck);
            decks = udecks.ToArray();

            foreach (UserCardData card in deck.cards)
            {
                AddCard(card.tid, card.variant, 1);
            }
        }

        /// <summary>
        /// 添加卡包
        /// </summary>
        public void AddPack(string tid, int quantity)
        {
            bool found = false;
            foreach (UserCardData pack in packs)
            {
                if (pack.tid == tid)
                {
                    found = true;
                    pack.quantity += quantity;
                }
            }
            if (!found)
            {
                UserCardData npack = new UserCardData();
                npack.tid = tid;
                npack.quantity = quantity;
                List<UserCardData> apacks = new List<UserCardData>(packs);
                apacks.Add(npack);
                packs = apacks.ToArray();
            }
        }

        /// <summary>
        /// 添加卡牌
        /// </summary>
        public void AddCard(string tid, string variant, int quantity)
        {
            bool found = false;
            foreach (UserCardData card in cards)
            {
                if (card.tid == tid && card.variant == variant)
                {
                    found = true;
                    card.quantity += quantity;
                }
            }
            if (!found)
            {
                UserCardData ncard = new UserCardData();
                ncard.tid = tid;
                ncard.variant = variant;
                ncard.quantity = quantity;
                List<UserCardData> acards = new List<UserCardData>(cards);
                acards.Add(ncard);
                cards = acards.ToArray();
            }
        }

        /// <summary>
        /// 添加奖励ID
        /// </summary>
        public void AddReward(string tid)
        {
            if (!HasReward(tid))
            {
                List<string> arewards = new List<string>(rewards);
                arewards.Add(tid);
                rewards = arewards.ToArray();
            }
        }

        /// <summary>
        /// 是否拥有指定卡牌
        /// </summary>
        public bool HasCard(string card_tid, string variant, int quantity = 1)
        {
            foreach (UserCardData card in cards)
            {
                if (card.tid == card_tid && card.variant == variant && card.quantity >= quantity)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 是否拥有指定卡包
        /// </summary>
        public bool HasPack(string pack_tid, int quantity=1)
        {
            foreach (UserCardData pack in packs)
            {
                if (pack.tid == pack_tid && pack.quantity >= quantity)
                    return true;
            }
            return false;
        }
		
		/// <summary>
		/// 是否拥有指定头像
		/// </summary>
		public bool HasAvatar(string avatar_tid)
		{
			return avatars.Contains(avatar_tid);
		}

		/// <summary>
		/// 是否拥有指定卡背
		/// </summary>
		public bool HasCardback(string cardback_tid)
		{
			return cardbacks.Contains(cardback_tid);
		}

        /// <summary>
        /// 是否拥有指定奖励
        /// </summary>
        public bool HasReward(string reward_id)
        {
            foreach (string reward in rewards)
            {
                if (reward == reward_id)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 获取金币字符串
        /// </summary>
        public string GetCoinsString()
        {
            return coins.ToString();
        }

        /// <summary>
        /// 是否是好友
        /// </summary>
        public bool HasFriend(string username)
        {
            List<string> flist = new List<string>(friends);
            return flist.Contains(username);
        }

        /// <summary>
        /// 添加好友
        /// </summary>
        public void AddFriend(string username)
        {
            List<string> flist = new List<string>(friends);
            if (!flist.Contains(username))
                flist.Add(username);
            friends = flist.ToArray();
        }

        /// <summary>
        /// 删除好友
        /// </summary>
        public void RemoveFriend(string username)
        {
            List<string> flist = new List<string>(friends);
            if (flist.Contains(username))
                flist.Remove(username);
            friends = flist.ToArray();
        }
    }

    /// <summary>
    /// 用户牌组数据
    /// </summary>
    [System.Serializable]
    public class UserDeckData : INetworkSerializable
    {
        public string tid;           // 牌组ID
        public string title;         // 牌组标题
        public UserCardData hero;    // 英雄卡
        public UserCardData[] cards; // 卡牌列表

        public UserDeckData() {}

        public UserDeckData(DeckData deck)
        {
            tid = deck.id;
            title = deck.title;
            hero = new UserCardData(deck.hero, VariantData.GetDefault());
            cards = new UserCardData[deck.cards.Length];
            for (int i = 0; i < deck.cards.Length; i++)
            {
                cards[i] = new UserCardData(deck.cards[i], VariantData.GetDefault());
            }
        }

        /// <summary>
        /// 获取牌组卡牌总数量
        /// </summary>
        public int GetQuantity()
        {
            int count = 0;
            foreach (UserCardData card in cards)
                count += card.quantity;
            return count;
        }

        /// <summary>
        /// 检查牌组是否合法（非空ID和标题，且卡牌数量足够）
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(tid) && !string.IsNullOrWhiteSpace(title) && GetQuantity() >= GameplayData.Get().deck_size;
        }

        /// <summary>
        /// 网络序列化
        /// </summary>
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tid);
            serializer.SerializeValue(ref title);
            serializer.SerializeValue(ref hero);
            NetworkTool.NetSerializeArray(serializer, ref cards);
        }

        /// <summary>
        /// 默认牌组
        /// </summary>
        public static UserDeckData Default
        {
            get
            {
                UserDeckData deck = new UserDeckData();
                deck.tid = "";
                deck.title = "";
                deck.hero = new UserCardData();
                deck.cards = new UserCardData[0];
                return deck;
            }
        }
    }

    /// <summary>
    /// 用户卡牌数据
    /// </summary>
    [System.Serializable]
    public class UserCardData : INetworkSerializable
    {
        public string tid;      // 卡牌ID
        public string variant;  // 卡牌变体
        public int quantity;    // 数量

        public UserCardData() { tid = ""; variant = ""; quantity = 1; }
        public UserCardData(string id, string v) { tid = id; variant = v; quantity = 1; }
        public UserCardData(CardData card, VariantData variant) 
        {
            this.tid = card != null ? card.id : "";
            this.variant = variant != null ? variant.id : "";
            this.quantity = 1;
        }

        /// <summary>
        /// 网络序列化
        /// </summary>
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tid);
            serializer.SerializeValue(ref variant);
            serializer.SerializeValue(ref quantity);
        }
    }
}
