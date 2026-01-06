using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// 卡包放大面板（PackZoomPanel）
    /// 当玩家点击 PackPanel 中的某个卡包时，会显示此面板展示卡包详细信息
    /// 玩家也可以在此面板中购买卡包
    /// </summary>

    public class PackZoomPanel : UIPanel
    {
        public PackUI pack_ui;           // 显示卡包的UI组件
        public Text desc;                // 卡包描述文本

        public GameObject buy_area;      // 购买区域
        public InputField buy_quantity;  // 输入购买数量
        public Text buy_cost;            // 显示购买总花费
        public Text buy_error;           // 显示购买错误信息

        private PackData pack;           // 当前显示的卡包数据

        private static PackZoomPanel instance;  // 单例实例

        protected override void Awake()
        {
            base.Awake();
            instance = this;

            // 注册所有 TabButton 点击事件
            TabButton.onClickAny += OnClickTab;
        }

        private void OnDestroy()
        {
            // 注销事件
            TabButton.onClickAny -= OnClickTab;
        }

        protected override void Update()
        {
            base.Update();

            // 动态更新购买总价
            if (pack != null)
            {
                int quantity = GetBuyQuantity();
                buy_cost.text = (pack.cost * quantity).ToString();
            }
        }

        /// <summary>
        /// 显示指定卡包的详细信息
        /// </summary>
        public void ShowPack(PackData pack)
        {
            this.pack = pack;

            UserData udata = Authenticator.Get().UserData;
            int quantity = udata.GetPackQuantity(pack.id);
            pack_ui.SetPack(pack, quantity);       // 设置UI显示数量
            desc.text = pack.GetDesc();            // 设置卡包描述
            buy_quantity.text = "1";               // 默认购买数量为1
            buy_error.text = "";                   // 清空错误信息
            buy_area?.SetActive(pack.available);   // 如果卡包可购买，显示购买区域

            Show();
        }

        /// <summary>
        /// 测试模式下购买卡包（直接扣除虚拟货币）
        /// </summary>
        private async void BuyPackTest()
        {
            int quantity = GetBuyQuantity();
            int cost = (quantity * pack.cost);
            if (quantity <= 0)
                return;

            UserData udata = Authenticator.Get().UserData;
            if (udata.coins < cost)
                return;

            udata.AddPack(pack.id, quantity);  // 增加卡包数量
            udata.coins -= cost;               // 扣除金币
            await Authenticator.Get().SaveUserData();  // 保存玩家数据
            PackPanel.Get().ReloadUserPack();           // 刷新PackPanel
            Hide();
        }

        /// <summary>
        /// API模式下购买卡包（向服务器发送购买请求）
        /// </summary>
        private async void BuyPackApi()
        {
            BuyPackRequest req = new BuyPackRequest();
            req.pack = pack.id;
            req.quantity = GetBuyQuantity();

            if (req.quantity <= 0)
                return;

            string url = ApiClient.ServerURL + "/users/packs/buy/";
            string jdata = ApiTool.ToJson(req);
            buy_error.text = "";

            WebResponse res = await ApiClient.Get().SendPostRequest(url, jdata);
            if (res.success)
            {
                PackPanel.Get().ReloadUserPack();  // 刷新卡包面板
                Hide();
            }
            else
            {
                buy_error.text = res.error;       // 显示错误信息
            }
        }

        /// <summary>
        /// 点击购买按钮
        /// </summary>
        public void OnClickBuy()
        {
            if (Authenticator.Get().IsTest())
            {
                BuyPackTest();
            }
            if (Authenticator.Get().IsApi())
            {
                BuyPackApi();
            }
        }

        /// <summary>
        /// 点击 Tab 时处理面板切换
        /// </summary>
        private void OnClickTab(TabButton btn)
        {
            if (btn.group == "menu")
                Hide();
        }

        /// <summary>
        /// 获取当前输入的购买数量
        /// </summary>
        public int GetBuyQuantity()
        {
            bool success = int.TryParse(buy_quantity.text, out int quantity);
            if (success)
                return quantity;
            return 0;
        }

        /// <summary>
        /// 获取当前显示的卡包数据
        /// </summary>
        public PackData GetPack()
        {
            return pack;
        }

        /// <summary>
        /// 获取单例
        /// </summary>
        public static PackZoomPanel Get()
        {
            return instance;
        }
    }
}
