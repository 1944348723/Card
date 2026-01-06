using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TcgEngine.Client;
using TcgEngine;

namespace TcgEngine.UI
{
    /// <summary>
    /// 匹配面板（Matchmaking Panel）
    /// 仅作为加载面板显示当前匹配状态，例如已找到多少玩家
    /// </summary>
    public class MatchmakingPanel : UIPanel
    {
        public Text text;          // 显示匹配状态文本（正在连接或正在寻找对手）
        public Text players_txt;   // 显示已找到的玩家数量
        public Text code_txt;      // 显示加入游戏的代码（Code模式）

        private static MatchmakingPanel instance; // 单例

        protected override void Awake()
        {
            base.Awake();
            instance = this; // 保存单例
        }

        protected override void Start()
        {
            base.Start();
            code_txt.text = ""; // 初始化 Code 文本为空
        }

        protected override void Update()
        {
            base.Update();

            // 更新匹配状态显示
            if (GameClientMatchmaker.Get().IsConnected())
                text.text = "Finding Opponent..."; // 已连接服务器，正在寻找对手
            else
                text.text = "Connecting to server..."; // 还未连接服务器

            // 初始化 Code 文本
            code_txt.text = "";

            // 如果当前匹配为 Code 模式，显示游戏代码
            string group = GameClientMatchmaker.Get().GetGroup();
            if (group != null && group.StartsWith("code_"))
                code_txt.text = group.Replace("code_", "");
        }

        /// <summary>
        /// 设置当前找到的玩家数量
        /// </summary>
        /// <param name="players">当前玩家数量</param>
        public void SetCount(int players)
        {
            if (players_txt != null)
                players_txt.text = players.ToString() + "/" + GameClientMatchmaker.Get().GetNbPlayers();
        }

        /// <summary>
        /// 点击取消匹配按钮
        /// </summary>
        public void OnClickCancel()
        {
            GameClientMatchmaker.Get().StopMatchmaking(); // 停止匹配
            Hide(); // 隐藏面板
        }

        /// <summary>
        /// 显示匹配面板
        /// </summary>
        /// <param name="instant">是否立即显示</param>
        public override void Show(bool instant = false)
        {
            base.Show(instant);
            if (players_txt != null)
                players_txt.text = ""; // 初始化玩家数量文本为空
        }

        /// <summary>
        /// 获取单例
        /// </summary>
        public static MatchmakingPanel Get()
        {
            return instance;
        }
    }
}
