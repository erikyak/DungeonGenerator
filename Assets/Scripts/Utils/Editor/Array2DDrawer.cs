using UnityEditor;
using UnityEngine;
using Utils;

namespace Editor
{
    [CustomPropertyDrawer(typeof(Array2D<>), true)]
    public class Array2DDrawer : PropertyDrawer
    {
        private const float CellSize = 40f;
        private const float Padding = 2f;
        
        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
        {
            var rowsProp = prop.FindPropertyRelative("rows");
            int rows = rowsProp.intValue;
            return EditorGUIUtility.singleLineHeight * 2
                   + (CellSize + Padding) * rows
                   + Padding;
        }
        
        public override void OnGUI(Rect r, SerializedProperty prop, GUIContent label)
        {
            var rowsProp = prop.FindPropertyRelative("rows");
            var colsProp = prop.FindPropertyRelative("cols");
            var dataProp = prop.FindPropertyRelative("data");
            
            var line = new Rect(r.x, r.y, r.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(line, rowsProp);
            line.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(line, colsProp);
            
            if (GUI.changed)
            {
                var arr = fieldInfo.GetValue(prop.serializedObject.targetObject);
                var method = arr.GetType().GetMethod("Resize");
                method?.Invoke(arr, new object[]{ rowsProp.intValue, colsProp.intValue });
                prop.serializedObject.Update();
                prop.serializedObject.ApplyModifiedProperties();
            }
            
            float startY = r.y + EditorGUIUtility.singleLineHeight * 2 + Padding;
            
            for (int rIdx = 0; rIdx < rowsProp.intValue; rIdx++)
            {
                for (int cIdx = 0; cIdx < colsProp.intValue; cIdx++)
                {
                    int idx = rIdx * colsProp.intValue + cIdx;
                    if (idx >= dataProp.arraySize) continue;
                    
                    var cellRect = new Rect(
                        r.x + cIdx * (CellSize + Padding),
                        startY + rIdx * (CellSize + Padding),
                        CellSize,
                        CellSize
                    );
                    var elem = dataProp.GetArrayElementAtIndex(idx);
                    EditorGUI.PropertyField(cellRect, elem, GUIContent.none);
                }
            }
        }
    }
}