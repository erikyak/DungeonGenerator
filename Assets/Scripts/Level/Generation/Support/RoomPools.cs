using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Level.Generation.Composites;

namespace Level.Generation.Support
{
    public class RoomPools
    {
        private readonly Dictionary<Room.RoomType, List<RoomFootprint>> _pools 
            = new Dictionary<Room.RoomType, List<RoomFootprint>>();
        
        public void AddPrefab(GameObject prefab, float tileSize = 1f)
        {
            var footprint = RoomFootprint.FromPrefab(prefab, tileSize);
            if (footprint == null) return;
            
            if (!_pools.ContainsKey(footprint.type))
                _pools[footprint.type] = new List<RoomFootprint>();
            _pools[footprint.type].Add(footprint);
        }
        
        public List<RoomFootprint> GetPool(Room.RoomType type)
        {
            return _pools.TryGetValue(type, out var list) ? list : new List<RoomFootprint>();
        }
        
        public bool HasAny(Room.RoomType type)
        {
            return _pools.ContainsKey(type) && _pools[type].Count > 0;
        }
        
        public RoomFootprint PickAny(Room.RoomType type, int minDoors, string prefabTag, SeedManager seed)
        {
            var pool = GetPool(type);
            var suitable = pool.Where(f => f.doors.Count >= minDoors);
            
            if (!string.IsNullOrEmpty(prefabTag))
                suitable = suitable.Where(f => f.tag == prefabTag);
            
            var list = suitable.ToList();
            if (list.Count == 0) return null;
            return list[seed.NextInt(list.Count)];
        }
    }
}