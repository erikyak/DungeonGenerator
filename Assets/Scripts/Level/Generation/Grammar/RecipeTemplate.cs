using System.Collections.Generic;
using Level.Generation.Composites;
using Level.Generation.Graph;
using Level.Generation.Support;

namespace Level.Generation.Grammar
{
    public interface RecipeTemplate
    {
        ZoneType TargetZoneType { get; }
        float SelectionWeight { get; }
        bool SupportsInterfaceCount(int n);
        bool CanApplyWithBudget(Budget budget, int remainingSameTypeZones);
        Composite Apply(int desiredInterfaces, Budget budget, RoomPools pools, SeedManager seed, int remainingSameTypeZones);
        Dictionary<Room.RoomType, int> GetMinRoomCounts();
    }
}