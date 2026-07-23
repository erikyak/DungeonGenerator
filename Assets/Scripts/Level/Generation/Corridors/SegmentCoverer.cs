using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Level.Generation.Composites;
using Level.Generation.Old;
using Level.Generation.Support;

namespace Level.Generation.Corridors
{
    public class SegmentCoverer
    {
        public bool CoverCorridor(CorridorPath corridor,
                                   List<CorridorSegmentFootprint> availableSegments,
                                   LocalCollisionChecker globalChecker,
                                   SeedManager seed,
                                   bool checkRoomOverlap = true,
                                   bool checkCorridorOverlap = true)
        {
            var allowedRoomTiles = new HashSet<Vector2Int>();
            if (checkRoomOverlap)
            {
                if (corridor.sourceRoom != null)
                    AddRoomTiles(allowedRoomTiles, corridor.sourceRoom);
                if (corridor.targetRoom != null)
                    AddRoomTiles(allowedRoomTiles, corridor.targetRoom);
            }
            var allowedCorridorTiles = new HashSet<Vector2Int>(corridor.pathTiles);
            var mainLine = corridor.mainPath != null && corridor.mainPath.Count > 0
                ? corridor.mainPath
                : ExtractMainLine(corridor);
            if (mainLine.Count < 3) return true;

            var turnCovered = new HashSet<Vector2Int>();
            var mainLineCovered = new HashSet<int>();

            for (int i = 1; i < mainLine.Count - 2; i++)
            {
                if (mainLineCovered.Contains(i)) continue;
                var d0 = mainLine[i] - mainLine[i - 1];
                var d1 = mainLine[i + 1] - mainLine[i];
                if (d1 == d0) continue;
                int j = i + 1;
                bool bad = false;
                while (j < mainLine.Count - 1)
                {
                    var dj = mainLine[j + 1] - mainLine[j];
                    if (dj == d1) { j++; continue; }
                    if (dj == d0) break;
                    bad = true; break;
                }
                if (bad || j >= mainLine.Count - 1) continue;
                int shiftDist = j - i;
                if (shiftDist < 1 || shiftDist > 3) continue;

                for (int k = i; k <= j; k++)
                {
                    if (IsInsideRoom(mainLine[k], corridor.sourceRoom) || IsInsideRoom(mainLine[k], corridor.targetRoom))
                    {
                        bad = true; break;
                    }
                }
                if (bad) continue;

                var incomingDir = OffsetToDirection(d0);
                var perpSigned = d1;
                var reqIn = DoorSocket.Opposite(incomingDir);
                var shiftMatch = FindShiftSegment(availableSegments, reqIn, incomingDir, perpSigned, shiftDist, corridor.sourceDoor.CompanionOffset());
                if (shiftMatch == null) continue;

                var origin = mainLine[i - 1] - shiftMatch.inConn.localTileOffset;
                if (checkRoomOverlap && globalChecker.AreaHasRoomExcept(origin, shiftMatch.segment.size, allowedRoomTiles)) continue;
                if (checkCorridorOverlap && globalChecker.AreaHasCorridorExcept(origin, shiftMatch.segment.size, allowedCorridorTiles)) continue;

                corridor.segments.Add(new PlacedCorridorSegment(shiftMatch.segment, origin));
                for (int dx = 0; dx < shiftMatch.segment.size.x; dx++)
                    for (int dy = 0; dy < shiftMatch.segment.size.y; dy++)
                    {
                        var tile = origin + new Vector2Int(dx, dy);
                        turnCovered.Add(tile);
                        allowedCorridorTiles.Add(tile);
                        globalChecker.OccupyCorridor(tile);
                    }
                for (int k = i; k <= j; k++) mainLineCovered.Add(k);
            }

            for (int i = 1; i < mainLine.Count - 1; i++)
            {
                if (mainLineCovered.Contains(i)) continue;
                var prev = mainLine[i - 1];
                var curr = mainLine[i];
                var next = mainLine[i + 1];
                if (IsInsideRoom(curr, corridor.sourceRoom) || IsInsideRoom(curr, corridor.targetRoom)) continue;
                var incomingDir = OffsetToDirection(curr - prev);
                var outgoingDir = OffsetToDirection(next - curr);
                if (incomingDir == outgoingDir) continue;

                var requiredIn = DoorSocket.Opposite(incomingDir);
                var turnMatch = FindTurnSegment(availableSegments, requiredIn, outgoingDir, corridor.sourceDoor.CompanionOffset());
                if (turnMatch == null)
                {
                    Debug.LogWarning($"No matching turn segment for {incomingDir}->{outgoingDir}");
                    return false;
                }
                var origin = curr - turnMatch.insideCorner;
                if (checkRoomOverlap && globalChecker.AreaHasRoomExcept(origin, turnMatch.segment.size, allowedRoomTiles))
                {
                    Debug.LogWarning($"Turn segment at {origin} would overlap a room");
                    return false;
                }
                if (checkCorridorOverlap && globalChecker.AreaHasCorridorExcept(origin, turnMatch.segment.size, allowedCorridorTiles))
                {
                    Debug.LogWarning($"Turn segment at {origin} would overlap another corridor");
                    return false;
                }
                corridor.segments.Add(new PlacedCorridorSegment(turnMatch.segment, origin));
                for (int dx = 0; dx < turnMatch.segment.size.x; dx++)
                    for (int dy = 0; dy < turnMatch.segment.size.y; dy++)
                    {
                        var tile = origin + new Vector2Int(dx, dy);
                        turnCovered.Add(tile);
                        allowedCorridorTiles.Add(tile);
                        globalChecker.OccupyCorridor(tile);
                    }
            }

            for (int i = 1; i < mainLine.Count - 1; i++)
            {
                var prev = mainLine[i - 1];
                var curr = mainLine[i];
                var next = mainLine[i + 1];
                if (IsInsideRoom(curr, corridor.sourceRoom) || IsInsideRoom(curr, corridor.targetRoom)) continue;
                var incomingDir = OffsetToDirection(curr - prev);
                var outgoingDir = OffsetToDirection(next - curr);
                if (incomingDir != outgoingDir) continue;
                if (turnCovered.Contains(curr)) continue;

                var requiredIn = DoorSocket.Opposite(incomingDir);
                var localCompanion = LocalCompanion(outgoingDir, corridor.sourceDoor.CompanionOffset());
                var match = FindMatchingSegment(availableSegments, requiredIn, outgoingDir, localCompanion);
                if (match == null)
                {
                    Debug.LogWarning($"No matching segment for {incomingDir}->{outgoingDir}");
                    return false;
                }
                var origin = ComputeOrigin(match.segment, match.inConnector, curr);
                if (checkRoomOverlap && globalChecker.AreaHasRoomExcept(origin, match.segment.size, allowedRoomTiles))
                {
                    Debug.LogWarning($"Straight segment at {origin} would overlap a room");
                    return false;
                }
                if (checkCorridorOverlap && globalChecker.AreaHasCorridorExcept(origin, match.segment.size, allowedCorridorTiles))
                {
                    Debug.LogWarning($"Straight segment at {origin} would overlap another corridor");
                    return false;
                }
                corridor.segments.Add(new PlacedCorridorSegment(match.segment, origin));
                for (int dx = 0; dx < match.segment.size.x; dx++)
                    for (int dy = 0; dy < match.segment.size.y; dy++)
                    {
                        var tile = origin + new Vector2Int(dx, dy);
                        allowedCorridorTiles.Add(tile);
                        globalChecker.OccupyCorridor(tile);
                    }
            }

            return true;
        }

        private TurnMatch FindTurnSegment(List<CorridorSegmentFootprint> available, Door.Direction requiredIn, Door.Direction outgoingDir, Vector2Int sourceCompanion)
        {
            var inOff = DoorSocket.DirectionOffset(requiredIn);
            var outOff = DoorSocket.DirectionOffset(outgoingDir);
            var expectedTiles = ExpectedTurnCorridorTiles(inOff, outOff, sourceCompanion);
            var passageSet = new HashSet<Vector2Int>();

            TurnMatch best = null;
            int bestScore = -1;

            foreach (var seg in available)
            {
                var hasIn = seg.connectors.Any(c => c.direction == requiredIn);
                var hasOut = seg.connectors.Any(c => c.direction == outgoingDir);
                if (!hasIn || !hasOut) continue;

                passageSet.Clear();
                foreach (var p in seg.passageCells) passageSet.Add(p);

                foreach (var cell in seg.passageCells)
                {
                    if (!passageSet.Contains(cell + inOff)) continue;
                    if (!passageSet.Contains(cell + outOff)) continue;

                    int score = 0;
                    foreach (var expected in expectedTiles)
                        if (passageSet.Contains(cell + expected)) score++;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = new TurnMatch { segment = seg, insideCorner = cell };
                    }
                }
            }
            return best;
        }

        private List<Vector2Int> ExpectedTurnCorridorTiles(Vector2Int inOff, Vector2Int outOff, Vector2Int companion)
        {
            var tiles = new List<Vector2Int>
            {
                Vector2Int.zero,
                companion,
                inOff,
                inOff + companion,
                inOff * 2,
                inOff * 2 + companion,
                outOff,
                outOff * 2,
            };
            return tiles;
        }

        private class TurnMatch
        {
            public CorridorSegmentFootprint segment;
            public Vector2Int insideCorner;
        }

        private void AddRoomTiles(HashSet<Vector2Int> set, Level.Generation.Layout.PlacedRoom room)
        {
            var size = room.footprint.size;
            for (int dx = 0; dx < size.x; dx++)
                for (int dy = 0; dy < size.y; dy++)
                    set.Add(room.worldPosition + new Vector2Int(dx, dy));
        }

        private bool IsInsideRoom(Vector2Int tile, Level.Generation.Layout.PlacedRoom room, int inflate = 0)
        {
            if (room == null) return false;
            var min = room.worldPosition - Vector2Int.one * inflate;
            var max = room.worldPosition + room.footprint.size + Vector2Int.one * inflate;
            return tile.x >= min.x && tile.x < max.x && tile.y >= min.y && tile.y < max.y;
        }

        private Vector2Int LocalCompanion(Door.Direction movement, Vector2Int sourceCompanion)
        {
            bool movementHorizontal = movement == Door.Direction.East || movement == Door.Direction.West;
            bool sourceHorizontal = sourceCompanion.x == 0;
            if (movementHorizontal == sourceHorizontal) return sourceCompanion;
            return movementHorizontal ? new Vector2Int(0, 1) : new Vector2Int(1, 0);
        }
        
        private List<Vector2Int> ExtractMainLine(CorridorPath corridor)
        {
            var companionOffset = corridor.sourceDoor.CompanionOffset();
            var result = new List<Vector2Int>();
            var seen = new HashSet<Vector2Int>();
            
            foreach (var tile in corridor.pathTiles)
            {
                if (seen.Contains(tile)) continue;
                
                var companion = tile + companionOffset;
                if (seen.Contains(companion))
                {
                    seen.Add(tile);
                    continue;
                }
                
                result.Add(tile);
                seen.Add(tile);
                seen.Add(companion);
            }
            
            return result;
        }
        
        private Door.Direction OffsetToDirection(Vector2Int offset)
        {
            if (offset.x > 0) return Door.Direction.East;
            if (offset.x < 0) return Door.Direction.West;
            if (offset.y > 0) return Door.Direction.North;
            return Door.Direction.South;
        }
        
        private SegmentMatch FindMatchingSegment(
            List<CorridorSegmentFootprint> available,
            Door.Direction requiredIn, Door.Direction requiredOut,
            Vector2Int companionOffset)
        {
            foreach (var seg in available)
            {
                var outConn = seg.connectors.FirstOrDefault(c => c.direction == requiredOut);
                if (outConn == null) continue;

                SegmentConnector inConn = null;
                foreach (var c in seg.connectors)
                {
                    if (c.direction != requiredIn) continue;
                    if (!seg.passageCells.Contains(c.localTileOffset)) continue;
                    if (!seg.passageCells.Contains(c.localTileOffset + companionOffset)) continue;
                    inConn = c;
                    break;
                }
                if (inConn == null)
                {
                    foreach (var c in seg.connectors)
                    {
                        if (c.direction != requiredIn) continue;
                        if (!seg.passageCells.Contains(c.localTileOffset)) continue;
                        inConn = c;
                        break;
                    }
                }
                if (inConn == null) continue;

                return new SegmentMatch { segment = seg, inConnector = inConn, outConnector = outConn };
            }
            return null;
        }
        
        private Vector2Int ComputeOrigin(CorridorSegmentFootprint segment, 
                                          SegmentConnector inConnector, 
                                          Vector2Int targetTile)
        {
            return targetTile - inConnector.localTileOffset;
        }
        
        private class SegmentMatch
        {
            public CorridorSegmentFootprint segment;
            public SegmentConnector inConnector;
            public SegmentConnector outConnector;
        }

        private ShiftMatch FindShiftSegment(
            List<CorridorSegmentFootprint> available,
            Door.Direction requiredIn,
            Door.Direction outgoingDir,
            Vector2Int perpSigned,
            int perpShift,
            Vector2Int sourceCompanion)
        {
            if (DoorSocket.Opposite(requiredIn) != outgoingDir) return null;

            var localCompanion = LocalCompanion(outgoingDir, sourceCompanion);
            var travelAxis = DoorSocket.DirectionOffset(outgoingDir);
            bool travelHorizontal = travelAxis.x != 0;
            int expectedPerp = travelHorizontal ? perpSigned.y * perpShift : perpSigned.x * perpShift;

            foreach (var seg in available)
            {
                var passageSet = new HashSet<Vector2Int>(seg.passageCells);
                var inConns = seg.connectors.Where(c => c.direction == requiredIn).ToList();
                var outConns = seg.connectors.Where(c => c.direction == outgoingDir).ToList();
                if (inConns.Count == 0 || outConns.Count == 0) continue;

                foreach (var inC in inConns)
                {
                    if (!passageSet.Contains(inC.localTileOffset)) continue;
                    if (!passageSet.Contains(inC.localTileOffset + localCompanion)) continue;
                    foreach (var outC in outConns)
                    {
                        if (!passageSet.Contains(outC.localTileOffset)) continue;
                        if (!passageSet.Contains(outC.localTileOffset + localCompanion)) continue;

                        var delta = outC.localTileOffset - inC.localTileOffset;
                        int perpDelta = travelHorizontal ? delta.y : delta.x;
                        if (perpDelta != expectedPerp) continue;

                        int travelDelta = travelHorizontal ? delta.x : delta.y;
                        int travelSign = travelHorizontal ? travelAxis.x : travelAxis.y;
                        if (travelDelta * travelSign <= 0) continue;

                        return new ShiftMatch { segment = seg, inConn = inC, outConn = outC };
                    }
                }
            }
            return null;
        }

        private class ShiftMatch
        {
            public CorridorSegmentFootprint segment;
            public SegmentConnector inConn;
            public SegmentConnector outConn;
        }
    }
}