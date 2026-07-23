using UnityEngine;

namespace Level.Generation.Corridors
{
    public class PlacedCorridorSegment
    {
        public CorridorSegmentFootprint footprint;
        public Vector2Int worldPosition;
        
        public PlacedCorridorSegment(CorridorSegmentFootprint footprint, Vector2Int worldPosition)
        {
            this.footprint = footprint;
            this.worldPosition = worldPosition;
        }
    }
}