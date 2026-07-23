using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Level.Generation.Composites;
using Level.Generation.Corridors;
using Level.Generation.Graph;
using Level.Generation.Support;

namespace Level.Generation.Layout
{
    public class MacroAssembler
    {
        private const int MaxAssemblyAttempts = 15;
        private const int MaxIterations = 300;
        private const float RepulsionStrength = 2f;
        private const float AttractionStrength = 0.35f;
        private const float DesiredZoneDistance = 25f;
        private const float ZonePadding = 8f;
        private const float Damping = 0.3f;
        private const float ConvergenceThreshold = 0.5f;

        public AssembledLevel Assemble(MacroGraph graph,
                                        List<CorridorSegmentFootprint> corridorSegments,
                                        SeedManager seed)
        {
            for (int attempt = 0; attempt < MaxAssemblyAttempts; attempt++)
            {
                var doorSnapshot = SnapshotUsedDoors(graph);
                var result = TryAssemble(graph, corridorSegments, seed);
                if (result != null) return result;
                RestoreUsedDoors(doorSnapshot);
            }
            return null;
        }

        private Dictionary<CompositeNode, List<DoorSocket>> SnapshotUsedDoors(MacroGraph graph)
        {
            var snapshot = new Dictionary<CompositeNode, List<DoorSocket>>();
            foreach (var zone in graph.zones)
            {
                if (zone.composite == null) continue;
                foreach (var node in zone.composite.nodes)
                    snapshot[node] = new List<DoorSocket>(node.usedDoors);
            }
            return snapshot;
        }

        private void RestoreUsedDoors(Dictionary<CompositeNode, List<DoorSocket>> snapshot)
        {
            foreach (var kv in snapshot)
            {
                kv.Key.usedDoors.Clear();
                kv.Key.usedDoors.AddRange(kv.Value);
            }
        }

        private void NormalizeCompositeToIncludeCorridors(Composite composite)
        {
            Vector2Int min = new Vector2Int(int.MaxValue, int.MaxValue);
            foreach (var node in composite.nodes)
            {
                if (!node.position.HasValue) continue;
                var pos = node.position.Value;
                if (pos.x < min.x) min.x = pos.x;
                if (pos.y < min.y) min.y = pos.y;
            }
            foreach (var edge in composite.edges)
            {
                if (edge.corridorPath == null) continue;
                foreach (var tile in edge.corridorPath)
                {
                    if (tile.x < min.x) min.x = tile.x;
                    if (tile.y < min.y) min.y = tile.y;
                }
            }

            if (min.x == 0 && min.y == 0) return;
            if (min.x == int.MaxValue) return;

            var shift = -min;
            foreach (var node in composite.nodes)
            {
                if (node.position.HasValue)
                    node.position = node.position.Value + shift;
            }
            foreach (var edge in composite.edges)
            {
                if (edge.corridorPath != null)
                    for (int i = 0; i < edge.corridorPath.Count; i++)
                        edge.corridorPath[i] = edge.corridorPath[i] + shift;
                if (edge.mainPath != null)
                    for (int i = 0; i < edge.mainPath.Count; i++)
                        edge.mainPath[i] = edge.mainPath[i] + shift;
            }
        }
        
        private AssembledLevel TryAssemble(MacroGraph graph, 
                                            List<CorridorSegmentFootprint> corridorSegments, 
                                            SeedManager seed)
        {
            var zonePositions = ComputeZonePhysicalLayout(graph, seed);
            if (zonePositions == null) return null;

            var assembled = new AssembledLevel { graph = graph };
            var globalChecker = new LocalCollisionChecker();
            var roomsByZone = new Dictionary<Zone, List<PlacedRoom>>();
            var coverer = new SegmentCoverer();

            foreach (var zone in graph.zones)
            {
                if (zone.composite == null) return null;
                var zoneWorldOrigin = zonePositions[zone];
                var zoneRooms = new List<PlacedRoom>();

                foreach (var node in zone.composite.nodes)
                {
                    if (!node.position.HasValue) return null;
                    var worldPos = zoneWorldOrigin + node.position.Value;

                    if (!globalChecker.IsAreaFree(worldPos, node.assignedFootprint.size)) return null;
                    globalChecker.OccupyRoom(worldPos, node.assignedFootprint.size);

                    var placed = new PlacedRoom(node, worldPos);
                    assembled.AddRoom(placed);
                    zoneRooms.Add(placed);
                }
                
                // Zone-edge A* opens a temporary passable set for the current pair
                // inside FindPathBetweenDoors; global door marking would let A* walk
                // through unrelated rooms' door tiles.
                
                foreach (var compEdge in zone.composite.edges)
                {
                    if (compEdge.corridorPath == null || compEdge.corridorPath.Count == 0) continue;

                    var corridor = new CorridorPath();
                    corridor.sourceRoom = zoneRooms.FirstOrDefault(r => r.sourceNode == compEdge.a);
                    corridor.targetRoom = zoneRooms.FirstOrDefault(r => r.sourceNode == compEdge.b);
                    corridor.sourceDoor = compEdge.doorA;
                    corridor.targetDoor = compEdge.doorB;

                    foreach (var tile in compEdge.corridorPath)
                    {
                        var worldTile = zoneWorldOrigin + tile;
                        corridor.pathTiles.Add(worldTile);
                        globalChecker.OccupyCorridor(worldTile);
                    }
                    if (compEdge.mainPath != null)
                    {
                        foreach (var t in compEdge.mainPath)
                            corridor.mainPath.Add(zoneWorldOrigin + t);
                    }
                    
                    if (!coverer.CoverCorridor(corridor, corridorSegments, globalChecker, seed, checkRoomOverlap: false, checkCorridorOverlap: false))
                    {
                        Debug.LogWarning($"Failed to cover internal corridor with segments in zone {zone}");
                        return null;
                    }
                    
                    assembled.AddCorridor(corridor);
                }
                
                roomsByZone[zone] = zoneRooms;
            }
            
            if (!RouteZoneEdges(graph, assembled, roomsByZone, globalChecker, corridorSegments, coverer, seed)) 
                return null;
            
            return assembled;
        }
        
        private Dictionary<Zone, Vector2Int> ComputeZonePhysicalLayout(MacroGraph graph, SeedManager seed)
        {
            var zoneAABBs = new Dictionary<Zone, Vector2Int>();
            foreach (var zone in graph.zones)
            {
                if (zone.composite == null) return null;

                NormalizeCompositeToIncludeCorridors(zone.composite);

                Vector2Int max = new Vector2Int(int.MinValue, int.MinValue);
                foreach (var node in zone.composite.nodes)
                {
                    if (!node.position.HasValue) return null;
                    var pos = node.position.Value;
                    var size = node.assignedFootprint.size;
                    if (pos.x + size.x > max.x) max.x = pos.x + size.x;
                    if (pos.y + size.y > max.y) max.y = pos.y + size.y;
                }
                foreach (var edge in zone.composite.edges)
                {
                    if (edge.corridorPath == null) continue;
                    foreach (var tile in edge.corridorPath)
                    {
                        if (tile.x + 1 > max.x) max.x = tile.x + 1;
                        if (tile.y + 1 > max.y) max.y = tile.y + 1;
                    }
                }
                zoneAABBs[zone] = max;
            }
            
            var positions = new Dictionary<Zone, Vector2>();
            float avgSize = 0;
            foreach (var kv in zoneAABBs) avgSize += (kv.Value.x + kv.Value.y) * 0.5f;
            avgSize /= graph.zones.Count;
            
            float initRadius = Mathf.Sqrt(graph.zones.Count) * avgSize * 3f;
            foreach (var zone in graph.zones)
            {
                float angle = seed.NextFloat() * Mathf.PI * 2;
                float r = seed.NextFloat() * initRadius;
                positions[zone] = new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
            }
            
            for (int iter = 0; iter < MaxIterations; iter++)
            {
                var forces = new Dictionary<Zone, Vector2>();
                foreach (var zone in graph.zones) forces[zone] = Vector2.zero;
                
                for (int i = 0; i < graph.zones.Count; i++)
                {
                    for (int j = i + 1; j < graph.zones.Count; j++)
                    {
                        var zA = graph.zones[i];
                        var zB = graph.zones[j];
                        var sizeA = new Vector2(zoneAABBs[zA].x, zoneAABBs[zA].y) * 0.5f + Vector2.one * ZonePadding;
                        var sizeB = new Vector2(zoneAABBs[zB].x, zoneAABBs[zB].y) * 0.5f + Vector2.one * ZonePadding;
                        var delta = positions[zA] - positions[zB];
                        var absDelta = new Vector2(Mathf.Abs(delta.x), Mathf.Abs(delta.y));
                        var overlap = (sizeA + sizeB) - absDelta;
                        if (overlap.x > 0 && overlap.y > 0)
                        {
                            Vector2 dir;
                            float mag;
                            if (overlap.x < overlap.y) { dir = new Vector2(Mathf.Sign(delta.x), 0); mag = overlap.x; }
                            else { dir = new Vector2(0, Mathf.Sign(delta.y)); mag = overlap.y; }
                            if (dir.magnitude < 0.01f) dir = new Vector2(1, 0);
                            var f = dir * mag * RepulsionStrength;
                            forces[zA] += f;
                            forces[zB] -= f;
                        }
                    }
                }
                
                foreach (var edge in graph.edges)
                {
                    var delta = positions[edge.b] - positions[edge.a];
                    float dist = delta.magnitude;
                    if (dist < 0.01f) continue;
                    if (dist > DesiredZoneDistance)
                    {
                        var f = delta.normalized * (dist - DesiredZoneDistance) * AttractionStrength;
                        forces[edge.a] += f;
                        forces[edge.b] -= f;
                    }
                }
                
                float totalMovement = 0;
                foreach (var zone in graph.zones)
                {
                    var displacement = forces[zone] * Damping;
                    positions[zone] += displacement;
                    totalMovement += displacement.magnitude;
                }
                if (totalMovement < ConvergenceThreshold) break;
            }
            
            var result = new Dictionary<Zone, Vector2Int>();
            foreach (var zone in graph.zones)
            {
                var p = new Vector2Int(
                    Mathf.RoundToInt(positions[zone].x - zoneAABBs[zone].x * 0.5f),
                    Mathf.RoundToInt(positions[zone].y - zoneAABBs[zone].y * 0.5f)
                );
                result[zone] = p;
            }

            ResolveIntegerAABBOverlaps(graph, result, zoneAABBs);

            Vector2Int min2 = new Vector2Int(int.MaxValue, int.MaxValue);
            foreach (var zone in graph.zones)
            {
                if (result[zone].x < min2.x) min2.x = result[zone].x;
                if (result[zone].y < min2.y) min2.y = result[zone].y;
            }
            foreach (var zone in graph.zones)
                result[zone] = result[zone] - min2;

            return result;
        }

        private void ResolveIntegerAABBOverlaps(MacroGraph graph, Dictionary<Zone, Vector2Int> positions, Dictionary<Zone, Vector2Int> aabbs)
        {
            const int iterPerRound = 300;
            const int maxScaleRounds = 6;

            if (RunOverlapResolvePass(graph, positions, aabbs, iterPerRound)) return;

            for (int round = 0; round < maxScaleRounds; round++)
            {
                ScalePositions(graph, positions, 1.5f);
                if (RunOverlapResolvePass(graph, positions, aabbs, iterPerRound)) return;
            }
            Debug.LogWarning($"ResolveIntegerAABBOverlaps: still overlapping after {maxScaleRounds} scaling rounds");
        }

        private bool RunOverlapResolvePass(MacroGraph graph, Dictionary<Zone, Vector2Int> positions, Dictionary<Zone, Vector2Int> aabbs, int maxIter)
        {
            int pad = Mathf.CeilToInt(ZonePadding);
            for (int iter = 0; iter < maxIter; iter++)
            {
                bool anyOverlap = false;
                for (int i = 0; i < graph.zones.Count; i++)
                {
                    for (int j = i + 1; j < graph.zones.Count; j++)
                    {
                        var zA = graph.zones[i];
                        var zB = graph.zones[j];
                        var pA = positions[zA];
                        var pB = positions[zB];
                        var sA = aabbs[zA];
                        var sB = aabbs[zB];

                        int aMinX = pA.x - pad, aMaxX = pA.x + sA.x + pad;
                        int aMinY = pA.y - pad, aMaxY = pA.y + sA.y + pad;
                        int bMinX = pB.x - pad, bMaxX = pB.x + sB.x + pad;
                        int bMinY = pB.y - pad, bMaxY = pB.y + sB.y + pad;

                        int overlapX = Mathf.Min(aMaxX, bMaxX) - Mathf.Max(aMinX, bMinX);
                        int overlapY = Mathf.Min(aMaxY, bMaxY) - Mathf.Max(aMinY, bMinY);

                        if (overlapX <= 0 || overlapY <= 0) continue;
                        anyOverlap = true;

                        var pMove = positions[zB];
                        var pAnchor = positions[zA];

                        if (overlapX < overlapY)
                        {
                            int direction = pMove.x >= pAnchor.x ? 1 : -1;
                            positions[zB] = new Vector2Int(pMove.x + direction * overlapX, pMove.y);
                        }
                        else
                        {
                            int direction = pMove.y >= pAnchor.y ? 1 : -1;
                            positions[zB] = new Vector2Int(pMove.x, pMove.y + direction * overlapY);
                        }
                    }
                }
                if (!anyOverlap) return true;
            }
            return false;
        }

        private void ScalePositions(MacroGraph graph, Dictionary<Zone, Vector2Int> positions, float factor)
        {
            foreach (var zone in graph.zones)
            {
                var p = positions[zone];
                positions[zone] = new Vector2Int(Mathf.RoundToInt(p.x * factor), Mathf.RoundToInt(p.y * factor));
            }
        }
        
        private bool RouteZoneEdges(MacroGraph graph, AssembledLevel assembled,
                                    Dictionary<Zone, List<PlacedRoom>> roomsByZone,
                                    LocalCollisionChecker checker, 
                                    List<CorridorSegmentFootprint> corridorSegments,
                                    SegmentCoverer coverer,
                                    SeedManager seed)
        {
            foreach (var edge in graph.edges)
            {
                if (!RouteOneZoneEdge(edge, assembled, roomsByZone, checker, corridorSegments, coverer, seed))
                {
                    Debug.LogWarning($"Failed to route corridor for zone edge {edge}");
                    return false;
                }
            }
            return true;
        }
        
        private bool RouteOneZoneEdge(ZoneEdge edge, AssembledLevel assembled,
                                      Dictionary<Zone, List<PlacedRoom>> roomsByZone,
                                      LocalCollisionChecker checker,
                                      List<CorridorSegmentFootprint> corridorSegments,
                                      SegmentCoverer coverer,
                                      SeedManager seed)
        {
            var interfacesA = GetInterfaceRoomsAndFreeDoors(edge.a, roomsByZone[edge.a]);
            var interfacesB = GetInterfaceRoomsAndFreeDoors(edge.b, roomsByZone[edge.b]);

            var candidatePairs = new List<(PlacedRoom rA, DoorSocket dA, PlacedRoom rB, DoorSocket dB, int score)>();

            foreach (var iA in interfacesA)
            {
                foreach (var iB in interfacesB)
                {
                    var wA = iA.room.worldPosition + iA.door.primaryTile;
                    var wB = iB.room.worldPosition + iB.door.primaryTile;
                    int dist = Mathf.Abs(wA.x - wB.x) + Mathf.Abs(wA.y - wB.y);

                    var outA = DoorSocket.DirectionOffset(iA.door.direction);
                    var outB = DoorSocket.DirectionOffset(iB.door.direction);
                    var toB = wB - wA;
                    int dotA = outA.x * toB.x + outA.y * toB.y;
                    int dotB = outB.x * (-toB.x) + outB.y * (-toB.y);
                    int facingPenalty = 0;
                    if (dotA <= 0) facingPenalty += 1000;
                    if (dotB <= 0) facingPenalty += 1000;

                    int score = facingPenalty + dist;
                    candidatePairs.Add((iA.room, iA.door, iB.room, iB.door, score));
                }
            }

            candidatePairs.Sort((a, b) => a.score.CompareTo(b.score));

            var buffers = BuildForeignBuffers(assembled, roomsByZone, edge);

            foreach (var pair in candidatePairs)
            {
                var pathResult = FindPathBetweenDoors(pair.rA, pair.dA, pair.rB, pair.dB, checker, buffers);
                if (pathResult == null) continue;

                var corridor = new CorridorPath
                {
                    sourceRoom = pair.rA,
                    targetRoom = pair.rB,
                    sourceDoor = pair.dA,
                    targetDoor = pair.dB,
                    pathTiles = pathResult.full,
                    mainPath = pathResult.main
                };
                
                foreach (var tile in pathResult.full)
                {
                    checker.OccupyCorridor(tile);
                }
                
                if (!coverer.CoverCorridor(corridor, corridorSegments, checker, seed, checkRoomOverlap: true))
                {
                    Debug.LogWarning($"Failed to cover zone edge corridor with segments between {edge.a} and {edge.b}");
                    return false;
                }
                
                pair.rA.sourceNode.usedDoors.Add(pair.dA);
                pair.rB.sourceNode.usedDoors.Add(pair.dB);
                assembled.AddCorridor(corridor);
                return true;
            }
            return false;
        }

        private class ForeignBuffers
        {
            public HashSet<Vector2Int> blocked;
            public HashSet<Vector2Int> avoidTurn;
        }

        private ForeignBuffers BuildForeignBuffers(AssembledLevel assembled, Dictionary<Zone, List<PlacedRoom>> roomsByZone, ZoneEdge edge)
        {
            const int WallClearance = 2;
            const int TurnClearance = 4;
            const int CorridorTurnClearance = 3;
            var friendlyRooms = new HashSet<PlacedRoom>();
            if (roomsByZone.TryGetValue(edge.a, out var aRooms))
                foreach (var r in aRooms) friendlyRooms.Add(r);
            if (roomsByZone.TryGetValue(edge.b, out var bRooms))
                foreach (var r in bRooms) friendlyRooms.Add(r);

            var blocked = new HashSet<Vector2Int>();
            var avoidTurn = new HashSet<Vector2Int>();
            foreach (var room in assembled.rooms)
            {
                if (friendlyRooms.Contains(room)) continue;
                var size = room.footprint.size;
                for (int dx = -TurnClearance; dx < size.x + TurnClearance; dx++)
                    for (int dy = -TurnClearance; dy < size.y + TurnClearance; dy++)
                    {
                        var t = room.worldPosition + new Vector2Int(dx, dy);
                        avoidTurn.Add(t);
                        if (dx >= -WallClearance && dx < size.x + WallClearance &&
                            dy >= -WallClearance && dy < size.y + WallClearance)
                            blocked.Add(t);
                    }
            }

            foreach (var corridor in assembled.corridors)
            {
                if (corridor.pathTiles == null) continue;
                foreach (var tile in corridor.pathTiles)
                {
                    for (int dx = -CorridorTurnClearance; dx <= CorridorTurnClearance; dx++)
                        for (int dy = -CorridorTurnClearance; dy <= CorridorTurnClearance; dy++)
                            avoidTurn.Add(tile + new Vector2Int(dx, dy));
                }
            }

            return new ForeignBuffers { blocked = blocked, avoidTurn = avoidTurn };
        }

        private List<(PlacedRoom room, DoorSocket door)> GetInterfaceRoomsAndFreeDoors(Zone zone, List<PlacedRoom> zoneRooms)
        {
            var result = new List<(PlacedRoom, DoorSocket)>();
            foreach (var room in zoneRooms)
            {
                if (!room.sourceNode.isInterface) continue;
                foreach (var door in room.footprint.doors)
                {
                    if (!room.sourceNode.usedDoors.Contains(door))
                        result.Add((room, door));
                }
            }
            return result;
        }
        
        private class PathResult { public List<Vector2Int> full; public List<Vector2Int> main; }

        private PathResult FindPathBetweenDoors(PlacedRoom rA, DoorSocket dA, PlacedRoom rB, DoorSocket dB, LocalCollisionChecker checker, ForeignBuffers buffers)
        {
            var startPrimary = rA.worldPosition + dA.primaryTile;
            var startSecondary = rA.worldPosition + dA.secondaryTile;
            var endPrimary = rB.worldPosition + dB.primaryTile;
            var endSecondary = rB.worldPosition + dB.secondaryTile;
            var start = startPrimary + DoorSocket.DirectionOffset(dA.direction);
            var end = endPrimary + DoorSocket.DirectionOffset(dB.direction);
            var companionOffset = dA.CompanionOffset();

            var pairPassable = new HashSet<Vector2Int>
            {
                startPrimary, startSecondary,
                endPrimary, endSecondary,
                start, start + companionOffset,
                end, end + companionOffset,
            };

            bool TilePassable(Vector2Int t)
            {
                if (pairPassable.Contains(t)) return true;
                if (!checker.IsTilePassable(t)) return false;
                if (buffers != null && buffers.blocked.Contains(t)) return false;
                return true;
            }

            var openSet = new SortedSet<(int f, int x, int y)>();
            int h0 = Heuristic(start, end);
            openSet.Add((h0, start.x, start.y));
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, int> { { start, 0 } };
            var fScore = new Dictionary<Vector2Int, int> { { start, h0 } };

            int iterations = 0;
            const int maxIter = 20000;

            while (openSet.Count > 0)
            {
                iterations++;
                if (iterations > maxIter) return null;

                var head = openSet.Min;
                openSet.Remove(head);
                var current = new Vector2Int(head.x, head.y);
                if (head.f > fScore[current]) continue;
                
                if (current == end)
                {
                    var mainPath = new List<Vector2Int> { startPrimary };
                    var tempPath = new List<Vector2Int>();
                    var walker = end;
                    while (walker != start)
                    {
                        tempPath.Add(walker);
                        walker = cameFrom[walker];
                    }
                    tempPath.Add(start);
                    tempPath.Reverse();
                    mainPath.AddRange(tempPath);
                    mainPath.Add(endPrimary);
                    
                    var fullPath = new List<Vector2Int>();
                    var fullSet = new HashSet<Vector2Int>();
                    foreach (var t in mainPath) if (fullSet.Add(t)) fullPath.Add(t);
                    foreach (var t in mainPath)
                    {
                        var companion = t + companionOffset;
                        if (fullSet.Add(companion)) fullPath.Add(companion);
                    }
                    return new PathResult { full = fullPath, main = mainPath };
                }

                var neighbors = new[] {
                    current + new Vector2Int(1, 0), current + new Vector2Int(-1, 0),
                    current + new Vector2Int(0, 1), current + new Vector2Int(0, -1)
                };
                foreach (var n in neighbors)
                {
                    if (Mathf.Abs(n.x) > 10000 || Mathf.Abs(n.y) > 10000) continue;
                    if (!TilePassable(n)) continue;
                    if (!TilePassable(n + companionOffset)) continue;

                    int stepCost = 1;
                    if (cameFrom.TryGetValue(current, out var prevTile))
                    {
                        var prevDir = current - prevTile;
                        var newDir = n - current;
                        if (prevDir != newDir)
                        {
                            stepCost += 5;
                            if (buffers != null && buffers.avoidTurn.Contains(current))
                                stepCost += 2000;

                            var walker = prevTile;
                            var lastDir = prevDir;
                            for (int k = 0; k < 4; k++)
                            {
                                if (!cameFrom.TryGetValue(walker, out var grand)) break;
                                var d = walker - grand;
                                if (d != lastDir) { stepCost += 500; break; }
                                walker = grand;
                            }
                        }
                    }
                    int tentative = gScore[current] + stepCost;
                    if (!gScore.TryGetValue(n, out var existing) || tentative < existing)
                    {
                        cameFrom[n] = current;
                        gScore[n] = tentative;
                        int f = tentative + Heuristic(n, end);
                        fScore[n] = f;
                        openSet.Add((f, n.x, n.y));
                    }
                }
            }
            return null;
        }

        private int Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
    }
}