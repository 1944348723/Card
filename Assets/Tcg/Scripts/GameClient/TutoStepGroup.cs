using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine.Client
{
    /// <summary>
    /// 教程步骤组（TutoStepGroup）
    /// 步骤组不需要按顺序触发，会在满足 start_trigger 条件时触发
    /// 一旦触发，组内的所有 TutoStep 会按顺序执行
    /// </summary>
    public class TutoStepGroup : MonoBehaviour
    {
        [Header("触发条件")]
        public int turn_min = 0;              // 触发步骤组的最小回合数
        public int turn_max = 99;             // 触发步骤组的最大回合数
        public TutoStartTrigger start_trigger; // 步骤组触发类型（例如：StartTurn, PlayCard 等）
        public CardData start_target;          // 步骤组触发的目标卡牌（可选）
        public bool forced;                    // 是否强制完成组内所有步骤后才能触发其他组

        private int step;                     // 当前步骤组在父对象下的索引
        private bool triggered = false;       // 标记是否已被触发

        // 全局保存所有步骤组的列表
        private static List<TutoStepGroup> groups = new List<TutoStepGroup>();

        /// <summary>
        /// Awake 生命周期：初始化索引并加入全局步骤组列表
        /// </summary>
        void Awake()
        {
            step = transform.GetSiblingIndex(); // 获取当前组在父对象下的索引
            groups.Add(this);                   // 添加到全局组列表
        }

        /// <summary>
        /// 设置本步骤组为已触发
        /// </summary>
        public void SetTriggered()
        {
            triggered = true;
        }

        /// <summary>
        /// 根据触发类型和回合数获取未触发的步骤组
        /// </summary>
        public static TutoStepGroup Get(TutoStartTrigger trigger, int turn)
        {
            foreach (TutoStepGroup s in groups)
            {
                if (s.start_trigger == trigger && !s.triggered) // 触发类型匹配且未触发过
                {
                    if (turn >= s.turn_min && turn <= s.turn_max) // 回合数在范围内
                        return s;
                }
            }
            return null;
        }

        /// <summary>
        /// 根据触发类型、目标卡牌和回合数获取未触发的步骤组
        /// </summary>
        public static TutoStepGroup Get(TutoStartTrigger trigger, CardData target, int turn)
        {
            foreach (TutoStepGroup s in groups)
            {
                if (s.start_trigger == trigger && !s.triggered) // 触发类型匹配且未触发过
                {
                    if (turn >= s.turn_min && turn <= s.turn_max) // 回合数在范围内
                    {
                        // 如果没有指定目标卡牌，或者目标卡牌匹配
                        if (s.start_target == null || s.start_target == target)
                            return s;
                    }
                }
            }
            return null;
        }
    }
}
