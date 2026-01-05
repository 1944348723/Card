using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.UI;

namespace TcgEngine.FX
{
    /// <summary>
    /// 伤害数字特效(Damage FX)
    /// 当卡牌受到伤害时，会在卡牌上显示伤害数值
    /// </summary>
    public class DamageFX : MonoBehaviour
    {
        public Text text_value;  // 用于显示数字的UI文本组件
        
        /// <summary>
        /// 设置显示的整数伤害数值
        /// </summary>
        /// <param name="value">伤害数值</param>
        public void SetValue(int value)
        {
            if (text_value != null)
                text_value.text = value.ToString(); // 将整数转换成字符串显示
        }

        /// <summary>
        /// 设置显示的字符串值
        /// </summary>
        /// <param name="value">字符串数值</param>
        public void SetValue(string value)
        {
            if (text_value != null)
                text_value.text = value; // 直接显示字符串
        }
    }
}