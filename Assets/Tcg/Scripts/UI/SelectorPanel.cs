using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine.UI
{
    /// <summary>
    /// 选择面板基类，用于在游戏中显示可选项（例如卡牌技能选择）
    /// 所有选择面板继承自此类
    /// </summary>
    public class SelectorPanel : UIPanel
    {
        private static List<SelectorPanel> panel_list = new List<SelectorPanel>(); // 所有选择面板实例的列表

        protected override void Awake()
        {
            base.Awake();
            panel_list.Add(this); // 添加到静态列表中
        }

        protected virtual void OnDestroy()
        {
            panel_list.Remove(this); // 从静态列表中移除
        }

        /// <summary>
        /// 显示面板的方法，可重写以显示具体内容
        /// </summary>
        /// <param name="ability">技能数据</param>
        /// <param name="card">卡牌数据</param>
        public virtual void Show(AbilityData ability, Card card)
        {
            // 重写此方法以显示面板
        }

        /// <summary>
        /// 判断面板是否应显示，可重写
        /// </summary>
        /// <returns>是否显示</returns>
        public virtual bool ShouldShow()
        {
            return false; // 重写此函数以决定何时显示面板
        }

        /// <summary>
        /// 获取所有选择面板实例
        /// </summary>
        public static List<SelectorPanel> GetAll()
        {
            return panel_list;
        }

        /// <summary>
        /// 隐藏所有可见的选择面板
        /// </summary>
        public static void HideAll()
        {
            foreach (SelectorPanel panel in panel_list)
            {
                if(panel.IsVisible())
                    panel.Hide();
            }
        }
    }
}