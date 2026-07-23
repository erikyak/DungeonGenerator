using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Level.Generation.Composites;
using Level.Generation.Corridors;
using Level.Generation.Graph;
using Level.Generation.Grammar;
using Level.Generation.Layout;
using Level.Generation.Runtime;
using Level.Generation.Scoring;
using Level.Generation.Support;
using Level.Minimap;
using Scripts;

namespace Level.Generation
{
    public class DungeonGenerator : MonoBehaviour
    {
        [Header("Config")]
        public GenerationConfig config;
        
        [Header("Player")]
        public GameObject playerPrefab;
        
        [Header("Debug")]
        public KeyCode regenerateKey = KeyCode.R;
        public bool generateOnStart = true;
        
        private GameObject _currentLevelRoot;
        private GameObject _currentPlayer;
        private AssembledLevel _lastAssembled;
        private MinimapController _minimap;
        private PlayerMove _inputController;

        private void Start()
        {
            if (generateOnStart) Generate();
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(regenerateKey)) Regenerate();
        }

        public void Generate()
        {
            if (config == null)
            {
                Debug.LogError("GenerationConfig is not set");
                return;
            }

            var segmentFootprints = new List<CorridorSegmentFootprint>();
            if (config.corridorSegmentPrefabs != null)
                foreach (var prefab in config.corridorSegmentPrefabs)
                {
                    var fp = CorridorSegmentFootprint.FromPrefab(prefab);
                    if (fp != null) segmentFootprints.Add(fp);
                }
            if (segmentFootprints.Count == 0)
            {
                Debug.LogError("No corridor segment prefabs configured");
                return;
            }

            int maxRetries = Mathf.Max(1, config.maxRetries);
            var rng = new System.Random();

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                int seedValue = config.seed >= 0 ? config.seed + attempt : rng.Next();
                var assembled = TryGenerateOnce(seedValue, segmentFootprints, out string failureStage);
                if (assembled == null)
                {
                    Debug.LogWarning($"Attempt {attempt + 1}/{maxRetries} failed at {failureStage} (seed {seedValue})");
                    continue;
                }

                _lastAssembled = assembled;

                var instantiator = new LevelInstantiator();
                _currentLevelRoot = instantiator.Instantiate(assembled, transform);

                SpawnPlayer(assembled);
                RescanPathfinding();
                SetupMinimap(assembled, instantiator);

                Debug.Log($"Level generated with seed {seedValue} on attempt {attempt + 1}. Rooms: {assembled.rooms.Count}, corridors: {assembled.corridors.Count}");
                return;
            }

            Debug.LogError($"Failed to generate level after {maxRetries} attempts");
        }

        private AssembledLevel TryGenerateOnce(int seedValue, List<CorridorSegmentFootprint> segmentFootprints, out string failureStage)
        {
            failureStage = "";
            var seed = new SeedManager(seedValue);

            var pools = new RoomPools();
            var budget = new Budget();
            foreach (var req in config.roomRequirements)
            {
                budget.SetInitial(req.type, req.maximumRequiredCount);
                if (req.prefabs != null)
                    foreach (var prefab in req.prefabs)
                        pools.AddPrefab(prefab);
            }

            var grammar = new RecipeGrammar();
            grammar.RegisterFromConfig(config.recipes);

            var builder = new MacroGraphBuilder();
            var graph = builder.Build(config.roomRequirements, grammar, seed);
            if (graph == null) { failureStage = "MacroGraphBuilder"; return null; }

            var zoneInjector = new ZoneInjector
            {
                secretRoomChance = config.secretRoomChance,
                restAreaChance = config.restAreaChance
            };
            zoneInjector.Inject(graph, grammar, seed);

            var corridorInjector = new CorridorInjector
            {
                maxLoopEdges = config.maxLoopEdges,
                maxShortcutEdges = config.maxShortcutEdges,
                maxDeepShortcutEdges = config.maxDeepShortcutEdges
            };
            corridorInjector.Inject(graph, grammar, seed);

            var expander = new ZoneExpander();
            if (!expander.Expand(graph, grammar, budget, pools, seed)) { failureStage = "ZoneExpander"; return null; }

            var assembler = new MacroAssembler();
            var assembled = assembler.Assemble(graph, segmentFootprints, seed);
            if (assembled == null) { failureStage = "MacroAssembler"; return null; }

            return assembled;
        }

        public void Regenerate()
        {
            if (_currentLevelRoot != null)
            {
                Destroy(_currentLevelRoot);
                _currentLevelRoot = null;
            }
            if (_currentPlayer != null)
            {
                Destroy(_currentPlayer);
                _currentPlayer = null;
            }
            if (_minimap != null)
            {
                Destroy(_minimap.gameObject);
                _minimap = null;
            }

            StartCoroutine(RegenerateAfterFrame());
        }

        private void SetupMinimap(AssembledLevel level, LevelInstantiator instantiator)
        {
            var minimapGo = new GameObject("Minimap");
            _minimap = minimapGo.AddComponent<MinimapController>();
            _minimap.Initialize(level, _currentPlayer != null ? _currentPlayer.transform : null);
            instantiator.OnRoomEntered += _minimap.MarkZoneExplored;
        }
        
        private IEnumerator RegenerateAfterFrame()
        {
            yield return null;
            Generate();
        }
        
        private void SpawnPlayer(AssembledLevel level)
        {
            if (playerPrefab == null) return;
            
            PlacedRoom entryRoom = null;
            foreach (var room in level.rooms)
            {
                if (room.sourceNode.type == Room.RoomType.Entry)
                {
                    entryRoom = room;
                    break;
                }
            }
            
            if (entryRoom == null)
            {
                Debug.LogWarning("No Entry room found for player spawn");
                return;
            }
            
            Vector3 spawnPos = new Vector3(
                entryRoom.worldPosition.x + entryRoom.footprint.size.x * 0.5f,
                entryRoom.worldPosition.y + entryRoom.footprint.size.y * 0.5f,
                0
            );
            
            _currentPlayer = Object.Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        }
        
        private void RescanPathfinding()
        {
            var astarType = System.Type.GetType("Pathfinding.AstarPath, AstarPathfindingProject");
            if (astarType == null) return;
            
            var activeProperty = astarType.GetProperty("active", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (activeProperty == null) return;
            
            var activeInstance = activeProperty.GetValue(null);
            if (activeInstance == null) return;
            
            var scanMethod = astarType.GetMethod("Scan", new System.Type[0]);
            scanMethod?.Invoke(activeInstance, null);
        }
    }
}