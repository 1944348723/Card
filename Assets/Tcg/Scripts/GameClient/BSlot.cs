using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine.Client
{
    /// <summary>
    /// BoardSlot、BoardSlotPlayer、BoardSlotGroup 的基类
    /// 负责通用的高亮显示、碰撞区域判断、Slot 选择等功能
    /// </summary>
    public class BSlot : MonoBehaviour
    {
        protected SpriteRenderer render;    // 用于显示槽位的精灵渲染器
        protected Collider collide;         // 用于检测鼠标/物体是否进入的碰撞体
        protected Bounds bounds;            // 槽位的包围盒范围

        protected float start_alpha = 0f;   // 初始透明度
        protected float current_alpha = 0f; // 当前透明度
        protected float target_alpha = 0f;  // 目标透明度，用于渐变

        private static List<BSlot> slot_list = new List<BSlot>();   // 所有槽位对象列表（全局管理）

        // 初始化
        protected virtual void Awake()
        {
            slot_list.Add(this);
            render = GetComponent<SpriteRenderer>();
            collide = GetComponent<Collider>();

            start_alpha = render.color.a;   // 记录原始透明度
            render.color = new Color(render.color.r, render.color.g, render.color.b, 0f); // 初始完全透明
            bounds = collide.bounds;        // 记录碰撞包围盒
        }

        // 销毁时移除管理
        protected virtual void OnDestroy()
        {
            slot_list.Remove(this);
        }

        // 每帧更新：用于透明度渐变动画（高亮/隐藏）
        protected virtual void Update()
        {
            // 向目标透明度平滑移动
            current_alpha = Mathf.MoveTowards(current_alpha, target_alpha * start_alpha, 2f * Time.deltaTime);

            // 应用透明度
            render.color = new Color(render.color.r, render.color.g, render.color.b, current_alpha);
        }

        /// <summary>
        /// 获取该槽对应的 Slot（默认返回 Slot.None，子类需要重写）
        /// </summary>
        public virtual Slot GetSlot()
        {
            return Slot.None;
        }

        /// <summary>
        /// 根据世界坐标获取 Slot（默认行为同 GetSlot，子类可实现基于位置的逻辑）
        /// </summary>
        public virtual Slot GetSlot(Vector3 wpos)
        {
            return GetSlot();
        }

        /// <summary>
        /// 获取空槽位（用于放置卡牌，默认同 GetSlot，子类重写）
        /// </summary>
        public virtual Slot GetEmptySlot(Vector3 wpos)
        {
            return GetSlot();
        }

        /// <summary>
        /// 获取该槽位上当前放置的卡牌
        /// </summary>
        public virtual Card GetSlotCard(Vector3 wpos)
        {
            Game gdata = GameClient.Get().GetGameData();
            Slot slot = GetSlot(wpos);
            return gdata.GetSlotCard(slot);
        }

        /// <summary>
        /// 根据 Slot 返回世界坐标位置（默认返回自身位置）
        /// </summary>
        public virtual Vector3 GetPosition(Slot slot)
        {
            return transform.position;
        }

        /// <summary>
        /// 获取该槽位绑定的玩家，默认 null，子类实现
        /// </summary>
        public virtual Player GetPlayer()
        {
            return null;
        }

        /// <summary>
        /// 判断该槽位是否匹配给定 Slot
        /// </summary>
        public virtual bool HasSlot(Slot slot)
        {
            Slot aslot = GetSlot();
            return aslot == slot;
        }

        /// <summary>
        /// 槽是否代表一个“玩家槽”（即玩家本体区域）
        /// </summary>
        public virtual bool IsPlayer()
        {
            Slot slot = GetSlot();
            return slot.IsPlayerSlot();
        }

        /// <summary>
        /// 判断一个世界坐标是否处于该槽区域内
        /// </summary>
        public virtual bool IsInside(Vector3 wpos)
        {
            return bounds.Contains(wpos);
        }

        /// <summary>
        /// 获取距离某个位置最近且包含该位置的槽位
        /// （用于拖拽选择最近槽）
        /// </summary>
        public static BSlot GetNearest(Vector3 pos)
        {
            BSlot nearest = null;
            float min_dist = 999f;

            foreach (BSlot slot in GetAll())
            {
                float dist = (slot.transform.position - pos).magnitude;
                if (slot.IsInside(pos) && dist < min_dist)
                {
                    min_dist = dist;
                    nearest = slot;
                }
            }
            return nearest;
        }

        /// <summary>
        /// 精确根据 Slot 查找槽对象
        /// </summary>
        public static BSlot Get(Slot slot)
        {
            foreach (BSlot bslot in GetAll())
            {
                if (bslot.HasSlot(slot))
                    return bslot;
            }
            return null;
        }

        /// <summary>
        /// 获取当前所有槽对象
        /// </summary>
        public static List<BSlot> GetAll()
        {
            return slot_list;
        }
    }

    /// <summary>
    /// 槽位类型（一般由服务器/规则系统解释）
    /// </summary>
    public enum BoardSlotType
    {
        Fixed = 0,              // 固定槽位：x,y,p 直接表示具体槽
        PlayerSelf = 5,         // 当前客户端玩家的槽位（p = 本地玩家 id）
        PlayerOpponent = 7,     // 对手玩家槽位（p = 对手 id）
        FlipX = 10,             // x 轴镜像：第一玩家正常，第二玩家 x 翻转
        FlipY = 11,             // y 轴镜像：第一玩家正常，第二玩家 y 翻转
    }
}
