using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(UnitAttribute))]
public class UnitDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        UnitAttribute unitAttribute = (UnitAttribute)attribute;

        Rect textFieldRect = new(position.x, position.y, position.width - 40, position.height);
        Rect unitLabelRect = new(position.x + position.width - 35, position.y, 35, position.height);

        EditorGUI.PropertyField(textFieldRect, property, label);
        GUI.Label(unitLabelRect, unitAttribute.Unit);
    }
}