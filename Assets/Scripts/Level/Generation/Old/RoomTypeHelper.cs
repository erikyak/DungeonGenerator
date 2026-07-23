using System.Collections.Generic;
using System.Linq;

namespace Level.Generation.Old
{
    public static class RoomTypeHelper
    {
        public static Room.RoomType GetNextRoomType(Dictionary<Room.RoomType, int> remainingCounts)
        {
            Room.RoomType[] priorityOrder =
            {
                Room.RoomType.Fight,
                Room.RoomType.Treasure,
                Room.RoomType.Boss
            };

            foreach (var type in priorityOrder)
                if (remainingCounts.ContainsKey(type) && remainingCounts[type] > 0)
                    return type;
            return Room.RoomType.Null;
        }

        public static bool CanPlaceMoreRooms(Dictionary<Room.RoomType, int> remainingCounts)
        {
            return remainingCounts.Values.Any(count => count > 0);
        }

        public static bool IsGenerationComplete(Dictionary<Room.RoomType, int> remainingCounts)
        {
            return remainingCounts.Values.All(count => count <= 0);
        }
    }
}