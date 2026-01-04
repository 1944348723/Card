using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 卡牌类型枚举
    /// </summary>
    public enum CardType
    {
        None = 0,       // 无类型
        Hero = 5,       // 英雄
        Character = 10, // 角色
        Spell = 20,     // 法术
        Artifact = 30,  // 遗物/神器
        Secret = 40,    // 秘密
        Equipment = 50, // 装备
    }

    /// <summary>
    /// 定义游戏中所有卡牌数据
    /// </summary>

    [CreateAssetMenu(fileName = "card", menuName = "TcgEngine/CardData", order = 5)]
    public class CardData : ScriptableObject
    {
        public string id; // 卡牌唯一ID

        [Header("Display")]
        public string title;       // 卡牌名称
        public Sprite art_full;    // 卡牌完整图像
        public Sprite art_board;   // 卡牌在场上显示图像

        [Header("Stats")]
        public CardType type;      // 卡牌类型
        public TeamData team;      // 卡牌所属阵营
        public RarityData rarity;  // 稀有度
        public int mana;           // 法力值消耗
        public int attack;         // 攻击力
        public int hp;             // 生命值/耐久

        [Header("Traits")]
        public TraitData[] traits; // 特性列表
        public TraitStat[] stats;  // 属性统计列表（可叠加效果）

        [Header("Abilities")]
        public AbilityData[] abilities; // 能力列表

        [Header("Card Text")]
        [TextArea(3, 5)]
        public string text; // 卡牌简短文本说明

        [Header("Description")]
        [TextArea(5, 10)]
        public string desc; // 卡牌详细描述

        [Header("FX")]
        public GameObject spawn_fx;   // 卡牌生成特效
        public GameObject death_fx;   // 卡牌死亡特效
        public GameObject attack_fx;  // 攻击特效
        public GameObject damage_fx;  // 受伤特效
        public GameObject idle_fx;    // 待机特效
        public AudioClip spawn_audio; // 生成音效
        public AudioClip death_audio; // 死亡音效
        public AudioClip attack_audio;// 攻击音效
        public AudioClip damage_audio;// 受伤音效

        [Header("Availability")]
        public bool deckbuilding = false; // 是否可用于构筑牌组
        public int cost = 100;             // 卡牌获取成本
        public PackData[] packs;           // 卡牌所属卡包

        // 静态集合用于加速访问
        public static List<CardData> card_list = new List<CardData>();                           // 循环访问更快
        public static Dictionary<string, CardData> card_dict = new Dictionary<string, CardData>(); // 根据ID快速获取

        /// <summary>
        /// 从Resources加载所有CardData
        /// </summary>
        public static void Load(string folder = "")
        {
            if (card_list.Count == 0)
            {
                card_list.AddRange(Resources.LoadAll<CardData>(folder));
                foreach (CardData card in card_list)
                    card_dict.Add(card.id, card); // 建立字典索引
            }
        }

        // ------------------- 获取图像/文本 -------------------
        public Sprite GetBoardArt(VariantData variant) { return art_board; } // 获取场上显示图
        public Sprite GetFullArt(VariantData variant) { return art_full; }  // 获取完整卡面图
        public string GetTitle() { return title; }                           // 获取名称
        public string GetText() { return text; }                             // 获取卡牌简短文本
        public string GetDesc() { return desc; }                             // 获取详细描述

        /// <summary>
        /// 获取卡牌类型字符串
        /// </summary>
        public string GetTypeId()
        {
            if (type == CardType.Hero) return "hero";
            if (type == CardType.Character) return "character";
            if (type == CardType.Artifact) return "artifact";
            if (type == CardType.Spell) return "spell";
            if (type == CardType.Secret) return "secret";
            if (type == CardType.Equipment) return "equipment";
            return "";
        }

        /// <summary>
        /// 获取所有能力描述
        /// </summary>
        public string GetAbilitiesDesc()
        {
            string txt = "";
            foreach (AbilityData ability in abilities)
            {
                if (!string.IsNullOrWhiteSpace(ability.desc))
                    txt += "<b>" + ability.GetTitle() + ":</b> " + ability.GetDesc(this) + "\n";
            }
            return txt;
        }

        // ------------------- 类型判断 -------------------
        public bool IsCharacter() { return type == CardType.Character; }   // 是否角色
        public bool IsSecret() { return type == CardType.Secret; }         // 是否秘密
        public bool IsBoardCard() { return type == CardType.Character || type == CardType.Artifact; } // 是否可上场
        public bool IsRequireTarget() { return type == CardType.Equipment || IsRequireTargetSpell(); } // 是否需要选择目标
        public bool IsRequireTargetSpell() { return type == CardType.Spell && HasAbility(AbilityTrigger.OnPlay, AbilityTarget.PlayTarget); } // 法术是否需要目标
        public bool IsEquipment() { return type == CardType.Equipment; }  // 是否装备
        public bool IsDynamicManaCost() { return mana > 99; }              // 是否动态法力消耗

        // ------------------- 特性与统计 -------------------
        public bool HasTrait(string trait)
        {
            foreach (TraitData t in traits) { if (t.id == trait) return true; }
            return false;
        }
        public bool HasTrait(TraitData trait) { return trait != null && HasTrait(trait.id); }

        public bool HasStat(string trait)
        {
            if (stats == null) return false;
            foreach (TraitStat stat in stats) { if (stat.trait.id == trait) return true; }
            return false;
        }
        public bool HasStat(TraitData trait) { return trait != null && HasStat(trait.id); }

        public int GetStat(string trait_id)
        {
            if (stats == null) return 0;
            foreach (TraitStat stat in stats) { if (stat.trait.id == trait_id) return stat.value; }
            return 0;
        }
        public int GetStat(TraitData trait) { return trait != null ? GetStat(trait.id) : 0; }

        // ------------------- 能力相关 -------------------
        public bool HasAbility(AbilityData tability)
        {
            foreach (AbilityData ability in abilities) { if (ability && ability.id == tability.id) return true; }
            return false;
        }
        public bool HasAbility(AbilityTrigger trigger)
        {
            foreach (AbilityData ability in abilities) { if (ability && ability.trigger == trigger) return true; }
            return false;
        }
        public bool HasAbility(AbilityTrigger trigger, AbilityTarget target)
        {
            foreach (AbilityData ability in abilities) { if (ability && ability.trigger == trigger && ability.target == target) return true; }
            return false;
        }
        public AbilityData GetAbility(AbilityTrigger trigger)
        {
            foreach (AbilityData ability in abilities) { if (ability && ability.trigger == trigger) return ability; }
            return null;
        }

        // ------------------- 卡包相关 -------------------
        public bool HasPack(PackData pack)
        {
            foreach (PackData apack in packs) { if (apack == pack) return true; }
            return false;
        }

        // ------------------- 静态查询方法 -------------------
        public static CardData Get(string id)
        {
            if (id == null) return null;
            bool success = card_dict.TryGetValue(id, out CardData card);
            if (success) return card;
            return null;
        }

        public static List<CardData> GetAllDeckbuilding()
        {
            List<CardData> multi_list = new List<CardData>();
            foreach (CardData acard in GetAll()) { if (acard.deckbuilding) multi_list.Add(acard); }
            return multi_list;
        }

        public static List<CardData> GetAll(PackData pack)
        {
            List<CardData> multi_list = new List<CardData>();
            foreach (CardData acard in GetAll()) { if (acard.HasPack(pack)) multi_list.Add(acard); }
            return multi_list;
        }

        public static List<CardData> GetAll() { return card_list; }
    }
}
