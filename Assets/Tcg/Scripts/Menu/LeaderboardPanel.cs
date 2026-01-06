using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// LeaderboardPanel 类
    /// 排行榜面板，显示所有玩家的排名信息。
    /// 继承自 UIPanel。
    /// </summary>
    public class LeaderboardPanel : UIPanel
    {
        /// <summary>
        /// 排行榜内容容器（用于存放 RankLine）
        /// </summary>
        public RectTransform content;

        /// <summary>
        /// 排行榜单行模板
        /// </summary>
        public RankLine line_template;

        /// <summary>
        /// 当前玩家自己的排行行
        /// </summary>
        public RankLine my_line;

        /// <summary>
        /// 每行的垂直间距
        /// </summary>
        public float line_spacing = 80f;

        /// <summary>
        /// 测试文本（当非 API 模式下显示）
        /// </summary>
        public Text test_text;

        /// <summary>
        /// 所有生成的排行榜行
        /// </summary>
        private List<RankLine> lines = new List<RankLine>();

        /// <summary>
        /// 单例实例
        /// </summary>
        private static LeaderboardPanel instance;

        /// <summary>
        /// Awake 生命周期
        /// 初始化单例，设置当前玩家排行行的点击事件，初始化排行榜行
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            instance = this;

            my_line.onClick += OnClickLine;
            InitLines();
        }

        private void OnDestroy()
        {
            // 可用于销毁资源或取消事件订阅
        }

        /// <summary>
        /// 初始化排行榜行
        /// 清空原有内容，并根据模板创建固定数量行
        /// </summary>
        private void InitLines()
        {
            for (int i = 0; i < content.transform.childCount; i++)
                Destroy(content.transform.GetChild(i).gameObject);

            int nlines = 100; // 默认生成100行
            for (int i = 0; i < nlines; i++)
            {
                RankLine line = AddLine(line_template, i);
                lines.Add(line);
            }

            content.sizeDelta = new Vector2(content.sizeDelta.x, nlines * line_spacing + 20f);
        }

        /// <summary>
        /// 根据模板生成单条排行榜行
        /// </summary>
        /// <param name="template">行模板</param>
        /// <param name="index">行索引</param>
        /// <returns>生成的 RankLine 对象</returns>
        private RankLine AddLine(RankLine template, int index)
        {
            Vector2 pos = Vector2.down * line_spacing;
            GameObject line = Instantiate(template.gameObject, content);
            RectTransform rtrans = line.GetComponent<RectTransform>();
            RankLine rline = line.GetComponent<RankLine>();
            rtrans.anchorMin = new Vector2(0.5f, 1f);
            rtrans.anchorMax = new Vector2(0.5f, 1f);
            rtrans.anchoredPosition = pos + Vector2.down * index * line_spacing;
            rline.onClick += OnClickLine;
            return rline;
        }

        /// <summary>
        /// 刷新排行榜面板
        /// 从 API 获取用户数据，排序后显示，更新当前玩家行
        /// </summary>
        private async void RefreshPanel()
        {
            // 隐藏当前玩家行和所有行
            my_line.Hide();
            foreach (RankLine line in lines)
                line.Hide();

            // 非 API 模式下显示测试文本
            test_text.enabled = !Authenticator.Get().IsApi();

            if (!Authenticator.Get().IsApi())
                return;

            UserData udata = ApiClient.Get().UserData;

            int index = 0;
            string url = ApiClient.ServerURL + "/users";
            WebResponse res = await ApiClient.Get().SendGetRequest(url);

            UserData[] users = ApiTool.JsonToArray<UserData>(res.data);
            List<UserData> sorted_users = new List<UserData>(users);
            // 按 Elo 排序，分数高的排在前面
            sorted_users.Sort((UserData a, UserData b) => { return b.elo.CompareTo(a.elo); });

            int previous_rank = 0;
            int previous_index = 0;

            foreach (UserData user in sorted_users)
            {
                // 忽略管理员和未参加过比赛的玩家
                if (user.permission_level != 1 || user.matches == 0)
                    continue;

                // 当前用户行显示在 my_line
                if (user.username == udata.username)
                {
                    my_line.SetLine(user, index + 1, true);
                }

                if (index < lines.Count)
                {
                    RankLine line = lines[index];
                    int rank_order = (previous_rank == user.elo) ? previous_index : index;
                    line.SetLine(user, rank_order + 1, user.username == udata.username);
                    previous_rank = user.elo;
                    previous_index = rank_order;
                }

                index++;
            }
        }

        /// <summary>
        /// 排行榜行点击事件（可以扩展，比如查看玩家详情）
        /// </summary>
        /// <param name="username">点击的玩家用户名</param>
        private void OnClickLine(string username)
        {

        }

        /// <summary>
        /// 显示排行榜面板时刷新数据
        /// </summary>
        public override void Show(bool instant = false)
        {
            base.Show(instant);
            RefreshPanel();
        }

        /// <summary>
        /// 点击返回按钮，隐藏排行榜面板
        /// </summary>
        public void OnClickBack()
        {
            Hide();
        }

        /// <summary>
        /// 获取单例实例
        /// </summary>
        /// <returns>LeaderboardPanel 单例</returns>
        public static LeaderboardPanel Get()
        {
            return instance;
        }
    }
}
