using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    /// <summary>
    /// LoadPanel 加载面板
    /// 在对局开始时显示，等待玩家连接
    /// </summary>
    public class LoadPanel : UIPanel
    {
        public Text load_txt;          // 显示加载文字的文本组件

        private static LoadPanel instance;  // 单例实例

        protected override void Awake()
        {
            base.Awake();
            instance = this;          // 设置单例
            if (load_txt != null)
                load_txt.text = "";   // 初始化文本为空
        }

        protected override void Start()
        {
            base.Start();

            // 订阅游戏客户端事件
            GameClient.Get().onConnectGame += OnConnect;       // 连接服务器事件
            GameClient.Get().onPlayerReady += OnReady;        // 玩家准备事件
            GameClient.Get().onGameStart += OnStart;          // 游戏开始事件

            SetLoadText("Connecting to server...");          // 初始提示文字
        }

        /// <summary>
        /// 当连接服务器时调用
        /// </summary>
        private void OnConnect()
        {
            SetLoadText("Sending player data...");           // 提示正在发送玩家数据
        }

        /// <summary>
        /// 当游戏开始时调用
        /// </summary>
        private void OnStart()
        {
            SetLoadText("");                                 // 清空加载文字
        }

        /// <summary>
        /// 当玩家准备完成时调用
        /// </summary>
        private void OnReady(int player_id)
        {
            if (player_id == GameClient.Get().GetPlayerID())
            {
                SetLoadText("Waiting for other player..."); // 如果是本地玩家，提示等待对手
            }
        }

        /// <summary>
        /// 设置加载文字
        /// </summary>
        private void SetLoadText(string text)
        {
            if (IsOnline())
            {
                if (load_txt != null)
                    load_txt.text = text;
            }
        }

        /// <summary>
        /// 检查当前游戏是否为在线对局
        /// </summary>
        public bool IsOnline()
        {
            return GameClient.game_settings.IsOnline();
        }

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static LoadPanel Get()
        {
            return instance;
        }
    }
}
