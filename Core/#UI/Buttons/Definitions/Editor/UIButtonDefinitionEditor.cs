using UnityEditor;
using UnityEngine;

/// <summary>
/// Проверяет ссылки и обязательные настройки кнопки до запуска игры и рисует её картинкой ассета.
/// </summary>
/// <remarks>
/// Без своей картинки окно выбора и Project показывают всем кнопкам один значок ScriptableObject,
/// а имена в сетке обрезаны: «AchievementsButton» и «AutoPilotButton» различались только по
/// первым буквам.
/// </remarks>
[CustomEditor(typeof(UIButtonDefinition), true)]
public sealed class UIButtonDefinitionEditor : Editor
{
    /// <inheritdoc />
    /// <remarks>
    /// Слои те же, что у кнопки в игре и в окне кнопок: подложка в цвет кнопки, поверх картинка
    /// между ними и иконка, каждая своего размера относительно подложки. Результат Unity кеширует,
    /// поэтому смена иконки видна после переимпорта ассета.
    /// </remarks>
    public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
    {
        var definition = (UIButtonDefinition)target;
        // Рисовать нечего — пусть Unity покажет свой значок: по нему хотя бы виден тип ассета.
        if (definition.Icon == null && definition.Decoration == null && definition.Background == null)
            return base.RenderStaticPreview(assetPath, subAssets, width, height);
        RenderTexture buffer = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = buffer;
        GL.Clear(true, true, Color.clear);
        GL.PushMatrix();
        GL.LoadPixelMatrix(0f, width, height, 0f);
        var area = new Rect(0f, 0f, width, height);
        // Все слои помещаются в картинке, сохраняя заданные размеры относительно подложки.
        float largestSize = Mathf.Max(1f, Mathf.Max(
            definition.Decoration != null ? definition.DecorationSize : 0f,
            definition.Icon != null ? definition.IconSize : 0f));
        Rect plate = SizedRect(area, 1f / largestSize);
        DrawSprite(plate, definition.Background, definition.Color);
        DrawSprite(SizedRect(plate, definition.DecorationSize), definition.Decoration, Color.white);
        DrawSprite(SizedRect(plate, definition.IconSize), definition.Icon, Color.white);
        GL.PopMatrix();
        var preview = new Texture2D(width, height, TextureFormat.RGBA32, false);
        preview.ReadPixels(area, 0, 0);
        preview.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(buffer);
        return preview;
    }

    private static Rect SizedRect(Rect rect, float size)
    {
        Vector2 dimensions = rect.size * size;
        return new Rect(rect.center - dimensions * 0.5f, dimensions);
    }

    /// <summary>
    /// Рисует спрайт, сохраняя пропорции.
    /// </summary>
    /// <remarks>
    /// Рисованием на видеокарте, а не чтением пикселей: оно требует Read/Write у текстуры, а у иконок
    /// он выключен. Координаты текстуры считаются по спрайту: он может быть частью атласа.
    /// </remarks>
    private static void DrawSprite(Rect rect, Sprite sprite, Color tint)
    {
        if (sprite == null || sprite.texture == null || rect.width <= 0f || rect.height <= 0f)
            return;
        Texture2D texture = sprite.texture;
        Rect source = sprite.textureRect;
        var coordinates = new Rect(source.x / texture.width, source.y / texture.height,
            source.width / texture.width, source.height / texture.height);
        float scale = Mathf.Min(rect.width / source.width, rect.height / source.height);
        Vector2 dimensions = source.size * scale;
        var fitted = new Rect(rect.center - dimensions * 0.5f, dimensions);
        // Нейтральный цвет у этого вызова — серый 0.5, а не белый.
        Graphics.DrawTexture(fitted, texture, coordinates, 0, 0, 0, 0, tint * 0.5f);
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        serializedObject.Update();
        if (!serializedObject.FindProperty("openWindow").boolValue &&
            serializedObject.FindProperty("action").objectReferenceValue == null && target.GetType() == typeof(UIButtonDefinition))
            EditorGUILayout.HelpBox("Назначьте Action или включите Open Window: сейчас кнопка ничего не выполняет.", MessageType.Warning);
        var definition = (UIButtonDefinition)target;
        if (definition.ShowDescription && string.IsNullOrWhiteSpace(definition.LocalizationKey) &&
            (definition.LocalizationValues == null || definition.LocalizationValues.Count == 0))
            EditorGUILayout.HelpBox("Заполните Description для трёх языков или задайте ключ базы локализации. До этого показывается запасная подпись.", MessageType.Warning);
        if (!definition.ShowButton && definition.Hotkey == KeyCode.None)
            EditorGUILayout.HelpBox("Кнопка скрыта и клавиша не назначена: вызвать её из интерфейса нельзя.", MessageType.Warning);
    }
}
