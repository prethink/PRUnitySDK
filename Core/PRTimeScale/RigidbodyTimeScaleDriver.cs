using UnityEngine;

/// <summary>
/// Драйвер масштаба времени тела с явно заданным слоем.
/// <para>
/// Подходит объектам без сущности: снаряды, реквизит, платформы. Если объект - сущность,
/// используйте <see cref="EntityTimeScaleDriver"/>: он берёт слой у неё и не требует
/// дублировать настройку в двух местах.
/// </para>
/// </summary>
public class RigidbodyTimeScaleDriver : RigidbodyTimeScaleDriverBase
{
    [Header("Слой")]
    [Tooltip("Слой масштаба времени. Пусто - тело живёт в глобальном темпе, и драйвер ничего не делает.")]
    [SerializeField] private EnumerationReference<PRTimeScaleEnumerations> timeScaleLayer;

    /// <inheritdoc />
    protected override Enumeration GetTimeScaleLayer()
    {
        return timeScaleLayer?.ToEnumeration();
    }

    /// <summary>
    /// Задать слой масштаба времени в рантайме.
    /// </summary>
    public void SetTimeScaleLayer(Enumeration layer)
    {
        timeScaleLayer ??= new EnumerationReference<PRTimeScaleEnumerations>();

        if (layer != null)
            timeScaleLayer.Set(layer);

        ApplyScaleChange();
    }
}
