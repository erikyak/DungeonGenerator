using System.Collections.Generic;
using Level.Generation.Grammar;
using UnityEditor;
using UnityEngine;

namespace Level.Generation.Editor
{
    [CustomEditor(typeof(CompositeRecipe))]
    public class CompositeRecipeEditor : UnityEditor.Editor
    {
        private Dictionary<int, Vector2> _nodePositions = new Dictionary<int, Vector2>();
        private const float VisualizationHeight = 300f;
        private const float NodeRadius = 25f;
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            var recipe = target as CompositeRecipe;
            if (recipe == null) return;
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Graph Preview", EditorStyles.boldLabel);
            
            Rect canvas = GUILayoutUtility.GetRect(0, VisualizationHeight, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(canvas, new Color(0.15f, 0.15f, 0.15f));
            
            LayoutNodes(recipe, canvas);
            DrawEdges(recipe, canvas);
            DrawNodes(recipe, canvas);
        }
        
        private void LayoutNodes(CompositeRecipe recipe, Rect canvas)
        {
            _nodePositions.Clear();
            if (recipe.nodes.Count == 0) return;
            
            var rng = new System.Random(recipe.nodes.Count * 7 + recipe.edges.Count * 13);
            
            foreach (var node in recipe.nodes)
            {
                float x = canvas.x + NodeRadius + (float)rng.NextDouble() * (canvas.width - NodeRadius * 2);
                float y = canvas.y + NodeRadius + (float)rng.NextDouble() * (canvas.height - NodeRadius * 2);
                _nodePositions[node.id] = new Vector2(x, y);
            }
            
            for (int iteration = 0; iteration < 100; iteration++)
            {
                var forces = new Dictionary<int, Vector2>();
                foreach (var node in recipe.nodes) forces[node.id] = Vector2.zero;
                
                foreach (var a in recipe.nodes)
                {
                    foreach (var b in recipe.nodes)
                    {
                        if (a.id == b.id) continue;
                        var delta = _nodePositions[a.id] - _nodePositions[b.id];
                        float dist = Mathf.Max(delta.magnitude, 1f);
                        forces[a.id] += delta.normalized * (2000f / (dist * dist));
                    }
                }
                
                foreach (var edge in recipe.edges)
                {
                    if (!_nodePositions.ContainsKey(edge.fromId) || !_nodePositions.ContainsKey(edge.toId)) continue;
                    var delta = _nodePositions[edge.toId] - _nodePositions[edge.fromId];
                    float dist = delta.magnitude;
                    if (dist > 80f)
                    {
                        var pull = delta.normalized * (dist - 80f) * 0.05f;
                        forces[edge.fromId] += pull;
                        forces[edge.toId] -= pull;
                    }
                }
                
                foreach (var node in recipe.nodes)
                {
                    _nodePositions[node.id] += forces[node.id] * 0.1f;
                    var pos = _nodePositions[node.id];
                    pos.x = Mathf.Clamp(pos.x, canvas.x + NodeRadius, canvas.xMax - NodeRadius);
                    pos.y = Mathf.Clamp(pos.y, canvas.y + NodeRadius, canvas.yMax - NodeRadius);
                    _nodePositions[node.id] = pos;
                }
            }
        }
        
        private void DrawEdges(CompositeRecipe recipe, Rect canvas)
        {
            Handles.color = Color.white;
            foreach (var edge in recipe.edges)
            {
                if (!_nodePositions.ContainsKey(edge.fromId) || !_nodePositions.ContainsKey(edge.toId)) continue;
                var from = _nodePositions[edge.fromId];
                var to = _nodePositions[edge.toId];
                Handles.DrawLine(new Vector3(from.x, from.y), new Vector3(to.x, to.y));
            }
        }
        
        private void DrawNodes(CompositeRecipe recipe, Rect canvas)
        {
            foreach (var node in recipe.nodes)
            {
                if (!_nodePositions.ContainsKey(node.id)) continue;
                var pos = _nodePositions[node.id];
                
                Color color = ColorForRoomType(node.roomType);
                Handles.color = color;
                Handles.DrawSolidDisc(new Vector3(pos.x, pos.y), Vector3.forward, NodeRadius);
                
                if (node.canBeInterface)
                {
                    Handles.color = Color.white;
                    Handles.DrawWireDisc(new Vector3(pos.x, pos.y), Vector3.forward, NodeRadius + 2);
                }
                
                var labelRect = new Rect(pos.x - 30, pos.y - 10, 60, 20);
                var style = new GUIStyle(EditorStyles.label);
                style.alignment = TextAnchor.MiddleCenter;
                style.normal.textColor = Color.white;
                style.fontStyle = FontStyle.Bold;
                
                string label = $"{node.id}\n{node.roomType}";
                if (node.minRepeat != node.maxRepeat)
                    label += $"\n{node.minRepeat}-{node.maxRepeat}";
                
                GUI.Label(labelRect, label, style);
            }
        }
        
        private Color ColorForRoomType(Room.RoomType type)
        {
            switch (type)
            {
                case Room.RoomType.Entry: return Color.green;
                case Room.RoomType.Fight: return new Color(0.7f, 0.3f, 0.3f);
                case Room.RoomType.Treasure: return new Color(1f, 0.85f, 0.3f);
                case Room.RoomType.Boss: return Color.magenta;
                case Room.RoomType.Exit: return new Color(0.3f, 0.3f, 1f);
                case Room.RoomType.Corridor: return Color.gray;
                default: return Color.white;
            }
        }
    }
}