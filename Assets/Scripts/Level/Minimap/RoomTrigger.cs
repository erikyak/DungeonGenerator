using System;
using UnityEngine;
using Level.Generation.Graph;

namespace Level.Minimap
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class RoomTrigger : MonoBehaviour
    {
        public Zone zone;
        public Action<Zone> OnPlayerEntered;

        private BoxCollider2D _collider;
        private bool _reported;

        private void Awake()
        {
            _collider = GetComponent<BoxCollider2D>();
            _collider.isTrigger = true;
        }

        public void Configure(Zone owningZone, Vector2 size)
        {
            zone = owningZone;
            _collider.size = size;
            _collider.offset = Vector2.zero;
            _reported = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_reported) return;
            if (!other.CompareTag("Player")) return;
            _reported = true;
            OnPlayerEntered?.Invoke(zone);
        }
    }
}
