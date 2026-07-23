using System.Collections.Generic;
using UnityEngine;
using Level.Generation.Composites;
using Level.Generation.Layout;

namespace Level.Generation.Corridors
{
    public class CorridorPath
    {
        public PlacedRoom sourceRoom;
        public PlacedRoom targetRoom;
        public DoorSocket sourceDoor;
        public DoorSocket targetDoor;
        public List<Vector2Int> pathTiles = new List<Vector2Int>();
        public List<Vector2Int> mainPath = new List<Vector2Int>();
        public List<PlacedCorridorSegment> segments = new List<PlacedCorridorSegment>();
        public bool isInjected = false;
    }
}