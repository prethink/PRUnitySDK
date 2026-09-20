using UnityEngine;

/// <summary>
/// Рисует под полем сглаживания график того, как оно выглядит.
/// </summary>
/// <remarks>
/// По названию вроде <c>OutSine</c> или <c>InOutBack</c> не видно, что получится:
/// разгон, торможение, отскок за край. График показывает это сразу, и подбирать
/// сглаживание перебором запусков больше не нужно.
/// </remarks>
public class EasePreviewAttribute : PropertyAttribute
{
    /// <summary>
    /// Высота графика в пикселях.
    /// </summary>
    public float Height { get; }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="height">Высота графика в пикселях.</param>
    public EasePreviewAttribute(float height = 52f)
    {
        Height = height;
    }
}
