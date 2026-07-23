using Level.Generation.Old;
using UnityEngine;

namespace Level.Generation.Corridors
{
    public class SegmentConnector
    {
        public Vector2Int cellPosition;
        public Door.Direction direction;
        public Vector2Int localTileOffset;
    }
}