using Level.Generation.Support;

namespace Level.Generation.Composites
{
    public class CompositeLayouter
    {
        private const int MaxLayoutAttempts = 5;
        
        public bool Layout(Composite composite, SeedManager seed)
        {
            if (composite == null || composite.nodes.Count == 0) return false;
            
            for (int attempt = 0; attempt < MaxLayoutAttempts; attempt++)
            {
                ResetComposite(composite);
                
                var physical = new PhysicalLayout();
                if (!physical.ArrangeRooms(composite, seed)) continue;
                
                var checker = new LocalCollisionChecker();
                foreach (var node in composite.nodes)
                    checker.OccupyRoom(node.position.Value, node.assignedFootprint.size);
                
                var router = new CorridorRouter();
                if (router.RouteAllCorridors(composite, checker, seed)) return true;
            }
            
            return false;
        }
        
        private void ResetComposite(Composite composite)
        {
            foreach (var node in composite.nodes)
            {
                node.position = null;
                node.floatPosition = UnityEngine.Vector2.zero;
                node.usedDoors.Clear();
            }
            foreach (var edge in composite.edges)
            {
                edge.doorA = null;
                edge.doorB = null;
                edge.corridorPath.Clear();
            }
        }
    }
}