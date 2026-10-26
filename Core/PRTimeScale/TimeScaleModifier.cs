using System;

/// <summary>
/// Наложенное изменение масштаба времени с известным владельцем.
/// <para>
/// Модификаторы перемножаются, поэтому два независимых источника замедления не спорят за одно
/// значение: снятие одного не отменяет второй.
/// </para>
/// </summary>
public class TimeScaleModifier
{
    /// <summary>
    /// Идентификатор наложения.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// На какие слои модификатор действует, как его наложили.
    /// </summary>
    public TimeLayer Affects { get; }

    /// <summary>
    /// Какие слои модификатор не трогает, даже если они есть в <see cref="Affects"/>.
    /// </summary>
    public TimeLayer Except { get; }

    /// <summary>
    /// Слои, которые модификатор задевает на деле: <see cref="Affects"/> без <see cref="Except"/>.
    /// </summary>
    public TimeLayer Targets => Affects & ~Except;

    /// <summary>
    /// Множитель масштаба времени.
    /// </summary>
    public float Value { get; internal set; }

    /// <summary>
    /// Кто наложил модификатор. Нужен для отладки и для снятия всех изменений
    /// одного источника разом.
    /// </summary>
    public object Owner { get; }

    /// <summary>
    /// Момент <see cref="PRTime.RealTime"/>, когда модификатор снимается сам.
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

    public TimeScaleModifier(Guid id, TimeLayer affects, TimeLayer except, float value, object owner, float? endRealTime)
    {
        Id = id;
        Affects = affects;
        Except = except;
        Value = value;
        Owner = owner;
        EndRealTime = endRealTime;
    }

    /// <summary>
    /// Задевает ли модификатор объект с этими слоями: есть хоть один общий.
    /// </summary>
    public bool Hits(TimeLayer layers) => (Targets & layers) != 0;

    /// <summary>
    /// Истёк ли срок к этому моменту.
    /// </summary>
    public bool IsExpired(float realTime) => EndRealTime.HasValue && realTime >= EndRealTime.Value;
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
    /// Указывает ли ссылка на реальное наложение.
    /// </summary>
    public bool IsValid => Id != Guid.Empty;

    public TimeScaleModifierHandle(Guid id)
    {
        Id = id;
    }
}
