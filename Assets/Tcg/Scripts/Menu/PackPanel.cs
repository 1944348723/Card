using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// 卡包面板（PackPanel）
    /// 类似于卡牌收藏面板（Collection），但显示玩家拥有的所有卡包以及可购买/可获得的所有卡包
    /// </summary>

    public class PackPanel : UIPanel
    {
        [Header("Packs")]
        public ScrollRect scroll_rect;          // 滚动区域
        public RectTransform scroll_content;    // 滚动内容容器
        public CardGrid grid_content;           // 网格布局，用于排列卡包
        public GameObject pack_prefab;          // 卡包预制体，用于实例化卡包UI

        private List<GameObject> pack_list = new List<GameObject>();  // 当前显示的卡包对象列表

        private static PackPanel instance;      // 单例实例

        protected override void Awake()
        {
            base.Awake();
            instance = this;

            // 删除网格中的所有旧内容
            for (int i = 0; i < grid_content.transform.childCount; i++)
                Destroy(grid_content.transform.GetChild(i).gameObject);
        }

        protected override void Start()
        {
            base.Start();
        }

        protected override void Update()
        {
            base.Update();
        }

        /// <summary>
        /// 重新加载玩家卡包信息
        /// </summary>
        public async void ReloadUserPack()
        {
            await Authenticator.Get().LoadUserData();  // 加载玩家数据
            RefreshPacks();                            // 刷新卡包显示
        }

        /// <summary>
        /// 刷新所有内容（卡包 + 初始卡组）
        /// </summary>
        private void RefreshAll()
        {
            RefreshPacks();
            RefreshStarterDeck();
        }

        /// <summary>
        /// 刷新玩家拥有的卡包显示
        /// </summary>
        public void RefreshPacks()
        {
            UserData udata = Authenticator.Get().UserData;

            // 清空之前显示的卡包
            foreach (GameObject card in pack_list)
                Destroy(card.gameObject);
            pack_list.Clear();

            // 遍历所有可用卡包并创建UI
            foreach (PackData pack in PackData.GetAllAvailable())
            {
                GameObject nPack = Instantiate(pack_prefab, grid_content.transform);
                PackUI pack_ui = nPack.GetComponentInChildren<PackUI>();
                pack_ui.SetPack(pack, udata.GetPackQuantity(pack.id));  // 设置卡包及数量
                pack_ui.onClick += OnClickPack;                        // 点击事件
                pack_ui.onClickRight += OnClickPack;                   // 右键点击事件
                pack_list.Add(nPack);
            }
        }

        /// <summary>
        /// 检查并刷新初始卡组，如果玩家没有任何卡牌或奖励，则显示初始卡组面板
        /// </summary>
        private void RefreshStarterDeck()
        {
            UserData udata = Authenticator.Get().UserData;
            if (udata != null && (udata.cards.Length == 0 || udata.rewards.Length == 0))
            {
                if (GameplayData.Get().starter_decks.Length > 0)
                {
                    StarterDeckPanel.Get().Show();  // 显示初始卡组选择面板
                }
            }
        }
        
        /// <summary>
        /// 点击卡包时显示卡包详情
        /// </summary>
        public void OnClickPack(PackUI pack)
        {
            PackZoomPanel.Get().ShowPack(pack.GetPack());
        }

        /// <summary>
        /// 右键点击卡包时显示卡包详情
        /// </summary>
        public void OnClickCardRight(PackUI pack)
        {
            PackZoomPanel.Get().ShowPack(pack.GetPack());
        }

        /// <summary>
        /// 打开开包界面
        /// </summary>
        public void OnClickOpenPacks()
        {
            MainMenu.Get().FadeToScene("OpenPack");
        }

        /// <summary>
        /// 显示面板时刷新内容
        /// </summary>
        public override void Show(bool instant = false)
        {
            base.Show(instant);
            RefreshAll();
        }

        /// <summary>
        /// 获取单例
        /// </summary>
        public static PackPanel Get()
        {
            return instance;
        }
    }
}
