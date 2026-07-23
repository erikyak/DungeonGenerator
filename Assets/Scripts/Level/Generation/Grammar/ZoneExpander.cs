using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Level.Generation.Composites;
using Level.Generation.Graph;
using Level.Generation.Support;

namespace Level.Generation.Grammar
{
    public class ZoneExpander
    {
        private CompositeLayouter _layouter = new CompositeLayouter();
        
        public bool Expand(MacroGraph graph, RecipeGrammar grammar, Budget budget, 
                          RoomPools pools, SeedManager seed)
        {
            var expansionOrder = OrderZones(graph);

            for (int i = 0; i < expansionOrder.Count; i++)
            {
                var zone = expansionOrder[i];
                int remainingSameType = CountRemainingSameType(expansionOrder, i);

                var template = grammar.SelectTemplate(zone, budget, seed, remainingSameType);
                if (template == null)
                {
                    Debug.LogWarning($"No template found for zone {zone} (interfaces: {zone.InterfaceCount()})");
                    return false;
                }

                var composite = template.Apply(zone.InterfaceCount(), budget, pools, seed, remainingSameType);
                if (composite == null)
                {
                    Debug.LogWarning($"Template failed to apply for zone {zone}");
                    return false;
                }

                composite.parentZone = zone;
                zone.assignedRecipe = template;
                zone.composite = composite;
                
                if (!_layouter.Layout(composite, seed))
                {
                    Debug.LogWarning($"Failed to lay out composite for zone {zone}");
                    return false;
                }
            }
            
            return true;
        }
        
        private int CountRemainingSameType(List<Zone> order, int currentIndex)
        {
            int count = 1;
            var type = order[currentIndex].type;
            for (int j = currentIndex + 1; j < order.Count; j++)
                if (order[j].type == type) count++;
            return count;
        }

        private List<Zone> OrderZones(MacroGraph graph)
        {
            var priority = new Dictionary<ZoneType, int>
            {
                { ZoneType.Entry, 0 },
                { ZoneType.Boss, 1 },
                { ZoneType.Treasure, 2 },
                { ZoneType.Shop, 2 },
                { ZoneType.Fight, 3 },
                { ZoneType.SecretRoom, 3 },
                { ZoneType.RestArea, 3 },
                { ZoneType.NpcRoom, 3 },
                { ZoneType.Hall, 4 },
            };
            
            return graph.zones
                .OrderBy(z => priority.ContainsKey(z.type) ? priority[z.type] : 99)
                .ToList();
        }
    }
}