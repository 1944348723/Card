using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 列表交换工具（优化用）
    /// 可以重复使用两个 List，避免频繁实例化新的 List，从而减少 GC 压力
    /// </summary>
    /// <typeparam name="T">列表元素类型</typeparam>
    public class ListSwap<T>
    {
        /// <summary>
        /// 第一个列表
        /// </summary>
        public List<T> swap1 = new List<T>();

        /// <summary>
        /// 第二个列表
        /// </summary>
        public List<T> swap2 = new List<T>();

        /// <summary>
        /// 获取任意一个可用列表（默认返回 swap1）
        /// 使用前会清空列表
        /// </summary>
        public List<T> Get()
        {
            swap1.Clear(); // 使用前清空
            return swap1;
        }

        /// <summary>
        /// 获取另一个列表（用于当前列表正在使用的情况）
        /// 使用前会清空列表
        /// </summary>
        /// <param name="skip">正在使用的列表，需要跳过</param>
        public List<T> GetOther(List<T> skip)
        {
            if (skip == swap1)
            {
                swap2.Clear(); // 使用前清空
                return swap2;
            }
            swap1.Clear(); // 使用前清空
            return swap1;
        }

        /// <summary>
        /// 清空两个列表
        /// </summary>
        public void Clear()
        {
            swap1.Clear();
            swap2.Clear();
        }
    }
}