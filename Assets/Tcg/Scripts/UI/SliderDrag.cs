using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// 自定义滑条组件，支持拖动事件和数值变化回调
    /// 封装了Unity的Slider，并添加了开始拖动、结束拖动、值变化事件
    /// </summary>
    public class SliderDrag : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
    {
        public UnityAction onStartDrag;   // 开始拖动时触发的回调
        public UnityAction onEndDrag;     // 拖动结束时触发的回调
        public UnityAction onValueChanged; // 滑条数值变化时触发的回调

        private Slider slider; // 内部的Slider组件

        void Awake()
        {
            slider = GetComponent<Slider>();
            // 绑定Slider的值变化事件，触发onValueChanged回调
            slider.onValueChanged.AddListener((float v) => { onValueChanged?.Invoke(); });
        }

        /// <summary>
        /// 当按下滑条时触发
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            onStartDrag?.Invoke();
        }

        /// <summary>
        /// 当释放滑条时触发
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            onEndDrag?.Invoke();
        }

        /// <summary>
        /// 获取Slider组件，如果为空则重新获取
        /// </summary>
        public Slider Slider
        {
            get
            {
                if (slider == null)
                    slider = GetComponent<Slider>();
                return slider;
            }
        }

        /// <summary>
        /// 滑条最大值
        /// </summary>
        public float maxValue
        {
            get { return Slider.maxValue; }
            set { Slider.maxValue = value; }
        }

        /// <summary>
        /// 滑条最小值
        /// </summary>
        public float minValue
        {
            get { return Slider.minValue; }
            set { Slider.minValue = value; }
        }

        /// <summary>
        /// 滑条当前数值
        /// </summary>
        public float value
        {
            get { return Slider.value; }
            set { Slider.value = value; }
        }
    }
}
