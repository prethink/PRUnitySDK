using UnityEngine;

/// <summary>
/// Показывает рядом с полем иконку объекта, на который оно ссылается.
/// </summary>
/// <remarks>
/// В отличие от <see cref="SpritePreviewAttribute"/> работает не только со спрайтом,
/// но и с любым определением, у которого есть иконка — валютой, предметом, наградой.
/// По названию ассета не всегда понятно, что именно выбрано, а по монете или кристаллу —
/// сразу.
/// </remarks>
public class IconPreviewAttribute : PropertyAttribute
{
    /// <summary>
    /// Сторона квадрата превью.
    /// </summary>
    public float Size { get; }

    /// <summary>
    /// Рисовать превью под полем, а не справа от него.
    /// </summary>
    /// <remarks>
    /// Справа помещается только мелкая картинка, зато поле не растёт в высоту — так удобно
    /// спискам однотипных полей. Крупное превью занимает отдельную строку, как у
    /// <see cref="SpritePreviewAttribute"/>: разглядеть награду важнее, чем сэкономить
    /// высоту на единственном поле.
    /// </remarks>
    public bool Below { get; }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="size">Сторона квадрата превью в пикселях.</param>
    /// <param name="below">Рисовать превью под полем.</param>
    public IconPreviewAttribute(float size = 36f, bool below = false)
    {
        Size = size;
        Below = below;
    }
}
