using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class EnumerationReference<T> : EnumerationReference
    where T : IEnumerationProvider, new()
{
    public static IEnumerable<Enumeration> GetOptions()
    {
        return typeof(T).GetEnumerationsSmart(true);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Незаполненная ссылка отдаёт значение по умолчанию набора, а не <c>null</c>:
    /// в инспекторе пункт показан и без выбора, код должен получать его же.
    /// </remarks>
    public override Enumeration ToEnumeration()
    {
        return base.ToEnumeration() ?? GetDefault();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Никогда не <c>null</c>: без выбора — значение по умолчанию, а без него — пустая строка.
    /// </remarks>
    public override string Value =>
        string.IsNullOrEmpty(base.Value) ? GetDefault()?.Value ?? string.Empty : base.Value;

    /// <summary>
    /// Значение по умолчанию этого набора; <c>null</c>, если набор его не объявил.
    /// </summary>
    /// <remarks>
    /// Кеш общий и лежит в <see cref="EnumerationExtensions"/>: статическое поле
    /// обобщённого типа пережило бы вход в Play Mode без перезагрузки домена.
    /// </remarks>
    public static Enumeration GetDefault()
    {
        return typeof(T).GetEnumerationDefault();
    }

    /// <summary>
    /// Значение ссылки, которой может не быть вовсе.
    /// </summary>
    /// <remarks>
    /// У объекта, созданного кодом, и сразу после <c>AddComponent</c> поле бывает пустым,
    /// поэтому вызывающему не нужны ни <c>?.</c>, ни проверка на <c>null</c>.
    /// </remarks>
    public static Enumeration ToEnumeration(EnumerationReference<T> reference)
    {
        return reference != null ? reference.ToEnumeration() : GetDefault();
    }

    /// <summary>
    /// Значение ссылки строкой; пустая строка вместо <c>null</c>.
    /// </summary>
    public static string ToValue(EnumerationReference<T> reference)
    {
        return reference != null ? reference.Value : GetDefault()?.Value ?? string.Empty;
    }

    public void SetDefaultIfNull(Enumeration enumeration)
    {
        if(string.IsNullOrEmpty(value))
            Set(enumeration);
    }

    public void Set(Enumeration enumeration)
    {
        if (enumeration == null)
            throw new ArgumentNullException(nameof(enumeration));

        var available = GetOptions();
        if (!available.Any(e => e == enumeration))
            throw new ArgumentException($"Enumeration '{enumeration}' не существует в {typeof(T).Name}.", nameof(enumeration));

        value = enumeration.Value;
    }
}

/// <summary>
/// Базовая часть ссылки: хранит выбранную строку.
/// </summary>
/// <remarks>
/// В полях используйте <see cref="EnumerationReference{T}"/>: здесь нет набора, а значит
/// ни выпадающего списка, ни значения по умолчанию.
/// </remarks>
[Serializable]
public class EnumerationReference
{
    [SerializeField]
    protected string value;

    /// <summary>
    /// Выбранное значение.
    /// </summary>
    public virtual string Value => value;

    /// <summary>
    /// Значение ссылки как <see cref="Enumeration"/>.
    /// </summary>
    public virtual Enumeration ToEnumeration() => Enumeration.GetOrCreate(value);

    public const string ProtectedStringValueName = nameof(value);
}
