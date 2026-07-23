using UnityEngine;
using Level.Generation.Composites;

namespace Level.Generation.Layout
{
    public class PlacedRoom
    {
        public CompositeNode sourceNode;
        public Vector2Int worldPosition;
        
        public RoomFootprint footprint => sourceNode.assignedFootprint;
        
        public PlacedRoom(CompositeNode sourceNode, Vector2Int worldPosition)
        {
            this.sourceNode = sourceNode;
            this.worldPosition = worldPosition;
        }
        
        public Vector2Int DoorPrimaryWorldPosition(DoorSocket door)
        {
            return worldPosition + door.primaryTile;
        }
        
        public Vector2Int DoorSecondaryWorldPosition(DoorSocket door)
        {
            return worldPosition + door.secondaryTile;
        }
    }
}