using UnityEditor;
using UnityEngine;

/// <summary>
/// Рисует поле вместе с иконкой объекта, на который оно ссылается.
/// </summary>
[CustomPropertyDrawer(typeof(IconPreviewAttribute))]
public class IconPreviewDrawer : PropertyDrawer
{
    private const float Gap = 4f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var settings = (IconPreviewAttribute)attribute;
        Sprite icon = ResolveIcon(property);

        var fieldRect = new Rect(
            position.x,
            position.y,
            position.width,
            EditorGUIUtility.singleLineHeight);

        if (icon == null)
        {
            // Без иконки поле занимает всю ширину: пустой квадрат рядом только сбивал бы
            // с толку, будто превью не загрузилось.
            EditorGUI.PropertyField(fieldRect, property, label);
            return;
        }

        if (settings.Below)
        {
            EditorGUI.PropertyField(fieldRect, property, label);

            var previewRect = new Rect(
                position.x,
                fieldRect.yMax + Gap,
                position.width,
                settings.Size);

            DrawSprite(previewRect, icon);
            return;
        }

        fieldRect.width = Mathf.Max(60f, position.width - settings.Size - Gap);
        var iconRect = new Rect(position.xMax - settings.Size, position.y, settings.Size, settings.Size);

        EditorGUI.PropertyField(fieldRect, property, label);
        DrawSprite(iconRect, icon);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var settings = (IconPreviewAttribute)attribute;

        if (ResolveIcon(property) == null)
            return EditorGUIUtility.singleLineHeight;

        return settings.Below
            ? EditorGUIUtility.singleLineHeight + Gap + settings.Size
            : Mathf.Max(EditorGUIUtility.singleLineHeight, settings.Size);
    }

    /// <summary>
    /// Достаёт иконку из значения поля.
    /// </summary>
    /// <remarks>
    /// Спрайт показывается сам собой, а у определения берётся его иконка через
    /// <see cref="IIconProvider"/> — так атрибут одинаково работает и с полем спрайта,
    /// и с полем валюты, предмета или награды.
    /// <para>
    /// Иконка бывает вычисляемой — например, награда отдаёт иконку выданного предмета.
    /// У недонастроенной награды такое свойство может бросить исключение, а исключение
    /// из <c>OnGUI</c> ломает отрисовку всего окна: показать нечего именно там, где
    /// проблему и надо увидеть.
    /// </para>
    /// </remarks>
    private static Sprite ResolveIcon(SerializedProperty property)
    {
        if (property.propertyType != SerializedPropertyType.ObjectReference)
            return null;

        try
        {
            return property.objectReferenceValue switch
            {
                Sprite sprite => sprite,
                IIconProvider provider => provider.Icon,
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Рисует спрайт, сохраняя пропорции.
    /// </summary>
    /// <remarks>
    /// Через координаты текстуры, а не <c>GUI.DrawTexture</c> целиком: спрайт может быть
    /// частью атласа, и без пересчёта нарисовался бы весь лист.
    /// </remarks>
    private static void DrawSprite(Rect rect, Sprite sprite)
    {
        Texture2D texture = sprite.texture;

        if (texture == null)
            return;

        Rect textureRect = sprite.textureRect;
        var coordinates = new Rect(
            textureRect.x / texture.width,
            textureRect.y / texture.height,
            textureRect.width / texture.width,
            textureRect.height / texture.height);

        float spriteAspect = textureRect.width / textureRect.height;
        float rectAspect = rect.width / rect.height;

        if (spriteAspect > rectAspect)
        {
            float height = rect.width / spriteAspect;
            rect.y += (rect.height - height) * 0.5f;
            rect.height = height;
        }
        else
        {
            float width = rect.height * spriteAspect;
            rect.x += (rect.width - width) * 0.5f;
            rect.width = width;
        }

        GUI.DrawTextureWithTexCoords(rect, texture, coordinates, alphaBlend: true);
    }
}
