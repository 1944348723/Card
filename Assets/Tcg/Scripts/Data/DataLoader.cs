using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Client;

namespace TcgEngine
{
    /// <summary>
    /// 数据加载器
    /// 负责在游戏启动时加载所有游戏数据（卡牌、能力、套牌、变体等）
    /// </summary>
    public class DataLoader : MonoBehaviour
    {
        public GameplayData data; // 游戏玩法数据
        public AssetData assets;  // 游戏资源数据（特效、音效等）

        private HashSet<string> card_ids = new HashSet<string>();    // 用于检查卡牌ID重复
        private HashSet<string> ability_ids = new HashSet<string>(); // 用于检查能力ID重复
        private HashSet<string> deck_ids = new HashSet<string>();    // 用于检查套牌ID重复

        private static DataLoader instance; // 单例

        void Awake()
        {
            instance = this;   // 初始化单例
            LoadData();        // 加载所有数据
        }

        /// <summary>
        /// 加载所有游戏数据
        /// </summary>
        public void LoadData()
        {
            // 为提高加载速度，可在每个Load()函数中添加相对Resources文件夹的路径
            // 例如 CardData.Load("Cards") 只加载 Resources/Cards 文件夹下的数据
            CardData.Load();
            TeamData.Load();
            RarityData.Load();
            TraitData.Load();
            VariantData.Load();
            PackData.Load();
            LevelData.Load();
            DeckData.Load();
            AbilityData.Load();
            StatusData.Load();
            AvatarData.Load();
            CardbackData.Load();
            RewardData.Load();

            CheckCardData();     // 检查卡牌数据有效性
            CheckAbilityData();  // 检查能力数据有效性
            CheckDeckData();     // 检查套牌数据有效性
            CheckVariantData();  // 检查默认变体数据
        }

        /// <summary>
        /// 检查所有卡牌数据是否有效
        /// </summary>
        private void CheckCardData()
        {
            card_ids.Clear();
            foreach (CardData card in CardData.GetAll())
            {
                if (string.IsNullOrEmpty(card.id))
                    Debug.LogError(card.name + " id is empty"); // ID为空
                if (card_ids.Contains(card.id))
                    Debug.LogError("Dupplicate Card ID: " + card.id); // 重复ID

                if (card.team == null)
                    Debug.LogError(card.id + " team is null"); // 队伍为空
                if (card.rarity == null)
                    Debug.LogError(card.id + " rarity is null"); // 稀有度为空

                foreach (TraitData trait in card.traits)
                {
                    if (trait == null)
                        Debug.LogError(card.id + " has null trait"); // 特性为空
                }

                if (card.stats != null)
                {
                    foreach (TraitStat stat in card.stats)
                    {
                        if (stat.trait == null)
                            Debug.LogError(card.id + " has null stat trait"); // 属性特性为空
                    }
                }

                foreach (AbilityData ability in card.abilities)
                {
                    if(ability == null)
                        Debug.LogError(card.id + " has null ability"); // 能力为空
                }

                card_ids.Add(card.id); // 添加到ID集合中
            }
        }

        /// <summary>
        /// 检查所有能力数据是否有效
        /// </summary>
        private void CheckAbilityData()
        {
            ability_ids.Clear();
            foreach (AbilityData ability in AbilityData.GetAll())
            {
                if (string.IsNullOrEmpty(ability.id))
                    Debug.LogError(ability.name + " id is empty"); // ID为空
                if (ability_ids.Contains(ability.id))
                    Debug.LogError("Dupplicate Ability ID: " + ability.id); // 重复ID

                foreach (AbilityData chain in ability.chain_abilities)
                {
                    if (chain == null)
                        Debug.LogError(ability.id + " has null chain ability"); // 链接能力为空
                }

                ability_ids.Add(ability.id);
            }
        }

        /// <summary>
        /// 检查所有套牌数据是否有效
        /// </summary>
        private void CheckDeckData()
        {
            GameplayData gdata = GameplayData.Get();
            CheckDeckArray(gdata.ai_decks);       // 检查AI套牌数组
            CheckDeckArray(gdata.free_decks);     // 检查自由套牌数组
            CheckDeckArray(gdata.starter_decks);  // 检查初始套牌数组

            if(gdata.test_deck == null || gdata.test_deck_ai == null)
                Debug.Log("Deck is null in Resources/GameplayData"); // 测试套牌为空

            deck_ids.Clear();
            foreach (DeckData deck in DeckData.GetAll())
            {
                if (string.IsNullOrEmpty(deck.id))
                    Debug.LogError(deck.name + " id is empty"); // 套牌ID为空
                if (deck_ids.Contains(deck.id))
                    Debug.LogError("Dupplicate Deck ID: " + deck.id); // 套牌ID重复

                foreach (CardData card in deck.cards)
                {
                    if (card == null)
                        Debug.LogError(deck.id + " has null card"); // 套牌中卡牌为空
                }

                deck_ids.Add(deck.id);
            }
        }

        /// <summary>
        /// 检查套牌数组是否有空元素
        /// </summary>
        private void CheckDeckArray(DeckData[] decks)
        {
            foreach (DeckData deck in decks)
            {
                if (deck == null)
                    Debug.Log("Deck is null in Resources/GameplayData"); // 套牌为空
            }
        }

        /// <summary>
        /// 检查默认变体数据是否存在
        /// </summary>
        private void CheckVariantData()
        {
            VariantData dvariant = VariantData.GetDefault();
            if(dvariant == null)
                Debug.LogError("No default variant data found, make sure you have a default VariantData"); // 默认变体不存在
        }

        /// <summary>
        /// 获取DataLoader单例
        /// </summary>
        public static DataLoader Get()
        {
            return instance;
        }
    }
}
