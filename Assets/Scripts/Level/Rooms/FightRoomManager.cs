using System.Collections.Generic;
using UnityEngine;

namespace Level.Rooms
{
    [RequireComponent(typeof(Collider2D))]
    public class FightRoomManager : MonoBehaviour
    {
        [SerializeField] private string targetTag = "Player";
        [SerializeField] private List<GameObject> doors;
        private readonly List<GameObject> _closedDoors = new();
        private int _currentWave = 0;
        private int _wavesNumber = 0;
        private bool _isOpen;

        private void Start()
        {
            _isOpen = true;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            Debug.Log($"Entered {collision.gameObject.name}");
            if (!_isOpen) return;
            if (collision.CompareTag(targetTag))
            {
                CloseDoors();
                SpawnNextWave();
                _isOpen = false;
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            Debug.Log($"Exited {collision.gameObject.name}");
            if (_isOpen) return;
            collision.transform.position = transform.position;
        }

        private void CloseDoors()
        {
            foreach (var door in doors)
            {
                if (!door.activeInHierarchy) _closedDoors.Add(door);
                door.SetActive(true);
            }
        }

        private void OpenDoors()
        {
            foreach (var door in _closedDoors) door.SetActive(false);
        }

        private void SpawnNextWave()
        {
            if (_currentWave < _wavesNumber)
            {
                return;
            }

            OpenDoors();
            _isOpen = true;
            gameObject.SetActive(false);
        }
    }
}