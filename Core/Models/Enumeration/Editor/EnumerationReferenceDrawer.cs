using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(EnumerationReference<>), true)]
public class EnumerationReferenceDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var valueProp = property.FindPropertyRelative("value");

        var options = GetOptions();

        if (options == null)
        {
            EditorGUI.PropertyField(position, valueProp, label);
            return;
        }

        var optionsArray = options.Select(o => o.Value).ToArray();

        int index = Mathf.Max(0, Array.IndexOf(optionsArray, valueProp.stringValue));

        int newIndex = EditorGUI.Popup(position, label.text, index, optionsArray);

        valueProp.stringValue = optionsArray[newIndex];
    }

    private IEnumerable<Enumeration> GetOptions()
    {
        var type = fieldInfo.FieldType;

        if (!type.IsGenericType)
            return null;

        var genericType = type.GetGenericArguments()[0];

        var provider = Activator.CreateInstance(genericType) as IEnumerationProvider;

        return provider?.GetOptions();
    }
}