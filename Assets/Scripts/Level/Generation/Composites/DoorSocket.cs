using Level.Generation.Old;
using UnityEngine;

namespace Level.Generation.Composites
{
    public class DoorSocket
    {
        public Vector2Int primaryTile;
        public Vector2Int secondaryTile;
        public Door.Direction direction;
        public int prefabDoorIndex;
        
        public DoorSocket(Vector2Int primaryTile, Vector2Int secondaryTile, Door.Direction direction, int prefabDoorIndex)
        {
            this.primaryTile = primaryTile;
            this.secondaryTile = secondaryTile;
            this.direction = direction;
            this.prefabDoorIndex = prefabDoorIndex;
        }
        
        public Vector2Int OutsidePrimaryTile()
        {
            return primaryTile + DirectionOffset(direction);
        }
        
        public Vector2Int OutsideSecondaryTile()
        {
            return secondaryTile + DirectionOffset(direction);
        }
        
        public Vector2Int CompanionOffset()
        {
            return secondaryTile - primaryTile;
        }
        
        public static Vector2Int DirectionOffset(Door.Direction dir)
        {
            switch (dir)
            {
                case Door.Direction.North: return new Vector2Int(0, 1);
                case Door.Direction.South: return new Vector2Int(0, -1);
                case Door.Direction.East:  return new Vector2Int(1, 0);
                case Door.Direction.West:  return new Vector2Int(-1, 0);
                default: return Vector2Int.zero;
            }
        }
        
        public static Door.Direction Opposite(Door.Direction dir)
        {
            switch (dir)
            {
                case Door.Direction.North: return Door.Direction.South;
                case Door.Direction.South: return Door.Direction.North;
                case Door.Direction.East:  return Door.Direction.West;
                case Door.Direction.West:  return Door.Direction.East;
                default: return dir;
            }
        }
        
        public override string ToString() => $"Door[{primaryTile}+{secondaryTile}, {direction}, idx={prefabDoorIndex}]";
    }
}