using System;
using System.Collections.Generic;

/// <summary>
/// Сохранённое состояние одного объекта сцены.
/// </summary>
/// <remarks>
/// Активность лежит отдельным полем, потому что нужна почти всем. Остальное наследники
/// <c>SaveableObjectState</c> кладут своими ключами <see cref="EnumerationType{T}"/>,
/// которые несут и имя значения, и его тип, поэтому <c>Set</c> и <c>TryGet</c> работают
/// с настоящим типом.
/// <para>
/// Значения разложены по словарям на тип, как в <c>ProjectProperties</c>: после записи
/// в JSON иначе не понять, чем была строка <c>"1"</c>.
/// </para>
/// </remarks>
[Serializable]
public class SceneObjectState : ICloneable
{
    /// <summary>
    /// Был ли объект включён на момент сохранения.
    /// </summary>
    public bool IsActive = true;

    /// <remarks>
    /// Словари создаются, только когда в них впервые пишут, и остаются <c>null</c>
    /// у записей без своих значений. Ради веса сохранения: подбираемых предметов
    /// на уровне бывают сотни, и пять пустых словарей на каждый — это сотни пустых
    /// объектов в JSON без единого полезного байта.
    /// </remarks>
    public Dictionary<string, long> LongValues;
    public Dictionary<string, float> FloatValues;
    public Dictionary<string, bool> BoolValues;
    public Dictionary<string, string> StringValues;
    public Dictionary<string, DateTime> DateTimeValues;

    /// <summary>
    /// В записи есть хоть одно своё значение.
    /// </summary>
    public bool HasValues =>
        HasAny(LongValues) || HasAny(FloatValues) || HasAny(BoolValues) ||
        HasAny(StringValues) || HasAny(DateTimeValues);

    /// <summary>
    /// Запоминает значение.
    /// </summary>
    public void Set<T>(EnumerationType<T> key, T value)
    {
        GetOrCreateValues<T>()[GetName(key)] = value;
    }

    /// <summary>
    /// Читает значение, если оно сохранялось.
    /// </summary>
    public bool TryGet<T>(EnumerationType<T> key, out T value)
    {
        value = default;

        Dictionary<string, T> values = GetValues<T>();
        return values != null && values.TryGetValue(GetName(key), out value);
    }

    /// <summary>
    /// Читает значение либо возвращает запасное.
    /// </summary>
    /// <remarks>
    /// Запасное значение стоит указывать там, где сохранённые <c>0</c> или <c>false</c>
    /// нужно отличать от «не сохранялось ни разу».
    /// </remarks>
    public T Get<T>(EnumerationType<T> key, T fallback = default)
    {
        return TryGet(key, out T value) ? value : fallback;
    }

    /// <summary>
    /// Забывает значение.
    /// </summary>
    public bool Remove<T>(EnumerationType<T> key)
    {
        Dictionary<string, T> values = GetValues<T>();
        return values != null && values.Remove(GetName(key));
    }

    /// <inheritdoc />
    public object Clone()
    {
        return new SceneObjectState
        {
            IsActive = IsActive,
            LongValues = Copy(LongValues),
            FloatValues = Copy(FloatValues),
            BoolValues = Copy(BoolValues),
            StringValues = Copy(StringValues),
            DateTimeValues = Copy(DateTimeValues)
        };
    }

    /// <summary>
    /// Словарь под тип значения.
    /// </summary>
    /// <remarks>
    /// Перебор типов, а не общий словарь <c>object</c>: список поддерживаемых типов
    /// закрытый и совпадает с тем, что умеет хранить <c>ProjectProperties</c>.
    /// Незнакомый тип — ошибка программиста, и молчать о ней нельзя: значение просто
    /// не сохранилось бы.
    /// </remarks>
    private Dictionary<string, T> GetValues<T>()
    {
        if (typeof(T) == typeof(long))
            return (Dictionary<string, T>)(object)LongValues;

        if (typeof(T) == typeof(float))
            return (Dictionary<string, T>)(object)FloatValues;

        if (typeof(T) == typeof(bool))
            return (Dictionary<string, T>)(object)BoolValues;

        if (typeof(T) == typeof(string))
            return (Dictionary<string, T>)(object)StringValues;

        if (typeof(T) == typeof(DateTime))
            return (Dictionary<string, T>)(object)DateTimeValues;

        throw new NotSupportedException(
            $"Тип значения [{typeof(T)}] в состоянии объекта не поддерживается.");
    }

    /// <summary>
    /// Словарь под тип значения, создавая его при первой записи.
    /// </summary>
    private Dictionary<string, T> GetOrCreateValues<T>()
    {
        Dictionary<string, T> values = GetValues<T>();

        if (values != null)
            return values;

        values = new Dictionary<string, T>();
        SetValues(values);

        return values;
    }

    private void SetValues<T>(Dictionary<string, T> values)
    {
        if (typeof(T) == typeof(long))
            LongValues = (Dictionary<string, long>)(object)values;
        else if (typeof(T) == typeof(float))
            FloatValues = (Dictionary<string, float>)(object)values;
        else if (typeof(T) == typeof(bool))
            BoolValues = (Dictionary<string, bool>)(object)values;
        else if (typeof(T) == typeof(string))
            StringValues = (Dictionary<string, string>)(object)values;
        else if (typeof(T) == typeof(DateTime))
            DateTimeValues = (Dictionary<string, DateTime>)(object)values;
    }

    private static bool HasAny<TValue>(Dictionary<string, TValue> values)
    {
        return values != null && values.Count > 0;
    }

    private static Dictionary<string, TValue> Copy<TValue>(Dictionary<string, TValue> values)
    {
        return values == null ? null : new Dictionary<string, TValue>(values);
    }

    private static string GetName<T>(EnumerationType<T> key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        return key.Value;
    }
}
