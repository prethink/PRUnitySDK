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
    /// Кого модификатор не трогает: объект, у которого есть хоть один из этих слоёв, не задевается вовсе —
    /// даже если другие его слои есть в <see cref="Affects"/>.
    /// </summary>
    public TimeLayer Except { get; }

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
    /// Задевает ли модификатор объект с этими слоями: есть общий слой с <see cref="Affects"/> и нет ни одного
    /// из <see cref="Except"/>.
    /// </summary>
    /// <remarks>
    /// Исключение — по объекту, а не по слою. Враг-моб (<c>Mobs | Enemy</c>) при «всё, кроме врагов» не замедляется:
    /// вычитание слоя из маски задело бы его через <see cref="TimeLayer.Mobs"/>.
    /// </remarks>
    public bool Hits(TimeLayer layers) => (Affects & layers) != 0 && (Except & layers) == 0;

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
