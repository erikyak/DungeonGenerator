using System.Collections.Generic;
using System.Linq;
using Level.Rooms;
using UnityEngine;

namespace Level.Generation
{
    public static class RoomPlacer
    {
        public static void PlaceInitialRoom(
            Dictionary<Room.RoomType, List<GameObject>> roomPools, 
            List<Room> placedRooms, 
            List<GameObject> placedObjects, 
            CollisionChecker collisionChecker, 
            ref Dictionary<Room.RoomType, int> remainingCounts)
        {
            var entryType = Room.RoomType.Entry;
            if (!roomPools.ContainsKey(entryType) || roomPools[entryType].Count == 0)
                return;

            var entryPrefab = roomPools[entryType][0];
            var entryRoom = Object.Instantiate(entryPrefab, Vector3.zero, Quaternion.identity).GetComponent<Room>();
            placedRooms.Add(entryRoom);
            placedObjects.Add(entryRoom.gameObject);
            remainingCounts[entryType]--;
            collisionChecker.Register(entryRoom.bounds);
        }

        public static bool TryPlaceCorridorAndRoom(
            Door originDoor, 
            List<GameObject> corridorPrefabs,
            Dictionary<Room.RoomType, List<GameObject>> roomPools, 
            ref Dictionary<Room.RoomType, int> remainingCounts, 
            List<GameObject> placedObjects, 
            List<Room> placedRooms, 
            CollisionChecker collisionChecker, 
            out Room placedRoom)
        {
            placedRoom = null;
            // Determine the required corridor entry direction.
            Door.Direction requiredCorridorEntryDir = CorridorHelper.OppositeDirection(originDoor.direction);
            GameObject corridorPrefab = CorridorHelper.SelectCorridorPrefab(corridorPrefabs, requiredCorridorEntryDir);
            if (!corridorPrefab)
                return false;

            var corridorComp = corridorPrefab.GetComponent<Room>();
            if (!corridorComp || corridorComp.doors == null || corridorComp.doors.Length < 2)
                return false;

            // Find the door on the corridor prefab matching the entry requirement.
            var entryCorridorDoorPrefab = corridorComp.doors.FirstOrDefault(d => d.direction == requiredCorridorEntryDir);
            if (!entryCorridorDoorPrefab)
                return false;

            // Choose an exit door randomly.
            var possibleExitDoors = corridorComp.doors.Where(d => d != entryCorridorDoorPrefab).ToList();
            if (possibleExitDoors.Count == 0)
                return false;
            var exitCorridorDoorPrefab = possibleExitDoors[Random.Range(0, possibleExitDoors.Count)];

            // Align corridor so its entry door meets the origin door.
            Vector3 corridorPosition = originDoor.transform.position - entryCorridorDoorPrefab.transform.position + originDoor.offset;
            GameObject corridorInstance = Object.Instantiate(corridorPrefab, corridorPosition, Quaternion.identity);
            var corridorInstanceComp = corridorInstance.GetComponent<Room>();
            if (!corridorInstanceComp)
            {
                Object.Destroy(corridorInstance);
                return false;
            }

            corridorInstanceComp.UpdateBounds();
            Bounds corridorBounds = new Bounds(corridorInstance.transform.position + corridorInstanceComp.bounds.center, corridorInstanceComp.bounds.size);
            if (collisionChecker.CheckCollision(corridorBounds))
            {
                Object.Destroy(corridorInstance);
                return false;
            }

            // Identify the corridor's exit door.
            var corridorExitDoor = corridorInstanceComp.doors.FirstOrDefault(d =>
                d.direction == exitCorridorDoorPrefab.direction &&
                d.transform.localPosition == exitCorridorDoorPrefab.transform.localPosition);
            if (!corridorExitDoor)
                corridorExitDoor = corridorInstanceComp.doors.FirstOrDefault(d => d.direction != requiredCorridorEntryDir);
            if (!corridorExitDoor)
            {
                Object.Destroy(corridorInstance);
                return false;
            }

            // Mark corridor doors as connected.
            var corridorEntryDoor = corridorInstanceComp.doors.FirstOrDefault(d => d.direction == requiredCorridorEntryDir);
            if (corridorEntryDoor)
            {
                corridorEntryDoor.isConnected = true;
                corridorEntryDoor.gameObject.SetActive(false);
            }
            corridorExitDoor.isConnected = true;
            corridorExitDoor.gameObject.SetActive(false);

            // Determine position for the new room.
            Vector3 corridorExitWorldPos = corridorExitDoor.transform.position;
            Door.Direction requiredNewRoomDoorDir = CorridorHelper.OppositeDirection(corridorExitDoor.direction);
            Room.RoomType newRoomType = RoomTypeHelper.GetNextRoomType(remainingCounts);
            GameObject newRoomPrefab = CorridorHelper.FindRoomWithDoorDirection(roomPools, newRoomType, requiredNewRoomDoorDir);
            if (!newRoomPrefab)
            {
                Object.Destroy(corridorInstance);
                return false;
            }

            var newRoomComp = newRoomPrefab.GetComponent<Room>();
            var newRoomDoor = newRoomComp.doors.FirstOrDefault(d => d.direction == requiredNewRoomDoorDir);
            if (!newRoomDoor)
            {
                Object.Destroy(corridorInstance);
                return false;
            }

            // Place the new room so its door aligns with the corridor's exit.
            Vector3 newRoomPosition = corridorExitWorldPos - newRoomDoor.transform.position + corridorExitDoor.offset;
            Bounds newRoomBounds = new Bounds(newRoomPosition + newRoomComp.bounds.center, newRoomComp.bounds.size);
            if (collisionChecker.CheckCollision(newRoomBounds))
            {
                Object.Destroy(corridorInstance);
                return false;
            }

            var newRoomInstance = Object.Instantiate(newRoomPrefab, newRoomPosition, Quaternion.identity).GetComponent<Room>();
            if (!newRoomInstance)
            {
                Object.Destroy(corridorInstance);
                return false;
            }

            // Mark the connecting door on the new room as connected.
            var placedNewRoomDoor = newRoomInstance.doors.FirstOrDefault(d => d.direction == requiredNewRoomDoorDir);
            if (!placedNewRoomDoor)
            {
                Object.Destroy(corridorInstance);
                Object.Destroy(newRoomInstance.gameObject);
                return false;
            }
            placedNewRoomDoor.isConnected = true;
            placedNewRoomDoor.gameObject.SetActive(false);

            // Register new objects and update collision.
            placedObjects.Add(corridorInstance);
            placedObjects.Add(newRoomInstance.gameObject);
            placedRooms.Add(newRoomInstance);
            collisionChecker.Register(corridorBounds);
            collisionChecker.Register(newRoomBounds);

            remainingCounts[newRoomType]--;
            
            placedRoom = newRoomInstance;
            return true;
        }

        public static void PlaceExitRoom(
            Room bossRoom, 
            List<GameObject> corridorPrefabs,
            Dictionary<Room.RoomType, List<GameObject>> roomPools, 
            ref Dictionary<Room.RoomType, int> remainingCounts, 
            List<GameObject> placedObjects, 
            List<Room> placedRooms, 
            CollisionChecker collisionChecker, 
            ref bool exitPlaced)
        {
            if (exitPlaced || !bossRoom)
                return;

            foreach (var originDoor in bossRoom.doors.Where(d => !d.isConnected).ToList())
            {
                Door.Direction requiredCorridorEntryDir = CorridorHelper.OppositeDirection(originDoor.direction);
                GameObject corridorPrefab = CorridorHelper.SelectCorridorPrefab(corridorPrefabs, requiredCorridorEntryDir);
                if (!corridorPrefab)
                    continue;

                var corridorComp = corridorPrefab.GetComponent<Room>();
                if (!corridorComp || corridorComp.doors == null || corridorComp.doors.Length < 2)
                    continue;

                var entryCorridorDoorPrefab = corridorComp.doors.FirstOrDefault(d => d.direction == requiredCorridorEntryDir);
                if (!entryCorridorDoorPrefab)
                    continue;

                var possibleExitDoors = corridorComp.doors.Where(d => d != entryCorridorDoorPrefab).ToList();
                if (possibleExitDoors.Count == 0)
                    continue;
                var exitCorridorDoorPrefab = possibleExitDoors[Random.Range(0, possibleExitDoors.Count)];

                Vector3 corridorPosition = originDoor.transform.position - entryCorridorDoorPrefab.transform.position + originDoor.offset;
                GameObject corridorInstance = Object.Instantiate(corridorPrefab, corridorPosition, Quaternion.identity);
                var corridorInstanceComp = corridorInstance.GetComponent<Room>();
                if (!corridorInstanceComp)
                {
                    Object.Destroy(corridorInstance);
                    continue;
                }

                corridorInstanceComp.UpdateBounds();
                Bounds corridorBounds = new Bounds(corridorInstance.transform.position + corridorInstanceComp.bounds.center, corridorInstanceComp.bounds.size);
                if (collisionChecker.CheckCollision(corridorBounds))
                {
                    Object.Destroy(corridorInstance);
                    continue;
                }

                var corridorExitDoor = corridorInstanceComp.doors.FirstOrDefault(d =>
                    d.direction == exitCorridorDoorPrefab.direction &&
                    d.transform.localPosition == exitCorridorDoorPrefab.transform.localPosition);
                if (!corridorExitDoor)
                    corridorExitDoor = corridorInstanceComp.doors.FirstOrDefault(d => d.direction != requiredCorridorEntryDir);
                if (!corridorExitDoor)
                {
                    Object.Destroy(corridorInstance);
                    continue;
                }

                var corridorEntryDoor = corridorInstanceComp.doors.FirstOrDefault(d => d.direction == requiredCorridorEntryDir);
                if (corridorEntryDoor)
                {
                    corridorEntryDoor.isConnected = true;
                    corridorEntryDoor.gameObject.SetActive(false);
                }
                corridorExitDoor.isConnected = true;
                corridorExitDoor.gameObject.SetActive(false);

                Vector3 corridorExitWorldPos = corridorExitDoor.transform.position;
                Door.Direction requiredNewRoomDoorDir = CorridorHelper.OppositeDirection(corridorExitDoor.direction);
                GameObject newRoomPrefab = CorridorHelper.FindRoomWithDoorDirection(roomPools, Room.RoomType.Exit, requiredNewRoomDoorDir);
                if (!newRoomPrefab)
                {
                    Object.Destroy(corridorInstance);
                    continue;
                }

                var newRoomComp = newRoomPrefab.GetComponent<Room>();
                var newRoomDoor = newRoomComp.doors.FirstOrDefault(d => d.direction == requiredNewRoomDoorDir);
                if (!newRoomDoor)
                {
                    Object.Destroy(corridorInstance);
                    continue;
                }

                Vector3 newRoomPosition = corridorExitWorldPos - newRoomDoor.transform.position + corridorExitDoor.offset;
                Bounds newRoomBounds = new Bounds(newRoomPosition + newRoomComp.bounds.center, newRoomComp.bounds.size);
                if (collisionChecker.CheckCollision(newRoomBounds))
                {
                    Object.Destroy(corridorInstance);
                    continue;
                }

                var newRoomInstance = Object.Instantiate(newRoomPrefab, newRoomPosition, Quaternion.identity).GetComponent<Room>();
                if (!newRoomInstance)
                {
                    Object.Destroy(corridorInstance);
                    continue;
                }

                // Connect the boss door and the exit room door.
                originDoor.isConnected = true;
                var placedNewRoomDoor = newRoomInstance.doors.FirstOrDefault(d => d.direction == requiredNewRoomDoorDir);
                if (!placedNewRoomDoor)
                {
                    Object.Destroy(corridorInstance);
                    Object.Destroy(newRoomInstance.gameObject);
                    continue;
                }
                placedNewRoomDoor.isConnected = true;
                originDoor.gameObject.SetActive(false);
                placedNewRoomDoor.gameObject.SetActive(false);

                placedObjects.Add(corridorInstance);
                placedObjects.Add(newRoomInstance.gameObject);
                placedRooms.Add(newRoomInstance);
                remainingCounts[Room.RoomType.Exit]--;
                collisionChecker.Register(corridorBounds);
                collisionChecker.Register(newRoomBounds);
                exitPlaced = true;
                break;
            }
        }

        public static void GenerateDungeonEmergency(
        Dictionary<Room.RoomType, List<GameObject>> roomPools,
        List<GameObject> corridorPrefabs,
        ref Dictionary<Room.RoomType, int> remainingCounts,
        List<GameObject> placedObjects,
        List<Room> placedRooms,
        CollisionChecker collisionChecker,
        ref bool exitPlaced)
        {
            // Assume the initial room has already been placed (placedRooms[0])
            Room initialRoom = placedRooms[0];

            // Iterate over each door of the initial room in a random order.
            foreach (var originDoor in initialRoom.doors.OrderBy(_ => Random.value))
            {
                Door.Direction requiredCorridorEntryDir = CorridorHelper.OppositeDirection(originDoor.direction);
                GameObject corridorPrefab = CorridorHelper.SelectCorridorPrefab(corridorPrefabs, requiredCorridorEntryDir);
                if (!corridorPrefab)
                    continue;

                var corridorPrefabComp = corridorPrefab.GetComponent<Room>();
                if (!corridorPrefabComp || corridorPrefabComp.doors == null || corridorPrefabComp.doors.Length < 2)
                    continue;

                // Find the entry door on the corridor prefab.
                var entryCorridorDoorPrefab = corridorPrefabComp.doors.FirstOrDefault(d => d.direction == requiredCorridorEntryDir);
                if (!entryCorridorDoorPrefab)
                    continue;

                // Choose a random exit door from the corridor prefab.
                var possibleExitDoors = corridorPrefabComp.doors.Where(d => d != entryCorridorDoorPrefab).ToList();
                if (possibleExitDoors.Count == 0)
                    continue;
                var exitCorridorDoorPrefab = possibleExitDoors[Random.Range(0, possibleExitDoors.Count)];

                Vector3 corridorPosition = originDoor.transform.position - entryCorridorDoorPrefab.transform.position + originDoor.offset;
                GameObject corridorInstance = Object.Instantiate(corridorPrefab, corridorPosition, Quaternion.identity);
                var corridorInstanceComp = corridorInstance.GetComponent<Room>();
                if (!corridorInstanceComp)
                {
                    Object.Destroy(corridorInstance);
                    continue;
                }

                corridorInstanceComp.UpdateBounds();
                Bounds corridorBounds = new Bounds(
                    corridorInstance.transform.position + corridorInstanceComp.bounds.center,
                    corridorInstanceComp.bounds.size
                );
                if (collisionChecker.CheckCollision(corridorBounds))
                {
                    Object.Destroy(corridorInstance);
                    continue;
                }

                // Find the corridor's exit door.
                var corridorExitDoor = corridorInstanceComp.doors.FirstOrDefault(d =>
                    d.direction == exitCorridorDoorPrefab.direction &&
                    d.transform.localPosition == exitCorridorDoorPrefab.transform.localPosition);
                if (!corridorExitDoor)
                    corridorExitDoor = corridorInstanceComp.doors.FirstOrDefault(d => d.direction != requiredCorridorEntryDir);
                if (!corridorExitDoor)
                {
                    Object.Destroy(corridorInstance);
                    continue;
                }

                Door.Direction requiredNewRoomDoorDir = CorridorHelper.OppositeDirection(corridorExitDoor.direction);
                GameObject newRoomPrefab = CorridorHelper.FindRoomWithDoorDirection(roomPools, Room.RoomType.Exit, requiredNewRoomDoorDir);
                if (!newRoomPrefab)
                {
                    Object.Destroy(corridorInstance);
                    continue;
                }

                var newRoomPrefabComp = newRoomPrefab.GetComponent<Room>();
                var newRoomDoor = newRoomPrefabComp.doors.FirstOrDefault(d => d.direction == requiredNewRoomDoorDir);
                if (!newRoomDoor)
                {
                    Object.Destroy(corridorInstance);
                    continue;
                }

                Vector3 newRoomPosition = corridorExitDoor.transform.position - newRoomDoor.transform.position + corridorExitDoor.offset;
                Bounds newRoomBounds = new Bounds(
                    newRoomPosition + newRoomPrefabComp.bounds.center,
                    newRoomPrefabComp.bounds.size
                );
                if (collisionChecker.CheckCollision(newRoomBounds))
                {
                    Object.Destroy(corridorInstance);
                    continue;
                }

                // Instantiate the exit room.
                Room newRoomInstance = Object.Instantiate(newRoomPrefab, newRoomPosition, Quaternion.identity).GetComponent<Room>();
                if (!newRoomInstance)
                {
                    Object.Destroy(corridorInstance);
                    continue;
                }

                originDoor.isConnected = true;
                var placedNewRoomDoor = newRoomInstance.doors.First(d => d.direction == requiredNewRoomDoorDir);
                placedNewRoomDoor.isConnected = true;
                originDoor.gameObject.SetActive(false);
                placedNewRoomDoor.gameObject.SetActive(false);

                var corridorEntryDoor = corridorInstanceComp.doors.FirstOrDefault(d => d.direction == requiredCorridorEntryDir);
                if (corridorEntryDoor)
                {
                    corridorEntryDoor.isConnected = true;
                    corridorEntryDoor.gameObject.SetActive(false);
                }
                corridorExitDoor.isConnected = true;
                corridorExitDoor.gameObject.SetActive(false);

                // Register the new objects.
                placedObjects.Add(corridorInstance);
                placedObjects.Add(newRoomInstance.gameObject);
                placedRooms.Add(newRoomInstance);
                remainingCounts[newRoomInstance.type]--;
                collisionChecker.Register(corridorBounds);
                collisionChecker.Register(newRoomBounds);

                // Mark that the exit room has been placed.
                exitPlaced = true;
                return;
            }
        }

    }
}
