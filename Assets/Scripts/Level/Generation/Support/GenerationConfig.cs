using System.Collections.Generic;
using UnityEngine;
using Level.Rooms;
using Level.Generation.Grammar;
using Level.Generation.Old;

namespace Level.Generation.Support
{
    [CreateAssetMenu(fileName = "GenerationConfig", menuName = "Level Generation/Config", order = 1)]
    public class GenerationConfig : ScriptableObject
    {
        [Header("Requirements")]
        [Tooltip("Room type limits: minimums, maximums, available prefabs")]
        public List<RoomTypeRequirements> roomRequirements = new List<RoomTypeRequirements>();
        
        [Header("Recipes")]
        [Tooltip("All CompositeRecipe assets available for generation")]
        public List<CompositeRecipe> recipes = new List<CompositeRecipe>();
        
        [Header("Corridor segments")]
        [Tooltip("All CorridorSegment prefabs available for corridor construction")]
        public List<GameObject> corridorSegmentPrefabs = new List<GameObject>();
        
        [Header("Seed")]
        [Tooltip("-1 for random seed each generation, other for fixed reproducible")]
        public int seed = -1;
        
        [Header("Multi-attempt")]
        [Tooltip("How many generations attempts, best is selected. 1 for single attempt")]
        public int multiAttemptCount = 1;

        [Header("Retry on failure")]
        [Tooltip("If a generation attempt fails at any pipeline stage, retry with a different seed up to this many times")]
        public int maxRetries = 5;

        [Header("Zone injection")]
        [Range(0f, 1f)]
        [Tooltip("Chance to inject a SecretRoom zone after macrograph build")]
        public float secretRoomChance = 0.05f;

        [Range(0f, 1f)]
        [Tooltip("Chance to inject a RestArea zone after macrograph build")]
        public float restAreaChance = 0.15f;

        [Header("Corridor injection (Balanced policy)")]
        [Tooltip("Max short-loop shortcuts (between zones at graph distance 2)")]
        public int maxLoopEdges = 1;

        [Tooltip("Max medium shortcuts (between zones at graph distance 3-4)")]
        public int maxShortcutEdges = 1;

        [Tooltip("Max deep shortcuts (between zones at graph distance >=5)")]
        public int maxDeepShortcutEdges = 1;
    }
}