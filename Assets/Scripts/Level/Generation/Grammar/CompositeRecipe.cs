using System.Collections.Generic;
using UnityEngine;
using Level.Generation.Composites;
using Level.Generation.Graph;

namespace Level.Generation.Grammar
{
    [CreateAssetMenu(fileName = "CompositeRecipe", menuName = "Level Generation/Composite Recipe", order = 10)]
    public class CompositeRecipe : ScriptableObject
    {
        [Header("Zone metadata")]
        [Tooltip("Which type of zone this recipe applies to")]
        public ZoneType targetZoneType = ZoneType.Hall;
        
        [Tooltip("Minimum interfaces this recipe supports")]
        public int minInterfaces = 2;
        
        [Tooltip("Maximum interfaces this recipe supports")]
        public int maxInterfaces = 2;
        
        [Tooltip("Higher weight = more likely to be picked among candidates")]
        public float selectionWeight = 1f;
        
        [Header("Structure hint")]
        [Tooltip("How to lay out this composite (must match the edges)")]
        public CompositeType structureType = CompositeType.Tree;
        
        [Header("Application conditions (optional)")]
        public bool onlyOnCriticalPath = false;
        public bool onlyOffCriticalPath = false;
        
        [Header("Nodes and edges")]
        [Tooltip("The rooms that will compose this composite")]
        public List<RecipeNode> nodes = new List<RecipeNode>();
        
        [Tooltip("The internal connections between rooms")]
        public List<RecipeEdge> edges = new List<RecipeEdge>();
    }
}