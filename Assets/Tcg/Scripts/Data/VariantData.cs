using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 定义卡牌的变体（Variant）数据
    /// 卡牌变体可以用于不同的视觉效果、框架或额外属性
    /// </summary>
    [CreateAssetMenu(fileName = "VariantData", menuName = "TcgEngine/VariantData", order = 5)]
    public class VariantData : ScriptableObject
    {
        public string id;             //变体唯一ID
        public string title;          //变体名称
        public Sprite frame;          //完整卡牌界面框架
        public Sprite frame_board;    //游戏棋盘上显示的框架
        public Color color = Color.white;  //变体颜色
        public int cost_factor = 1;        //影响卡牌费用的系数
        public bool is_default;            //是否为默认变体

        public static List<VariantData> variant_list = new List<VariantData>(); //存放所有变体数据列表

        /// <summary>
        /// 获取变体的后缀（用于命名或资源路径）
        /// </summary>
        public string GetSuffix()
        {
            return "_" + id;
        }

        /// <summary>
        /// 从Resources加载所有变体数据
        /// folder: 可指定相对Resources的路径
        /// </summary>
        public static void Load(string folder = "")
        {
            if (variant_list.Count == 0)
                variant_list.AddRange(Resources.LoadAll<VariantData>(folder));
        }

        /// <summary>
        /// 获取默认变体
        /// </summary>
        public static VariantData GetDefault()
        {
            foreach (VariantData variant in GetAll())
            {
                if (variant.is_default)
                    return variant;
            }
            return null;
        }

        /// <summary>
        /// 获取非默认（特殊）变体
        /// </summary>
        public static VariantData GetSpecial()
        {
            foreach (VariantData variant in GetAll())
            {
                if (!variant.is_default)
                    return variant;
            }
            return null;
        }

        /// <summary>
        /// 根据ID获取变体，如果未找到则返回默认变体
        /// </summary>
        public static VariantData Get(string id)
        {
            foreach (VariantData variant in GetAll())
            {
                if (variant.id == id)
                    return variant;
            }
            return GetDefault();
        }

        /// <summary>
        /// 获取所有变体数据
        /// </summary>
        public static List<VariantData> GetAll()
        {
            return variant_list;
        }
    }
}
