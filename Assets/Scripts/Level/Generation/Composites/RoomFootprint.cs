using System.Collections.Generic;
using Level.Generation.Old;
using UnityEngine;

namespace Level.Generation.Composites
{
    public class RoomFootprint
    {
        public GameObject prefab;
        public Vector2Int size;
        public Vector2Int origin;
        public List<DoorSocket> doors = new List<DoorSocket>();
        public Room.RoomType type;
        public string tag = "";
        
        public static RoomFootprint FromPrefab(GameObject prefab, float tileSize = 1f)
        {
            if (prefab == null)
            {
                Debug.LogWarning("RoomFootprint.FromPrefab: prefab is null");
                return null;
            }
            
            var room = prefab.GetComponent<Room>();
            if (room == null)
            {
                Debug.LogWarning($"RoomFootprint.FromPrefab: no Room component on prefab {prefab.name}");
                return null;
            }
            
            var footprint = new RoomFootprint
            {
                prefab = prefab,
                type = room.type,
                tag = string.IsNullOrEmpty(room.tag) ? "" : room.tag
            };
            
            var collider = prefab.GetComponent<BoxCollider2D>();
            Vector2 boundsSize;
            Vector2 boundsCenter;
            
            if (collider != null)
            {
                boundsSize = collider.size;
                boundsCenter = collider.offset;
            }
            else
            {
                boundsSize = room.bounds.size;
                boundsCenter = room.bounds.center;
            }
            
            footprint.size = new Vector2Int(
                Mathf.RoundToInt(boundsSize.x / tileSize),
                Mathf.RoundToInt(boundsSize.y / tileSize)
            );
            
            float minX = boundsCenter.x - boundsSize.x / 2f;
            float minY = boundsCenter.y - boundsSize.y / 2f;
            
            footprint.origin = new Vector2Int(
                Mathf.FloorToInt(minX / tileSize),
                Mathf.FloorToInt(minY / tileSize)
            );
            
            if (room.doors != null)
            {
                for (int i = 0; i < room.doors.Length; i++)
                {
                    var door = room.doors[i];
                    if (door == null) continue;
                    
                    Vector3 doorLocalPos = door.transform.localPosition;
                    var socket = ExtractDoorSocket(doorLocalPos, door.direction, i, footprint.origin, tileSize);
                    if (socket != null) footprint.doors.Add(socket);
                }
            }
            
            return footprint;
        }
        
        private static DoorSocket ExtractDoorSocket(Vector3 localPos, Door.Direction dir, int idx, Vector2Int origin, float tileSize)
        {
            Vector2Int primary, secondary;
            
            if (dir == Door.Direction.East || dir == Door.Direction.West)
            {
                int xTile = Mathf.FloorToInt(localPos.x / tileSize);
                float yFloat = localPos.y / tileSize;
                int yLow = Mathf.FloorToInt(yFloat - 0.5f);
                int yHigh = yLow + 1;
                var primWorld = new Vector2Int(xTile, yLow);
                var secWorld = new Vector2Int(xTile, yHigh);
                primary = primWorld - origin;
                secondary = secWorld - origin;
            }
            else
            {
                int yTile = Mathf.FloorToInt(localPos.y / tileSize);
                float xFloat = localPos.x / tileSize;
                int xLow = Mathf.FloorToInt(xFloat - 0.5f);
                int xHigh = xLow + 1;
                var primWorld = new Vector2Int(xLow, yTile);
                var secWorld = new Vector2Int(xHigh, yTile);
                primary = primWorld - origin;
                secondary = secWorld - origin;
            }
            
            return new DoorSocket(primary, secondary, dir, idx);
        }
    }
}