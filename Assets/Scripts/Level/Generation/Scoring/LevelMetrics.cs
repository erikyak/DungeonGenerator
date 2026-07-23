namespace Level.Generation.Scoring
{
    public struct LevelMetrics
    {
        public int roomCount;
        public int corridorCount;
        public int uniqueRoomTypes;
        public int totalRoomTiles;
        public int boundingBoxArea;
        public float compactness;
        public float avgCorridorLength;
        public int totalTurns;
        public bool hasTreasure;
        public bool hasRest;
    }
}
