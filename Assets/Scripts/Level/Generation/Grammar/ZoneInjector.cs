using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Level.Generation.Graph;
using Level.Generation.Support;

namespace Level.Generation.Grammar
{
    public class ZoneInjector
    {
        public float secretRoomChance = 0.05f;
        public float restAreaChance = 0.15f;

        public void Inject(MacroGraph graph, RecipeGrammar grammar, SeedManager seed)
        {
            var limits = ComputeInterfaceLimits(grammar);
            TryInject(graph, ZoneType.SecretRoom, secretRoomChance, seed, limits);
            TryInject(graph, ZoneType.RestArea, restAreaChance, seed, limits);
        }

        private void TryInject(MacroGraph graph, ZoneType type, float chance, SeedManager seed,
                               Dictionary<ZoneType, (int min, int max)> limits)
        {
            if (seed.NextFloat() > chance) return;
            if (!limits.ContainsKey(type)) return;

            var candidates = graph.zones
                .Where(z => z.type == ZoneType.Hall && CanAddEdge(z, limits))
                .ToList();
            if (candidates.Count == 0) return;

            var host = candidates[seed.NextInt(candidates.Count)];
            var newZone = graph.AddZone(type);
            newZone.isInjected = true;
            graph.AddEdge(host, newZone);
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
