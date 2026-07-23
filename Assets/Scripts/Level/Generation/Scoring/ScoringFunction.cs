using System.Collections.Generic;
using UnityEngine;
using Level.Generation.Corridors;
using Level.Generation.Graph;
using Level.Generation.Layout;

namespace Level.Generation.Scoring
{
    public class ScoringFunction
    {
        public float compactnessWeight = 100f;
        public float avgCorridorLengthWeight = 2f;
        public float diversityBonusPerType = 15f;
        public float turnsPenalty = 3f;
        public float treasureBonus = 25f;
        public float restBonus = 25f;

        public LevelMetrics ComputeMetrics(AssembledLevel level)
        {
            var metrics = new LevelMetrics();
            if (level == null || level.rooms == null || level.rooms.Count == 0) return metrics;

            metrics.roomCount = level.rooms.Count;
            metrics.corridorCount = level.corridors == null ? 0 : level.corridors.Count;

            var typesSeen = new HashSet<Room.RoomType>();
            var min = new Vector2Int(int.MaxValue, int.MaxValue);
            var max = new Vector2Int(int.MinValue, int.MinValue);
            int totalRoomTiles = 0;

            foreach (var room in level.rooms)
            {
                var pos = room.worldPosition;
                var size = room.footprint.size;
                totalRoomTiles += size.x * size.y;
                if (pos.x < min.x) min.x = pos.x;
                if (pos.y < min.y) min.y = pos.y;
                if (pos.x + size.x > max.x) max.x = pos.x + size.x;
                if (pos.y + size.y > max.y) max.y = pos.y + size.y;
                typesSeen.Add(room.sourceNode.type);
            }

            metrics.totalRoomTiles = totalRoomTiles;
            metrics.boundingBoxArea = Mathf.Max(1, (max.x - min.x) * (max.y - min.y));
            metrics.compactness = (float)totalRoomTiles / metrics.boundingBoxArea;
            metrics.uniqueRoomTypes = typesSeen.Count;
            metrics.hasTreasure = typesSeen.Contains(Room.RoomType.Treasure);
            metrics.hasRest = false;
            if (level.graph != null && level.graph.zones != null)
            {
                foreach (var zone in level.graph.zones)
                    if (zone.type == ZoneType.RestArea) { metrics.hasRest = true; break; }
            }

            int corridorTileTotal = 0;
            int turns = 0;
            if (level.corridors != null)
            {
                foreach (var corridor in level.corridors)
                {
                    var path = corridor.mainPath != null && corridor.mainPath.Count > 0 ? corridor.mainPath : corridor.pathTiles;
                    if (path == null || path.Count < 2) continue;
                    corridorTileTotal += path.Count;
                    turns += CountTurns(path);
                }
            }
            metrics.avgCorridorLength = metrics.corridorCount == 0 ? 0f : (float)corridorTileTotal / metrics.corridorCount;
            metrics.totalTurns = turns;

            return metrics;
        }

        public float Score(AssembledLevel level)
        {
            return Score(ComputeMetrics(level));
        }

        public float Score(LevelMetrics metrics)
        {
            float score = 0f;
            score += metrics.compactness * compactnessWeight;
            score -= metrics.avgCorridorLength * avgCorridorLengthWeight;
            score += metrics.uniqueRoomTypes * diversityBonusPerType;
            score -= metrics.totalTurns * turnsPenalty;
            if (metrics.hasTreasure) score += treasureBonus;
            if (metrics.hasRest) score += restBonus;
            return score;
        }

        private int CountTurns(List<Vector2Int> path)
        {
            int turns = 0;
            for (int i = 1; i < path.Count - 1; i++)
            {
                var prevDir = path[i] - path[i - 1];
                var nextDir = path[i + 1] - path[i];
                if (prevDir != nextDir) turns++;
            }
            return turns;
        }
    }
}
