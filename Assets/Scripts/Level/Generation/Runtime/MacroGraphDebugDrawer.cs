using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Level.Generation.Composites;
using Level.Generation.Corridors;
using Level.Generation.Graph;
using Level.Generation.Grammar;
using Level.Generation.Layout;
using Level.Generation.Support;

namespace Level.Generation.Runtime
{
    public class MacroGraphDebugDrawer : MonoBehaviour
    {
        public GenerationConfig config;
        
        private MacroGraph _graph;
        private AssembledLevel _assembled;
        private Dictionary<Zone, Vector2> _zonePositions = new Dictionary<Zone, Vector2>();
        private bool _expanded;
        
        [ContextMenu("Regenerate")]
        public void Regenerate()
        {
            if (config == null)
            {
                Debug.LogError("GenerationConfig is not set");
                return;
            }
            
            int seedValue = config.seed >= 0 ? config.seed : new System.Random().Next();
            var seed = new SeedManager(seedValue);
            
            var pools = new RoomPools();
            var budget = new Budget();
            foreach (var req in config.roomRequirements)
            {
                budget.SetInitial(req.type, req.maximumRequiredCount);
                if (req.prefabs != null)
                {
                    foreach (var prefab in req.prefabs)
                        pools.AddPrefab(prefab);
                }
            }

            var grammar = new RecipeGrammar();
            grammar.RegisterFromConfig(config.recipes);

            var builder = new MacroGraphBuilder();
            _graph = builder.Build(config.roomRequirements, grammar, seed);
            
            var expander = new ZoneExpander();
            _expanded = expander.Expand(_graph, grammar, budget, pools, seed);
            
            LayoutForDrawing();
            
            if (!_expanded)
            {
                Debug.LogError("Failed to expand zones");
                _assembled = null;
                return;
            }
            
            var segmentFootprints = new List<CorridorSegmentFootprint>();
            if (config.corridorSegmentPrefabs != null)
            {
                foreach (var prefab in config.corridorSegmentPrefabs)
                {
                    var fp = CorridorSegmentFootprint.FromPrefab(prefab);
                    if (fp != null) segmentFootprints.Add(fp);
                }
            }
            
            if (segmentFootprints.Count == 0)
            {
                Debug.LogError("No corridor segment prefabs configured");
                _assembled = null;
                return;
            }
            
            var assembler = new MacroAssembler();
            _assembled = assembler.Assemble(_graph, segmentFootprints, seed);
            
            if (_assembled == null)
            {
                Debug.LogError("Failed to assemble level");
                return;
            }
            
            PrintLevelSummary(seedValue);
        }
        
        private void PrintLevelSummary(int seed)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Level generated with seed: {seed}");
            sb.AppendLine($"Macrograph: {_graph.zones.Count} zones, {_graph.edges.Count} edges");
            
            if (_assembled != null)
            {
                sb.AppendLine($"Assembled: {_assembled.rooms.Count} rooms, {_assembled.corridors.Count} corridors");
                sb.AppendLine($"Bounds: {_assembled.worldBoundsMin} to {_assembled.worldBoundsMax}");
                
                int totalSegments = 0;
                foreach (var corr in _assembled.corridors) totalSegments += corr.segments.Count;
                sb.AppendLine($"Total corridor segments: {totalSegments}");
            }
            
            sb.AppendLine("Zones and composites:");
            foreach (var zone in _graph.zones)
            {
                sb.Append($"  {zone}");
                if (zone.composite != null)
                {
                    sb.Append($" -> composite (structure: {zone.composite.structureType}, {zone.composite.nodes.Count} rooms: ");
                    for (int i = 0; i < zone.composite.nodes.Count; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(zone.composite.nodes[i].type);
                    }
                    sb.Append($", {zone.composite.interfaceNodes.Count} interfaces)");
                }
                sb.AppendLine();
            }
            
            Debug.Log(sb.ToString());
        }
        
        private void LayoutForDrawing()
        {
            _zonePositions.Clear();
            if (_graph == null) return;
            
            _graph.ComputeCriticalPath();
            
            var criticalPath = new List<Zone>();
            var walker = _graph.entry;
            var visited = new HashSet<Zone>();
            
            while (walker != null && walker != _graph.boss)
            {
                criticalPath.Add(walker);
                visited.Add(walker);
                Zone next = null;
                foreach (var neighbor in walker.Neighbors())
                {
                    if (neighbor.isOnCriticalPath && !visited.Contains(neighbor))
                    {
                        next = neighbor;
                        break;
                    }
                }
                walker = next;
            }
            if (_graph.boss != null) criticalPath.Add(_graph.boss);
            
            for (int i = 0; i < criticalPath.Count; i++)
                _zonePositions[criticalPath[i]] = new Vector2(i * 4f, 0);
            
            int aboveCounter = 0;
            int belowCounter = 0;
            bool goAbove = true;
            
            foreach (var zone in _graph.zones)
            {
                if (_zonePositions.ContainsKey(zone)) continue;
                
                Zone host = null;
                foreach (var neighbor in zone.Neighbors())
                {
                    if (_zonePositions.ContainsKey(neighbor))
                    {
                        host = neighbor;
                        break;
                    }
                }
                if (host == null) continue;
                
                var hostPos = _zonePositions[host];
                float yOffset;
                if (goAbove)
                {
                    aboveCounter++;
                    yOffset = 3f * aboveCounter;
                }
                else
                {
                    belowCounter++;
                    yOffset = -3f * belowCounter;
                }
                goAbove = !goAbove;
                
                _zonePositions[zone] = new Vector2(hostPos.x, hostPos.y + yOffset);
            }
        }
        
        private void OnDrawGizmos()
        {
            if (_assembled != null)
            {
                DrawAssembledLevel();
                return;
            }
            
            if (_graph == null) return;
            
            foreach (var edge in _graph.edges)
            {
                if (!_zonePositions.TryGetValue(edge.a, out var posA)) continue;
                if (!_zonePositions.TryGetValue(edge.b, out var posB)) continue;
                
                Gizmos.color = edge.isCycleEdge ? Color.yellow : Color.white;
                Gizmos.DrawLine(new Vector3(posA.x, posA.y, 0), new Vector3(posB.x, posB.y, 0));
            }
            
            foreach (var zone in _graph.zones)
            {
                if (!_zonePositions.TryGetValue(zone, out var pos)) continue;
                
                Gizmos.color = ColorForZoneType(zone.type);
                Gizmos.DrawCube(new Vector3(pos.x, pos.y, 0), Vector3.one * 0.6f);
                
                if (zone.isOnCriticalPath)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireCube(new Vector3(pos.x, pos.y, 0), Vector3.one * 0.8f);
                }
            }
        }
        
        private void DrawAssembledLevel()
        {
            if (_assembled == null) return;
            
            foreach (var room in _assembled.rooms)
            {
                var pos = room.worldPosition;
                var size = room.footprint.size;
                Vector3 center = new Vector3(pos.x + size.x * 0.5f, pos.y + size.y * 0.5f, 0);
                Vector3 sizeVec = new Vector3(size.x, size.y, 0);
                
                Gizmos.color = ColorForRoomType(room.sourceNode.type);
                Gizmos.DrawCube(center, sizeVec);
                
                if (room.sourceNode.isInterface)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawWireCube(center, sizeVec * 1.02f);
                }
            }
            
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f);
            foreach (var corridor in _assembled.corridors)
            {
                for (int i = 0; i < corridor.pathTiles.Count - 1; i++)
                {
                    var a = corridor.pathTiles[i];
                    var b = corridor.pathTiles[i + 1];
                    Vector3 pa = new Vector3(a.x + 0.5f, a.y + 0.5f, 0);
                    Vector3 pb = new Vector3(b.x + 0.5f, b.y + 0.5f, 0);
                    Gizmos.DrawLine(pa, pb);
                }
            }
            
            Gizmos.color = new Color(0.3f, 0.7f, 0.3f, 0.4f);
            foreach (var corridor in _assembled.corridors)
            {
                foreach (var segment in corridor.segments)
                {
                    var pos = segment.worldPosition;
                    var size = segment.footprint.size;
                    Vector3 center = new Vector3(pos.x + size.x * 0.5f, pos.y + size.y * 0.5f, 0);
                    Vector3 sizeVec = new Vector3(size.x, size.y, 0);
                    Gizmos.DrawWireCube(center, sizeVec);
                }
            }
        }
        
        private Color ColorForZoneType(ZoneType type)
        {
            switch (type)
            {
                case ZoneType.Entry: return Color.green;
                case ZoneType.Boss: return Color.magenta;
                case ZoneType.Hall: return Color.gray;
                case ZoneType.Fight: return new Color(0.7f, 0.3f, 0.3f);
                case ZoneType.Treasure: return new Color(1f, 0.85f, 0.3f);
                case ZoneType.Shop: return new Color(0.3f, 0.7f, 1f);
                case ZoneType.SecretRoom: return new Color(0.5f, 0f, 0.5f);
                case ZoneType.RestArea: return new Color(0.4f, 1f, 0.4f);
                case ZoneType.NpcRoom: return new Color(1f, 0.5f, 0.8f);
                default: return Color.white;
            }
        }
        
        private Color ColorForRoomType(Room.RoomType type)
        {
            switch (type)
            {
                case Room.RoomType.Entry: return Color.green;
                case Room.RoomType.Fight: return new Color(0.7f, 0.3f, 0.3f);
                case Room.RoomType.Treasure: return new Color(1f, 0.85f, 0.3f);
                case Room.RoomType.Boss: return Color.magenta;
                case Room.RoomType.Exit: return new Color(0.3f, 0.3f, 1f);
                default: return Color.gray;
            }
        }
    }
}