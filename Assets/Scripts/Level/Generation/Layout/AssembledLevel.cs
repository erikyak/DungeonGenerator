using System.Collections.Generic;
using UnityEngine;
using Level.Generation.Graph;
using Level.Generation.Corridors;

namespace Level.Generation.Layout
{
    public class AssembledLevel
    {
        public MacroGraph graph;
        public List<PlacedRoom> rooms = new List<PlacedRoom>();
        public List<CorridorPath> corridors = new List<CorridorPath>();
        public Vector2Int worldBoundsMin = new Vector2Int(int.MaxValue, int.MaxValue);
        public Vector2Int worldBoundsMax = new Vector2Int(int.MinValue, int.MinValue);
        
        public void AddRoom(PlacedRoom room)
        {
            rooms.Add(room);
            UpdateBounds(room.worldPosition, room.worldPosition + room.footprint.size);
        }
        
        public void AddCorridor(CorridorPath corridor)
        {
            corridors.Add(corridor);
            foreach (var tile in corridor.pathTiles)
                UpdateBounds(tile, tile + Vector2Int.one);
        }
        
        private void UpdateBounds(Vector2Int min, Vector2Int max)
        {
            if (min.x < worldBoundsMin.x) worldBoundsMin.x = min.x;
            if (min.y < worldBoundsMin.y) worldBoundsMin.y = min.y;
            if (max.x > worldBoundsMax.x) worldBoundsMax.x = max.x;
            if (max.y > worldBoundsMax.y) worldBoundsMax.y = max.y;
        }
    }
}