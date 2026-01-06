using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 摄像机自适应缩放脚本，将摄像机画面调整为支持的长宽比
    /// 默认支持16:9和16:10
    /// 如果窗口比例与目标比例不符，会在两侧或上下显示黑边
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraResize : MonoBehaviour
    {
        private Camera cam;    // 摄像机组件
        private int sheight;   // 屏幕高度
        private int swidth;    // 屏幕宽度

        void Start()
        {
            cam = GetComponent<Camera>();
            sheight = Screen.height;
            swidth = Screen.width;
            UpdateSize(); // 初始化画面尺寸
        }

        private void Update()
        {
            // 检测屏幕尺寸变化
            if (sheight != Screen.height || swidth != Screen.width)
            {
                sheight = Screen.height;
                swidth = Screen.width;
                UpdateSize();
            }
        }

        /// <summary>
        /// 更新摄像机视口大小，自动添加黑边以保持长宽比
        /// </summary>
        public void UpdateSize()
        {
            float screenRatio = Screen.width / (float)Screen.height; // 当前屏幕比例
            float targetRatio = GetAspectRatio();                     // 目标长宽比

            if (Mathf.Approximately(screenRatio, targetRatio))
            {
                // 屏幕比例与目标比例相符：全屏显示
                cam.rect = new Rect(0, 0, 1, 1);
            }
            else if (screenRatio > targetRatio)
            {
                // 屏幕比目标宽：左右添加黑边（pillarbox）
                float normalizedWidth = targetRatio / screenRatio;
                float barThickness = (1f - normalizedWidth) / 2f;
                cam.rect = new Rect(barThickness, 0, normalizedWidth, 1);
            }
            else
            {
                // 屏幕比目标窄：上下添加黑边（letterbox）
                float normalizedHeight = screenRatio / targetRatio;
                float barThickness = (1f - normalizedHeight) / 2f;
                cam.rect = new Rect(0, barThickness, 1, normalizedHeight);
            }

            /* 移动端动态调整摄像机正交大小（暂时注释）
            if (TheGame.IsMobile())
            {
                float size_min = GetCamSizeMin();
                float size_max = GetCamSizeMax();
                float value = GetAspectValue();
                float cam_size = value * size_min + (1f - value) * size_max;
                cam.orthographicSize = cam_size;
            }
            */
        }

        /// <summary>
        /// 获取支持的最小长宽比（16:10）
        /// </summary>
        public static float GetAspectMin()
        {
            return 16f / 10f;
        }

        /// <summary>
        /// 获取支持的最大长宽比（16:9）
        /// </summary>
        public static float GetAspectMax()
        {
            //bool allow_wide = TheGame.IsMobile() && TheGame.Get() != null;
            //float max = allow_wide ? 16f / 8f : 16f / 9f;
            return 16f / 9f;
        }

        /// <summary>
        /// 摄像机正交尺寸最小值（移动端可动态调整）
        /// </summary>
        public static float GetCamSizeMin()
        {
            //bool allow_wide = TheGame.IsMobile() && TheGame.Get() != null;
            //float max = allow_wide ? 4.2f : 4.5f;
            return 4.5f;
        }

        /// <summary>
        /// 摄像机正交尺寸最大值
        /// </summary>
        public static float GetCamSizeMax()
        {
            return 5f;
        }

        /// <summary>
        /// 获取最终目标长宽比（限制在最小和最大范围内）
        /// </summary>
        public static float GetAspectRatio()
        {
            float max = GetAspectMax();
            float min = GetAspectMin();
            float screenRatio = Screen.width / (float)Screen.height;
            float targetRatio = Mathf.Clamp(screenRatio, min, max);
            return targetRatio;
        }

        /// <summary>
        /// 获取当前长宽比在最小和最大之间的比例值（0~1）
        /// 可用于移动端动态缩放摄像机大小
        /// </summary>
        public static float GetAspectValue()
        {
            float max = GetAspectMax();
            float min = GetAspectMin();
            float aspect = GetAspectRatio();
            float value = (aspect - min) / (max - min);
            return value;
        }
    }
}
