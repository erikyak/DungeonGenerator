using System;
using System.Collections.Generic;
using UnityEngine;
using Level.Generation.Composites;
using Level.Generation.Corridors;
using Level.Generation.Graph;
using Level.Generation.Layout;
using Level.Minimap;
using Object = UnityEngine.Object;

namespace Level.Generation.Runtime
{
    public class LevelInstantiator
    {
        public Action<Zone> OnRoomEntered;

        public GameObject Instantiate(AssembledLevel level, Transform parent)
        {
            var root = new GameObject("GeneratedLevel");
            if (parent != null) root.transform.SetParent(parent, false);

            var roomsContainer = new GameObject("Rooms");
            roomsContainer.transform.SetParent(root.transform, false);

            var corridorsContainer = new GameObject("Corridors");
            corridorsContainer.transform.SetParent(root.transform, false);

            var placedRoomsByNode = new Dictionary<CompositeNode, GameObject>();

            foreach (var placedRoom in level.rooms)
            {
                var instance = InstantiateRoom(placedRoom, roomsContainer.transform);
                if (instance != null)
                {
                    placedRoomsByNode[placedRoom.sourceNode] = instance;
                    AttachRoomTrigger(instance, placedRoom, level);
                }
            }

            foreach (var corridor in level.corridors)
            {
                InstantiateCorridor(corridor, corridorsContainer.transform);
            }

            HandleUnusedDoors(level, placedRoomsByNode);

            return root;
        }
        
        private GameObject InstantiateRoom(PlacedRoom placedRoom, Transform parent)
        {
            var footprint = placedRoom.footprint;
            if (footprint == null || footprint.prefab == null) return null;
            
            var collider = footprint.prefab.GetComponent<BoxCollider2D>();
            Vector2 colliderOffset = collider != null ? collider.offset : Vector2.zero;
            
            Vector3 worldCenter = new Vector3(
                placedRoom.worldPosition.x + footprint.size.x * 0.5f,
                placedRoom.worldPosition.y + footprint.size.y * 0.5f,
                0
            );
            
            Vector3 spawnPosition = worldCenter - new Vector3(colliderOffset.x, colliderOffset.y, 0);
            
            var instance = Object.Instantiate(footprint.prefab, spawnPosition, Quaternion.identity, parent);
            instance.name = $"Room_{placedRoom.sourceNode.type}_{placedRoom.sourceNode.localId}";
            
            return instance;
        }
        
        private void AttachRoomTrigger(GameObject instance, PlacedRoom placedRoom, AssembledLevel level)
        {
            var zone = FindZoneForNode(level, placedRoom.sourceNode);
            if (zone == null) return;

            var triggerGo = new GameObject("RoomTrigger");
            triggerGo.transform.SetParent(instance.transform, false);
            var size = placedRoom.footprint.size;
            triggerGo.transform.localPosition = new Vector3(
                placedRoom.worldPosition.x + size.x * 0.5f - instance.transform.position.x,
                placedRoom.worldPosition.y + size.y * 0.5f - instance.transform.position.y,
                0f);

            var col = triggerGo.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(size.x, size.y);

            var trigger = triggerGo.AddComponent<RoomTrigger>();
            trigger.Configure(zone, new Vector2(size.x, size.y));
            trigger.OnPlayerEntered += z => OnRoomEntered?.Invoke(z);
        }

        private Zone FindZoneForNode(AssembledLevel level, CompositeNode node)
        {
            foreach (var zone in level.graph.zones)
            {
                if (zone.composite == null) continue;
                foreach (var n in zone.composite.nodes)
                    if (n == node) return zone;
            }
            return null;
        }

        private void InstantiateCorridor(CorridorPath corridor, Transform parent)
        {
            var corridorRoot = new GameObject($"Corridor_{corridor.sourceRoom.sourceNode.localId}_to_{corridor.targetRoom.sourceNode.localId}");
            corridorRoot.transform.SetParent(parent, false);

            foreach (var segment in corridor.segments)
            {
                var prefab = segment.footprint.prefab;
                if (prefab == null) continue;

                var collider = prefab.GetComponent<BoxCollider2D>();
                Vector2 offset = collider != null ? collider.offset : Vector2.zero;

                Vector3 worldCenter = new Vector3(
                    segment.worldPosition.x + segment.footprint.size.x * 0.5f,
                    segment.worldPosition.y + segment.footprint.size.y * 0.5f,
                    0
                );
                Vector3 spawnPosition = worldCenter - new Vector3(offset.x, offset.y, 0);

                Object.Instantiate(prefab, spawnPosition, Quaternion.identity, corridorRoot.transform);
            }
        }
        
        private void HandleUnusedDoors(AssembledLevel level, Dictionary<CompositeNode, GameObject> placedRoomsByNode)
        {
            foreach (var placedRoom in level.rooms)
            {
                if (!placedRoomsByNode.TryGetValue(placedRoom.sourceNode, out var instance)) continue;
                
                var roomComponent = instance.GetComponent<Room>();
                if (roomComponent == null || roomComponent.doors == null) continue;
                
                for (int i = 0; i < roomComponent.doors.Length; i++)
                {
                    var doorGO = roomComponent.doors[i];
                    if (doorGO == null) continue;
                    
                    bool isUsed = IsDoorUsed(placedRoom.sourceNode, i);

                    if (isUsed)
                    {
                        doorGO.gameObject.SetActive(false);
                    }
                }
            }
        }
        
        private bool IsDoorUsed(CompositeNode node, int doorIndex)
        {
            foreach (var usedDoor in node.usedDoors)
            {
                if (usedDoor.prefabDoorIndex == doorIndex) return true;
            }
            return false;
        }
    }
}