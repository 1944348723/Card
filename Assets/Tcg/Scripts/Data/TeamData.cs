using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 定义所有阵营（势力/队伍）数据
    /// 阵营可以用于卡牌归属、颜色标记、UI显示等
    /// </summary>
    [CreateAssetMenu(fileName = "TeamData", menuName = "TcgEngine/TeamData", order = 1)]
    public class TeamData : ScriptableObject
    {
        public string id;       //阵营唯一ID
        public string title;    //阵营名称
        public Sprite icon;     //阵营图标
        public Color color;     //阵营颜色，用于UI显示或卡牌边框

        private static List<TeamData> team_list = new(); //存放所有加载的阵营数据列表

        /// <summary>
        /// 从Resources加载所有阵营数据
        /// folder: 可指定相对Resources的路径
        /// </summary>
        public static void Load(string folder = "")
        {
            if (team_list.Count == 0)
                team_list.AddRange(Resources.LoadAll<TeamData>(folder));
        }

        /// <summary>
        /// 获取所有阵营数据
        /// </summary>
        public static IReadOnlyList<TeamData> AllTeams()
        {
            return team_list;
        }
    }
}