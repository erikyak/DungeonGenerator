using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Level.Generation.Composites;
using Level.Generation.Graph;
using Level.Generation.Support;

namespace Level.Generation.Grammar
{
    public class CompositeRecipeInterpreter : RecipeTemplate
    {
        private readonly CompositeRecipe _recipe;
        
        public CompositeRecipeInterpreter(CompositeRecipe recipe)
        {
            _recipe = recipe;
        }
        
        public ZoneType TargetZoneType => _recipe.targetZoneType;
        public float SelectionWeight => _recipe.selectionWeight;
        
        public bool SupportsInterfaceCount(int n)
        {
            return n >= _recipe.minInterfaces && n <= _recipe.maxInterfaces;
        }
        
        public bool CanApplyWithBudget(Budget budget, int remainingSameTypeZones)
        {
            var totalMin = new Dictionary<Room.RoomType, int>();
            foreach (var node in _recipe.nodes)
            {
                if (!totalMin.ContainsKey(node.roomType))
                    totalMin[node.roomType] = 0;
                totalMin[node.roomType] += node.minRepeat;
            }

            int denom = Mathf.Max(1, remainingSameTypeZones);
            foreach (var kv in totalMin)
            {
                int fairShare = budget.GetRemaining(kv.Key) / denom;
                if (kv.Value > fairShare) return false;
            }
            return true;
        }

        public Composite Apply(int desiredInterfaces, Budget budget, RoomPools pools, SeedManager seed, int remainingSameTypeZones)
        {
            var fairShareByType = ComputeFairShareByType(budget, remainingSameTypeZones);

            var finalRepeats = new Dictionary<int, int>();
            foreach (var node in _recipe.nodes)
            {
                int max = Mathf.Max(node.minRepeat, node.maxRepeat);
                int min = node.minRepeat;
                finalRepeats[node.id] = seed.NextInt(min, max + 1);
            }

            if (!AdjustRepeatsForFairShare(finalRepeats, fairShareByType)) return null;
            
            var composite = new Composite();
            composite.structureType = _recipe.structureType;
            
            var instantiation = new RecipeInstantiation();
            int nextLocalId = 0;
            
            foreach (var recipeNode in _recipe.nodes)
            {
                int count = finalRepeats[recipeNode.id];
                var instances = new List<CompositeNode>();
                
                for (int i = 0; i < count; i++)
                {
                    var node = new CompositeNode(nextLocalId++, recipeNode.roomType);
                    
                    var footprint = pools.PickAny(
                        recipeNode.roomType, 
                        recipeNode.minDoors, 
                        recipeNode.prefabTag, 
                        seed
                    );
                    if (footprint == null) return null;
                    
                    node.assignedFootprint = footprint;
                    composite.AddNode(node);
                    instances.Add(node);
                }
                
                instantiation.nodesByRecipeId[recipeNode.id] = instances;
            }
            
            foreach (var recipeNode in _recipe.nodes)
            {
                var instances = instantiation.nodesByRecipeId[recipeNode.id];
                for (int i = 0; i < instances.Count - 1; i++)
                {
                    composite.AddEdge(new CompositeEdge(instances[i], instances[i + 1]));
                }
            }
            
            foreach (var recipeEdge in _recipe.edges)
            {
                if (!instantiation.nodesByRecipeId.ContainsKey(recipeEdge.fromId)) return null;
                if (!instantiation.nodesByRecipeId.ContainsKey(recipeEdge.toId)) return null;
                
                var fromNode = instantiation.LastNode(recipeEdge.fromId);
                var toNode = instantiation.FirstNode(recipeEdge.toId);
                composite.AddEdge(new CompositeEdge(fromNode, toNode));
            }
            
            var interfaceCandidates = new List<CompositeNode>();
            foreach (var recipeNode in _recipe.nodes)
            {
                if (!recipeNode.canBeInterface) continue;
                var instances = instantiation.nodesByRecipeId[recipeNode.id];
                interfaceCandidates.Add(instances[0]);
                if (instances.Count > 1)
                    interfaceCandidates.Add(instances[instances.Count - 1]);
            }
            
            int targetInterfaces = Mathf.Min(desiredInterfaces, interfaceCandidates.Count);
            
            interfaceCandidates = interfaceCandidates
                .OrderBy(_ => seed.NextInt())
                .ToList();
            
            for (int i = 0; i < targetInterfaces; i++)
            {
                composite.MarkAsInterface(interfaceCandidates[i], i);
            }
            
            foreach (var node in composite.nodes)
            {
                budget.Consume(node.type, 1);
            }
            
            return composite;
        }
        
        private Dictionary<Room.RoomType, int> ComputeFairShareByType(Budget budget, int remainingSameTypeZones)
        {
            int denom = Mathf.Max(1, remainingSameTypeZones);
            var result = new Dictionary<Room.RoomType, int>();
            foreach (var node in _recipe.nodes)
            {
                if (result.ContainsKey(node.roomType)) continue;
                result[node.roomType] = budget.GetRemaining(node.roomType) / denom;
            }
            return result;
        }

        private bool AdjustRepeatsForFairShare(Dictionary<int, int> finalRepeats,
                                               Dictionary<Room.RoomType, int> fairShareByType)
        {
            var neededByType = new Dictionary<Room.RoomType, int>();
            foreach (var recipeNode in _recipe.nodes)
            {
                if (!neededByType.ContainsKey(recipeNode.roomType))
                    neededByType[recipeNode.roomType] = 0;
                neededByType[recipeNode.roomType] += finalRepeats[recipeNode.id];
            }

            foreach (var type in neededByType.Keys.ToList())
            {
                int cap = fairShareByType.TryGetValue(type, out var share) ? share : int.MaxValue;
                while (neededByType[type] > cap)
                {
                    RecipeNode toReduce = null;
                    foreach (var rn in _recipe.nodes)
                    {
                        if (rn.roomType != type) continue;
                        if (finalRepeats[rn.id] > rn.minRepeat)
                        {
                            toReduce = rn;
                            break;
                        }
                    }
                    if (toReduce == null) return false;

                    finalRepeats[toReduce.id]--;
                    neededByType[type]--;
                }
            }

            return true;
        }
        
        public Dictionary<Room.RoomType, int> GetMinRoomCounts()
        {
            var result = new Dictionary<Room.RoomType, int>();
            foreach (var node in _recipe.nodes)
            {
                if (!result.ContainsKey(node.roomType))
                    result[node.roomType] = 0;
                result[node.roomType] += node.minRepeat;
            }
            return result;
        }
        
        private class RecipeInstantiation
        {
            public Dictionary<int, List<CompositeNode>> nodesByRecipeId = new Dictionary<int, List<CompositeNode>>();
            
            public CompositeNode FirstNode(int recipeId) => nodesByRecipeId[recipeId][0];
            public CompositeNode LastNode(int recipeId) => nodesByRecipeId[recipeId][nodesByRecipeId[recipeId].Count - 1];
        }
    }
}