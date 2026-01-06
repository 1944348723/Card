using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// 新手套牌选择面板
    /// 仅在 API 模式下新账号登录主菜单时显示
    /// 用户可以选择一个 starter deck（初始套牌）
    /// </summary>
    public class StarterDeckPanel : UIPanel
    {
        public DeckDisplay[] decks;  // 套牌显示组件数组，每个显示一个可选的 starter deck

        public Text error;           // 错误提示文本

        private static StarterDeckPanel instance; // 单例实例

        protected override void Awake()
        {
            base.Awake();
            instance = this;
        }

        /// <summary>
        /// 刷新面板显示，把所有 starter deck 显示在面板上
        /// </summary>
        private void RefreshPanel()
        {
            int index = 0;
            foreach (DeckData deck in GameplayData.Get().starter_decks)
            {
                if (index < decks.Length)
                {
                    DeckDisplay display = decks[index];
                    display.SetDeck(deck); // 设置每个 DeckDisplay 显示对应的套牌
                    index++;
                }
            }
        }

        /// <summary>
        /// 选择套牌
        /// 根据当前模式调用 Test 或 API 方法
        /// </summary>
        private void ChooseDeck(string deck_id)
        {
            if (Authenticator.Get().IsTest())
                ChooseDeckTest(deck_id);
            if (Authenticator.Get().IsApi())
                ChooseDeckApi(deck_id);
        }

        /// <summary>
        /// 测试模式下选择套牌
        /// </summary>
        private async void ChooseDeckTest(string deck_id)
        {
            UserData udata = Authenticator.Get().UserData;
            DeckData deck = DeckData.Get(deck_id);
            if (deck == null)
                return;

            // 创建用户套牌
            UserDeckData udeck = new UserDeckData();
            udeck.tid = deck_id + "_" + GameTool.GenerateRandomID(4, 7); // 给套牌加随机 ID，防止和 starter deck 冲突
            udeck.title = deck.title;
            udeck.hero = new UserCardData(deck.hero, VariantData.GetDefault());

            List<UserCardData> cards = new List<UserCardData>();
            foreach (CardData card in deck.cards)
            {
                UserCardData ucard = new UserCardData(card, VariantData.GetDefault());
                cards.Add(ucard);
            }

            udeck.cards = cards.ToArray();
            udata.AddDeck(udeck);       // 添加到用户套牌
            udata.AddReward(udeck.tid); // 添加为已领取奖励

            await Authenticator.Get().SaveUserData();

            CollectionPanel.Get().ReloadUserDecks(); // 刷新收藏面板
            Hide();                                  // 隐藏 starter deck 面板
        }

        /// <summary>
        /// API 模式下选择套牌
        /// </summary>
        private async void ChooseDeckApi(string deck_id)
        {
            RewardGainRequest req = new RewardGainRequest();
            req.reward = deck_id;

            if (error != null)
                error.text = "";

            string url = ApiClient.ServerURL + "/users/rewards/gain/" + ApiClient.Get().UserID;
            string json = ApiTool.ToJson(req);
            WebResponse res = await ApiClient.Get().SendPostRequest(url, json);
            if (res.success)
            {
                CollectionPanel.Get().ReloadUserDecks(); // 刷新收藏面板
                Hide();
            }
            else
            {
                if (error != null)
                    error.text = res.error; // 显示错误信息
            }
        }

        public override void Show(bool instant = false)
        {
            base.Show(instant);
            if(error != null)
                error.text = "";
            RefreshPanel(); // 显示面板时刷新显示
        }

        /// <summary>
        /// 点击套牌时调用
        /// </summary>
        public void OnClickDeck(int index)
        {
            if (index < decks.Length)
            {
                DeckDisplay display = decks[index];
                string deck = display.GetDeck();
                ChooseDeck(deck); // 选择对应套牌
            }
        }

        public static StarterDeckPanel Get()
        {
            return instance; // 获取单例
        }
    }
}
