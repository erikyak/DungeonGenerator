using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Level.Rooms;

namespace Level.Generation.Old
{
    public static class RoomInitializer
    {
        public static void Initialize(
            List<RoomTypeRequirements> roomRequirements, 
            out Dictionary<Room.RoomType, List<GameObject>> roomPools, 
            out Dictionary<Room.RoomType, int> remainingCounts)
        {
            roomPools = new Dictionary<Room.RoomType, List<GameObject>>();
            remainingCounts = new Dictionary<Room.RoomType, int>();

            foreach (var req in roomRequirements)
            {
                roomPools[req.type] = new List<GameObject>(req.prefabs.OrderBy(_ => Random.value));
                remainingCounts[req.type] = Random.Range(req.minimumRequiredCount, req.maximumRequiredCount + 1);
            }
        }
    }
}