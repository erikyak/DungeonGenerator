using System.Collections.Generic;
using UnityEngine;

namespace Level.Generation.Composites
{
    public class CompositeEdge
    {
        public CompositeNode a;
        public CompositeNode b;
        public DoorSocket doorA;
        public DoorSocket doorB;
        public List<Vector2Int> corridorPath = new List<Vector2Int>();
        public List<Vector2Int> mainPath = new List<Vector2Int>();
        
        public CompositeEdge(CompositeNode a, CompositeNode b)
        {
            this.a = a;
            this.b = b;
        }
        
        public CompositeNode Other(CompositeNode from)
        {
            if (from == a) return b;
            if (from == b) return a;
            throw new System.ArgumentException($"Node {from} not incident to this edge");
        }
    }
}