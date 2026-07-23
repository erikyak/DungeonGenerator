using System.Collections.Generic;
using UnityEngine;

namespace Level.Generation.Composites
{
    public class LocalCollisionChecker
    {
        private readonly HashSet<Vector2Int> _roomTiles = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> _corridorTiles = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> _doorTiles = new HashSet<Vector2Int>();
        
        public bool IsAreaFree(Vector2Int origin, Vector2Int size)
        {
            for (int dx = 0; dx < size.x; dx++)
            {
                for (int dy = 0; dy < size.y; dy++)
                {
                    var tile = origin + new Vector2Int(dx, dy);
                    if (_roomTiles.Contains(tile)) return false;
                    if (_corridorTiles.Contains(tile)) return false;
                }
            }
            return true;
        }
        
        public bool IsTileFree(Vector2Int tile)
        {
            return !_roomTiles.Contains(tile) && !_corridorTiles.Contains(tile);
        }
        
        public bool IsTilePassable(Vector2Int tile)
        {
            if (_doorTiles.Contains(tile)) return true;
            if (_corridorTiles.Contains(tile)) return false;
            return !_roomTiles.Contains(tile);
        }
        
        public void OccupyRoom(Vector2Int origin, Vector2Int size)
        {
            for (int dx = 0; dx < size.x; dx++)
            {
                for (int dy = 0; dy < size.y; dy++)
                {
                    _roomTiles.Add(origin + new Vector2Int(dx, dy));
                }
            }
        }
        
        public void OccupyCorridor(Vector2Int tile)
        {
            _corridorTiles.Add(tile);
        }
        
        public void MarkDoorTile(Vector2Int tile)
        {
            _doorTiles.Add(tile);
        }
        
        public void MarkDoorSocket(DoorSocket door, Vector2Int roomWorldOrigin)
        {
            _doorTiles.Add(roomWorldOrigin + door.primaryTile);
            _doorTiles.Add(roomWorldOrigin + door.secondaryTile);
        }
        
        public void Clear()
        {
            _roomTiles.Clear();
            _corridorTiles.Clear();
            _doorTiles.Clear();
        }
        
        public int RoomTilesCount => _roomTiles.Count;
        public int CorridorTilesCount => _corridorTiles.Count;

        public bool AreaHasRoom(Vector2Int origin, Vector2Int size)
        {
            for (int dx = 0; dx < size.x; dx++)
            for (int dy = 0; dy < size.y; dy++)
                if (_roomTiles.Contains(origin + new Vector2Int(dx, dy))) return true;
            return false;
        }

        public bool AreaHasRoomExcept(Vector2Int origin, Vector2Int size, HashSet<Vector2Int> allowedRoomTiles)
        {
            for (int dx = 0; dx < size.x; dx++)
            for (int dy = 0; dy < size.y; dy++)
            {
                var t = origin + new Vector2Int(dx, dy);
                if (_roomTiles.Contains(t) && !allowedRoomTiles.Contains(t)) return true;
            }
            return false;
        }

        public bool AreaHasCorridorExcept(Vector2Int origin, Vector2Int size, HashSet<Vector2Int> allowedCorridorTiles)
        {
            for (int dx = 0; dx < size.x; dx++)
            for (int dy = 0; dy < size.y; dy++)
            {
                var t = origin + new Vector2Int(dx, dy);
                if (_corridorTiles.Contains(t) && !allowedCorridorTiles.Contains(t)) return true;
            }
            return false;
        }

        public bool WallCellHitsRoomExcept(Vector2Int origin, Vector2Int size, HashSet<Vector2Int> passageCellsLocal, HashSet<Vector2Int> allowedRoomTiles)
        {
            for (int dx = 0; dx < size.x; dx++)
            for (int dy = 0; dy < size.y; dy++)
            {
                var localTile = new Vector2Int(dx, dy);
                if (passageCellsLocal.Contains(localTile)) continue;
                var world = origin + localTile;
                if (_roomTiles.Contains(world) && !allowedRoomTiles.Contains(world)) return true;
            }
            return false;
        }
    }
}