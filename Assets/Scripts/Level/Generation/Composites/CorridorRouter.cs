using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Level.Generation.Support;

namespace Level.Generation.Composites
{
    public class CorridorRouter
    {
        private const int MaxPathTiles = 400;
        private const int DoorApproachTiles = 1;
        
        public bool RouteAllCorridors(Composite composite, LocalCollisionChecker checker, SeedManager seed)
        {
            foreach (var node in composite.nodes)
            {
                foreach (var door in node.assignedFootprint.doors)
                {
                    checker.MarkDoorSocket(door, node.position.Value);
                }
            }
            
            foreach (var edge in composite.edges)
            {
                if (!RouteOneCorridor(edge, checker, seed)) return false;
            }
            return true;
        }
        
        private bool RouteOneCorridor(CompositeEdge edge, LocalCollisionChecker checker, SeedManager seed)
        {
            var doorPairs = GenerateDoorPairs(edge);

            foreach (var pair in doorPairs)
            {
                var result = FindPath(edge.a, pair.doorA, edge.b, pair.doorB, checker);
                if (result == null) continue;

                edge.doorA = pair.doorA;
                edge.doorB = pair.doorB;
                edge.corridorPath = result.full;
                edge.mainPath = result.main;
                edge.a.usedDoors.Add(pair.doorA);
                edge.b.usedDoors.Add(pair.doorB);

                foreach (var tile in result.full)
                {
                    checker.OccupyCorridor(tile);
                    checker.OccupyCorridor(tile + new Vector2Int(1, 0));
                    checker.OccupyCorridor(tile + new Vector2Int(-1, 0));
                    checker.OccupyCorridor(tile + new Vector2Int(0, 1));
                    checker.OccupyCorridor(tile + new Vector2Int(0, -1));
                }
                return true;
            }
            return false;
        }

        private class PathResult
        {
            public List<Vector2Int> full;
            public List<Vector2Int> main;
        }
        
        private List<DoorPair> GenerateDoorPairs(CompositeEdge edge)
        {
            var pairs = new List<DoorPair>();
            var freeDoorsA = edge.a.assignedFootprint.doors.Where(d => !edge.a.usedDoors.Contains(d)).ToList();
            var freeDoorsB = edge.b.assignedFootprint.doors.Where(d => !edge.b.usedDoors.Contains(d)).ToList();
            
            foreach (var dA in freeDoorsA)
            {
                foreach (var dB in freeDoorsB)
                {
                    var wA = edge.a.position.Value + dA.primaryTile;
                    var wB = edge.b.position.Value + dB.primaryTile;
                    int dist = Mathf.Abs(wA.x - wB.x) + Mathf.Abs(wA.y - wB.y);
                    pairs.Add(new DoorPair { doorA = dA, doorB = dB, score = dist });
                }
            }
            
            return pairs.OrderBy(p => p.score).ToList();
        }
        
        private PathResult FindPath(CompositeNode nodeA, DoorSocket doorA,
                                          CompositeNode nodeB, DoorSocket doorB,
                                          LocalCollisionChecker checker)
        {
            var startPrimary = nodeA.position.Value + doorA.primaryTile;
            var endPrimary = nodeB.position.Value + doorB.primaryTile;

            var startDir = DoorSocket.DirectionOffset(doorA.direction);
            var endDir = DoorSocket.DirectionOffset(doorB.direction);

            var companionOffset = doorA.CompanionOffset();

            var approachStart = new List<Vector2Int>();
            for (int k = 1; k <= DoorApproachTiles; k++)
            {
                var t = startPrimary + startDir * k;
                if (!checker.IsTilePassable(t)) return null;
                if (!checker.IsTilePassable(t + companionOffset)) return null;
                approachStart.Add(t);
            }
            var approachEnd = new List<Vector2Int>();
            for (int k = DoorApproachTiles; k >= 1; k--)
            {
                var t = endPrimary + endDir * k;
                if (!checker.IsTilePassable(t)) return null;
                if (!checker.IsTilePassable(t + companionOffset)) return null;
                approachEnd.Add(t);
            }

            var astarStart = approachStart[approachStart.Count - 1];
            var astarEnd = approachEnd[0];

            if (astarStart == astarEnd)
            {
                var mainSame = new List<Vector2Int> { startPrimary };
                mainSame.AddRange(approachStart);
                for (int i = 1; i < approachEnd.Count; i++) mainSame.Add(approachEnd[i]);
                mainSame.Add(endPrimary);
                return BuildResult(mainSame, companionOffset);
            }

            var openSet = new List<Vector2Int> { astarStart };
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, int> { { astarStart, 0 } };
            var fScore = new Dictionary<Vector2Int, int> { { astarStart, Heuristic(astarStart, astarEnd) } };

            int iterations = 0;

            while (openSet.Count > 0)
            {
                iterations++;
                if (iterations > MaxPathTiles * 10) return null;

                openSet.Sort((a, b) => fScore[a].CompareTo(fScore[b]));
                var current = openSet[0];

                if (current == astarEnd)
                {
                    var walker = astarEnd;
                    var tempPath = new List<Vector2Int>();
                    while (walker != astarStart)
                    {
                        tempPath.Add(walker);
                        walker = cameFrom[walker];
                    }
                    tempPath.Reverse();

                    var mainPath = new List<Vector2Int> { startPrimary };
                    mainPath.AddRange(approachStart);
                    mainPath.AddRange(tempPath);
                    for (int i = 1; i < approachEnd.Count; i++) mainPath.Add(approachEnd[i]);
                    mainPath.Add(endPrimary);

                    return BuildResult(mainPath, companionOffset);
                }

                openSet.RemoveAt(0);

                var neighbors = new[] {
                    current + new Vector2Int(1, 0),
                    current + new Vector2Int(-1, 0),
                    current + new Vector2Int(0, 1),
                    current + new Vector2Int(0, -1)
                };

                foreach (var neighbor in neighbors)
                {
                    if (!checker.IsTilePassable(neighbor)) continue;
                    if (!checker.IsTilePassable(neighbor + companionOffset)) continue;
                    if (Mathf.Abs(neighbor.x) > 10000 || Mathf.Abs(neighbor.y) > 10000) continue;

                    int tentative = gScore[current] + 1;
                    if (!gScore.ContainsKey(neighbor) || tentative < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentative;
                        fScore[neighbor] = tentative + Heuristic(neighbor, astarEnd);
                        if (!openSet.Contains(neighbor)) openSet.Add(neighbor);
                    }
                }
            }
            return null;
        }

        private PathResult BuildResult(List<Vector2Int> mainPath, Vector2Int companionOffset)
        {
            var fullPath = new List<Vector2Int>();
            var seen = new HashSet<Vector2Int>();
            foreach (var t in mainPath) if (seen.Add(t)) fullPath.Add(t);
            foreach (var t in mainPath)
            {
                var companion = t + companionOffset;
                if (seen.Add(companion)) fullPath.Add(companion);
            }
            if (fullPath.Count > MaxPathTiles) return null;
            return new PathResult { full = fullPath, main = mainPath };
        }
        
        private int Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
        
        private class DoorPair
        {
            public DoorSocket doorA;
            public DoorSocket doorB;
            public int score;
        }
    }
}