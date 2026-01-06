using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine.UI
{
    /// <summary>
    /// 设备可见性脚本
    /// 将该脚本添加到任何 UI 元素上，可根据当前设备类型决定该元素是否显示
    /// 例如：在移动端隐藏某些控件，或在桌面端隐藏某些控件
    /// </summary>
    public class DeviceVisibility : MonoBehaviour
    {
        public bool desktop = true; // 在桌面端是否可见
        public bool mobile = true;  // 在移动端是否可见

        void Start()
        {
            bool ismobile = GameTool.IsMobile(); // 判断当前设备是否为移动端
            if (ismobile && !mobile)
                gameObject.SetActive(false); // 移动端且设置不可见，则隐藏
            else if (!ismobile && !desktop)
                gameObject.SetActive(false); // 桌面端且设置不可见，则隐藏
        }
    }
}