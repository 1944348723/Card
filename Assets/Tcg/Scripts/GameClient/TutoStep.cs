using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>
    /// 教程步骤类（TutoStep）
    /// 属于一个步骤组（TutoStepGroup），按顺序触发
    /// 游戏在满足 end_trigger 条件或另一个步骤组被触发时，进入下一步骤
    /// </summary>
    public class TutoStep : UIPanel
    {
        [Header("教程步骤设置")]
        public TutoEndTrigger end_trigger;  // 本步骤结束触发类型（玩家必须做什么才能结束本步骤）
        public CardData trigger_target;     // 本步骤关联的目标卡牌（可选）
        public bool forced;                 // 是否强制玩家必须完成 end_trigger 动作才能继续

        private TutoStepGroup group;        // 所属的步骤组
        private int step;                   // 当前步骤在组中的索引
        private TutoBox tuto_box;           // 教程提示框组件

        // 所有已创建的教程步骤列表（用于全局管理）
        private static List<TutoStep> steps = new List<TutoStep>();

        /// <summary>
        /// Awake 生命周期：初始化步骤索引、所属组和提示框，并加入全局步骤列表
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            step = transform.GetSiblingIndex();          // 当前步骤在父对象下的索引
            group = GetComponentInParent<TutoStepGroup>(); // 获取父对象的步骤组
            tuto_box = GetComponentInChildren<TutoBox>(); // 获取子对象的提示框组件
            steps.Add(this);                             // 加入全局列表
        }

        /// <summary>
        /// Start 生命周期：设置提示框按钮是否显示
        /// 当结束触发类型为 Click 时，显示“下一步”按钮
        /// </summary>
        protected override void Start()
        {
            base.Start();
            tuto_box.SetNextButton(end_trigger == TutoEndTrigger.Click);
        }

        /// <summary>
        /// OnDestroy 生命周期：移除全局列表
        /// </summary>
        protected virtual void OnDestroy()
        {
            steps.Remove(this);
        }

        /// <summary>
        /// 获取当前步骤索引
        /// </summary>
        public int GetStepIndex()
        {
            return step;
        }

        /// <summary>
        /// 根据步骤组和索引获取特定的步骤
        /// </summary>
        public static TutoStep Get(TutoStepGroup group, int step)
        {
            foreach (TutoStep s in steps)
            {
                if (s.group == group && s.step == step)
                    return s;
            }
            return null;
        }

        /// <summary>
        /// 隐藏所有教程步骤
        /// </summary>
        public static void HideAll()
        {
            foreach (TutoStep s in steps)
            {
                s.Hide(); // UIPanel 提供的隐藏方法
            }
        }
    }
}
