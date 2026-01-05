using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TcgEngine.FX
{
    /// <summary>
    /// 材质动画工具类
    /// 用于对材质(Material)的浮点属性执行动画序列
    /// 可以让材质属性按时间渐变，并支持动作完成后的回调
    /// </summary>
    public class AnimMatFX : MonoBehaviour
    {
        private Material target;              // 动画目标材质
        private float timer = 0f;             // 当前动画计时器

        private float start_val;              // 动画起始值
        private float current_val;            // 动画当前值

        private AnimMatAction current = null; // 当前正在执行的动画动作
        private Queue<AnimMatAction> sequence = new Queue<AnimMatAction>(); // 动画动作队列

        void Start()
        {
            // 初始化，可留空
        }

        void Update()
        {
            if (target == null)
                return; // 没有目标材质，退出

            // 如果当前没有动作且队列中还有动作，则取出下一个动作
            if (current == null && sequence.Count > 0)
            {
                current = sequence.Dequeue();
                start_val = target.GetFloat(current.target_name); // 获取材质当前属性值
                current_val = start_val;
                timer = 0f;
            }

            // 执行动画
            if (current != null)
            {
                if (timer < current.duration) // 动画进行中
                {
                    timer += Time.deltaTime;

                    if (current.type == AnimMatActionType.Float) // 浮点动画
                    {
                        // 计算动画速度
                        float dist = Mathf.Abs(current.target_value - start_val);
                        float speed = dist / Mathf.Max(current.duration, 0.01f);

                        // 更新当前值
                        current_val = Mathf.MoveTowards(current_val, current.target_value, speed * Time.deltaTime);

                        // 应用到材质属性
                        target.SetFloat(current.target_name, current_val);
                    }
                }
                else // 动画完成
                {
                    current.callback?.Invoke(); // 执行动画完成回调
                    current = null;             // 准备下一个动作
                }
            }
        }

        /// <summary>
        /// 添加浮点属性动画
        /// </summary>
        /// <param name="name">材质属性名</param>
        /// <param name="value">目标值</param>
        /// <param name="duration">动画持续时间（秒）</param>
        public void SetFloat(string name, float value, float duration)
        {
            AnimMatAction action = new AnimMatAction();
            action.type = AnimMatActionType.Float;
            action.duration = duration;
            action.target_name = name;
            action.target_value = value;
            sequence.Enqueue(action); // 入队列
        }

        /// <summary>
        /// 添加延迟回调动作
        /// </summary>
        /// <param name="duration">等待时间（秒）</param>
        /// <param name="callback">回调函数</param>
        public void Callback(float duration, UnityAction callback)
        {
            AnimMatAction action = new AnimMatAction();
            action.type = AnimMatActionType.None; // 无动画，仅回调
            action.duration = duration;
            action.callback = callback;
            sequence.Enqueue(action);
        }

        /// <summary>
        /// 清空动画队列和计时器
        /// </summary>
        public void Clear()
        {
            target = null;
            timer = 0f;
            sequence.Clear();
        }

        /// <summary>
        /// 静态方法创建 AnimMatFX 并绑定到指定对象
        /// </summary>
        /// <param name="obj">目标 GameObject</param>
        /// <param name="target">目标材质</param>
        public static AnimMatFX Create(GameObject obj, Material target)
        {
            AnimMatFX anim = obj.GetComponent<AnimMatFX>();
            if (anim == null)
                anim = obj.AddComponent<AnimMatFX>();

            anim.Clear();   // 清空之前的动画
            anim.target = target;
            return anim;
        }
    }

    /// <summary>
    /// 材质动画类型
    /// </summary>
    public enum AnimMatActionType
    {
        None = 0,   // 无动作，仅延迟回调
        Float = 5,  // 浮点属性渐变
    }

    /// <summary>
    /// 材质动画动作数据
    /// </summary>
    public class AnimMatAction
    {
        public AnimMatActionType type;   // 动作类型
        public string target_name;        // 材质属性名
        public float target_value;        // 目标值
        public float duration = 1f;       // 持续时间
        public UnityAction callback = null; // 动画完成回调
    }
}
