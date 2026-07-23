using System.Collections.Generic;
using System.Linq;
using Level.Generation.Grammar;
using Level.Generation.Old;
using Level.Generation.Support;
using Level.Rooms;
using UnityEngine;
using Random = System.Random;

namespace Level.Generation.Graph
{
    public class MacroGraphBuilder
    {
        private const int MaxBuildAttempts = 20;
        
        public MacroGraph Build(List<RoomTypeRequirements> requirements, RecipeGrammar grammar, SeedManager seed)
        {
            var interfaceLimits = ComputeInterfaceLimits(grammar);
            
            for (int attempt = 0; attempt < MaxBuildAttempts; attempt++)
            {
                var graph = TryBuild(requirements, grammar, interfaceLimits, seed);
                if (graph == null) continue;
                
                var validation = graph.Validate();
                if (validation.isValid) return graph;
            }
            return null;
        }
        
        private Dictionary<ZoneType, (int min, int max)> ComputeInterfaceLimits(RecipeGrammar grammar)
        {
            var result = new Dictionary<ZoneType, (int min, int max)>();
            
            foreach (var template in grammar.UserTemplates())
            {
                var t = template.TargetZoneType;

                int min = int.MaxValue;
                int max = int.MinValue;
                for (int n = 1; n <= 8; n++)
                {
                    if (template.SupportsInterfaceCount(n))
                    {
                        if (n < min) min = n;
                        if (n > max) max = n;
                    }
                }
                
                if (min == int.MaxValue) continue;
                
                if (result.ContainsKey(t))
                {
                    var existing = result[t];
                    result[t] = (System.Math.Min(existing.min, min), System.Math.Max(existing.max, max));
                }
                else
                {
                    result[t] = (min, max);
                }
            }
            
            return result;
        }
        
        private MacroGraph TryBuild(List<RoomTypeRequirements> requirements, 
                                     RecipeGrammar grammar,
                                     Dictionary<ZoneType, (int min, int max)> interfaceLimits, 
                                     SeedManager seed)
        {
            var graph = new MacroGraph();
            var rng = seed.GetRng();
            
            var counts = ComputeZoneCounts(requirements, grammar, seed);
            
            var entry = graph.AddZone(ZoneType.Entry);
            var boss = graph.AddZone(ZoneType.Boss);
            
            int hallCount = counts.ContainsKey(ZoneType.Hall) ? counts[ZoneType.Hall] : 3;
            var halls = new List<Zone>();
            for (int i = 0; i < hallCount; i++)
                halls.Add(graph.AddZone(ZoneType.Hall));
            
            var terminals = new List<Zone>();
            foreach (var kv in counts)
            {
                if (kv.Key == ZoneType.Entry || kv.Key == ZoneType.Boss || kv.Key == ZoneType.Hall)
                    continue;
                for (int i = 0; i < kv.Value; i++)
                    terminals.Add(graph.AddZone(kv.Key));
            }
            
            graph.AddEdge(entry, halls[0]);
            for (int i = 0; i < halls.Count - 1; i++)
                graph.AddEdge(halls[i], halls[i + 1]);
            graph.AddEdge(halls[halls.Count - 1], boss);
            
            foreach (var terminal in terminals)
            {
                var validHalls = halls.Where(h =>
                    AdjacencyRules.IsAllowed(h.type, terminal.type) &&
                    CanAddEdge(h, interfaceLimits)
                ).ToList();

                if (validHalls.Count == 0) return null;
                var host = validHalls[rng.Next(validHalls.Count)];
                graph.AddEdge(host, terminal);
            }
            
            int targetCycles = 1 + rng.Next(3);
            AddCycleEdges(graph, targetCycles, interfaceLimits, seed);
            
            return graph;
        }
        
        private bool CanAddEdge(Zone zone, Dictionary<ZoneType, (int min, int max)> interfaceLimits)
        {
            if (!interfaceLimits.TryGetValue(zone.type, out var limits)) return false;
            return zone.edges.Count < limits.max;
        }
        
        private Dictionary<ZoneType, int> ComputeZoneCounts(List<RoomTypeRequirements> requirements, 
                                                             RecipeGrammar grammar,
                                                             SeedManager seed)
        {
            var counts = new Dictionary<ZoneType, int>();
            var rng = seed.GetRng();
            
            var budgetPerType = new Dictionary<Room.RoomType, int>();
            foreach (var req in requirements)
            {
                int n = rng.Next(req.minimumRequiredCount, req.maximumRequiredCount + 1);
                budgetPerType[req.type] = n;
            }
            
            var entryCost = EstimateMinCostForZoneType(grammar, ZoneType.Entry);
            var bossCost = EstimateMinCostForZoneType(grammar, ZoneType.Boss);
            var treasureCost = EstimateMinCostForZoneType(grammar, ZoneType.Treasure);
            var hallCost = EstimateMinCostForZoneType(grammar, ZoneType.Hall);
            
            counts[ZoneType.Entry] = 1;
            counts[ZoneType.Boss] = 1;
            
            var reserved = new Dictionary<Room.RoomType, int>();
            AddCost(reserved, entryCost);
            AddCost(reserved, bossCost);
            
            int treasureBudget = budgetPerType.ContainsKey(Room.RoomType.Treasure) ? budgetPerType[Room.RoomType.Treasure] : 0;
            int treasureCountFinal = 0;
            for (int i = 0; i < treasureBudget; i++)
            {
                if (CanReserve(reserved, treasureCost, budgetPerType))
                {
                    AddCost(reserved, treasureCost);
                    treasureCountFinal++;
                }
                else break;
            }
            counts[ZoneType.Treasure] = treasureCountFinal;
            
            int hallCount = 0;
            while (hallCount < 12)
            {
                if (CanReserve(reserved, hallCost, budgetPerType))
                {
                    AddCost(reserved, hallCost);
                    hallCount++;
                }
                else break;
            }
            hallCount = Mathf.Max(2, hallCount);
            counts[ZoneType.Hall] = hallCount;
            
            return counts;
        }
        
        private Dictionary<Room.RoomType, int> EstimateMinCostForZoneType(RecipeGrammar grammar, ZoneType zoneType)
        {
            Dictionary<Room.RoomType, int> minCost = null;
            int minTotal = int.MaxValue;
            
            foreach (var template in grammar.UserTemplates())
            {
                if (template.TargetZoneType != zoneType) continue;
                var cost = template.GetMinRoomCounts();
                int total = cost.Values.Sum();
                if (total < minTotal)
                {
                    minTotal = total;
                    minCost = cost;
                }
            }
            
            if (minCost == null)
            {
                minCost = new Dictionary<Room.RoomType, int> { { Room.RoomType.Fight, 1 } };
            }
            
            return minCost;
        }
        
        private string FormatCost(Dictionary<Room.RoomType, int> dict)
        {
            if (dict == null) return "null";
            var parts = dict.Select(kv => $"{kv.Key}={kv.Value}");
            return "{" + string.Join(",", parts) + "}";
        }

        private void AddCost(Dictionary<Room.RoomType, int> accumulator, Dictionary<Room.RoomType, int> cost)
        {
            foreach (var kv in cost)
            {
                if (!accumulator.ContainsKey(kv.Key)) accumulator[kv.Key] = 0;
                accumulator[kv.Key] += kv.Value;
            }
        }
        
        private bool CanReserve(Dictionary<Room.RoomType, int> currentlyReserved, 
                                Dictionary<Room.RoomType, int> additionalCost,
                                Dictionary<Room.RoomType, int> budget)
        {
            foreach (var kv in additionalCost)
            {
                int reserved = currentlyReserved.ContainsKey(kv.Key) ? currentlyReserved[kv.Key] : 0;
                int available = budget.ContainsKey(kv.Key) ? budget[kv.Key] : 0;
                if (reserved + kv.Value > available) return false;
            }
            return true;
        }
        
        private void AddCycleEdges(MacroGraph graph, int targetCycles, 
                                    Dictionary<ZoneType, (int min, int max)> interfaceLimits, 
                                    SeedManager seed)
        {
            var rng = seed.GetRng();
            int cyclesAdded = 0;
            int attempts = 0;
            const int maxAttempts = 50;
            
            while (cyclesAdded < targetCycles && attempts < maxAttempts)
            {
                attempts++;
                
                var a = graph.zones[rng.Next(graph.zones.Count)];
                var b = graph.zones[rng.Next(graph.zones.Count)];
                
                if (a == b) continue;
                if (graph.HasEdge(a, b)) continue;
                if (!AdjacencyRules.IsAllowed(a.type, b.type)) continue;
                if (!CanAddEdge(a, interfaceLimits)) continue;
                if (!CanAddEdge(b, interfaceLimits)) continue;
                
                int dist = TreeDistance(a, b);
                if (dist < 2) continue;
                
                graph.AddEdge(a, b, isCycleEdge: true);
                cyclesAdded++;
            }
        }
        
        private int TreeDistance(Zone from, Zone to)
        {
            var distances = new Dictionary<Zone, int>();
            var queue = new Queue<Zone>();
            distances[from] = 0;
            queue.Enqueue(from);
            
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == to) return distances[current];
                
                foreach (var neighbor in current.Neighbors())
                {
                    if (distances.ContainsKey(neighbor)) continue;
                    distances[neighbor] = distances[current] + 1;
                    queue.Enqueue(neighbor);
                }
            }
            return int.MaxValue;
        }
    }
}