using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 定义所有状态类型
    /// 状态可以影响游戏流程，通常由技能产生，可持续若干回合
    /// </summary>
    public enum StatusType
    {
        None = 0,               //无状态

        AddAttack = 4,          //攻击力提升，可持续X回合
        AddHP = 5,              //生命值提升，可持续X回合
        AddManaCost = 6,        //法力消耗增减，可持续X回合

        Stealth = 10,           //潜行，未行动前无法被攻击
        Invincibility = 12,     //无敌，X回合内无法被攻击
        Shell = 13,             //护壳，首次受到伤害无效
        Protection = 14,        //保护，给予其他随从保护状态（嘲讽效果）
        Protected = 15,         //被保护的随从状态
        Armor = 16,             //护甲，受到伤害减少
        SpellImmunity = 18,     //法术免疫，无法被法术选中或伤害

        Deathtouch = 20,        //死触，攻击随从时直接杀死
        Fury = 22,              //狂怒，每回合可攻击两次
        Intimidate = 23,        //威慑，被攻击目标无法反击
        Flying = 24,            //飞行，可无视嘲讽
        Trample = 26,           //践踏，多余伤害传递给玩家
        LifeSteal = 28,         //吸血，战斗时治疗玩家

        Silenced = 30,          //沉默，取消所有技能效果
        Paralysed = 32,         //麻痹，X回合内无法行动
        Poisoned = 34,          //中毒，每回合开始时失去生命
        Sleep = 36,             //睡眠，回合开始时不重置行动
    }

    /// <summary>
    /// 定义所有状态效果数据
    /// 状态可由技能获得或失去，并影响游戏行为
    /// 状态可有持续时间
    /// </summary>
    [CreateAssetMenu(fileName = "status", menuName = "TcgEngine/StatusData", order = 7)]
    public class StatusData : ScriptableObject
    {
        public StatusType effect;   //状态类型

        [Header("Display")]
        public string title;        //状态名称
        public Sprite icon;         //状态图标

        [TextArea(3, 5)]
        public string desc;         //状态描述，可包含占位符 <value>

        [Header("FX")]
        public GameObject status_fx; //状态特效

        [Header("AI")]
        public int hvalue;           //AI对状态的权重，用于决策

        public static List<StatusData> status_list = new(); //所有状态数据列表

        /// <summary>
        /// 获取状态名称
        /// </summary>
        public string GetTitle()
        {
            return title;
        }

        /// <summary>
        /// 获取状态描述（默认数值为1）
        /// </summary>
        public string GetDesc()
        {
            return GetDesc(1);
        }

        /// <summary>
        /// 获取状态描述，可替换 <value> 为指定数值
        /// </summary>
        // TODO: 函数名不合适
        public string GetDesc(int value)
        {
            string des = desc.Replace("<value>", value.ToString());
            return des;
        }

        /// <summary>
        /// 加载Resources下所有StatusData资源
        /// </summary>
        public static void Load(string folder = "")
        {
            if (status_list.Count == 0)
                status_list.AddRange(Resources.LoadAll<StatusData>(folder));
        }

        /// <summary>
        /// 根据状态类型获取StatusData
        /// </summary>
        public static StatusData Get(StatusType effect)
        {
            foreach (StatusData status in GetAll())
            {
                if (status.effect == effect)
                    return status;
            }
            return null;
        }

        /// <summary>
        /// 获取所有状态数据
        /// </summary>
        public static List<StatusData> GetAll()
        {
            return status_list;
        }
    }
}
