using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// 在卡牌收集界面中显示单张卡牌的视觉组件
    /// 可用于卡牌库或卡组编辑器中
    /// </summary>
    public class CollectionCard : MonoBehaviour
    {
        public CardUI card_ui; // 卡牌 UI 组件，用于显示卡牌图像和信息
        public Image quantity_bar; // 卡牌数量条，用于显示玩家持有该卡的数量
        public Text quantity; // 卡牌数量文本显示

        [Header("Mat")]
        public Material color_mat; // 彩色材质，正常显示卡牌
        public Material grayscale_mat; // 灰度材质，用于显示未拥有或禁用状态的卡牌

        public UnityAction<CardUI> onClick; // 左键点击事件回调
        public UnityAction<CardUI> onClickRight; // 右键点击事件回调

        private void Start()
        {
            // 将 CardUI 的点击事件绑定到本组件的回调
            card_ui.onClick += onClick;
            card_ui.onClickRight += onClickRight;
        }

        /// <summary>
        /// 设置卡牌数据和数量条显示
        /// </summary>
        /// <param name="card">卡牌数据</param>
        /// <param name="variant">卡牌变体数据</param>
        /// <param name="quantity">拥有数量</param>
        public void SetCard(CardData card, VariantData variant, int quantity)
        {
            card_ui.SetCard(card, variant); // 设置卡牌 UI 显示
            SetQuantity(quantity); // 更新数量条显示
        }

        /// <summary>
        /// 更新数量条和数量文本
        /// 数量为 0 时隐藏数量条和文本
        /// </summary>
        /// <param name="quantity">卡牌数量</param>
        public void SetQuantity(int quantity)
        {
            if (this.quantity_bar != null)
                this.quantity_bar.enabled = quantity > 0; // 数量大于 0 才显示数量条
            if (this.quantity != null)
                this.quantity.text = quantity.ToString(); // 更新数量文本
            if (this.quantity != null)
                this.quantity.enabled = quantity > 0; // 数量为 0 隐藏文本
        }

        /// <summary>
        /// 设置卡牌材质为灰度或彩色
        /// 用于显示可用性或禁用状态
        /// </summary>
        /// <param name="grayscale">是否灰度显示</param>
        public void SetGrayscale(bool grayscale)
        {
            if (grayscale)
            {
                quantity_bar.material = grayscale_mat; // 数量条灰度
                quantity_bar.material = grayscale_mat; // 数量条灰度（重复设置安全）
                card_ui.SetMaterial(grayscale_mat); // 卡牌本体灰度
            }
            else
            {
                quantity_bar.material = color_mat; // 数量条彩色
                quantity_bar.material = color_mat; // 数量条彩色（重复设置安全）
                card_ui.SetMaterial(color_mat); // 卡牌本体彩色
            }
        }

        /// <summary>
        /// 获取当前显示的卡牌数据
        /// </summary>
        /// <returns>卡牌数据对象</returns>
        public CardData GetCard()
        {
            return card_ui.GetCard();
        }

        /// <summary>
        /// 获取当前显示的卡牌变体
        /// </summary>
        /// <returns>卡牌变体对象</returns>
        public VariantData GetVariant()
        {
            return card_ui.GetVariant();
        }
    }
}
