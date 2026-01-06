using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// 显示卡牌的特性（Trait）或自定义属性
    /// 将该组件添加到CardUI的特性数组中即可显示
    /// </summary>
    public class TraitUI : MonoBehaviour
    {
        public TraitData trait;   // 对应的特性数据
        public Image bg;          // 背景图片，用于显示特性存在状态
        public Text text;         // 显示特性值的文字

        void Start()
        {
            // 初始无需操作
        }

        /// <summary>
        /// 根据Card实例设置特性显示
        /// </summary>
        /// <param name="card">卡牌实例</param>
        public void SetCard(Card card)
        {
            // 判断卡牌是否拥有该特性
            bool has_trait = card.HasTrait(trait);
            // 获取特性数值
            int val = card.GetTraitValue(trait);
            // 更新显示文字
            text.text = val.ToString();
            // 根据是否拥有特性显示或隐藏背景和文字
            bg.enabled = has_trait;
            text.enabled = has_trait;
        }

        /// <summary>
        /// 根据CardData设置特性显示
        /// </summary>
        /// <param name="card">卡牌数据</param>
        public void SetCard(CardData card)
        {
            // 判断卡牌数据是否拥有该特性
            bool has_trait = card.HasTrait(trait);
            // 获取特性数值
            int val = card.GetStat(trait.id);
            // 更新显示文字
            text.text = val.ToString();
            // 根据是否拥有特性显示或隐藏背景和文字
            bg.enabled = has_trait;
            text.enabled = has_trait;
        }
    }
}