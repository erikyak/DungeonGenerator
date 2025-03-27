using System;
using System.Collections.Generic;
using Level.Rooms;
using UnityEngine;

namespace Level.Generation
{
    [Serializable]
    public class RoomTypeRequirements
    {
        public Room.RoomType type;
        public int minimumRequiredCount;
        public int maximumRequiredCount;
        public List<GameObject> prefabs;
    }
}