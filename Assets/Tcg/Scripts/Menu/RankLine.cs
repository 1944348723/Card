using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// 排行榜面板中的一行（RankLine）
    /// 显示玩家排名、名字、Elo值、胜率，并可高亮显示
    /// </summary>

    public class RankLine : MonoBehaviour
    {
        public Text ranking;       // 排名文本
        public Text player;        // 玩家名字文本
        public Text elo_txt;       // Elo值文本
        public Text winrate_txt;   // 胜率文本
        public Image highlight;    // 高亮显示图片

        public UnityAction<string> onClick; // 点击行事件，返回用户名

        private string username;   // 当前行对应的用户名

        void Start()
        {
            highlight.enabled = false; // 初始隐藏高亮
        }

        /// <summary>
        /// 设置行内容
        /// </summary>
        /// <param name="udata">玩家数据</param>
        /// <param name="ranking">玩家排名</param>
        /// <param name="highlight">是否高亮显示</param>
        public void SetLine(UserData udata, int ranking, bool highlight)
        {
            this.username = udata.username;
            this.ranking.text = ranking.ToString();
            this.player.text = username;
            this.elo_txt.text = udata.elo.ToString();

            // 计算胜率
            int win_rate = Mathf.RoundToInt(udata.victories * 100f / Mathf.Max(udata.matches, 1));
            this.winrate_txt.text = win_rate.ToString() + "%";

            this.highlight.enabled = highlight; // 设置是否高亮
            gameObject.SetActive(true);         // 显示该行
        }

        /// <summary>
        /// 隐藏该行
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 获取该行的用户名
        /// </summary>
        /// <returns>用户名</returns>
        public string GetUsername()
        {
            return username;
        }

        /// <summary>
        /// 点击该行时触发事件
        /// </summary>
        public void OnClick()
        {
            onClick?.Invoke(username);
        }
    }
}
