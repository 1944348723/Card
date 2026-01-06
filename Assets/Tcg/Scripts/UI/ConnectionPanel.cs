using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    /// <summary>
    /// 当网络连接丢失时显示的面板
    /// </summary>
    public class ConnectionPanel : UIPanel
    {
        private static ConnectionPanel instance; // 单例实例

        protected override void Awake()
        {
            base.Awake();
            instance = this; // 初始化单例
        }

        // 点击退出按钮
        public void OnClickQuit()
        {
            GameClient.Get()?.Disconnect(); // 断开客户端连接
            SceneNav.GoTo("LoginMenu");     // 返回登录菜单
        }

        // 获取单例实例
        public static ConnectionPanel Get()
        {
            return instance;
        }
    }
}