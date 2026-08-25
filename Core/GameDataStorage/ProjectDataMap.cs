using System;
using System.Collections.Generic;

/// <summary>
/// Предоставляет общие операции над одним словарём внутри текущего
/// <see cref="ProjectData"/> без знания о сохранении и доменных событиях.
/// </summary>
/// <typeparam name="TKey">Тип ключа.</typeparam>
/// <typeparam name="TValue">Тип значения.</typeparam>
public sealed class ProjectDataMap<TKey, TValue>
{
    private readonly Func<ProjectData> projectDataProvider;
    private readonly Func<ProjectData, IDictionary<TKey, TValue>> valuesSelector;
    private readonly IEqualityComparer<TValue> valueComparer;

    /// <summary>
    /// Создаёт адаптер к словарю текущих данных проекта.
    /// </summary>
    /// <param name="projectDataProvider">Источник текущего экземпляра ProjectData.</param>
    /// <param name="valuesSelector">Селектор словаря внутри ProjectData.</param>
    /// <param name="valueComparer">Необязательный компаратор значений.</param>
    public ProjectDataMap(
        Func<ProjectData> projectDataProvider,
        Func<ProjectData, IDictionary<TKey, TValue>> valuesSelector,
        IEqualityComparer<TValue> valueComparer = null)
    {
        this.projectDataProvider = projectDataProvider
            ?? throw new ArgumentNullException(nameof(projectDataProvider));
        this.valuesSelector = valuesSelector
            ?? throw new ArgumentNullException(nameof(valuesSelector));
        this.valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;
    }

    /// <summary>
    /// Пытается получить сохранённое значение.
    /// </summary>
    public bool TryGetValue(TKey key, out TValue value)
    {
        ValidateKey(key);
        return GetValues().TryGetValue(key, out value);
    }

    /// <summary>
    /// Возвращает сохранённое значение или fallback без изменения ProjectData.
    /// </summary>
    public TValue GetValue(TKey key, TValue fallback = default)
    {
        return TryGetValue(key, out var value) ? value : fallback;
    }

    /// <summary>
    /// Возвращает существующее значение либо добавляет initialValue под новым ключом.
    /// </summary>
    public ValueChange<TValue> GetOrCreateValue(TKey key, TValue initialValue = default)
    {
        ValidateKey(key);

        var values = GetValues();
        if (values.TryGetValue(key, out var currentValue))
            return new ValueChange<TValue>(true, currentValue, currentValue, false);

        values.Add(key, initialValue);
        return new ValueChange<TValue>(false, default, initialValue, true);
    }

    /// <summary>
    /// Устанавливает значение и сообщает предыдущее состояние ключа.
    /// Повторная установка эквивалентного значения не изменяет словарь.
    /// </summary>
    public ValueChange<TValue> SetValue(TKey key, TValue value)
    {
        ValidateKey(key);

        var values = GetValues();
        var hadPreviousValue = values.TryGetValue(key, out var previousValue);
        var changed = !hadPreviousValue || !valueComparer.Equals(previousValue, value);

        if (changed)
            values[key] = value;

        return new ValueChange<TValue>(hadPreviousValue, previousValue, value, changed);
    }

    /// <summary>
    /// Удаляет значение и возвращает его через previousValue.
    /// </summary>
    public bool TryRemoveValue(TKey key, out TValue previousValue)
    {
        ValidateKey(key);

        var values = GetValues();
        if (!values.TryGetValue(key, out previousValue))
            return false;

        return values.Remove(key);
    }

    private IDictionary<TKey, TValue> GetValues()
    {
        var projectData = projectDataProvider();
        if (projectData == null)
            throw new InvalidOperationException(
                "ProjectDataMap: данные проекта ещё не загружены.");

        var values = valuesSelector(projectData);
        if (values == null)
            throw new InvalidOperationException(
                "ProjectDataMap: селектор вернул null вместо словаря значений.");

        return values;
    }

    private static void ValidateKey(TKey key)
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));
    }
}
