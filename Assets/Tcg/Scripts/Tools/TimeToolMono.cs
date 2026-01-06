using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// MonoBehaviour辅助类，用于TimeTool中启动和停止协程
    /// 提供单例实例，确保在非MonoBehaviour类中也可以使用协程
    /// </summary>
    public class TimeToolMono : MonoBehaviour
    {
        // 静态单例实例
        private static TimeToolMono _instance;

        /// <summary>
        /// 启动协程
        /// </summary>
        /// <param name="routine">IEnumerator协程对象</param>
        /// <returns>返回Coroutine对象</returns>
        public Coroutine StartRoutine(IEnumerator routine)
        {
            return StartCoroutine(routine); //调用MonoBehaviour的StartCoroutine
        }

        /// <summary>
        /// 停止协程
        /// </summary>
        /// <param name="routine">要停止的Coroutine对象</param>
        public void StopRoutine(Coroutine routine)
        {
            StopCoroutine(routine); //调用MonoBehaviour的StopCoroutine
        }

        /// <summary>
        /// 获取单例实例
        /// 如果实例不存在，会创建一个新的GameObject并添加TimeToolMono组件
        /// </summary>
        public static TimeToolMono Inst
        {
            get
            {
                if (_instance == null)
                {
                    GameObject ntool = new GameObject("TimeTool"); //创建一个新的空对象
                    _instance = ntool.AddComponent<TimeToolMono>(); //添加TimeToolMono组件
                }
                return _instance; //返回单例实例
            }
        }
    }
}