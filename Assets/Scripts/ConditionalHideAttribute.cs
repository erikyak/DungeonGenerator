using UnityEngine;

namespace Scripts
{
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class ConditionalHideAttribute : PropertyAttribute
    {
        public string ConditionalSourceField { get; private set; }
        public bool HideInInspector { get; private set; }

        public ConditionalHideAttribute(string conditionalSourceField, bool hideInInspector = false)
        {
            ConditionalSourceField = conditionalSourceField;
            HideInInspector = hideInInspector;
        }
    }
}