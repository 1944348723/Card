using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    // 默认特效（FX）和音效（Audio），某些可以在单个卡牌上覆盖

    [CreateAssetMenu(fileName = "AssetData", menuName = "TcgEngine/AssetData", order = 0)]
    public class AssetData : ScriptableObject
    {
        [Header("FX（特效）")]
        public GameObject card_spawn_fx;           // 卡牌生成特效
        public GameObject card_destroy_fx;         // 卡牌销毁特效
        public GameObject card_attack_fx;          // 卡牌攻击特效
        public GameObject card_damage_fx;          // 卡牌受伤特效
        public GameObject card_exhausted_fx;       // 卡牌疲劳/已用特效
        public GameObject player_damage_fx;        // 玩家受到伤害特效
        public GameObject damage_fx;               // 通用伤害特效
        public GameObject play_card_fx;            // 出牌特效
        public GameObject play_card_other_fx;      // 对手出牌特效
        public GameObject play_secret_fx;          // 出秘密卡特效
        public GameObject play_secret_other_fx;    // 对手出秘密卡特效
        public GameObject dice_roll_fx;            // 骰子掷出特效
        public GameObject hover_text_box;          // 鼠标悬停显示文本框特效
        public GameObject new_turn_fx;             // 新回合开始特效
        public GameObject win_fx;                  // 获胜特效
        public GameObject lose_fx;                 // 失败特效
        public GameObject tied_fx;                 // 平局特效

        [Header("Audio（音效）")]
        public AudioClip card_spawn_audio;         // 卡牌生成音效
        public AudioClip card_destroy_audio;       // 卡牌销毁音效
        public AudioClip card_attack_audio;        // 卡牌攻击音效
        public AudioClip card_move_audio;          // 卡牌移动音效
        public AudioClip card_damage_audio;        // 卡牌受伤音效
        public AudioClip player_damage_audio;      // 玩家受伤音效
        public AudioClip hand_card_click_audio;    // 手牌点击音效
        public AudioClip new_turn_audio;           // 新回合开始音效
        public AudioClip win_audio;                // 获胜音效
        public AudioClip defeat_audio;             // 失败音效
        public AudioClip win_music;                // 胜利背景音乐
        public AudioClip defeat_music;             // 失败背景音乐

        // 获取全局资源数据（单例方式）
        public static AssetData Get()
        {
            return DataLoader.Instance.assets;        // 从 DataLoader 获取 AssetData 实例
        }
    } 
}
