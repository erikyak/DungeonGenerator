using System.Collections.Generic;

namespace Level.Generation.Graph
{
    public class ValidationResult
    {
        public bool isValid = true;
        public List<string> errors = new List<string>();
        
        public static ValidationResult Success() => new ValidationResult { isValid = true };
        
        public static ValidationResult Failure(string error)
        {
            var r = new ValidationResult { isValid = false };
            r.errors.Add(error);
            return r;
        }
        
        public void AddError(string error)
        {
            isValid = false;
            errors.Add(error);
        }
    }
    
    public class MacroGraph
    {
        public List<Zone> zones = new List<Zone>();
        public List<ZoneEdge> edges = new List<ZoneEdge>();
        
        public Zone entry;
        public Zone boss;
        
        private int _nextZoneId = 0;
        
        public Zone AddZone(ZoneType type)
        {
            var zone = new Zone(_nextZoneId++, type);
            zones.Add(zone);
            
            if (type == ZoneType.Entry) entry = zone;
            if (type == ZoneType.Boss) boss = zone;
            
            return zone;
        }
        
        public ZoneEdge AddEdge(Zone a, Zone b, bool isCycleEdge = false)
        {
            var edge = new ZoneEdge(a, b, isCycleEdge);
            edges.Add(edge);
            a.edges.Add(edge);
            b.edges.Add(edge);
            return edge;
        }
        
        public bool HasEdge(Zone a, Zone b)
        {
            foreach (var edge in a.edges)
            {
                if (edge.Other(a) == b) return true;
            }
            return false;
        }
        
        public IEnumerable<ZoneEdge> EdgesOf(Zone zone) => zone.edges;
        
        public void ComputeCriticalPath()
        {
            foreach (var z in zones)
            {
                z.depth = -1;
                z.isOnCriticalPath = false;
            }
            
            if (entry == null || boss == null) return;
            
            var predecessor = new Dictionary<Zone, Zone>();
            var queue = new Queue<Zone>();
            
            entry.depth = 0;
            queue.Enqueue(entry);
            
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == boss) break;
                
                foreach (var neighbor in current.Neighbors())
                {
                    if (neighbor.depth != -1) continue;
                    neighbor.depth = current.depth + 1;
                    predecessor[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }
            
            if (boss.depth == -1) return;
            
            var walker = boss;
            while (walker != null)
            {
                walker.isOnCriticalPath = true;
                if (walker == entry) break;
                predecessor.TryGetValue(walker, out walker);
            }
        }
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult { isValid = true };
            
            if (entry == null) result.AddError("Entry zone missing");
            if (boss == null) result.AddError("Boss zone missing");
            
            if (!result.isValid) return result;
            
            var reached = new HashSet<Zone>();
            var queue = new Queue<Zone>();
            queue.Enqueue(entry);
            reached.Add(entry);
            
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var neighbor in current.Neighbors())
                {
                    if (reached.Add(neighbor)) queue.Enqueue(neighbor);
                }
            }
            
            foreach (var zone in zones)
            {
                if (!reached.Contains(zone))
                    result.AddError($"Zone {zone} is not reachable from Entry");
            }
            
            ComputeCriticalPath();
            
            const int minCriticalPathLength = 3;
            if (boss.depth != -1 && boss.depth < minCriticalPathLength)
                result.AddError($"Critical path Entry-Boss too short: {boss.depth} zones (min {minCriticalPathLength})");
            
            foreach (var edge in edges)
            {
                if (!AdjacencyRules.IsAllowed(edge.a.type, edge.b.type))
                    result.AddError($"Edge {edge} violates adjacency rules");
            }
            
            return result;
        }
    }
}