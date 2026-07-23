using System;
using System.Collections.Generic;
using UnityEngine;

namespace Level.Generation.Old
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