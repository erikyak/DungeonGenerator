#if UNITY_EDITOR
using Scripts;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomPropertyDrawer(typeof(ConditionalHideAttribute))]
    public class ConditionalHidePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ConditionalHideAttribute condHAtt = (ConditionalHideAttribute)attribute;
            SerializedProperty sourceProperty = property.serializedObject.FindProperty(condHAtt.ConditionalSourceField);

            if (sourceProperty == null)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            bool isConditionMet = sourceProperty.boolValue ^ condHAtt.HideInInspector;
            if (isConditionMet)
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            ConditionalHideAttribute condHAtt = (ConditionalHideAttribute)attribute;
            SerializedProperty sourceProperty = property.serializedObject.FindProperty(condHAtt.ConditionalSourceField);

            if (sourceProperty == null)
                return EditorGUI.GetPropertyHeight(property, label);

            bool isConditionMet = sourceProperty.boolValue ^ condHAtt.HideInInspector;
            return isConditionMet ? EditorGUI.GetPropertyHeight(property, label) : 0;
        }
    }
}
#endif