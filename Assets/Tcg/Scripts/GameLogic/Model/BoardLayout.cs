using System;
using System.Collections.Generic;

namespace TcgEngine
{
    /// <summary>一局游戏的棋盘定义及槽位查询来源。</summary>
    public sealed class BoardLayout
    {
        public const int DefaultMinX = 1;
        public const int DefaultMaxX = 5;
        public const int DefaultMinY = 1;
        public const int DefaultMaxY = 1;

        private readonly IReadOnlyList<Slot>[] playerSlots;
        private readonly IReadOnlyList<Slot> allSlots;

        public int MinX { get; }
        public int MaxX { get; }
        public int MinY { get; }
        public int MaxY { get; }
        public int PlayerCount { get; }

        public static BoardLayout Default { get; } = CreateDefault(2);

        public BoardLayout(int minX, int maxX, int minY, int maxY, int playerCount)
        {
            if (minX > maxX) throw new ArgumentOutOfRangeException(nameof(minX));
            if (minY > maxY) throw new ArgumentOutOfRangeException(nameof(minY));
            if (playerCount <= 0) throw new ArgumentOutOfRangeException(nameof(playerCount));

            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;
            PlayerCount = playerCount;

            playerSlots = new IReadOnlyList<Slot>[playerCount];
            var all = new List<Slot>(playerCount * (maxX - minX + 1) * (maxY - minY + 1));
            for (int playerId = 0; playerId < playerCount; playerId++)
            {
                var slots = new List<Slot>();
                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        slots.Add(new Slot(x, y, playerId));
                    }
                }

                playerSlots[playerId] = slots.AsReadOnly();
                all.AddRange(slots);
            }
            allSlots = all.AsReadOnly();
        }

        public static BoardLayout CreateDefault(int playerCount) =>
            new(DefaultMinX, DefaultMaxX, DefaultMinY, DefaultMaxY, playerCount);

        public bool Contains(Slot slot) =>
            slot.x >= MinX && slot.x <= MaxX
            && slot.y >= MinY && slot.y <= MaxY
            && slot.p >= 0 && slot.p < PlayerCount;

        public IReadOnlyList<Slot> GetAll() => allSlots;

        public IReadOnlyList<Slot> GetAll(int playerId)
        {
            if (playerId < 0 || playerId >= PlayerCount)
                throw new ArgumentOutOfRangeException(nameof(playerId));
            return playerSlots[playerId];
        }

        public Slot GetRandom(int playerId, Random random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            IReadOnlyList<Slot> slots = GetAll(playerId);
            return slots[random.Next(slots.Count)];
        }

        public Slot GetRandom(Random random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            return allSlots[random.Next(allSlots.Count)];
        }

        public int MirrorX(int x) => MaxX - x + MinX;
        public int MirrorY(int y) => MaxY - y + MinY;
    }
}
