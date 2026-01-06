using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    /// <summary>
    /// 冒险模式面板 (AdventurePanel)
    /// 负责显示冒险关卡列表，并在显示时刷新关卡信息
    /// 继承自 UIPanel，支持 Show/Hide 等 UI 基础功能
    /// 使用单例模式，方便其他类直接访问
    /// </summary>
    public class AdventurePanel : UIPanel
    {
        // 存储关卡的 UI 元素，每个 LevelUI 对应一个关卡
        private List<LevelUI> level_uis = new List<LevelUI>();

        // 单例引用
        private static AdventurePanel instance;

        /// <summary>
        /// 初始化方法，Awake 时设置单例
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            instance = this;
        }

        /// <summary>
        /// Start 生命周期，获取所有子对象中的 LevelUI 并加入列表
        /// </summary>
        protected override void Start()
        {
            base.Start();
            // 将该面板下所有 LevelUI 组件加入 level_uis 列表
            level_uis.AddRange(GetComponentsInChildren<LevelUI>());
        }

        /// <summary>
        /// 刷新所有关卡 UI
        /// 遍历 level_uis 列表，调用每个 LevelUI 的 RefreshLevel 方法
        /// 用于更新显示关卡进度、星级、解锁状态等信息
        /// </summary>
        private void RefreshLevels()
        {
            foreach (LevelUI level in level_uis)
            {
                level.RefreshLevel();
            }
        }

        /// <summary>
        /// 显示冒险面板
        /// 参数 instant 控制是否立即显示，不使用动画
        /// 在显示时刷新所有关卡 UI
        /// </summary>
        /// <param name="instant">是否立即显示</param>
        public override void Show(bool instant = false)
        {
            base.Show(instant);
            RefreshLevels();
        }

        /// <summary>
        /// 获取单例实例
        /// 方便其他类直接调用 AdventurePanel.Get() 来访问面板
        /// </summary>
        public static AdventurePanel Get()
        {
            return instance;
        }
    }
}