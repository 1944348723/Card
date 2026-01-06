using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// 用于出售重复卡牌的面板
    /// 当玩家拥有多张相同卡牌时，可通过此面板出售多余的卡牌
    /// </summary>
    public class SellDuplicatePanel : UIPanel
    {
        public Text error; // 显示错误信息的文本

        private static SellDuplicatePanel instance; // 单例实例

        protected override void Awake()
        {
            base.Awake();
            instance = this; // 设置单例
        }

        /// <summary>
        /// 点击出售按钮时调用，向服务器发送出售重复卡牌请求
        /// </summary>
        public async void OnClickSell()
        {
            SellDuplicateRequest req = new SellDuplicateRequest();
            req.keep = 2; // 保留2张，出售多余的

            string url = ApiClient.ServerURL + "/users/cards/sell/duplicate";
            string jdata = ApiTool.ToJson(req);
            error.text = ""; // 清空错误信息

            // 发送请求
            WebResponse res = await ApiClient.Get().SendPostRequest(url, jdata);
            if (res.success)
            {
                // 刷新收藏面板并隐藏当前面板
                CollectionPanel.Get().ReloadUser();
                Hide();
            }
            else
            {
                // 显示错误信息
                error.text = res.error;
            }
        }

        /// <summary>
        /// 显示面板时调用，初始化错误信息
        /// </summary>
        /// <param name="instant">是否立即显示</param>
        public override void Show(bool instant = false)
        {
            base.Show(instant);
            error.text = "";
        }

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static SellDuplicatePanel Get()
        {
            return instance;
        }
    }
}