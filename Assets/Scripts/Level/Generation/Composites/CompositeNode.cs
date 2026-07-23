using System.Collections.Generic;
using UnityEngine;

namespace Level.Generation.Composites
{
    public class CompositeNode
    {
        public int localId;
        public Room.RoomType type;
        public List<CompositeEdge> edges = new List<CompositeEdge>();
        
        public RoomFootprint assignedFootprint;
        public bool isInterface;
        public int interfaceSlotIndex = -1;
        public Vector2Int? position;
        public Vector2 floatPosition;
        public List<DoorSocket> usedDoors = new List<DoorSocket>();
        
        public CompositeNode(int localId, Room.RoomType type)
        {
            this.localId = localId;
            this.type = type;
        }
        
        public override string ToString() => $"CNode#{localId}({type})";
    }
}