using UnityEngine;

/// <summary>
/// Где спрайт лежит в своей текстуре.
/// </summary>
public static class SpriteUtils
{
    /// <summary>
    /// Прямоугольник спрайта в пикселях его текстуры (<see cref="Sprite.texture"/>).
    /// </summary>
    /// <remarks>
    /// Обычно это <see cref="Sprite.textureRect"/>. Но у спрайта, плотно упакованного в атлас
    /// (<see cref="SpritePackingMode.Tight"/>), обращение к нему бросает исключение — в окне
    /// редактора оно ломает отрисовку всего окна. Такому спрайту берутся границы его UV: они есть
    /// всегда. В рамку могут попасть края соседей по атласу, но для превью и снимка иконки это
    /// лучше, чем ничего.
    /// </remarks>
    /// <returns><c>false</c>, если у спрайта нет текстуры или прямоугольник пустой.</returns>
    public static bool TryGetTextureRect(Sprite sprite, out Rect rect)
    {
        rect = default;

        if (sprite == null || sprite.texture == null)
            return false;

        if (!sprite.packed || sprite.packingMode != SpritePackingMode.Tight)
        {
            rect = sprite.textureRect;
            return rect.width > 0f && rect.height > 0f;
        }

        Vector2[] uv = sprite.uv;

        if (uv == null || uv.Length == 0)
            return false;

        Vector2 min = uv[0];
        Vector2 max = uv[0];

        foreach (Vector2 point in uv)
        {
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        Texture2D texture = sprite.texture;
        rect = Rect.MinMaxRect(
            min.x * texture.width,
            min.y * texture.height,
            max.x * texture.width,
            max.y * texture.height);

        return rect.width > 0f && rect.height > 0f;
    }

    /// <summary>
    /// Прямоугольник спрайта в долях размера его текстуры — для рисования по координатам текстуры.
    /// </summary>
    /// <inheritdoc cref="TryGetTextureRect"/>
    public static bool TryGetTextureCoordinates(Sprite sprite, out Rect coordinates)
    {
        coordinates = default;

        if (!TryGetTextureRect(sprite, out Rect rect))
            return false;

        Texture2D texture = sprite.texture;
        coordinates = new Rect(
            rect.x / texture.width,
            rect.y / texture.height,
            rect.width / texture.width,
            rect.height / texture.height);

        return true;
    }
}
