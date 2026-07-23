using System.Collections.Generic;
using Level.Generation.Composites;
using Level.Generation.Grammar;

namespace Level.Generation.Graph
{
    public class Zone
    {
        public int id;
        public ZoneType type;
        public List<ZoneEdge> edges = new List<ZoneEdge>();
        
        public RecipeTemplate assignedRecipe;
        public Composite composite;
        
        public bool isOnCriticalPath;
        public int depth;
        public bool isInjected;
        
        public Zone(int id, ZoneType type)
        {
            this.id = id;
            this.type = type;
        }
        
        public int InterfaceCount() => edges.Count;
        
        public IEnumerable<Zone> Neighbors()
        {
            foreach (var edge in edges)
            {
                yield return edge.Other(this);
            }
        }
        
        public override string ToString() => $"Zone#{id}({type})";
    }
}