using UnityEngine;

/// <summary>
/// Драйвер масштаба времени тела с явно заданными слоями.
/// <para>
/// Подходит объектам без сущности: снаряды, реквизит, платформы. Если объект - сущность,
/// используйте <see cref="EntityTimeScaleDriver"/>: он берёт слои у неё и не требует
/// дублировать настройку в двух местах.
/// </para>
/// </summary>
public class RigidbodyTimeScaleDriver : RigidbodyTimeScaleDriverBase
{
    [Header("Слои")]
    [Tooltip("Слои времени тела: какие замедления его задевают. Только World — тело живёт в темпе мира, и драйвер ничего не делает.")]
    [SerializeField] private TimeLayer timeLayers = TimeLayer.World;

    /// <inheritdoc />
    protected override TimeLayer GetTimeLayers()
    {
        return timeLayers;
    }

    /// <summary>
    /// Задать слои времени в рантайме.
    /// </summary>
    public void SetTimeLayers(TimeLayer layers)
    {
        timeLayers = layers;
        ApplyScaleChange();
    }
}
