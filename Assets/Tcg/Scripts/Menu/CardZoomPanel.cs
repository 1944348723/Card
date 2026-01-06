using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// 卡牌缩放面板 (CardZoomPanel)
    /// 当在菜单中点击一张卡牌时显示，用于查看详细信息、购买或出售卡牌
    /// 继承自 UIPanel，支持 Show/Hide 等 UI 功能
    /// 使用单例模式，方便其他类直接访问
    /// </summary>
    public class CardZoomPanel : UIPanel
    {
        // -----------------------
        // UI 元素
        // -----------------------
        public CardUI card_ui;          // 显示卡牌的 UI 元素
        public Text desc;               // 卡牌描述文本
        public Image quantity_bar;      // 显示拥有数量的进度条
        public Text quantity_txt;       // 拥有数量文本

        public GameObject trade_area;   // 交易区域（买/卖卡牌）
        public InputField trade_quantity; // 用户输入的交易数量
        public Text buy_cost;           // 购买所需金币
        public Text sell_cost;          // 出售可获得金币
        public Text trade_error;        // 交易错误提示

        // -----------------------
        // 卡牌数据
        // -----------------------
        private CardData card;          // 当前显示的卡牌
        private VariantData variant;    // 当前卡牌的变体信息

        private static CardZoomPanel instance; // 单例引用

        // -----------------------
        // 生命周期
        // -----------------------

        /// <summary>
        /// Awake 时设置单例，并监听 TabButton 点击事件
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            instance = this;
            TabButton.onClickAny += OnClickTab;
        }

        private void OnDestroy()
        {
            TabButton.onClickAny -= OnClickTab;
        }

        /// <summary>
        /// 每帧更新购买和出售的金币数
        /// </summary>
        protected override void Update()
        {
            base.Update();

            if (card != null)
            {
                int quantity = GetBuyQuantity();
                int cost = quantity * card.cost * variant.cost_factor;
                buy_cost.text = cost.ToString();
                sell_cost.text = Mathf.RoundToInt(cost * GameplayData.Get().sell_ratio).ToString();
            }
        }

        // -----------------------
        // 显示卡牌信息
        // -----------------------

        /// <summary>
        /// 显示指定卡牌的详细信息
        /// </summary>
        public void ShowCard(CardData card, VariantData variant)
        {
            this.card = card;
            this.variant = variant;

            // 获取用户数据
            UserData udata = Authenticator.Get().UserData;
            int quantity = udata.GetCardQuantity(card, variant);
            quantity_txt.text = quantity.ToString();
            quantity_txt.enabled = quantity > 0;
            quantity_bar.enabled = quantity > 0;
            trade_quantity.text = "1";
            trade_error.text = "";
            trade_area?.SetActive(card.deckbuilding && card.cost > 0);

            // 设置卡牌 UI
            card_ui.SetCard(card, variant);

            // 设置卡牌描述和技能描述
            string desc = card.GetDesc();
            string adesc = card.GetAbilitiesDesc();
            if(!string.IsNullOrWhiteSpace(desc))
                this.desc.text = desc + "\n\n" + adesc;
            else
                this.desc.text = adesc;

            Show();
        }

        /// <summary>
        /// 刷新卡牌显示信息（重新调用 ShowCard）
        /// </summary>
        public void RefreshCard()
        {
            ShowCard(card, variant);
        }

        // -----------------------
        // 买卡逻辑
        // -----------------------

        /// <summary>
        /// 测试模式下购买卡牌
        /// </summary>
        private async void BuyCardTest()
        {
            int quantity = GetBuyQuantity();
            int cost = (quantity * card.cost * variant.cost_factor);
            if (quantity <= 0)
                return;

            UserData udata = Authenticator.Get().UserData;
            if (udata.coins < cost)
                return;

            // 扣金币并添加卡牌
            udata.AddCard(card.id, variant.id, quantity);
            udata.coins -= cost;
            await Authenticator.Get().SaveUserData();

            // 刷新用户收藏面板
            CollectionPanel.Get().ReloadUser();
            Hide();
        }

        /// <summary>
        /// 在线模式通过 API 购买卡牌
        /// </summary>
        private async void BuyCardApi()
        {
            BuyCardRequest req = new BuyCardRequest();
            req.card = card.id;
            req.variant = variant.id;
            req.quantity = GetBuyQuantity();

            if (req.quantity <= 0)
                return;

            string url = ApiClient.ServerURL + "/users/cards/buy/";
            string jdata = ApiTool.ToJson(req);
            trade_error.text = "";

            WebResponse res = await ApiClient.Get().SendPostRequest(url, jdata);
            if (res.success)
            {
                CollectionPanel.Get().ReloadUser();
                Hide();
            }
            else
            {
                trade_error.text = res.error;
            }
        }

        // -----------------------
        // 卖卡逻辑
        // -----------------------

        private async void SellCardTest()
        {
            int quantity = GetBuyQuantity();
            int cost = Mathf.RoundToInt(quantity * card.cost * variant.cost_factor * GameplayData.Get().sell_ratio);
            if (quantity <= 0)
                return;

            UserData udata = Authenticator.Get().UserData;
            if (!udata.HasCard(card.id, variant.id, quantity))
                return;

            udata.AddCard(card.id, variant.id, -quantity);
            udata.coins += cost;
            await Authenticator.Get().SaveUserData();

            CollectionPanel.Get().ReloadUser();
            MainMenu.Get().RefreshDeckList();
            Hide();
        }

        private async void SellCardApi()
        {
            BuyCardRequest req = new BuyCardRequest();
            req.card = card.id;
            req.variant = variant.id;
            req.quantity = GetBuyQuantity();

            if (req.quantity <= 0)
                return;

            string url = ApiClient.ServerURL + "/users/cards/sell/";
            string jdata = ApiTool.ToJson(req);
            trade_error.text = "";

            WebResponse res = await ApiClient.Get().SendPostRequest(url, jdata);
            if (res.success)
            {
                CollectionPanel.Get().ReloadUser();
                Hide();
            }
            else
            {
                trade_error.text = res.error;
            }
        }

        // -----------------------
        // 按钮回调
        // -----------------------

        public void OnClickBuy()
        {
            if (Authenticator.Get().IsTest())
                BuyCardTest();
            if (Authenticator.Get().IsApi())
                BuyCardApi();
        }

        public void OnClickSell()
        {
            if (Authenticator.Get().IsTest())
                SellCardTest();
            if (Authenticator.Get().IsApi())
                SellCardApi();
        }

        private void OnClickTab(TabButton btn)
        {
            if (btn.group == "menu")
                Hide();
        }

        // -----------------------
        // 辅助方法
        // -----------------------

        /// <summary>
        /// 获取用户输入的购买或出售数量
        /// </summary>
        public int GetBuyQuantity()
        {
            bool success = int.TryParse(trade_quantity.text, out int quantity);
            if (success)
                return quantity;
            return 0;
        }

        public CardData GetCard() => card;
        public string GetCardId() => card.id;
        public string GetCardVariant() => variant.id;

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static CardZoomPanel Get()
        {
            return instance;
        }
    }
}
