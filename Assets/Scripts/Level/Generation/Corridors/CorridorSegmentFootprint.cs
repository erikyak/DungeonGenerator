using System.Collections.Generic;
using Level.Generation.Old;
using UnityEngine;
using Utils;

namespace Level.Generation.Corridors
{
    public class CorridorSegmentFootprint
    {
        public GameObject prefab;
        public Vector2Int size;
        public List<Vector2Int> passageCells = new List<Vector2Int>();
        public List<SegmentConnector> connectors = new List<SegmentConnector>();
        
        public static CorridorSegmentFootprint FromPrefab(GameObject prefab)
        {
            if (prefab == null) return null;
            
            var comp = prefab.GetComponent<CorridorSegment>();
            if (comp == null || comp.layout == null)
            {
                Debug.LogWarning($"No CorridorSegment component or layout on prefab {prefab.name}");
                return null;
            }
            
            var footprint = new CorridorSegmentFootprint
            {
                prefab = prefab,
                size = new Vector2Int(comp.layout.cols, comp.layout.rows)
            };
            
            for (int r = 0; r < comp.layout.rows; r++)
            {
                for (int c = 0; c < comp.layout.cols; c++)
                {
                    var flags = comp.layout[r, c];
                    var tilePos = new Vector2Int(c, comp.layout.rows - 1 - r);
                    
                    if ((flags & CellFlags.Passage) != 0)
                        footprint.passageCells.Add(tilePos);
                    
                    if ((flags & CellFlags.ConnectorN) != 0)
                        footprint.connectors.Add(new SegmentConnector
                        {
                            cellPosition = new Vector2Int(r, c),
                            direction = Door.Direction.North,
                            localTileOffset = tilePos
                        });
                    if ((flags & CellFlags.ConnectorS) != 0)
                        footprint.connectors.Add(new SegmentConnector
                        {
                            cellPosition = new Vector2Int(r, c),
                            direction = Door.Direction.South,
                            localTileOffset = tilePos
                        });
                    if ((flags & CellFlags.ConnectorE) != 0)
                        footprint.connectors.Add(new SegmentConnector
                        {
                            cellPosition = new Vector2Int(r, c),
                            direction = Door.Direction.East,
                            localTileOffset = tilePos
                        });
                    if ((flags & CellFlags.ConnectorW) != 0)
                        footprint.connectors.Add(new SegmentConnector
                        {
                            cellPosition = new Vector2Int(r, c),
                            direction = Door.Direction.West,
                            localTileOffset = tilePos
                        });
                }
            }
            
            return footprint;
        }
    }
}