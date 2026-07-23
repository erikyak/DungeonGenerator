using UnityEngine;
using Utils;

namespace Level.Generation.Corridors
{
    public class CorridorSegment : MonoBehaviour
    {
        [Tooltip("Layout of the corridor segment. Row 0 = top, col 0 = left.")]
        public Array2D<CellFlags> layout;
    }
}