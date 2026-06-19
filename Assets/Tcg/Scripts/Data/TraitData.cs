using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 特性数值结构体
    /// 用于卡牌的具体数值，如攻击加成、生命加成等
    /// </summary>
    [System.Serializable]
    public struct TraitStat
    {
        public TraitData trait; //特性对象
        public int value;       //对应特性的数值
    }

    /// <summary>
    /// 定义所有特性（Trait）和数值属性（Stat）数据
    /// 特性用于卡牌的分类、标记或能力触发条件
    /// </summary>
    [CreateAssetMenu(fileName = "TraitData", menuName = "TcgEngine/TraitData", order = 1)]
    public class TraitData : ScriptableObject
    {
        public string id;       //特性唯一ID
        public string title;    //特性名称
        public Sprite icon;     //特性图标

        private static List<TraitData> trait_list = new(); //存放所有特性数据列表

        /// <summary>
        /// 获取特性名称
        /// </summary>
        public string GetTitle()
        {
            return title;
        }

        /// <summary>
        /// 从Resources加载所有特性数据
        /// folder: 可指定相对Resources的路径
        /// </summary>
        public static void Load(string folder = "")
        {
            if (trait_list.Count == 0)
                trait_list.AddRange(Resources.LoadAll<TraitData>(folder));
        }

        /// <summary>
        /// 根据ID获取特性数据
        /// </summary>
        public static TraitData Get(string id)
        {
            foreach (TraitData trait in trait_list)
            {
                if (trait.id == id)
                    return trait;
            }
            return null;
        }
    }
}