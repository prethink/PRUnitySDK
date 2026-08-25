using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Управляет скоростью времени по независимым слоям.
/// <para>
/// Значение слоя складывается из базового значения и наложенных модификаторов:
/// базовое задаётся напрямую через Set*, а модификаторы накладывают источники
/// эффектов и снимают по своей ссылке. Модификаторы перемножаются, поэтому два
/// независимых замедления не спорят за одно значение - снятие одного не отменяет
/// второе, как это было при прямой записи.
/// </para>
/// </summary>
public class PRTimeScale : SingletonProviderBase<PRTimeScale>, ISingletonInitializer, IOnPRUpdateEvent
{
    #region Поля и свойства

    public const float DefaultTimeScale = 1f;

    /// <summary>
    /// Базовые значения слоёв - то, что задаётся методами Set*.
    /// </summary>
    private readonly Dictionary<Enumeration, float> layers = new();

    /// <summary>
    /// Наложенные модификаторы по слоям.
    /// </summary>
    private readonly Dictionary<Enumeration, List<TimeScaleModifier>> modifiers = new();

    private bool isInitialize;

    public int InitializeOrder => -1;

    /// <summary>
    /// Есть ли хотя бы один модификатор со сроком действия.
    /// </summary>
    public bool HasActiveTemporaryTimeScales => modifiers.Values
        .Any(list => list.Any(modifier => modifier.EndRealTime.HasValue));

    #endregion

    #region Чтение

    /// <summary>
    /// Действует ли на слое модификатор со сроком действия.
    /// </summary>
    public bool IsTimeScaleTemporaryActive(Enumeration layer)
    {
        return layer != null
            && modifiers.TryGetValue(layer, out var list)
            && list.Any(modifier => modifier.EndRealTime.HasValue);
    }

    /// <summary>
    /// Значение слоя с учётом наложенных модификаторов, без комбинирования с глобальным.
    /// Неизвестный слой возвращает глобальное значение - так же, как и раньше.
    /// </summary>
    public float GetTimeScale(Enumeration layer = null)
    {
        if (!isInitialize)
            return DefaultTimeScale;

        if (layer == null || !layers.ContainsKey(layer))
            return GetLayerValue(PRTimeScaleEnumerationProvider.Global);

        return GetLayerValue(layer);
    }

    /// <summary>
    /// Глобальное значение с учётом модификаторов.
    /// </summary>
    public float GetGlobalTimeScale()
    {
        if (!isInitialize)
            return DefaultTimeScale;

        return GetLayerValue(PRTimeScaleEnumerationProvider.Global);
    }

    /// <summary>
    /// Базовое значение слоя без модификаторов - то, что было задано методами Set*.
    /// </summary>
    public float GetBaseTimeScale(Enumeration layer)
    {
        if (!isInitialize || layer == null || !layers.TryGetValue(layer, out var value))
            return DefaultTimeScale;

        return value;
    }

    /// <summary>
    /// Итоговый масштаб для слоя с учётом глобального и режима комбинирования.
    /// </summary>
    public float Resolve(Enumeration layer = null)
    {
        if (!isInitialize)
            return DefaultTimeScale;

        var globalLayer = PRTimeScaleEnumerationProvider.Global;
        var global = GetLayerValue(globalLayer);

        // Global is the root scale, not a child layer. Combining it with itself
        // would square the value in Multiply mode (0.5 -> 0.25) and make global
        // animations run at a different speed than PRTime and physics.
        if (layer == null || layer == globalLayer)
            return global;

        // Неизвестный слой раньше ронял игру обращением к словарю: теперь он ведёт
        // себя как глобальный, о чём сообщается предупреждением.
        if (!layers.ContainsKey(layer))
        {
            PRLog.WriteWarning(this, $"Unknown time scale layer '{layer.Value}'. Falling back to global scale.");
            return global;
        }

        var value = GetLayerValue(layer);

        return PRUnitySDK.Settings.Project.TimeScaleCombineMode switch
        {
            TimeScaleCombineMode.Multiply => global * value,
            TimeScaleCombineMode.Max => Math.Max(global, value),
            TimeScaleCombineMode.Min => Math.Min(global, value),
            TimeScaleCombineMode.OverrideGlobal => value,
            _ => global * value
        };
    }

    /// <summary>
    /// Модификаторы, наложенные на слой. Пустой список, если слоя нет.
    /// </summary>
    public IReadOnlyList<TimeScaleModifier> GetModifiers(Enumeration layer)
    {
        if (layer == null || !modifiers.TryGetValue(layer, out var list))
            return Array.Empty<TimeScaleModifier>();

        return list;
    }

    #endregion

    #region Базовые значения

    /// <summary>
    /// Задать базовое значение глобального слоя.
    /// </summary>
    public void SetGlobalTimeScale(float value)
    {
        SetTimeScale(PRTimeScaleEnumerationProvider.Global, value);
    }

    /// <summary>
    /// Задать базовое значение слоя. Наложенные модификаторы продолжают действовать
    /// поверх нового значения.
    /// </summary>
    public void SetTimeScale(Enumeration layer, float value)
    {
        if (layer == null)
            throw new ArgumentNullException(nameof(layer));

        layers[layer] = value;
        RaiseChange(layer);
    }

    #endregion

    #region Модификаторы

    /// <summary>
    /// Наложить модификатор на слой.
    /// <para>
    /// В отличие от прямой записи значения, модификаторы одного слоя перемножаются
    /// и снимаются независимо: два источника замедления не мешают друг другу.
    /// </para>
    /// </summary>
    /// <param name="layer">Слой.</param>
    /// <param name="value">Множитель к базовому значению слоя.</param>
    /// <param name="owner">Источник изменения - виден в отладке и позволяет снять
    /// все свои модификаторы разом.</param>
    /// <param name="duration">Длительность в реальных секундах. Ноль или меньше -
    /// модификатор бессрочный.</param>
    public TimeScaleModifierHandle AddModifier(Enumeration layer, float value, object owner = null, float duration = 0f)
    {
        if (layer == null)
            throw new ArgumentNullException(nameof(layer));

        // Длительность считается в реальном времени: иначе замедление продлевало бы
        // само себя, и чем сильнее эффект, тем дольше он бы действовал.
        float? endRealTime = duration > 0f
            ? GetRealTime() + duration
            : null;

        var modifier = new TimeScaleModifier(Guid.NewGuid(), layer, value, owner, endRealTime);

        if (!modifiers.TryGetValue(layer, out var list))
        {
            list = new List<TimeScaleModifier>();
            modifiers[layer] = list;
        }

        list.Add(modifier);
        RaiseChange(layer);

        return new TimeScaleModifierHandle(modifier.Id, layer);
    }

    /// <summary>
    /// Наложить модификатор на глобальный слой.
    /// </summary>
    public TimeScaleModifierHandle AddGlobalModifier(float value, object owner = null, float duration = 0f)
    {
        return AddModifier(PRTimeScaleEnumerationProvider.Global, value, owner, duration);
    }

    /// <summary>
    /// Снять ранее наложенный модификатор.
    /// </summary>
    public bool RemoveModifier(TimeScaleModifierHandle handle)
    {
        if (!handle.IsValid || !modifiers.TryGetValue(handle.Layer, out var list))
            return false;

        var index = list.FindIndex(modifier => modifier.Id == handle.Id);
        if (index < 0)
            return false;

        list.RemoveAt(index);
        RaiseChange(handle.Layer);

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

        var removed = 0;

        foreach (var pair in modifiers)
        {
            var count = pair.Value.RemoveAll(modifier => ReferenceEquals(modifier.Owner, owner));

            if (count <= 0)
                continue;

            removed += count;
            RaiseChange(pair.Key);
        }

        return removed;
    }

    #endregion

    #region Совместимость

    /// <summary>
    /// Временно изменить глобальный масштаб.
    /// </summary>
    public void SetGlobalTimeScaleTemporarily(float value, float callBackTime)
    {
        SetTimeScaleTemporarily(PRTimeScaleEnumerationProvider.Global, value, callBackTime);
    }

    /// <summary>
    /// Временно изменить масштаб слоя.
    /// <para>
    /// Реализовано модификатором со сроком действия, поэтому повторный вызов больше
    /// не игнорируется: эффекты складываются и снимаются каждый в свой момент.
    /// </para>
    /// </summary>
    public TimeScaleModifierHandle SetTimeScaleTemporarily(Enumeration layer, float value, float callBackTime, object owner = null)
    {
        return AddModifier(layer, value, owner, callBackTime);
    }

    #endregion

    #region Сброс

    /// <summary>
    /// Вернуть все слои к значению по умолчанию и снять все модификаторы.
    /// </summary>
    public void Reset()
    {
        modifiers.Clear();

        foreach (var key in layers.Keys.ToList())
            layers[key] = DefaultTimeScale;

        foreach (var key in layers.Keys.ToList())
            RaiseChange(key);
    }

    #endregion

    #region Жизненный цикл

    public void SingletonInitialize()
    {
        var options = new PRTimeScaleEnumerationProvider().GetOptions();
        foreach (var item in options)
            layers[item] = DefaultTimeScale;

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

        var now = GetRealTime();

        foreach (var pair in modifiers)
        {
            var removed = pair.Value.RemoveAll(modifier =>
                modifier.EndRealTime.HasValue && now >= modifier.EndRealTime.Value);

            if (removed > 0)
                RaiseChange(pair.Key);
        }
    }

    #endregion

    #region Внутреннее

    /// <summary>
    /// Значение слоя: базовое, умноженное на все наложенные модификаторы.
    /// </summary>
    private float GetLayerValue(Enumeration layer)
    {
        var value = layers.TryGetValue(layer, out var baseValue) ? baseValue : DefaultTimeScale;

        if (!modifiers.TryGetValue(layer, out var list))
            return value;

        for (var i = 0; i < list.Count; i++)
            value *= list[i].Value;

        return value;
    }

    private void RaiseChange(Enumeration layer)
    {
        PRTimeScaleEvents.RaiseTimeScaleChange(layer, GetLayerValue(layer));
    }

    /// <summary>
    /// Реальное время для отсчёта длительности. PRTime может быть ещё не создан,
    /// поэтому есть запасной вариант.
    /// </summary>
    private static float GetRealTime()
    {
        return PRTime.Instance != null ? PRTime.Instance.RealTime : Time.realtimeSinceStartup;
    }

    #endregion
}
