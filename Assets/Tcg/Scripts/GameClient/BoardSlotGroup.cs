using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Client;
using TcgEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>
    /// Slot.cs 的可视化版本
    /// 用于一组连续 Slot 的自动排布（自动整理卡牌位置）
    /// 比如：一排随从 / 一排牌位
    /// </summary>
    public class BoardSlotGroup : BSlot
    {
        public BoardSlotType type; // Slot类型（是否需要翻转、归属玩家等）
        public int min_x = 1;      // 组内最小 X
        public int max_x = 5;      // 组内最大 X
        public int y = 1;          // 组所在行（Y 坐标）

        public float spacing = 2.5f;     // 组内卡牌之间间距
        public float reduce_delay = 1f;  // 卡牌离开插槽后，延迟减少占用的时间

        private int nb_occupied = 0;     // 当前有多少插槽被占用

        private List<GroupSlot> group_slots = new List<GroupSlot>(); // 此组中的所有 Slot（带运行时信息）

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        private void Start()
        {
            // 检查 group slot 是否超出棋盘允许范围
            if (min_x < Slot.x_min || max_x > Slot.x_max || y < Slot.y_min || y > Slot.y_max)
                Debug.LogError("Board Slot X / Y 必须在 Slot.cs 的范围内，否则无效。");

            // 连接游戏事件
            GameClient.Get().onConnectGame += OnConnect;

            nb_occupied = 0;
            collide.enabled = false; // group 不是单独 slot，所以禁用碰撞
        }

        /// <summary>
        /// 游戏连接完成时执行
        /// 用于初始化组内 Slot 数据
        /// </summary>
        private void OnConnect()
        {
            foreach (Slot slot in Slot.GetAll())
            {
                // 只把属于这个 Group 的 Slot 加入进来
                if (IsInGroup(slot))
                {
                    GroupSlot pos = new GroupSlot();
                    pos.slot = slot;
                    pos.pos = transform.position; // 初始位置 = group pivot
                    group_slots.Add(pos);
                }
            }
        }

        protected override void Update()
        {
            base.Update();

            if (!GameClient.Get().IsReady())
                return;

            Game gdata = GameClient.Get().GetGameData();
            HandCard drag_card = HandCard.GetDrag();
            bool your_turn = GameClient.Get().IsYourTurn();
            Card dcard = drag_card?.GetCard();

            // ====================
            // 高亮逻辑
            // ====================
            target_alpha = 0f;

            // 拖拽一张可以放到棋盘上的卡牌 → 检查组中每一个 slot
            if (your_turn && dcard != null && dcard.CardData.IsBoardCard())
            {
                foreach (GroupSlot slot in group_slots)
                {
                    if(gdata.CanPlayCard(dcard, slot.slot))
                        target_alpha = 1f; // 只要有一个合法 slot → 整组高亮
                }
            }

            UpdateOccupied(); // 更新每个 slot 是否被占用
            UpdatePositions(); // 重新计算组内卡牌排列位置
        }

        /// <summary>
        /// 更新占用状态（判断哪些 slot 里有卡）
        /// 带 timer 机制，可以实现平滑动画 / 延迟消失
        /// </summary>
        public void UpdateOccupied()
        {
            int count = 0;
            Game gdata = GameClient.Get().GetGameData();

            foreach (GroupSlot slot in group_slots)
            {
                Card card = gdata.GetSlotCard(slot.slot);

                // 有卡 → timer 正向累加
                // 无卡 → timer 反向递减
                slot.timer += (card != null ? 1f : -1f) * Time.deltaTime / reduce_delay;

                // 限制范围 0 ~ 1
                slot.timer = Mathf.Clamp01(slot.timer);

                if (slot.IsOccupied)
                    count += 1;
            }

            nb_occupied = count; // 记录当前有多少卡
        }

        /// <summary>
        /// 自动计算卡牌排列位置
        /// 让卡牌永远居中排列
        /// </summary>
        public void UpdatePositions()
        {
            bool even = nb_occupied % 2 == 0;            // 是否偶数
            float offset = (nb_occupied / 2) * -spacing; // 起始偏移

            if (even)
                offset += spacing * 0.5f;

            int index = 0;

            foreach (GroupSlot slot in group_slots)
            {
                if (slot.IsOccupied)
                {
                    // 按 index 排列
                    slot.pos = transform.position + Vector3.right * (index * spacing + offset);
                    index++;
                }
                else
                {
                    // 空位暂时被放到“尾部”
                    slot.pos = transform.position + Vector3.right * (nb_occupied * spacing + offset);
                }
            }
        }

        // ============================
        // 判断某 Slot 是否属于 Group
        // ============================
        public bool IsInGroup(Slot slot)
        {
            return IsInGroup(slot.x, slot.y, slot.p);
        }

        public bool IsInGroup(int x, int y)
        {
            Slot min = GetSlotMin();
            Slot max = GetSlotMax();
            return x >= min.x && x <= max.x && y >= min.y && y <= max.y;
        }

        public bool IsInGroup(int x, int y, int p)
        {
            Slot min = GetSlotMin();
            Slot max = GetSlotMax();
            return x >= min.x && x <= max.x && y >= min.y && y <= max.y && p >= min.p && p <= max.p;
        }

        /// <summary>组最小 Slot</summary>
        public Slot GetSlotMin()
        {
            return GetSlot(min_x, y);
        }

        /// <summary>组最大 Slot</summary>
        public Slot GetSlotMax()
        {
            return GetSlot(max_x, y);
        }

        /// <summary>
        /// 计算 Slot 真实逻辑坐标（考虑翻转 / 玩家身份）
        /// </summary>
        public Slot GetSlot(int x, int y)
        {
            int p = 0;

            // X 轴翻转
            if (type == BoardSlotType.FlipX)
            {
                int pid = GameClient.Get().GetPlayerID();
                int px = x;
                if ((pid % 2) == 1)
                    px = Slot.x_max - x + Slot.x_min;
                return new Slot(px, y, p);
            }

            // Y 轴翻转
            if (type == BoardSlotType.FlipY)
            {
                int pid = GameClient.Get().GetPlayerID();
                int py = y;
                if ((pid % 2) == 1)
                    py = Slot.y_max - y + Slot.y_min;
                return new Slot(x, py, p);
            }

            // 属于自己
            if (type == BoardSlotType.PlayerSelf)
                p = GameClient.Get().GetPlayerID();

            // 属于对手
            if(type == BoardSlotType.PlayerOpponent)
                p = GameClient.Get().GetOpponentPlayerID();
           
            return new Slot(x, y, p);
        }

        /// <summary>
        /// 根据世界坐标找到最近 Slot
        /// （用于拖拽选择）
        /// </summary>
        public override Slot GetSlot(Vector3 wpos)
        {
            GroupSlot nearest = null;
            float min_dist = 99f;

            foreach (GroupSlot spos in group_slots)
            {
                float dist = (spos.pos - wpos).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = spos;
                }
            }

            if (nearest != null)
                return nearest.slot;
            return Slot.None;
        }

        /// <summary>
        /// 找距离最近的【已被占用】的 slot
        /// </summary>
        public virtual Slot GetSlotOccupied(Vector3 wpos)
        {
            GroupSlot nearest = null;
            float min_dist = 99f;

            foreach (GroupSlot spos in group_slots)
            {
                float dist = (spos.pos - wpos).magnitude;
                if (spos.IsOccupied && dist < min_dist)
                {
                    min_dist = dist;
                    nearest = spos;
                }
            }

            if (nearest != null)
                return nearest.slot;
            return Slot.None;
        }

        /// <summary>
        /// 根据世界坐标获取该位置上的卡牌
        /// </summary>
        public override Card GetSlotCard(Vector3 wpos)
        {
            Game gdata = GameClient.Get().GetGameData();
            Slot slot = GetSlotOccupied(wpos);
            if (slot != Slot.None)
                return gdata.GetSlotCard(slot);
            return null;
        }

        /// <summary>
        /// 判断某 slot 是否属于该 group
        /// </summary>
        public override bool HasSlot(Slot slot)
        {
            foreach (GroupSlot spos in group_slots)
            {
                if (spos.slot == slot)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 获取某 slot 的可视化位置
        /// </summary>
        public override Vector3 GetPosition(Slot slot)
        {
            foreach (GroupSlot spos in group_slots)
            {
                if (spos.slot == slot)
                    return spos.pos;
            }
            return transform.position;
        }

        /// <summary>
        /// 获取一个空 slot（例如放新卡用）
        /// </summary>
        public override Slot GetEmptySlot(Vector3 wpos)
        {
            foreach (GroupSlot slot in group_slots)
            {
                if (!slot.IsOccupied)
                    return slot.slot;
            }
            return Slot.None;
        }

    }

    /// <summary>
    /// Group 内每个 Slot 的运行时信息
    /// </summary>
    public class GroupSlot
    {
        public Slot slot;     // 逻辑 slot
        public Vector3 pos;   // 渲染位置（随 auto 排列改变）
        public float timer;   // 占用计时器（用于平滑消失）

        public bool IsOccupied { get { return timer > 0.01f;  } } // 是否认为“被占用”
    }
}
