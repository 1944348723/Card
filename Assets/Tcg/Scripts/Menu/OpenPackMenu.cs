using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace TcgEngine.Client
{
    /// <summary>
    /// 开包界面主脚本（Open Pack Menu）
    /// 负责处理卡包的开启、显示卡片、随机抽取以及保存玩家卡牌数据
    /// </summary>
    public class OpenPackMenu : MonoBehaviour
    {
        public GameObject card_prefab; // 卡牌预制体，用于实例化卡牌

        private bool revealing = false; // 是否正在显示开包动画
        private static OpenPackMenu instance; // 单例

        void Awake()
        {
            instance = this; // 保存单例
        }

        void Update()
        {
            // 如果正在开包动画中，点击鼠标左键检查是否所有卡牌已显示
            if (revealing && Input.GetMouseButtonDown(0))
            {
                bool all_revealed = true;
                foreach (PackCard card in PackCard.GetAll())
                {
                    if (!card.IsRevealed())
                        all_revealed = false;
                }

                // 所有卡牌显示完毕，则停止显示
                if (all_revealed && PackCard.GetAll().Count > 0)
                    StopReveal();
            }
        }

        /// <summary>
        /// 开启卡包（通过卡包ID）
        /// </summary>
        /// <param name="pack_tid">卡包ID</param>
        public void OpenPack(string pack_tid)
        {
            PackData pack = PackData.Get(pack_tid);
            if (pack != null)
            {
                OpenPack(pack);
            }
        }

        /// <summary>
        /// 开启卡包（通过卡包数据）
        /// 根据运行模式调用 API 或 测试模式方法
        /// </summary>
        /// <param name="pack">卡包数据</param>
        public void OpenPack(PackData pack)
        {
            if (Authenticator.Get().IsApi())
            {
                OpenPackApi(pack); // API模式
            }
            if (Authenticator.Get().IsTest())
            {
                OpenPackTest(pack); // 测试模式
            }
        }

        /// <summary>
        /// 测试模式开包
        /// 随机抽取卡牌或固定卡牌，并更新玩家数据
        /// </summary>
        public async void OpenPackTest(PackData pack)
        {
            UserData udata = Authenticator.Get().UserData;
            if (!udata.HasPack(pack.id))
                return;

            List<UserCardData> cards = new List<UserCardData>();
            List<CardData> all_cards = CardData.GetAll(pack);

            // 随机包处理
            if (pack.type == PackType.Random)
            {
                for (int i = 0; i < pack.cards; i++)
                {
                    RarityData rarity = GetRandomRarity(pack, i == 0); // 第一张卡可能有特殊稀有度规则
                    VariantData variant = GetRandomVariant(pack); // 随机卡面
                    List<CardData> vcards = GetCardArray(all_cards, rarity);
                    if (vcards.Count > 0)
                    {
                        CardData card = vcards[Random.Range(0, vcards.Count)];
                        UserCardData ucard = new UserCardData(card, variant);
                        cards.Add(ucard);
                    }
                }
            }

            // 固定包处理
            if (pack.type == PackType.Fixed)
            {
                for (int i = 0; i < Mathf.Min(pack.cards, all_cards.Count); i++)
                {
                    CardData card = all_cards[i];
                    VariantData variant = VariantData.GetDefault();
                    UserCardData ucard = new UserCardData(card, variant);
                    cards.Add(ucard);
                }
            }

            // 展示卡牌动画
            RevealCards(pack, cards.ToArray());

            // 更新玩家数据
            udata.AddPack(pack.id, -1);
            foreach (UserCardData card in cards)
            {
                udata.AddCard(card.tid, card.variant, card.quantity);
            }

            await Authenticator.Get().SaveUserData(); // 保存玩家数据
            HandPackArea.Get().LoadPacks(); // 刷新卡包界面
        }

        /// <summary>
        /// API模式开包
        /// 向服务器请求开包结果并显示卡牌
        /// </summary>
        public async void OpenPackApi(PackData pack)
        {
            UserData udata = Authenticator.Get().UserData;
            if (!udata.HasPack(pack.id))
                return;

            udata.AddPack(pack.id, -1);

            OpenPackRequest req = new OpenPackRequest();
            req.pack = pack.id;

            string url = ApiClient.ServerURL + "/users/packs/open";
            string json = ApiTool.ToJson(req);

            WebResponse res = await ApiClient.Get().SendPostRequest(url, json);
            if (res.success)
            {
                UserCardData[] cards = ApiTool.JsonToArray<UserCardData>(res.data);
                RevealCards(pack, cards); // 显示卡牌
            }

            HandPackArea.Get().LoadPacks();
        }

        /// <summary>
        /// 展示卡牌动画
        /// 实例化卡牌预制体，并设置目标位置
        /// </summary>
        public void RevealCards(PackData pack, UserCardData[] cards)
        {
            UserData udata = Authenticator.Get().UserData;
            CardbackData cb = CardbackData.Get(udata.cardback);
            HandPackArea.Get().Lock(true); // 锁定操作
            revealing = true;

            int index = 0;
            foreach (UserCardData card in cards)
            {
                CardData icard = CardData.Get(card.tid);
                VariantData variant = VariantData.Get(card.variant);
                if (icard != null && variant != null)
                {
                    GameObject cobj = Instantiate(card_prefab, new Vector3(0f, -3f, 0f), Quaternion.identity);
                    PackCard pcard = cobj.GetComponent<PackCard>();
                    pcard.SetCard(pack, icard, variant);
                    BoardRef bref = BoardRef.Get(BoardRefType.PackCard, index);
                    Vector3 pos = bref != null ? bref.transform.position : Vector3.zero;
                    pcard.SetTarget(pos);
                    index++;
                }
            }
        }

        /// <summary>
        /// 获取指定稀有度的卡牌列表
        /// </summary>
        private List<CardData> GetCardArray(List<CardData> all_cards, RarityData rarity)
        {
            List<CardData> cards = new List<CardData>();
            foreach (CardData acard in all_cards)
            {
                if (acard.rarity == rarity)
                    cards.Add(acard);
            }
            return cards;
        }

        /// <summary>
        /// 随机获取卡包稀有度
        /// 第一张卡可能使用不同概率表
        /// </summary>
        private RarityData GetRandomRarity(PackData pack, bool is_first)
        {
            PackRarity[] rarities = is_first ? pack.rarities_1st : pack.rarities;
            if (rarities == null || rarities.Length == 0)
                return RarityData.GetFirst();

            int total = 0;
            foreach (PackRarity rarity in rarities)
            {
                total += rarity.probability;
            }

            int rvalue = Mathf.FloorToInt(Random.value * total);
            for (int i = 0; i < rarities.Length; i++)
            {
                PackRarity rarity = rarities[i];
                if (rvalue < rarity.probability)
                {
                    return rarity.rarity;
                }
                rvalue -= rarity.probability;
            }
            return RarityData.GetFirst();
        }

        /// <summary>
        /// 随机获取卡牌版本
        /// </summary>
        private VariantData GetRandomVariant(PackData pack)
        {
            PackVariant[] variants = pack.variants;
            if (variants == null || variants.Length == 0)
                return VariantData.GetDefault();

            int total = 0;
            foreach (PackVariant variant in variants)
            {
                total += variant.probability;
            }

            int rvalue = Mathf.FloorToInt(Random.value * total);
            for (int i = 0; i < variants.Length; i++)
            {
                PackVariant variant = variants[i];
                if (rvalue < variant.probability)
                {
                    return variant.variant;
                }
                rvalue -= variant.probability;
            }
            return VariantData.GetDefault();
        }

        /// <summary>
        /// 停止显示开包动画
        /// </summary>
        public void StopReveal()
        {
            revealing = false;
            HandPackArea.Get().Lock(false); // 解锁操作
            foreach (PackCard card in PackCard.GetAll())
            {
                card.Remove(); // 移除显示的卡牌
            }
        }

        public void OnClickBack()
        {
            SceneNav.GoTo("Menu"); // 返回主菜单
        }

        public static OpenPackMenu Get()
        {
            return instance;
        }
    }

}
