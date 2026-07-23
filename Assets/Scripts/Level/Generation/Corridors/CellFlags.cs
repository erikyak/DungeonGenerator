using System;

namespace Level.Generation.Corridors
{
    [Flags]
    public enum CellFlags
    {
        Empty       = 0,
        Passage     = 1,
        ConnectorN  = 2,
        ConnectorS  = 4,
        ConnectorE  = 8,
        ConnectorW  = 16,
    }
}