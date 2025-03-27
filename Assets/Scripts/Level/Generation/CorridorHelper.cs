using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Level.Rooms;

namespace Level.Generation
{
    public static class CorridorHelper
    {
        public static GameObject SelectCorridorPrefab(List<GameObject> corridorPrefabs, Door.Direction requiredEntryDirection)
        {
            if (corridorPrefabs == null || corridorPrefabs.Count == 0)
                return null;

            var compatibleCorridors = corridorPrefabs.OrderBy(_ => Random.value)
                .Where(prefab =>
                {
                    var corridor = prefab.GetComponent<Room>();
                    return corridor &&
                           corridor.doors is { Length: >= 2 } &&
                           corridor.doors.Any(d => d.direction == requiredEntryDirection);
                }).ToList();

            if (compatibleCorridors.Count == 0)
                return null;

            return compatibleCorridors[Random.Range(0, compatibleCorridors.Count)];
        }

        public static Door.Direction OppositeDirection(Door.Direction dir)
        {
            return dir switch
            {
                Door.Direction.North => Door.Direction.South,
                Door.Direction.South => Door.Direction.North,
                Door.Direction.East  => Door.Direction.West,
                Door.Direction.West  => Door.Direction.East,
                _                    => Door.Direction.North,
            };
        }

        public static GameObject FindRoomWithDoorDirection(
            Dictionary<Room.RoomType, List<GameObject>> roomPools, 
            Room.RoomType type, 
            Door.Direction requiredDirection)
        {
            if (!roomPools.TryGetValue(type, out var pool))
                return null;

            var candidates = pool.OrderBy(_ => Random.value)
                .Where(prefab =>
                    prefab.GetComponent<Room>().doors.Any(d => d.direction == requiredDirection)
                ).ToList();

            if (candidates.Count == 0)
                return null;

            return candidates[Random.Range(0, candidates.Count)];
        }
    }
}
