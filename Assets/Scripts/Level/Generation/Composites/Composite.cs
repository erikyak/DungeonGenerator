using System.Collections.Generic;
using Level.Generation.Graph;

namespace Level.Generation.Composites
{
    public enum CompositeType
    {
        Tree,
        Cycle,
        Hybrid
    }
    
    public class Composite
    {
        public List<CompositeNode> nodes = new List<CompositeNode>();
        public List<CompositeEdge> edges = new List<CompositeEdge>();
        public CompositeType structureType = CompositeType.Tree;
        public List<CompositeNode> interfaceNodes = new List<CompositeNode>();
        public Zone parentZone;
        
        public void AddNode(CompositeNode node)
        {
            nodes.Add(node);
        }
        
        public void AddEdge(CompositeEdge edge)
        {
            edges.Add(edge);
            edge.a.edges.Add(edge);
            edge.b.edges.Add(edge);
        }
        
        public void MarkAsInterface(CompositeNode node, int slotIndex)
        {
            if (!interfaceNodes.Contains(node))
            {
                interfaceNodes.Add(node);
                node.isInterface = true;
                node.interfaceSlotIndex = slotIndex;
            }
        }
        
        public IEnumerable<DoorSocket> UnusedInterfaceDoors()
        {
            foreach (var node in interfaceNodes)
            {
                if (node.assignedFootprint == null) continue;
                foreach (var door in node.assignedFootprint.doors)
                {
                    if (!node.usedDoors.Contains(door))
                        yield return door;
                }
            }
        }
    }
}