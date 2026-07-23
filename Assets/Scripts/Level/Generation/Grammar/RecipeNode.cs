using System;
using UnityEngine;

namespace Level.Generation.Grammar
{
    [Serializable]
    public class RecipeNode
    {
        public int id;
        public Room.RoomType roomType;
        public int minDoors = 2;
        public string prefabTag = "";
        public bool canBeInterface = false;
        
        [Header("Repetition (for stretchable nodes)")]
        [Tooltip("If min < max, node is stretchable: creates N copies in a chain")]
        public int minRepeat = 1;
        public int maxRepeat = 1;
    }
}