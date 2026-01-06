using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine;

namespace TcgEngine.UI
{
    /// <summary>
    /// 发送聊天消息时显示的 UI 气泡
    /// </summary>
    public class ChatBubble : MonoBehaviour
    {
        public Text msg_txt;        // 显示消息文本
        public Image bubble;        // 气泡背景图
        public CanvasGroup group;   // 控制透明度的 CanvasGroup

        private float timer = 0f;   // 消失计时器

        void Start()
        {

        }

        private void Update()
        {
            timer -= Time.deltaTime;      // 递减计时器
            group.alpha = timer;          // 根据计时器调整透明度

            if (timer < 0f)
                Hide();                   // 时间到隐藏气泡
        }

        // 设置显示的消息文本和持续时间
        public void SetLine(string msg, float duration)
        {
            msg_txt.text = msg;
            timer = duration;
            gameObject.SetActive(true);   // 显示气泡
        }

        // 隐藏气泡
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}