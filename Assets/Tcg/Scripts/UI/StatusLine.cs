using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// 卡牌预览界面中的状态行
    /// 每个StatusLine显示一个状态或效果，包括标题和描述
    /// </summary>
    public class StatusLine : MonoBehaviour
    {
        public Text title; // 状态标题文本
        public Text desc;  // 状态描述文本

        private float timer = 0f; // 计时器，用于延迟隐藏

        private void Start()
        {
            // 初始化时隐藏状态行
            gameObject.SetActive(false);
        }

        void Update()
        {
            // 更新时间计数
            timer += Time.deltaTime;
        }

        /// <summary>
        /// 设置状态行内容（通过卡牌和技能）
        /// </summary>
        public void SetLine(CardData icard, AbilityData ability)
        {
            if (!string.IsNullOrWhiteSpace(ability.desc))
            {
                title.text = ability.GetTitle();          // 设置标题为技能标题
                desc.text = ability.GetDesc(icard);       // 设置描述为技能描述
                gameObject.SetActive(true);               // 显示状态行
                timer = 0f;                               // 重置计时器
            }
        }

        /// <summary>
        /// 设置状态行内容（通过状态类型和数值）
        /// </summary>
        public void SetLine(StatusType effect, int value)
        {
            StatusData sdata = StatusData.Get(effect);
            if (sdata != null)
                SetLine(sdata, value);
        }

        /// <summary>
        /// 设置状态行内容（通过状态数据和数值）
        /// </summary>
        public void SetLine(StatusData effect, int value)
        {
            if (!string.IsNullOrWhiteSpace(effect.desc))
            {
                title.text = effect.GetTitle();       // 设置标题为状态标题
                desc.text = effect.GetDesc(value);    // 设置描述为状态值描述
                gameObject.SetActive(true);           // 显示状态行
                timer = 0f;                            // 重置计时器
            }
        }

        /// <summary>
        /// 隐藏状态行（延迟0.05秒后才隐藏，防止快速切换闪烁）
        /// </summary>
        public void Hide()
        {
            if (timer > 0.05f)
                gameObject.SetActive(false);
        }
    }
}
