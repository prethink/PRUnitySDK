using System;
using UnityEngine;

/// <summary>
/// Настройки сущностей.
/// </summary>
[Serializable]
[SettingsDescription("Сущности: как выглядит каркас, который остаётся на месте спрятанной сущности в режимах HideWire и HideWirePolygons.")]
public class EntitySettings
{
    [field: Header("Каркас")]

    [field: SerializeField, Tooltip("Цвет линий каркаса по треугольникам (HideWire).")]
    public Color TriangleWireColor { get; private set; } = new(0.2f, 0.9f, 1f, 0.8f);

    [field: SerializeField, Tooltip("Цвет линий каркаса по граням (HideWirePolygons).")]
    public Color PolygonWireColor { get; private set; } = new(0.2f, 0.9f, 1f, 0.8f);

    [field: SerializeField, Tooltip("Заливка между линиями. Видна только у моделей без доступа на чтение: рёбра у них не достать, и вместо каркаса рисуется сетка по поверхности.")]
    public Color WireFillColor { get; private set; } = new(0.1f, 0.7f, 0.9f, 0.08f);
}
