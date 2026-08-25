using System;

/// <summary>
/// Наложенное изменение масштаба времени с известным владельцем.
/// <para>
/// Модификаторы одного слоя перемножаются, поэтому два независимых источника
/// замедления не спорят за одно значение: снятие одного не отменяет второй.
/// </para>
/// </summary>
public class TimeScaleModifier
{
    /// <summary>
    /// Идентификатор наложения.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Слой, на который действует модификатор.
    /// </summary>
    public Enumeration Layer { get; }

    /// <summary>
    /// Множитель к базовому значению слоя.
    /// </summary>
    public float Value { get; internal set; }

    /// <summary>
    /// Кто наложил модификатор. Нужен для отладки и для снятия всех изменений
    /// одного источника разом.
    /// </summary>
    public object Owner { get; }

    /// <summary>
    /// Момент реального времени, когда модификатор снимается сам.
    /// Null - модификатор бессрочный и снимается только вручную.
    /// </summary>
    public float? EndRealTime { get; internal set; }

    /// <summary>
    /// Название источника для отладки.
    /// </summary>
    public string OwnerName => Owner switch
    {
        null => "unknown",
        UnityEngine.Object unityObject => unityObject != null ? unityObject.name : "destroyed",
        _ => Owner.GetType().Name
    };

    public TimeScaleModifier(Guid id, Enumeration layer, float value, object owner, float? endRealTime)
    {
        Id = id;
        Layer = layer;
        Value = value;
        Owner = owner;
        EndRealTime = endRealTime;
    }
}

/// <summary>
/// Ссылка на наложенный модификатор. Возвращается при наложении и нужна,
/// чтобы снять именно своё изменение, не задев чужие.
/// </summary>
public readonly struct TimeScaleModifierHandle
{
    /// <summary>
    /// Пустая ссылка - наложение не состоялось.
    /// </summary>
    public static readonly TimeScaleModifierHandle None = default;

    /// <summary>
    /// Идентификатор наложения.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Слой, на который наложен модификатор.
    /// </summary>
    public Enumeration Layer { get; }

    /// <summary>
    /// Указывает ли ссылка на реальное наложение.
    /// </summary>
    public bool IsValid => Id != Guid.Empty;

    public TimeScaleModifierHandle(Guid id, Enumeration layer)
    {
        Id = id;
        Layer = layer;
    }
}
