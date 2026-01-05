using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine.Client
{
    // Board 引用类型
    // 用来标识棋盘（或界面板）上的某一类位置
    public enum BoardRefType
    {
        None = 0,      // 无类型
        PackCard = 4,  // 抽卡包（开卡包界面中卡牌的位置）
    }

    /// <summary>
    /// Board（或界面板）上的一个位置引用
    /// 主要用于 PackOpen（开卡包）界面中
    /// 用来标识某个固定位置，例如第几张卡、属于谁的区域等
    /// </summary>
    public class BoardRef : MonoBehaviour
    {
        public BoardRefType type; // 该位置的类型
        public int index;         // 索引（第几个位置，比如第1张、第2张卡）
        public bool opponent;     // 是否为对手的区域（true 表示对手）

        // 静态列表，保存场景中所有 BoardRef 实例
        private static List<BoardRef> ref_list = new List<BoardRef>();

        void Awake()
        {
            // 对象创建时加入全局列表
            ref_list.Add(this);
        }

        void OnDestroy()
        {
            // 对象销毁时从全局列表移除
            ref_list.Remove(this);
        }

        // 根据类型 + 是否属于对手 来查找 BoardRef
        public static BoardRef Get(BoardRefType type, bool opponent)
        {
            foreach (BoardRef bref in ref_list)
            {
                if (bref.type == type && bref.opponent == opponent)
                    return bref;   // 找到则返回
            }
            return null; // 没找到则返回 null
        }

        // 根据类型 + 索引 来查找 BoardRef
        public static BoardRef Get(BoardRefType type, int index)
        {
            foreach (BoardRef bref in ref_list)
            {
                if (bref.type == type && bref.index == index)
                    return bref;   // 找到则返回
            }
            return null; // 没找到则返回 null
        }
    }
}