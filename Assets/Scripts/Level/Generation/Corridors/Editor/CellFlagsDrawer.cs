using UnityEditor;
using UnityEngine;
using Level.Generation.Corridors;

namespace Level.Generation.Corridors.Editor
{
    [CustomPropertyDrawer(typeof(CellFlags))]
    public class CellFlagsDrawer : PropertyDrawer
    {
        private const float CellSize = 40f;
        private const float ConnectorZoneSize = 8f; 
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return CellSize;
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Debug.Log($"CellFlagsDrawer OnGUI called, position: {position}");
            var current = (CellFlags)property.intValue;
            bool isPassage = (current & CellFlags.Passage) != 0;
            
            var bgColor = isPassage ? new Color(0.6f, 0.55f, 0.4f) : new Color(0.15f, 0.15f, 0.15f);
            EditorGUI.DrawRect(position, bgColor);
            
            var borderColor = new Color(0.05f, 0.05f, 0.05f);
            EditorGUI.DrawRect(new Rect(position.x, position.y, position.width, 1), borderColor);
            EditorGUI.DrawRect(new Rect(position.x, position.y + position.height - 1, position.width, 1), borderColor);
            EditorGUI.DrawRect(new Rect(position.x, position.y, 1, position.height), borderColor);
            EditorGUI.DrawRect(new Rect(position.x + position.width - 1, position.y, 1, position.height), borderColor);
            
            if (isPassage)
            {
                var connColor = new Color(0.9f, 0.7f, 0.2f);
                
                if ((current & CellFlags.ConnectorN) != 0)
                    EditorGUI.DrawRect(new Rect(position.x + ConnectorZoneSize, position.y, position.width - ConnectorZoneSize * 2, ConnectorZoneSize), connColor);
                
                if ((current & CellFlags.ConnectorS) != 0)
                    EditorGUI.DrawRect(new Rect(position.x + ConnectorZoneSize, position.y + position.height - ConnectorZoneSize, position.width - ConnectorZoneSize * 2, ConnectorZoneSize), connColor);
                
                if ((current & CellFlags.ConnectorW) != 0)
                    EditorGUI.DrawRect(new Rect(position.x, position.y + ConnectorZoneSize, ConnectorZoneSize, position.height - ConnectorZoneSize * 2), connColor);
                
                if ((current & CellFlags.ConnectorE) != 0)
                    EditorGUI.DrawRect(new Rect(position.x + position.width - ConnectorZoneSize, position.y + ConnectorZoneSize, ConnectorZoneSize, position.height - ConnectorZoneSize * 2), connColor);
            }
            
            if (Event.current.type == EventType.MouseDown && position.Contains(Event.current.mousePosition))
            {
                Vector2 mp = Event.current.mousePosition;
                Vector2 relative = mp - new Vector2(position.x, position.y);
                
                CellFlags newFlags = current;
                
                bool onNorthEdge = relative.y < ConnectorZoneSize;
                bool onSouthEdge = relative.y > position.height - ConnectorZoneSize;
                bool onWestEdge = relative.x < ConnectorZoneSize;
                bool onEastEdge = relative.x > position.width - ConnectorZoneSize;
                
                if (Event.current.button == 0)
                {
                    if (onNorthEdge && !onWestEdge && !onEastEdge)
                    {
                        if (isPassage) newFlags ^= CellFlags.ConnectorN;
                    }
                    else if (onSouthEdge && !onWestEdge && !onEastEdge)
                    {
                        if (isPassage) newFlags ^= CellFlags.ConnectorS;
                    }
                    else if (onWestEdge && !onNorthEdge && !onSouthEdge)
                    {
                        if (isPassage) newFlags ^= CellFlags.ConnectorW;
                    }
                    else if (onEastEdge && !onNorthEdge && !onSouthEdge)
                    {
                        if (isPassage) newFlags ^= CellFlags.ConnectorE;
                    }
                    else
                    {
                        newFlags ^= CellFlags.Passage;
                        if ((newFlags & CellFlags.Passage) == 0)
                        {
                            newFlags &= ~(CellFlags.ConnectorN | CellFlags.ConnectorS | CellFlags.ConnectorE | CellFlags.ConnectorW);
                        }
                    }
                    
                    property.intValue = (int)newFlags;
                    Event.current.Use();
                }
            }
        }
    }
}