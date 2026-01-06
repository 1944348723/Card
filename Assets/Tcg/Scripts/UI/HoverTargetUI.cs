using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace TcgEngine.UI
{
    /// <summary>
    /// 可悬停的UI目标
    /// 在UI中可以被鼠标悬停的元素，悬停时会显示文本信息
    /// </summary>
    public class HoverTargetUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [TextArea(5, 7)]
        public string text;          // 悬停显示的文本内容
        public float delay = 0.5f;   // 悬停延迟显示时间
        public int text_size = 22;   // 文本字号
        public int width = 350;      // 文本框宽度
        public int height = 140;     // 文本框高度

        private Canvas canvas;       // 所在UI画布
        private RectTransform rect;  // 画布的RectTransform
        //private LangTableText ltable; // 可用于本地化的文本表
        private float timer = 0f;    // 悬停计时器
        private bool hover = false;  // 是否处于悬停状态

        void Awake()
        {
            // 获取父级Canvas和RectTransform
            canvas = GetComponentInParent<Canvas>();
            //ltable = GetComponent<LangTableText>();
            rect = canvas?.GetComponent<RectTransform>();
        }

        void Start()
        {
            // 如果场景中没有悬停文本框，则实例化一个
            if (HoverTextBox.Get() == null)
            {
                Instantiate(AssetData.Get().hover_text_box, Vector3.zero, Quaternion.identity);
            }
        }

        void Update()
        {
            // 当鼠标悬停时计时，到达延迟后显示文本
            if (hover)
            {
                timer += Time.deltaTime;
                if (timer > delay)
                {
                    HoverTextBox.Get().Show(this);
                }
            }
        }

        /// <summary>
        /// 获取显示文本
        /// </summary>
        public string GetText()
        {
            // 如果使用本地化表，可替换为翻译文本
            //if (ltable != null)
            //    return ltable.GetTranslation(text);
            return text;
        }

        /// <summary>
        /// 鼠标指针进入UI元素时触发
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            timer = 0f;
            hover = true;
        }

        /// <summary>
        /// 鼠标指针离开UI元素时触发
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            timer = 0f;
            hover = false;
        }

        void OnDisable()
        {
            hover = false;
        }

        /// <summary>
        /// 获取所属Canvas
        /// </summary>
        public Canvas GetCanvas()
        {
            return canvas;
        }

        /// <summary>
        /// 获取Canvas的RectTransform
        /// </summary>
        public RectTransform GetRect()
        {
            return rect;
        }

        /// <summary>
        /// 当前是否处于悬停状态
        /// </summary>
        public bool IsHover()
        {
            return hover;
        }
    }
}
