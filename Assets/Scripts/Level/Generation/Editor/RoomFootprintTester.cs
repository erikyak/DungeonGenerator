using Level.Generation.Composites;
using UnityEditor;
using UnityEngine;

namespace Level.Generation.Editor
{
    public class RoomFootprintTester
    {
        [MenuItem("Level Generation/Test RoomFootprint")]
        public static void Test()
        {
            var selected = Selection.activeGameObject;
            if (selected == null) 
            { 
                Debug.LogError("Select a room prefab"); 
                return; 
            }
        
            var footprint = RoomFootprint.FromPrefab(selected);
            if (footprint == null) 
            { 
                Debug.LogError("FromPrefab returned null"); 
                return; 
            }
        
            Debug.Log($"Prefab: {footprint.prefab.name}");
            Debug.Log($"Type: {footprint.type}");
            Debug.Log($"Tag: '{footprint.tag}'");
            Debug.Log($"Size: {footprint.size}");
            Debug.Log($"Origin: {footprint.origin}");
            Debug.Log($"Doors: {footprint.doors.Count}");
            foreach (var door in footprint.doors)
            {
                Debug.Log($"  {door}");
            }
        }
    }
}