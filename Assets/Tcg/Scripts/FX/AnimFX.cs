using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TcgEngine.FX
{
    /// <summary>
    /// 用于定义动画序列的工具类
    /// 可对 GameObject 执行移动、缩放或延迟回调等动画效果
    /// </summary>
    public class AnimFX : MonoBehaviour
    {
        private GameObject target;           // 动画目标对象
        private float timer = 0f;            // 当前动画计时器

        private Vector3 start_pos;           // 动画起始位置
        private Vector3 current_pos;         // 动画当前坐标

        private AnimAction current = null;   // 当前执行的动画动作
        private Queue<AnimAction> sequence = new Queue<AnimAction>();  // 动画队列，按顺序执行

        void Start()
        {
            // 初始化，可留空
        }

        void Update()
        {
            if (target == null)
                return; // 如果没有目标对象，退出

            // 当前没有动画且队列有动作时，取出下一个动作执行
            if (current == null && sequence.Count > 0)
            {
                current = sequence.Dequeue();
                start_pos = target.transform.position;
                current_pos = target.transform.position;
                timer = 0f;
            }

            // 执行动画
            if (current != null)
            {
                if (timer < current.duration) // 动画进行中
                {
                    timer += Time.deltaTime;

                    if (current.type == AnimActionType.Move)
                    {
                        // 计算移动速度
                        float dist = (current.target_pos - start_pos).magnitude;
                        float speed = dist / Mathf.Max(current.duration, 0.01f);
                        // 更新位置
                        current_pos = Vector3.MoveTowards(current_pos, current.target_pos, speed * Time.deltaTime);
                        transform.position = current_pos;
                    }

                    if (current.type == AnimActionType.Size)
                    {
                        // 计算缩放速度
                        float dist = Mathf.Abs(transform.localScale.y - current.value);
                        float speed = dist / Mathf.Max(current.duration, 0.01f);
                        // 更新缩放
                        transform.localScale = Vector3.MoveTowards(transform.localScale, current.value * Vector3.one, speed * Time.deltaTime);
                    }
                }
                else // 动画完成
                {
                    current.callback?.Invoke(); // 执行回调（如果有）
                    current = null;
                }
            }
        }

        /// <summary>
        /// 添加移动动画
        /// </summary>
        /// <param name="pos">目标位置</param>
        /// <param name="duration">持续时间（秒）</param>
        public void MoveTo(Vector3 pos, float duration)
        {
            AnimAction action = new AnimAction();
            action.type = AnimActionType.Move;
            action.duration = duration;
            action.target_pos = pos;
            sequence.Enqueue(action);
        }

        /// <summary>
        /// 添加缩放动画
        /// </summary>
        /// <param name="value">目标缩放值（1 = 原始大小）</param>
        /// <param name="duration">持续时间（秒）</param>
        public void ScaleTo(float value, float duration)
        {
            AnimAction action = new AnimAction();
            action.type = AnimActionType.Size;
            action.duration = duration;
            action.value = value;
            sequence.Enqueue(action);
        }

        /// <summary>
        /// 添加延迟回调动作
        /// </summary>
        /// <param name="duration">等待时间</param>
        /// <param name="callback">回调函数</param>
        public void Callback(float duration, UnityAction callback)
        {
            AnimAction action = new AnimAction();
            action.type = AnimActionType.None;
            action.duration = duration;
            action.callback = callback;
            sequence.Enqueue(action);
        }

        /// <summary>
        /// 清空动画队列
        /// </summary>
        public void Clear()
        {
            target = null;
            timer = 0f;
            sequence.Clear();
        }

        /// <summary>
        /// 静态方法创建 AnimFX，并绑定到目标对象
        /// </summary>
        public static AnimFX Create(GameObject target)
        {
            AnimFX anim = target.GetComponent<AnimFX>();
            if (anim == null)
                anim = target.AddComponent<AnimFX>();

            anim.Clear();
            anim.target = target;
            return anim;
        }
    }

    /// <summary>
    /// 动画类型
    /// </summary>
    public enum AnimActionType
    {
        None = 0,   // 无动作，仅延迟回调
        Move = 5,   // 移动
        Size = 10,  // 缩放
    }

    /// <summary>
    /// 动画动作数据
    /// </summary>
    public class AnimAction
    {
        public AnimActionType type;      // 动作类型
        public Vector3 target_pos;       // 目标位置（仅 Move 类型使用）
        public float value = 0f;         // 目标缩放值（仅 Size 类型使用）
        public float duration = 1f;      // 持续时间
        public UnityAction callback = null; // 动画完成后的回调
    }
}
