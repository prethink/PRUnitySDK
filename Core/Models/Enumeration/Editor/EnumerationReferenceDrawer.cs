using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

[CustomPropertyDrawer(typeof(EnumerationReference))]
public class EnumerationReferenceDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Поле, где хранится значение
        var valueProp = property.FindPropertyRelative("value");

        // Получаем атрибут EnumerationOptionsAttribute, если он есть
        var attr = fieldInfo
            .GetCustomAttributes(typeof(EnumerationOptionsAttribute), false)
            .FirstOrDefault() as EnumerationOptionsAttribute;

        if (attr == null)
        {
            EditorGUI.PropertyField(position, valueProp, label);
            return;
        }

        // Сначала пытаемся получить статический метод
        IEnumerable<string> options = null;
        var method = attr.OptionsType.GetMethod(attr.StaticMethodName,
            BindingFlags.Public | BindingFlags.Static);

        if (method != null)
        {
            // Если метод есть, вызываем его
            var result = method.Invoke(null, null) as IEnumerable<Enumeration>;
            if (result != null)
                options = result.Select(e => e.Value);
        }

        // Если метода нет или вернул null, fallback на публичные статические поля
        if (options == null)
        {
            var fields = attr.OptionsType.GetFields(BindingFlags.Public | BindingFlags.Static);
            options = fields
                .Select(f => f.GetValue(null))
                .OfType<Enumeration>()
                .Select(e => e.Value);
        }

        var optionsArray = options.ToArray();

        // Текущий индекс
        int currentIndex = Mathf.Max(0, System.Array.IndexOf(optionsArray, valueProp.stringValue));

        // Рисуем popup
        int newIndex = EditorGUI.Popup(position, label.text, currentIndex, optionsArray);

        // Сохраняем новое значение
        valueProp.stringValue = optionsArray[newIndex];
    }
}
