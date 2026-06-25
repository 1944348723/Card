using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace TcgEngine
{
    /// <summary>
    /// 表示游戏中的一个槽位（数据结构）
    /// </summary>

    [System.Serializable]
    public struct Slot : INetworkSerializable
    {
        public int x; // 槽位在横向的位置，从 1 到 5
        public int y; // 纵向位置，目前未使用，可用于增加多行或不同板面位置
        public int p; // 玩家ID，0 或 1

        public static int x_min = 1; // 最小X值，不要更改，0,0,0 表示无效槽位
        public static int x_max = 5; // 每行/区域的槽位数量

        public static int y_min = 1; // 最小Y值，不要更改，0,0,0 表示无效槽位
        public static int y_max = 1; // 最大行数/位置数，可根据需求设置

        public static bool ignore_p = false; // 如果不想使用 P 值则设置为 true

        private static Dictionary<int, List<Slot>> player_slots = new(); // 玩家槽位字典
        private static List<Slot> all_slots = new(); // 所有有效槽位列表

        // 构造函数，使用玩家ID初始化
        public Slot(int pid)
        {
            this.x = 0;
            this.y = 0;
            this.p = pid;
        }

        // 构造函数，使用x,y,p初始化
        public Slot(int x, int y, int pid)
        {
            this.x = x;
            this.y = y;
            this.p = pid;
        }

        // 构造函数，根据SlotXY和玩家ID初始化
        public Slot(SlotXY slot, int pid)
        {
            this.x = slot.x;
            this.y = slot.y;
            this.p = pid;
        }

        // 判断槽位X是否在指定范围内
        public bool IsInRangeX(Slot slot, int range)
        {
            return Mathf.Abs(x - slot.x) <= range;
        }

        // 判断槽位Y是否在指定范围内
        public bool IsInRangeY(Slot slot, int range)
        {
            return Mathf.Abs(y - slot.y) <= range;
        }

        // 判断玩家ID是否在指定范围内
        public bool IsInRangeP(Slot slot, int range)
        {
            return Mathf.Abs(p - slot.p) <= range;
        }

        // 直线距离判断（不算斜线，斜线距离 = 2）
        public bool IsInDistanceStraight(Slot slot, int dist)
        {
            int r = Mathf.Abs(x - slot.x) + Mathf.Abs(y - slot.y) + Mathf.Abs(p - slot.p);
            return r <= dist;
        }

        // 包含斜线距离判断（斜线距离 = 1）
        public bool IsInDistance(Slot slot, int dist)
        {
            int dx = Mathf.Abs(x - slot.x);
            int dy = Mathf.Abs(y - slot.y);
            int dp = Mathf.Abs(p - slot.p);
            return dx <= dist && dy <= dist && dp <= dist;
        }

        // 判断是否为玩家槽位（0,0）
        public bool IsPlayerSlot()
        {
            return x == 0 && y == 0;
        }

        // 判断槽位是否合法（是否在棋盘范围内）
        public bool IsValid()
        {
            return x >= x_min && x <= x_max && y >= y_min && y <= y_max && p >= 0;
        }

        // 最大玩家ID
        public static int MaxP
        {
            get { return ignore_p ? 0 : 1; } 
        }

        // 获取玩家对应的P值，通常等于player_id，如果忽略P则返回0
        public static int GetP(int pid)
        {
            return ignore_p ? 0 : pid;
        }

        // 获取指定玩家的随机槽位
        public static Slot GetRandom(int pid, System.Random rand)
        {
            int p = GetP(pid);
            if (y_max > y_min)
                return new Slot(rand.Next(x_min, x_max + 1), rand.Next(y_min, y_max + 1), p);
            return new Slot(rand.Next(x_min, x_max + 1), y_min, p);
        }

        // 获取所有玩家中的随机槽位
        public static Slot GetRandom(System.Random rand)
        {
            if (y_max > y_min)
                return new Slot(rand.Next(x_min, x_max + 1), rand.Next(y_min, y_max + 1), rand.Next(0, 2));
            return new Slot(rand.Next(x_min, x_max + 1), y_min, rand.Next(0, 2));
        }
		
        // 根据x,y,p获取槽位
        public static Slot Get(int x, int y, int p)
        {
            List<Slot> slots = GetAll();
            foreach (Slot slot in slots)
            {
                if (slot.x == x && slot.y == y && slot.p == p)
                    return slot;
            }
            return new Slot(x, y, p);
        }

        // 获取指定玩家的所有槽位
        public static List<Slot> GetAll(int pid)
        {
            int p = GetP(pid);

            if (player_slots.ContainsKey(p))
                return player_slots[p]; // 快速访问

            List<Slot> list = new List<Slot>();
            for (int y = y_min; y <= y_max; y++)
            {
                for (int x = x_min; x <= x_max; x++)
                {
                    list.Add(new Slot(x, y, p));
                }
            }
            player_slots[p] = list;
            return list;
        }

        // 获取所有有效槽位
        public static List<Slot> GetAll()
        {
            if (all_slots.Count > 0)
                return all_slots; // 快速访问

            for (int p = 0; p <= MaxP; p++)
            {
                for (int y = y_min; y <= y_max; y++)
                {
                    for (int x = x_min; x <= x_max; x++)
                    {
                        all_slots.Add(new Slot(x, y, p));
                    }
                }
            }
            return all_slots;
        }

        // == 运算符重载
        public static bool operator ==(Slot slot1, Slot slot2)
        {
            return slot1.x == slot2.x && slot1.y == slot2.y && slot1.p == slot2.p;
        }

        // != 运算符重载
        public static bool operator !=(Slot slot1, Slot slot2)
        {
            return slot1.x != slot2.x || slot1.y != slot2.y || slot1.p != slot2.p;
        }

        public override bool Equals(object o)
        {
            return base.Equals(o);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        // 网络序列化
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref x);
            serializer.SerializeValue(ref y);
            serializer.SerializeValue(ref p);
        }

        // 表示无效槽位
        public static Slot None
        {
            get { return new Slot(0, 0, 0); }
        }
    }

    /// <summary>
    /// 表示槽位的X,Y坐标（不包含玩家信息）
    /// </summary>
    [System.Serializable]
    public struct SlotXY
    {
        public int x; // X坐标
        public int y; // Y坐标
    }
}
