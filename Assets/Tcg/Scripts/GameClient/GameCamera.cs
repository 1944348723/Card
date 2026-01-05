using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine.Client
{
    /// <summary>
    /// 游戏摄像机
    /// 具有一些实用功能，例如：
    /// 1. 震动效果
    /// 2. 将鼠标位置转换为 Ray / 世界坐标 / 屏幕百分比
    /// </summary>
    public class GameCamera : MonoBehaviour
    {
        private float shake_timer = 0f;      // 摄像机震动计时器
        private float shake_intensity = 1f;  // 震动强度

        private Camera cam;                  // Unity 摄像机组件
        private Vector3 shake_vector = Vector3.zero;  // 震动偏移向量
        private Vector3 start_pos;           // 摄像机初始位置

        private static GameCamera instance;  // 单例

        void Awake()
        {
            instance = this;
            start_pos = transform.position;  // 记录初始位置
            cam = GetComponent<Camera>();    // 获取 Camera 组件
        }

        void Update()
        {
            // 摄像机震动效果
            if (shake_timer > 0f)
            {
                shake_timer -= Time.deltaTime;

                // 使用正弦和余弦制造抖动轨迹
                shake_vector = new Vector3(
                    Mathf.Cos(shake_timer * Mathf.PI * 16f) * 0.02f,
                    Mathf.Sin(shake_timer * Mathf.PI * 12f) * 0.01f,
                    0f
                );

                // 在初始位置基础上叠加抖动偏移
                transform.position = start_pos + shake_vector * shake_intensity;
            }
            else
            {
                // 没有震动时恢复原位
                transform.position = start_pos;
            }
        }

        /// <summary>
        /// 触发摄像机震动
        /// intensity：震动强度
        /// duration：持续时间
        /// </summary>
        public void Shake(float intensity = 1f, float duration = 1f)
        {
            shake_intensity = intensity;
            shake_timer = duration;
        }

        /// <summary>
        /// 将鼠标屏幕坐标转为 0~1 的百分比坐标
        /// </summary>
        public Vector2 MouseToPercent(Vector3 mouse_pos)
        {
            float x = mouse_pos.x / Screen.width;
            float y = mouse_pos.y / Screen.height;
            return new Vector2(x, y);
        }

        /// <summary>
        /// 将鼠标位置转换为一条 Ray（用于物理射线检测）
        /// </summary>
        public Ray MouseToRay(Vector3 mouse_pos)
        {
            return cam.ScreenPointToRay(mouse_pos);
        }

        /// <summary>
        /// 将鼠标屏幕坐标转换为世界坐标
        /// distance：Z 方向深度（距离摄像机的距离）
        /// </summary>
        public Vector3 MouseToWorld(Vector2 mouse_pos, float distance = 10f)
        {
            Vector3 wpos = cam.ScreenToWorldPoint(
                new Vector3(mouse_pos.x, mouse_pos.y, distance)
            );
            return wpos;
        }

        /// <summary>
        /// 获取当前游戏摄像机组件
        /// </summary>
        public static Camera GetCamera()
        {
            if (instance != null)
                return instance.cam;
            return null;
        }

        /// <summary>
        /// 获取 GameCamera 单例
        /// </summary>
        public static GameCamera Get()
        {
            return instance;
        }
    }
}
