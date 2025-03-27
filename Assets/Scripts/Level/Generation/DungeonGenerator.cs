using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Level.Rooms;
using Scripts;

namespace Level.Generation
{
    public class DungeonGenerator : MonoBehaviour
    {
        private const int MaxAttempts = 10;
        private const int MaxOnePlacementAttempts = 100;

        public List<RoomTypeRequirements> roomRequirements = new()
        {
            new RoomTypeRequirements { type = Room.RoomType.Entry, minimumRequiredCount = 1, maximumRequiredCount = 1 },
            new RoomTypeRequirements { type = Room.RoomType.Exit, minimumRequiredCount = 1, maximumRequiredCount = 1 },
            new RoomTypeRequirements { type = Room.RoomType.Boss, minimumRequiredCount = 1, maximumRequiredCount = 1 },
            new RoomTypeRequirements { type = Room.RoomType.Treasure, minimumRequiredCount = 2, maximumRequiredCount = 2 },
            new RoomTypeRequirements { type = Room.RoomType.Fight, minimumRequiredCount = 5, maximumRequiredCount = 10 }
        };

        [Header("Corridor Settings")]
        public List<GameObject> corridorPrefabs;

        public bool lazyGeneration;
        
        [SerializeField] [ConditionalHide(nameof(lazyGeneration))]
        private float timeBetweenPlaceAttempts = 0.05f;

        // Internal state
        private List<GameObject> _placedObjects = new();
        private List<Room> _placedRooms = new();
        private Room _bossRoom;
        private CollisionChecker _collisionChecker;
        private bool _exitPlaced;
        private Dictionary<Room.RoomType, int> _remainingCounts;
        private Dictionary<Room.RoomType, List<GameObject>> _roomPools;

        private void Awake()
        {
            _collisionChecker = new CollisionChecker();

            // Use the RoomInitializer to set up room pools and counts.
            RoomInitializer.Initialize(roomRequirements, out _roomPools, out _remainingCounts);

            // Attempt to generate the dungeon.
            StartCoroutine(Generate());
        }

        private IEnumerator Generate()
        {
            bool generated = false;
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                // Place the initial room.
                RoomPlacer.PlaceInitialRoom(_roomPools, _placedRooms, _placedObjects, _collisionChecker, ref _remainingCounts);

                // Main loop for placing additional rooms.
                int onePlacementAttemptCount = MaxOnePlacementAttempts;
                List<Door> availableDoors = new List<Door>(_placedRooms[0].doors);
                availableDoors = availableDoors.OrderBy(_ => Random.value).ToList();

                while (availableDoors.Count > 0 && 
                       RoomTypeHelper.CanPlaceMoreRooms(_remainingCounts) && 
                       onePlacementAttemptCount-- > 0)
                {
                    if (lazyGeneration)
                        yield return new WaitForSeconds(timeBetweenPlaceAttempts);
                    int index = Random.Range(0, availableDoors.Count);
                    Door originDoor = availableDoors[index];

                    if (originDoor.isConnected)
                    {
                        availableDoors.RemoveAt(index);
                        continue;
                    }

                    if (RoomTypeHelper.GetNextRoomType(_remainingCounts) == Room.RoomType.Null)
                        break;

                    // Try to attach a corridor and a new room.
                    if (RoomPlacer.TryPlaceCorridorAndRoom(originDoor, corridorPrefabs, _roomPools, ref _remainingCounts,
                        _placedObjects, _placedRooms, _collisionChecker, out Room newRoom))
                    {
                        Debug.Log($"Placing room {newRoom.name}");
                        originDoor.isConnected = true;
                        originDoor.gameObject.SetActive(false);
                        // Add new doors.
                        foreach (var door in newRoom.doors)
                        {
                            if (!door.isConnected)
                                availableDoors.Add(door);
                        }
                        // If a boss room is placed, store it and try to place the exit.
                        if (newRoom.type == Room.RoomType.Boss)
                        {
                            _bossRoom = newRoom;
                            RoomPlacer.PlaceExitRoom(_bossRoom, corridorPrefabs, _roomPools, ref _remainingCounts,
                                _placedObjects, _placedRooms, _collisionChecker, ref _exitPlaced);
                        }
                        availableDoors.RemoveAt(index);
                        onePlacementAttemptCount = MaxOnePlacementAttempts;
                    }
                }

                if (RoomTypeHelper.IsGenerationComplete(_remainingCounts))
                {
                    generated = true;
                    break;
                }
                Debug.LogWarning("Failed to place rooms, resetting dungeon.");
                RemoveAll();
                // Reinitialize for next attempt.
                RoomInitializer.Initialize(roomRequirements, out _roomPools, out _remainingCounts);
            }

            if (!generated)
            {
                // Fallback emergency generation.
                RoomPlacer.PlaceInitialRoom(_roomPools, _placedRooms, _placedObjects, _collisionChecker, ref _remainingCounts);

                // Fallback emergency generation.
                RoomPlacer.GenerateDungeonEmergency(_roomPools, corridorPrefabs, ref _remainingCounts,
                    _placedObjects, _placedRooms, _collisionChecker, ref _exitPlaced);
            }

            yield return null;
        }

        private void RemoveAll()
        {
            foreach (var obj in _placedObjects.ToArray())
            {
                if (obj)
                    Destroy(obj);
            }
            _placedObjects.Clear();
            _placedRooms.Clear();
            _remainingCounts.Clear();
            _exitPlaced = false;
            _bossRoom = null;
            _collisionChecker.Clear();
        }
    }
}
