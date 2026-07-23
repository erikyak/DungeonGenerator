using Level.Generation.Old;
using UnityEngine;

namespace Level.Generation
{
    public class Room : MonoBehaviour
    {
        public enum RoomType
        {
            Entry,
            Fight,
            Treasure,
            Boss,
            Exit,
            Corridor,
            Null
        }

        public RoomType type;
        public Door[] doors;
        public Bounds bounds;
        
        [Header("Prefab filtering")]
        [Tooltip("Optional tag for filtering by recipe (e.g. BossChamber, ArenaHall)")]
        public string tag = "";

        private void OnDrawGizmos()
        {
            var worldBounds = new Bounds(
                transform.position + bounds.center,
                bounds.size
            );
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);
        }

        public void UpdateBounds()
        {
            var bc = GetComponent<BoxCollider2D>();
            if (bc != null)
            {
                bounds.size = bc.bounds.size;
                bounds.center = bc.offset;
            }
        }
    }
}