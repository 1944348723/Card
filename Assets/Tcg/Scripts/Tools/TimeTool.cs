using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 时间工具类
    /// 简化等待X秒或等待条件成立的代码
    /// 也允许在非MonoBehaviour类中调用协程
    /// </summary>
    public class TimeTool
    {
        //--- 协程部分代码 ----

        /// <summary>
        /// 等待指定时间后执行回调
        /// </summary>
        /// <param name="time">等待秒数</param>
        /// <param name="callback">等待结束后调用的回调函数</param>
        public static void WaitFor(float time, Action callback)
        {
            StartCoroutine(WaitForRun(time, callback)); //启动协程等待指定时间
        }

        /// <summary>
        /// 等待条件成立后执行回调
        /// </summary>
        /// <param name="condition">条件函数，返回true表示条件成立</param>
        /// <param name="callback">条件成立后调用的回调函数</param>
        public static void WaitUntil(Func<bool> condition, Action callback)
        {
            StartCoroutine(WaitUntilRun(condition, callback)); //启动协程等待条件成立
        }

        /// <summary>
        /// 协程实际执行等待指定秒数
        /// </summary>
        private static IEnumerator WaitForRun(float time, Action callback) 
        { 
            yield return new WaitForSeconds(time); //等待指定时间
            callback?.Invoke(); //调用回调
        }

        /// <summary>
        /// 协程实际执行等待条件成立
        /// </summary>
        private static IEnumerator WaitUntilRun(Func<bool> condition, Action callback) 
        { 
            yield return new WaitUntil(condition); //等待条件成立
            callback?.Invoke(); //调用回调
        }

        /// <summary>
        /// 启动协程
        /// </summary>
        /// <param name="routine">IEnumerator协程对象</param>
        /// <returns>返回Coroutine对象</returns>
        public static Coroutine StartCoroutine(IEnumerator routine)
        {
            return TimeToolMono.Inst.StartCoroutine(routine); //通过TimeToolMono实例启动协程
        }

        /// <summary>
        /// 停止协程
        /// </summary>
        /// <param name="routine">要停止的Coroutine对象</param>
        public static void StopCoroutine(Coroutine routine)
        {
            TimeToolMono.Inst.StopCoroutine(routine); //通过TimeToolMono实例停止协程
        }

        //--- Task异步部分代码 ----

        /// <summary>
        /// 延迟指定毫秒数再继续执行
        /// 注意：不要使用Task.Delay在WebGL上，因为它可能失效，使用此方法替代
        /// </summary>
        /// <param name="miliseconds">延迟毫秒数</param>
        public static async Task Delay(int miliseconds)
        {
#if UNITY_WEBGL
            // WebGL平台 Task.Delay 不可靠
            float seconds = miliseconds / 1000f;
            float start_time = Time.time;
            while (Time.time < start_time + seconds)
                await Task.Yield(); //每帧等待
#else
            // Desktop/移动平台性能更好
            await Task.Delay(miliseconds); 
#endif
        }

    }
}
