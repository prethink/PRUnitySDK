using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Масштаб времени по слоям (<see cref="TimeLayer"/>): замедления и ускорения накладываются модификаторами.
/// </summary>
/// <remarks>
/// Модификатор задевает набор слоёв и может часть исключить. Масштаб объекта — произведение всех модификаторов,
/// у которых с ним есть общий слой. Так собираются и общее замедление (<see cref="TimeLayer.Global"/> — все слои), и выборочные эффекты: замедлить
/// мир, кроме игрока, — как маска Кроноса в Crash Bandicoot 4, — или одних врагов. Модификаторы перемножаются и
/// снимаются каждый своим владельцем, поэтому два источника замедления не спорят за одно значение.
/// <para>
/// Темп мира (<see cref="TimeLayer.World"/>) — это шаг физики и <see cref="PRTime.GameDeltaTime"/>. Остальным
/// слоям драйверы добавляют разницу с миром (<see cref="RelativeToWorld"/>): симуляция PhysX одна на сцену.
/// Поэтому тело без драйвера идёт в темпе мира, даже если у его сущности свой слой. По той же причине мир нельзя
/// остановить совсем, оставив кого-то идти: при нуле физика не шагает ни для кого — для «всё, кроме игрока»
/// годится сильное замедление (0,05), но не 0.
/// </para>
/// </remarks>
public class PRTimeScale : SingletonProviderBase<PRTimeScale>, ISingletonInitializer, IOnPRUpdateEvent
{
    #region Поля и свойства

    public const float DefaultTimeScale = 1f;

    private readonly List<TimeScaleModifier> modifiers = new();

    private bool isInitialize;

    public int InitializeOrder => -1;

    /// <summary>
    /// Наложенные модификаторы.
    /// </summary>
    public IReadOnlyList<TimeScaleModifier> Modifiers => modifiers;

    /// <summary>
    /// Есть ли хотя бы один модификатор со сроком действия.
    /// </summary>
    public bool HasActiveTemporaryTimeScales
    {
        get
        {
            foreach (TimeScaleModifier modifier in modifiers)
                if (modifier.EndRealTime.HasValue)
                    return true;

            return false;
        }
    }

    #endregion

    #region Чтение

    /// <summary>
    /// Масштаб времени объекта с этими слоями: произведение модификаторов, задевающих хоть один из них.
    /// </summary>
    /// <remarks>
    /// Объект без слоёв считается частью мира. Модификатор с истёкшим сроком не учитывается, даже если его ещё не
    /// сняли: снимаются они после того, как <see cref="PRTime"/> посчитал время кадра, и иначе действовали бы
    /// лишний кадр.
    /// </remarks>
    public float Resolve(TimeLayer layers)
    {
        if (!isInitialize)
            return DefaultTimeScale;

        if (layers == TimeLayer.None)
            layers = TimeLayer.World;

        float now = GetRealTime();
        float value = DefaultTimeScale;

        for (int i = 0; i < modifiers.Count; i++)
        {
            TimeScaleModifier modifier = modifiers[i];
            if (modifier.Hits(layers) && !modifier.IsExpired(now))
                value *= modifier.Value;
        }

        return value;
    }

    /// <summary>
    /// Касается ли изменение объекта с этими слоями. Объект без слоёв — часть мира, как и в <see cref="Resolve"/>.
    /// </summary>
    /// <param name="changed">Слои из события <see cref="IOnPRTimeScaleChange"/>.</param>
    /// <param name="layers">Слои объекта.</param>
    public static bool Touches(TimeLayer changed, TimeLayer layers)
    {
        return (changed & (layers == TimeLayer.None ? TimeLayer.World : layers)) != 0;
    }

    /// <summary>
    /// Слои компонента — у его сущности на нём, в родителях или по ссылке (<see cref="TimeLayerClock"/>); нет — мир.
    /// </summary>
    /// <remarks>
    /// Ищет по иерархии при каждом вызове. Компоненту, которому время нужно каждый кадр, — держать
    /// <see cref="TimeLayerClock"/> полем.
    /// </remarks>
    public static TimeLayer LayersOf(Component component)
    {
        return component != null ? new TimeLayerClock(component).Layers : TimeLayer.World;
    }

    /// <summary>
    /// Темп мира: шаг физики и <see cref="PRTime.GameDeltaTime"/>.
    /// </summary>
    public float GetWorldTimeScale() => Resolve(TimeLayer.World);

    /// <summary>
    /// Во сколько раз объект с этими слоями идёт быстрее или медленнее мира.
    /// </summary>
    /// <remarks>
    /// Темп мира уже заложен в шаг физики и в <see cref="PRTime.GameDeltaTime"/>; учесть его второй раз — значит
    /// при замедлении мира в 4 раза замедлить объект в 16. При остановленном мире физика стоит, и множитель
    /// значения не имеет — тогда 1.
    /// </remarks>
    public float RelativeToWorld(TimeLayer layers)
    {
        float world = GetWorldTimeScale();
        if (world <= Mathf.Epsilon)
            return 1f;

        return Resolve(layers) / world;
    }

    #endregion

    #region Модификаторы

    /// <summary>
    /// Наложить модификатор.
    /// </summary>
    /// <param name="value">Множитель масштаба времени: 0,3 — втрое медленнее, 0 — стоп. NaN и бесконечность
    /// отклоняются (вернётся пустая ссылка), отрицательное — обрезается до нуля.</param>
    /// <param name="affects">На какие слои действует; по умолчанию — <see cref="TimeLayer.Global"/>, на всё.</param>
    /// <param name="except">Какие слои не трогать: «всё, кроме игрока» — <c>except: TimeLayer.Player</c>.</param>
    /// <param name="owner">Источник — виден в отладке и позволяет снять все свои модификаторы разом.</param>
    /// <param name="duration">Длительность в секундах <see cref="PRTime.RealTime"/>. Ноль или меньше — бессрочно.</param>
    public TimeScaleModifierHandle AddModifier(float value, TimeLayer affects = TimeLayer.Global, TimeLayer except = TimeLayer.None,
        object owner = null, float duration = 0f)
    {
        // NaN или бесконечность в произведении испортили бы масштаб всем задетым объектам, а с ним — скорости тел.
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            PRLog.WriteWarning(this, $"Time scale modifier from '{owner}' ignored: value {value} is not a finite number.");
            return TimeScaleModifierHandle.None;
        }

        // Отрицательного времени нет: такое значение — ошибка вызова, ближе всего к ней остановка.
        if (value < 0f)
        {
            PRLog.WriteWarning(this, $"Time scale modifier from '{owner}' clamped to 0: value {value} is negative.");
            value = 0f;
        }

        // Длительность считается по реальным часам, а не по игровым: иначе замедление продлевало бы само себя,
        // и чем сильнее эффект, тем дольше он бы действовал.
        float? endRealTime = duration > 0f ? GetRealTime() + duration : null;

        var modifier = new TimeScaleModifier(Guid.NewGuid(), affects, except, value, owner, endRealTime);
        modifiers.Add(modifier);
        RaiseChange(modifier.Affects);

        return new TimeScaleModifierHandle(modifier.Id);
    }

    /// <summary>
    /// Наложить модификатор на всё.
    /// </summary>
    public TimeScaleModifierHandle AddGlobalModifier(float value, object owner = null, float duration = 0f)
    {
        return AddModifier(value, TimeLayer.Global, TimeLayer.None, owner, duration);
    }

    /// <summary>
    /// Снять ранее наложенный модификатор.
    /// </summary>
    public bool RemoveModifier(TimeScaleModifierHandle handle)
    {
        if (!handle.IsValid)
            return false;

        int index = modifiers.FindIndex(modifier => modifier.Id == handle.Id);
        if (index < 0)
            return false;

        TimeLayer affects = modifiers[index].Affects;
        modifiers.RemoveAt(index);
        RaiseChange(affects);

        return true;
    }

    /// <summary>
    /// Снять все модификаторы указанного источника - удобно в OnDisable или
    /// при уничтожении эффекта, чтобы не хранить каждую ссылку отдельно.
    /// </summary>
    /// <returns>Сколько модификаторов было снято.</returns>
    public int RemoveModifiers(object owner)
    {
        if (owner == null)
            return 0;

        return RemoveWhere(modifier => ReferenceEquals(modifier.Owner, owner));
    }

    /// <summary>
    /// Снять все модификаторы: время снова обычное.
    /// </summary>
    public void Reset()
    {
        RemoveWhere(_ => true);
    }

    #endregion

    #region Жизненный цикл

    public void SingletonInitialize()
    {
        isInitialize = true;
        EventBus.Subscribe(this);
    }

    /// <summary>
    /// Снимает модификаторы, срок которых вышел.
    /// </summary>
    public void OnPRUpdateEvent()
    {
        if (modifiers.Count == 0)
            return;

        float now = GetRealTime();
        RemoveWhere(modifier => modifier.IsExpired(now));
    }

    #endregion

    #region Внутреннее

    /// <summary>
    /// Снимает подходящие модификаторы и одним событием сообщает, чьё время изменилось.
    /// </summary>
    private int RemoveWhere(Predicate<TimeScaleModifier> match)
    {
        TimeLayer changed = TimeLayer.None;
        int removed = 0;

        for (int i = modifiers.Count - 1; i >= 0; i--)
        {
            if (!match(modifiers[i]))
                continue;

            changed |= modifiers[i].Affects;
            modifiers.RemoveAt(i);
            removed++;
        }

        if (removed > 0)
            RaiseChange(changed);

        return removed;
    }

    private static void RaiseChange(TimeLayer layers)
    {
        PRTimeScaleEvents.RaiseTimeScaleChange(layers);
    }

    /// <summary>
    /// Реальное время для отсчёта длительности. PRTime может быть ещё не создан,
    /// поэтому есть запасной вариант.
    /// </summary>
    private static float GetRealTime()
    {
        return PRTime.HasInstance ? PRTime.Instance.RealTime : Time.realtimeSinceStartup;
    }

    #endregion
}
