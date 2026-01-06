using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 主音频管理脚本，可按频道播放音效和音乐
    /// 支持音量控制、淡入淡出、优先级播放等功能
    /// </summary>
    public class AudioTool : MonoBehaviour
    {
        private static AudioTool instance;

        //音效频道字典，key为频道名称，value为AudioSource
        private Dictionary<string, AudioSource> channels_sfx = new Dictionary<string, AudioSource>();
        //音乐频道字典，key为频道名称，value为AudioSource
        private Dictionary<string, AudioSource> channels_music = new Dictionary<string, AudioSource>();
        //当前频道音量
        private Dictionary<string, float> channels_volume = new Dictionary<string, float>();
        //目标音量，用于淡入淡出
        private Dictionary<string, float> tchannels_volume = new Dictionary<string, float>();

        [HideInInspector] public float master_vol = 1f; //总音量
        [HideInInspector] public float sfx_vol = 1f;    //音效音量
        [HideInInspector] public float music_vol = 1f;  //音乐音量

        private void Awake()
        {
            //加载存档音量设置
            LoadPrefs();
            //刷新音量应用到所有频道
            RefreshVolume();
        }

        private void Update()
        {
            //处理音乐频道淡入淡出
            foreach (KeyValuePair<string, AudioSource> pair in channels_music)
            {
                if (pair.Value.isPlaying)
                {
                    float tvol = tchannels_volume[pair.Key];
                    float vol = channels_volume[pair.Key];
                    //音量平滑过渡
                    vol = Mathf.MoveTowards(vol, tvol, 0.5f * Time.deltaTime);
                    channels_volume[pair.Key] = vol;
                    pair.Value.volume = vol * music_vol;

                    //音量过低时停止播放
                    if (vol < 0.01f && tvol < 0.01f)
                        StopMusic(pair.Key);
                }
            }

            //处理音效频道淡入淡出
            foreach (KeyValuePair<string, AudioSource> pair in channels_sfx)
            {
                if (pair.Value.isPlaying)
                {
                    float tvol = tchannels_volume[pair.Key];
                    float vol = channels_volume[pair.Key];
                    vol = Mathf.MoveTowards(vol, tvol, 0.5f * Time.deltaTime);
                    channels_volume[pair.Key] = vol;
                    pair.Value.volume = vol * sfx_vol;

                    if (vol < 0.01f && tvol < 0.01f)
                        StopSFX(pair.Key);
                }
            }
        }

        /// <summary>
        /// 播放音效
        /// channel: 频道名称，相同频道的音效不会同时播放
        /// priority: true表示打断当前音效，false表示不打断
        /// loop: 是否循环播放
        /// </summary>
        public void PlaySFX(string channel, AudioClip sound, float vol = 0.6f, bool priority = true, bool loop = false)
        {
            if (string.IsNullOrEmpty(channel) || sound == null)
                return;

            AudioSource source = GetChannel(channel);
            channels_volume[channel] = vol;
            tchannels_volume[channel] = vol;

            if (source == null)
            {
                source = CreateChannel(channel);
                channels_sfx[channel] = source;
            }

            if (source != null)
            {
                if (priority || !source.isPlaying)
                {
                    source.clip = sound;
                    source.volume = vol * sfx_vol;
                    source.loop = loop;
                    source.Play();
                }
            }
        }

        /// <summary>
        /// 播放音乐
        /// channel: 频道名称
        /// 相同频道的音乐如果不同会替换播放，否则不会重新播放
        /// </summary>
        public void PlayMusic(string channel, AudioClip music, float vol = 0.3f, bool loop = true)
        {
            if (string.IsNullOrEmpty(channel) || music == null)
                return;

            AudioSource source = GetMusicChannel(channel);
            channels_volume[channel] = vol;
            tchannels_volume[channel] = vol;

            if (source == null)
            {
                source = CreateChannel(channel);
                channels_music[channel] = source;
            }

            if (source != null)
            {
                if (!source.isPlaying || source.clip != music)
                {
                    source.clip = music;
                    source.volume = vol * music_vol;
                    source.loop = loop;
                    source.Play();
                }
            }
        }

        /// <summary>
        /// 播放随机音效（从数组中随机选取一个）
        /// </summary>
        public void PlaySFX(string channel, AudioClip[] sounds, float vol = 0.6f, bool priority = true, bool loop = false)
        {
            if (sounds != null && sounds.Length > 0)
            {
                AudioClip sound = sounds[Random.Range(0, sounds.Length)];
                PlaySFX(channel, sound, vol, priority, loop);
            }
        }

        /// <summary>
        /// 播放随机音乐（从数组中随机选取一个）
        /// </summary>
        public void PlayMusic(string channel, AudioClip[] musics, float vol = 0.6f, bool loop = false)
        {
            if (musics != null && musics.Length > 0)
            {
                AudioClip music = musics[Random.Range(0, musics.Length)];
                PlayMusic(channel, music, vol, loop);
            }
        }

        /// <summary>
        /// 停止指定频道音效
        /// </summary>
        public void StopSFX(string channel)
        {
            if (string.IsNullOrEmpty(channel))
                return;

            AudioSource source = GetChannel(channel);
            if (source)
            {
                source.Stop();
            }
        }

        /// <summary>
        /// 停止指定频道音乐
        /// </summary>
        public void StopMusic(string channel)
        {
            if (string.IsNullOrEmpty(channel))
                return;

            AudioSource source = GetMusicChannel(channel);
            if (source)
            {
                source.Stop();
            }
        }

        /// <summary>
        /// 淡出指定频道音乐
        /// </summary>
        public void FadeOutMusic(string channel)
        {
            if (tchannels_volume.ContainsKey(channel))
                tchannels_volume[channel] = 0f;
        }

        /// <summary>
        /// 淡出指定频道音效
        /// </summary>
        public void FadeOutSFX(string channel)
        {
            if (tchannels_volume.ContainsKey(channel))
                tchannels_volume[channel] = 0f;
        }

        //设置主音量并保存
        public void SetMasterVolume(float value)
        {
            master_vol = value;
            RefreshVolume();
            SavePrefs();
        }

        //设置音乐音量并保存
        public void SetMusicVolume(float value)
        {
            music_vol = value;
            RefreshVolume();
            SavePrefs();
        }

        //设置音效音量并保存
        public void SetSFXVolume(float value)
        {
            sfx_vol = value;
            RefreshVolume();
            SavePrefs();
        }

        //加载存档音量
        public void LoadPrefs()
        {
            master_vol = PlayerPrefs.GetFloat("audio_master_volume", 1f);
            music_vol = PlayerPrefs.GetFloat("audio_music_volume", 1f);
            sfx_vol = PlayerPrefs.GetFloat("audio_sfx_volume", 1f);
        }

        //保存音量设置
        public void SavePrefs()
        {
            PlayerPrefs.SetFloat("audio_master_volume", master_vol);
            PlayerPrefs.SetFloat("audio_music_volume", music_vol);
            PlayerPrefs.SetFloat("audio_sfx_volume", sfx_vol);
        }

        /// <summary>
        /// 刷新所有频道音量
        /// </summary>
        public void RefreshVolume()
        {
            AudioListener.volume = master_vol;

            foreach (KeyValuePair<string, AudioSource> pair in channels_sfx)
            {
                if (pair.Value != null)
                {
                    float vol = channels_volume.ContainsKey(pair.Key) ? channels_volume[pair.Key] : 0.8f;
                    pair.Value.volume = vol * sfx_vol;
                }
            }

            foreach (KeyValuePair<string, AudioSource> pair in channels_music)
            {
                if (pair.Value != null)
                {
                    float vol = channels_volume.ContainsKey(pair.Key) ? channels_volume[pair.Key] : 0.4f;
                    pair.Value.volume = vol * music_vol;
                }
            }
        }

        /// <summary>
        /// 判断指定音乐频道是否在播放
        /// </summary>
        public bool IsMusicPlaying(string channel)
        {
            AudioSource source = GetMusicChannel(channel);
            if (source != null)
                return source.isPlaying;
            return false;
        }

        /// <summary>
        /// 创建一个新的音频频道
        /// </summary>
        public AudioSource CreateChannel(string channel, int priority = 128)
        {
            if (string.IsNullOrEmpty(channel))
                return null;

            GameObject cobj = new GameObject("AudioChannel-" + channel);
            cobj.transform.SetParent(transform);
            AudioSource caudio = cobj.AddComponent<AudioSource>();
            caudio.playOnAwake = false;
            caudio.loop = false;
            caudio.priority = priority;
            return caudio;
        }

        /// <summary>
        /// 获取音效频道
        /// </summary>
        public AudioSource GetChannel(string channel)
        {
            if (channels_sfx.ContainsKey(channel))
                return channels_sfx[channel];
            return null;
        }

        /// <summary>
        /// 获取音乐频道
        /// </summary>
        public AudioSource GetMusicChannel(string channel)
        {
            if (channels_music.ContainsKey(channel))
                return channels_music[channel];
            return null;
        }

        public bool DoesChannelExist(string channel)
        {
            return channels_sfx.ContainsKey(channel);
        }

        public bool DoesMusicChannelExist(string channel)
        {
            return channels_music.ContainsKey(channel);
        }

        public float GetMasterVolume()
        {
            return master_vol;
        }

        public float GetSFXVolume()
        {
            return sfx_vol;
        }

        public float GetMusicVolume()
        {
            return music_vol;
        }

        /// <summary>
        /// 获取单例
        /// </summary>
        public static AudioTool Get()
        {
            if (instance == null)
            {
                GameObject audio_system = new GameObject("AudioSystem");
                instance = audio_system.AddComponent<AudioTool>();
                DontDestroyOnLoad(audio_system);
            }
            return instance;
        }
    }
}
