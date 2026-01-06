using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine
{
    /// <summary>
    /// 卡牌上传工具脚本
    /// 用于将卡牌、卡包、奖励等数据上传到 Mongo 数据库（会覆盖已有数据）
    /// </summary>
    public class CardUploader : MonoBehaviour
    {
        public string username = "admin"; // 默认管理员用户名

        [Header("引用组件")]
        public InputField username_txt;   // 用户名输入框
        public InputField password_txt;   // 密码输入框
        public Text msg_text;             // 提示信息文本

        [Header("上传选项")]
        public bool upload_cards = true;     // 是否上传卡牌
        public bool upload_packs = true;     // 是否上传卡包
        public bool upload_decks = true;     // 是否上传起始套牌
        public bool upload_variants = true;  // 是否上传卡牌变体
        public bool upload_rewards = true;   // 是否上传奖励

        void Start()
        {
            username_txt.text = username; // 初始化用户名输入框
            msg_text.text = "";
        }

        /// <summary>
        /// 管理员登录
        /// </summary>
        private async void Login()
        {
            LoginResponse res = await ApiClient.Get().Login(username_txt.text, password_txt.text);
            if (res.success && res.permission_level >= 10)
            {
                UploadAll(); // 登录成功，开始上传
            }
            else
            {
                ShowText("管理员登录失败");
            }
        }

        /// <summary>
        /// 上传所有选择的内容
        /// </summary>
        private async void UploadAll()
        {
            // 删除已有数据
            ShowText("正在删除已有数据...");

            if(upload_packs)
                await DeleteAllPacks();
            if (upload_cards)
                await DeleteAllCards();
            if (upload_variants)
                await DeleteAllVariants();
            if (upload_decks)
                await DeleteAllDecks();
            if (upload_rewards)
                await DeleteAllRewards();

            // 上传卡包
            if (upload_packs)
            {
                List<PackData> packs = PackData.GetAll();
                for (int i = 0; i < packs.Count; i++)
                {
                    PackData pack = packs[i];
                    if (pack.available)
                    {
                        ShowText("上传卡包: " + pack.id);
                        UploadPack(pack);
                        await TimeTool.Delay(100);
                    }
                }
            }

            // 上传卡牌（按批次处理）
            if (upload_cards)
            {
                List<CardData> cards = CardData.GetAll();
                for (int i = 0; i < cards.Count; i += 100)
                {
                    List<CardData> list = GetCardGroup(cards, i, 100);
                    ShowText("上传卡牌: " + i + "-" + (i + 100 - 1));
                    UploadCardList(list);
                    await TimeTool.Delay(200);
                }
            }

            // 上传卡牌变体
            if (upload_variants)
            {
                List<VariantData> variants = VariantData.GetAll();
                for (int i = 0; i < variants.Count; i++)
                {
                    VariantData variant = variants[i];
                    ShowText("上传卡牌变体: " + variant.id);
                    UploadVariant(variant);
                    await TimeTool.Delay(100);
                }
            }

            // 上传起始套牌及奖励
            if (upload_decks)
            {
                DeckData[] decks = GameplayData.Get().starter_decks;
                for (int i = 0; i < decks.Length; i++)
                {
                    DeckData deck = decks[i];
                    ShowText("上传套牌: " + deck.id);
                    UploadDeck(deck);
                    UploadDeckReward(deck);
                    await TimeTool.Delay(100);
                }
            }

            // 上传单人关卡奖励
            if (upload_rewards)
            {
                List<LevelData> levels = LevelData.GetAll();
                for (int i = 0; i < levels.Count; i++)
                {
                    LevelData level = levels[i];
                    ShowText("上传关卡奖励: " + level.id);
                    UploadLevelReward(level);

                    if (level.reward_decks != null)
                    {
                        foreach (DeckData deck in level.reward_decks)
                            UploadDeck(deck);
                    }

                    await TimeTool.Delay(100);
                }
            }

            // 上传自定义奖励
            if (upload_rewards)
            {
                List<RewardData> rewards = RewardData.GetAll();
                for (int i = 0; i < rewards.Count; i++)
                {
                    RewardData reward = rewards[i];
                    ShowText("上传自定义奖励: " + reward.id);
                    UploadReward(reward);

                    foreach (DeckData deck in reward.decks)
                        UploadDeck(deck);

                    await TimeTool.Delay(100);
                }
            }

            ShowText("上传完成！");
            ApiClient.Get().Logout();
        }

        #region 删除接口
        private async Task DeleteAllPacks()
        {
            string url = ApiClient.ServerURL + "/packs";
            await ApiClient.Get().SendRequest(url, WebRequest.METHOD_DELETE);
        }

        private async Task DeleteAllCards()
        {
            string url = ApiClient.ServerURL + "/cards";
            await ApiClient.Get().SendRequest(url, WebRequest.METHOD_DELETE);
        }

        private async Task DeleteAllVariants()
        {
            string url = ApiClient.ServerURL + "/variants";
            await ApiClient.Get().SendRequest(url, WebRequest.METHOD_DELETE);
        }

        private async Task DeleteAllDecks()
        {
            string url = ApiClient.ServerURL + "/decks";
            await ApiClient.Get().SendRequest(url, WebRequest.METHOD_DELETE);
        }

        private async Task DeleteAllRewards()
        {
            string url = ApiClient.ServerURL + "/rewards";
            await ApiClient.Get().SendRequest(url, WebRequest.METHOD_DELETE);
        }
        #endregion

        #region 上传接口
        private async void UploadPack(PackData pack)
        {
            PackAddRequest req = new PackAddRequest();
            req.tid = pack.id;
            req.cards = pack.cards;
            req.cost = pack.cost;
            req.random = pack.type == PackType.Random;

            req.rarities_1st = new PackAddProbability[pack.rarities_1st.Length];
            req.rarities = new PackAddProbability[pack.rarities.Length];
            req.variants = new PackAddProbability[pack.variants.Length];

            for (int i = 0; i < req.rarities_1st.Length; i++)
                req.rarities_1st[i] = AddPackRarity(pack.rarities_1st[i]);

            for (int i = 0; i < req.rarities.Length; i++)
                req.rarities[i] = AddPackRarity(pack.rarities[i]);

            for (int i = 0; i < req.variants.Length; i++)
                req.variants[i] = AddPackVariant(pack.variants[i]);

            string url = ApiClient.ServerURL + "/packs/add";
            string json = ApiTool.ToJson(req);
            await ApiClient.Get().SendPostRequest(url, json);
        }

        private PackAddProbability AddPackRarity(PackRarity rarity)
        {
            PackAddProbability add = new PackAddProbability();
            add.tid = rarity.rarity.id;
            add.value = rarity.probability;
            return add;
        }

        private PackAddProbability AddPackVariant(PackVariant rarity)
        {
            PackAddProbability add = new PackAddProbability();
            add.tid = rarity.variant.id;
            add.value = rarity.probability;
            return add;
        }

        private async void UploadCard(CardData card)
        {
            CardAddRequest req = new CardAddRequest();
            req.tid = card.id;
            req.type = card.GetTypeId();
            req.team = card.team.id;
            req.rarity = card.rarity.id;
            req.mana = card.mana;
            req.attack = card.attack;
            req.hp = card.hp;
            req.cost = card.cost;
            req.packs = new string[card.packs.Length];

            for (int i = 0; i < req.packs.Length; i++)
            {
                req.packs[i] = card.packs[i].id;
            }

            string url = ApiClient.ServerURL + "/cards/add";
            string json = ApiTool.ToJson(req);
            await ApiClient.Get().SendPostRequest(url, json);
        }

        private async void UploadCardList(List<CardData> cards)
        {
            CardAddListRequest req = new CardAddListRequest();
            req.cards = new CardAddRequest[cards.Count];
            for(int i=0; i<cards.Count; i++)
            {
                CardData card = cards[i];
                CardAddRequest rcard = new CardAddRequest();
                rcard.tid = card.id;
                rcard.type = card.GetTypeId();
                rcard.team = card.team.id;
                rcard.rarity = card.rarity.id;
                rcard.mana = card.mana;
                rcard.attack = card.attack;
                rcard.hp = card.hp;
                rcard.cost = card.cost;
                rcard.packs = new string[card.packs.Length];
                for (int p = 0; p < card.packs.Length; p++)
                {
                    rcard.packs[p] = card.packs[p].id;
                }
                req.cards[i] = rcard;
            }

            string url = ApiClient.ServerURL + "/cards/add/list";
            string json = ApiTool.ToJson(req);
            await ApiClient.Get().SendPostRequest(url, json);
        }

        private async void UploadVariant(VariantData variant)
        {
            VariantAddRequest req = new VariantAddRequest();
            req.tid = variant.id;
            req.cost_factor = variant.cost_factor;
            req.is_default = variant.is_default;

            string url = ApiClient.ServerURL + "/variants/add";
            string json = ApiTool.ToJson(req);
            await ApiClient.Get().SendPostRequest(url, json);
        }

        private async void UploadDeckReward(DeckData deck)
        {
            RewardAddRequest req = new RewardAddRequest();
            req.tid = deck.id;
            req.group = "starter_deck";
            req.decks = new string[1] { deck.id };

            string url = ApiClient.ServerURL + "/rewards/add";
            string json = ApiTool.ToJson(req);
            await ApiClient.Get().SendPostRequest(url, json);
        }

        private async void UploadDeck(DeckData deck)
        {
            UserDeckData req = new UserDeckData(deck);
            string url = ApiClient.ServerURL + "/decks/add";
            string json = ApiTool.ToJson(req);
            await ApiClient.Get().SendPostRequest(url, json);
        }

        private async void UploadReward(RewardData reward)
        {
            RewardAddRequest req = new RewardAddRequest();
            req.tid = reward.id;
            req.group = "";
            req.coins = reward.coins;
            req.xp = reward.xp;
            req.repeat = reward.repeat;

            if (reward.cards != null)
            {
                req.cards = new string[reward.cards.Length];
                for (int i = 0; i < reward.cards.Length; i++)
                {
                    req.cards[i] = reward.cards[i].id;
                }
            }

            if (reward.decks != null)
            {
                req.decks = new string[reward.decks.Length];
                for (int i = 0; i < reward.decks.Length; i++)
                {
                    req.decks[i] = reward.decks[i].id;
                }
            }

            if (reward.packs != null)
            {
                req.packs = new string[reward.packs.Length];
                for (int i = 0; i < reward.packs.Length; i++)
                {
                    req.packs[i] = reward.packs[i].id;
                }
            }

            string url = ApiClient.ServerURL + "/rewards/add";
            string json = ApiTool.ToJson(req);
            await ApiClient.Get().SendPostRequest(url, json);
        }

        private async void UploadLevelReward(LevelData level)
        {
            RewardAddRequest req = new RewardAddRequest();
            req.tid = level.id;
            req.group = "";
            req.coins = level.reward_coins;
            req.xp = level.reward_xp;
            req.repeat = false;

            if (level.reward_cards != null)
            {
                req.cards = new string[level.reward_cards.Length];
                for (int i = 0; i < level.reward_cards.Length; i++)
                {
                    req.cards[i] = level.reward_cards[i].id;
                }
            }

            if (level.reward_packs != null)
            {
                req.packs = new string[level.reward_packs.Length];
                for (int i = 0; i < level.reward_packs.Length; i++)
                {
                    req.packs[i] = level.reward_packs[i].id;
                }
            }

            if (level.reward_decks != null)
            {
                req.decks = new string[level.reward_decks.Length];
                for (int i = 0; i < level.reward_decks.Length; i++)
                {
                    req.decks[i] = level.reward_decks[i].id;
                }
            }

            string url = ApiClient.ServerURL + "/rewards/add";
            string json = ApiTool.ToJson(req);
            await ApiClient.Get().SendPostRequest(url, json);
        }
        #endregion

        /// <summary>
        /// 获取指定范围内的卡牌列表
        /// </summary>
        private List<CardData> GetCardGroup(List<CardData> all_cards, int start, int count)
        {
            List<CardData> list = new List<CardData>();
            for (int i = 0; i < count; i++)
            {
                int index = start + i;
                if (index < all_cards.Count)
                {
                    CardData card = all_cards[index];
                    if (card.deckbuilding)
                    {
                        list.Add(card);
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// 显示信息
        /// </summary>
        private void ShowText(string txt)
        {
            msg_text.text = txt;
            Debug.Log(txt);
        }

        /// <summary>
        /// 点击按钮开始上传
        /// </summary>
        public void OnClickStart()
        {
            msg_text.text = "";
            Login();
        }
    }

    #region 上传请求数据类
    [System.Serializable]
    public class CardAddListRequest
    {
        public CardAddRequest[] cards; // 批量卡牌请求
    }

    [System.Serializable]
    public class CardAddRequest
    {
        public string tid;      // 卡牌 ID
        public string type;     // 类型
        public string team;     // 队伍
        public string rarity;   // 稀有度
        public int mana;        // 法力值
        public int attack;      // 攻击力
        public int hp;          // 生命值
        public int cost;        // 消耗
        public string[] packs;  // 所属卡包
    }

    [System.Serializable]
    public class PackAddRequest
    {
        public string tid;                     // 卡包 ID
        public int cards;                       // 卡牌数量
        public int cost;                        // 花费
        public bool random;                     // 是否随机
        public PackAddProbability[] rarities_1st; // 初始稀有度概率
        public PackAddProbability[] rarities;    // 稀有度概率
        public PackAddProbability[] variants;    // 变体概率
    }

    [System.Serializable]
    public class PackAddProbability
    {
        public string tid;   // 卡牌或变体 ID
        public int value;    // 概率值
    }

    [System.Serializable]
    public class VariantAddRequest
    {
        public string tid;         // 变体 ID
        public int cost_factor;    // 成本系数
        public bool is_default;    // 是否默认
    }

    [System.Serializable]
    public class RewardAddRequest
    {
        public string tid;         // 奖励 ID
        public string group;       // 奖励组
        public int coins;          // 金币数量
        public int xp;             // 经验值
        public string[] packs;     // 奖励卡包
        public string[] cards;     // 奖励卡牌
        public string[] decks;     // 奖励套牌
        public bool repeat;        // 是否可重复
    }
    #endregion
}
