using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
    {
        if (prop.isArray && prop.propertyType == SerializedPropertyType.Generic)
        {
            EditorGUI.LabelField(position, label.text, $"Array ({prop.arraySize} elements):");

            EditorGUI.indentLevel++;
            for (int i = 0; i < prop.arraySize; i++)
            {
                SerializedProperty element = prop.GetArrayElementAtIndex(i);
                Rect elementRect = new(position.x, position.y + (i + 1) * EditorGUIUtility.singleLineHeight,
                    position.width, EditorGUIUtility.singleLineHeight);

                if (element.propertyType == SerializedPropertyType.Generic) // custom class
                {
                    EditorGUI.LabelField(elementRect, $"[{i}]: (Custom Class)");
                    ShowClassFields(element, ref position);
                }
                else // primitive type
                {
                    EditorGUI.LabelField(elementRect, $"[{i}]: {GetPropertyValue(element)}");
                }
            }

            EditorGUI.indentLevel--;
            return;
        }

        // single value
        EditorGUI.LabelField(position, label.text, GetPropertyValue(prop));
    }

    private void ShowClassFields(SerializedProperty element, ref Rect position)
    {
        EditorGUI.indentLevel++;
        SerializedProperty iterator = element.Copy();
        SerializedProperty endProperty = iterator.GetEndProperty();

        while (iterator.NextVisible(true) && !SerializedProperty.EqualContents(iterator, endProperty))
        {
            position.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.LabelField(position, $"   {iterator.displayName}: {GetPropertyValue(iterator)}");
        }

        EditorGUI.indentLevel--;
    }

    private string GetPropertyValue(SerializedProperty prop)
    {
        switch (prop.propertyType)
        {
            case SerializedPropertyType.Integer: return prop.intValue.ToString();
            case SerializedPropertyType.Boolean: return prop.boolValue.ToString();
            case SerializedPropertyType.Float: return prop.floatValue.ToString("0.00000");
            case SerializedPropertyType.String: return prop.stringValue;
            case SerializedPropertyType.Enum: return prop.enumNames[prop.enumValueIndex];
            case SerializedPropertyType.ObjectReference:
                return prop.objectReferenceValue ? prop.objectReferenceValue.name : "null";
            case SerializedPropertyType.Generic:
                if (prop.isArray)
                    return $"Array ({prop.arraySize} elements)"; // show array size instead of "(not supported)"
                return "(Custom Class)"; // mark it as a class but don't say "not supported"
            default: return "(not supported)";
        }
    }
}