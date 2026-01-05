using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine.Client
{
    /// <summary>
    /// 场景设置组件
    /// 用于在场景中添加一些通用的音效和背景音乐
    /// </summary>
    public class SceneSettings : MonoBehaviour
    {
        // 游戏开始时播放的音效
        public AudioClip start_audio;

        // 游戏背景音乐列表
        public AudioClip[] game_music;

        // 游戏环境音效列表
        public AudioClip[] game_ambience;

        // 静态实例，方便全局访问
        private static SceneSettings instance;

        private void Awake()
        {
            // 保存静态实例
            instance = this;
        }

        void Start()
        {
            // 播放开始音效
            AudioTool.Get().PlaySFX("game_sfx", start_audio);

            // 随机播放背景音乐
            if (game_music.Length > 0)
                AudioTool.Get().PlayMusic("music", game_music[Random.Range(0, game_music.Length)]);

            // 随机播放环境音效，音量为0.5并循环播放
            if (game_ambience.Length > 0)
                AudioTool.Get().PlaySFX("ambience", game_ambience[Random.Range(0, game_ambience.Length)], 0.5f, true);
        }

        void Update()
        {
            // 暂时没有逻辑，可留作后续扩展
        }

        /// <summary>
        /// 获取静态实例
        /// </summary>
        /// <returns>当前 SceneSettings 实例</returns>
        public static SceneSettings Get()
        {
            return instance;
        }
    }
}