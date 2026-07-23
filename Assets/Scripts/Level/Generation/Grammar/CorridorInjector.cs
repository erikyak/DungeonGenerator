using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Level.Generation.Graph;
using Level.Generation.Support;

namespace Level.Generation.Grammar
{
    public class CorridorInjector
    {
        public int maxLoopEdges = 1;
        public int maxShortcutEdges = 1;
        public int maxDeepShortcutEdges = 1;

        public void Inject(MacroGraph graph, RecipeGrammar grammar, SeedManager seed)
        {
            var limits = ComputeInterfaceLimits(grammar);
            var byCategory = ClassifyCandidates(graph);

            InjectCategory(graph, byCategory, InjectionCategory.Loop, maxLoopEdges, seed, limits);
            InjectCategory(graph, byCategory, InjectionCategory.Shortcut, maxShortcutEdges, seed, limits);
            InjectCategory(graph, byCategory, InjectionCategory.DeepShortcut, maxDeepShortcutEdges, seed, limits);
        }

        private void InjectCategory(MacroGraph graph,
                                    Dictionary<InjectionCategory, List<(Zone a, Zone b)>> byCategory,
                                    InjectionCategory category, int maxCount, SeedManager seed,
                                    Dictionary<ZoneType, (int min, int max)> limits)
        {
            if (maxCount <= 0) return;
            if (!byCategory.TryGetValue(category, out var candidates) || candidates.Count == 0) return;

            var pool = candidates.OrderBy(_ => seed.NextInt()).ToList();
            int added = 0;
            foreach (var pair in pool)
            {
                if (added >= maxCount) break;
                if (graph.HasEdge(pair.a, pair.b)) continue;
                if (!AdjacencyRules.IsAllowed(pair.a.type, pair.b.type)) continue;
                if (!CanAddEdge(pair.a, limits) || !CanAddEdge(pair.b, limits)) continue;

                var edge = graph.AddEdge(pair.a, pair.b, isCycleEdge: true);
                edge.isInjected = true;
                edge.category = category;
                added++;
            }
        }

        private Dictionary<InjectionCategory, List<(Zone a, Zone b)>> ClassifyCandidates(MacroGraph graph)
        {
            var result = new Dictionary<InjectionCategory, List<(Zone, Zone)>>
            {
                { InjectionCategory.Loop, new List<(Zone, Zone)>() },
                { InjectionCategory.Shortcut, new List<(Zone, Zone)>() },
                { InjectionCategory.DeepShortcut, new List<(Zone, Zone)>() },
            };

            for (int i = 0; i < graph.zones.Count; i++)
            {
                var distances = Bfs(graph.zones[i]);
                for (int j = i + 1; j < graph.zones.Count; j++)
                {
                    var b = graph.zones[j];
                    if (!distances.TryGetValue(b, out var dist)) continue;
                    if (dist < 2) continue;
                    var category = CategoryForDistance(dist);
                    result[category].Add((graph.zones[i], b));
                }
            }
            return result;
        }

        private InjectionCategory CategoryForDistance(int dist)
        {
            if (dist == 2) return InjectionCategory.Loop;
            if (dist <= 4) return InjectionCategory.Shortcut;
            return InjectionCategory.DeepShortcut;
        }

        private Dictionary<Zone, int> Bfs(Zone start)
        {
            var distances = new Dictionary<Zone, int> { { start, 0 } };
            var queue = new Queue<Zone>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var neighbor in current.Neighbors())
                {
                    if (distances.ContainsKey(neighbor)) continue;
                    distances[neighbor] = distances[current] + 1;
                    queue.Enqueue(neighbor);
                }
            }
            return distances;
        }

        private bool CanAddEdge(Zone zone, Dictionary<ZoneType, (int min, int max)> limits)
        {
            if (!limits.TryGetValue(zone.type, out var lim)) return false;
            return zone.edges.Count < lim.max;
        }

        private Dictionary<ZoneType, (int min, int max)> ComputeInterfaceLimits(RecipeGrammar grammar)
        {
            var result = new Dictionary<ZoneType, (int, int)>();
            foreach (var template in grammar.UserTemplates())
            {
                int min = int.MaxValue, max = int.MinValue;
                for (int n = 1; n <= 8; n++)
                {
                    if (!template.SupportsInterfaceCount(n)) continue;
                    if (n < min) min = n;
                    if (n > max) max = n;
                }
                if (min == int.MaxValue) continue;
                if (result.TryGetValue(template.TargetZoneType, out var existing))
                    result[template.TargetZoneType] = (Mathf.Min(existing.Item1, min), Mathf.Max(existing.Item2, max));
                else
                    result[template.TargetZoneType] = (min, max);
            }
            return result;
        }
    }
}
