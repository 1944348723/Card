using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TcgEngine
{
    /// <summary>
    /// 场景导航脚本
    /// 管理不同场景之间的切换操作
    /// </summary>
    public class SceneNav
    {
        /// <summary>
        /// 重新加载当前场景（重启关卡）
        /// </summary>
        public static void RestartLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name); //获取当前场景名并重新加载
        }

        /// <summary>
        /// 切换到指定场景
        /// </summary>
        /// <param name="scene">场景名称</param>
        public static void GoTo(string scene)
        {
            SceneManager.LoadScene(scene); //加载指定场景
        }

        /// <summary>
        /// 获取当前场景的名称
        /// </summary>
        /// <returns>返回当前场景名称</returns>
        public static string GetCurrentScene()
        {
            return SceneManager.GetActiveScene().name; //返回当前场景名
        }

        /// <summary>
        /// 检查指定场景是否存在（是否可以被加载）
        /// </summary>
        /// <param name="scene">场景名称</param>
        /// <returns>存在返回true，否则返回false</returns>
        public static bool DoSceneExist(string scene)
        {
            return Application.CanStreamedLevelBeLoaded(scene); //判断场景是否可加载
        }
    }
}