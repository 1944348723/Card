using System;
using Unity.Netcode;

namespace TcgEngine
{
    /// <summary>棋盘位置值对象</summary>
    [Serializable]
    public struct Slot : INetworkSerializable, IEquatable<Slot>
    {
        public int x;
        public int y;
        public int p;

        public Slot(int pid)
        {
            x = 0;
            y = 0;
            p = pid;
        }

        public Slot(int x, int y, int pid)
        {
            this.x = x;
            this.y = y;
            p = pid;
        }

        public Slot(SlotXY slot, int pid)
        {
            x = slot.x;
            y = slot.y;
            p = pid;
        }

        public static Slot None => new(-1, -1, -1);

        public bool IsInDistanceStraight(Slot slot, int distance)
        {
            int actual = Math.Abs(x - slot.x) + Math.Abs(y - slot.y) + Math.Abs(p - slot.p);
            return actual <= distance;
        }

        public bool IsInDistance(Slot slot, int distance)
        {
            return Math.Abs(x - slot.x) <= distance
                && Math.Abs(y - slot.y) <= distance
                && Math.Abs(p - slot.p) <= distance;
        }

        public bool IsPlayerSlot() => x == 0 && y == 0 && p >= 0;

        public bool BelongsToPlayer(int playerId) => p == playerId;

        public bool Equals(Slot other) => x == other.x && y == other.y && p == other.p;

        public override bool Equals(object obj) => obj is Slot other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + x;
                hash = hash * 31 + y;
                hash = hash * 31 + p;
                return hash;
            }
        }

        public static bool operator ==(Slot left, Slot right) => left.Equals(right);
        public static bool operator !=(Slot left, Slot right) => !left.Equals(right);

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref x);
            serializer.SerializeValue(ref y);
            serializer.SerializeValue(ref p);
        }
    }

    [Serializable]
    public struct SlotXY
    {
        public int x;
        public int y;
    }
}
