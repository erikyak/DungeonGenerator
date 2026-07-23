using System.Collections.Generic;

namespace Level.Generation.Graph
{
    public static class AdjacencyRules
    {
        private static readonly HashSet<ZoneType> TerminalTypes = new HashSet<ZoneType>
        {
            ZoneType.Fight, ZoneType.Treasure, ZoneType.Shop,
            ZoneType.SecretRoom, ZoneType.RestArea, ZoneType.NpcRoom
        };
        
        public static bool IsAllowed(ZoneType a, ZoneType b)
        {
            if (a == ZoneType.Null || b == ZoneType.Null) return false;
            
            if ((int)a > (int)b) (a, b) = (b, a);
            
            if (a == ZoneType.Entry) return b == ZoneType.Hall;
            if (b == ZoneType.Boss) return a == ZoneType.Hall;
            if (a == ZoneType.Hall || b == ZoneType.Hall) return true;
            
            return TerminalTypes.Contains(a) && TerminalTypes.Contains(b);
        }
    }
}