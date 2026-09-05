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
    /// Незаполненная ссылка отдаёт значение по умолчанию своего набора, а не <c>null</c>.
    /// Так поведение совпадает с тем, что видно в инспекторе: список и без выбора
    /// показывает пункт, и логично, чтобы код получал именно его.
    /// </remarks>
    public override Enumeration ToEnumeration()
    {
        return base.ToEnumeration() ?? GetDefault();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Никогда не <c>null</c>: без выбранного значения отдаётся значение по умолчанию
    /// набора, а если его нет — пустая строка. Так строку можно сравнивать и выводить,
    /// не проверяя её каждый раз.
    /// </remarks>
    public override string Value =>
        string.IsNullOrEmpty(base.Value) ? GetDefault()?.Value ?? string.Empty : base.Value;

    /// <summary>
    /// Значение по умолчанию этого набора.
    /// </summary>
    /// <remarks>
    /// <c>null</c>, если набор своего умолчания не объявил.
    /// </remarks>
    public static Enumeration GetDefault()
    {
        // Кеш общий и лежит в EnumerationExtensions: там он сбрасывается при запуске
        // вместе с остальными, а статическое поле у обобщённого типа пережило бы вход
        // в Play Mode без перезагрузки домена.
        return typeof(T).GetEnumerationDefault();
    }

    /// <summary>
    /// Значение ссылки, которой может не быть вовсе.
    /// </summary>
    /// <remarks>
    /// Поле сериализуемого класса Unity заполняет сама, но до первой сериализации —
    /// у объекта, созданного кодом, или сразу после <c>AddComponent</c> — оно
    /// действительно бывает пустым. Эта форма закрывает и такой случай, поэтому
    /// вызывающему не нужен ни <c>?.</c>, ни проверка на <c>null</c>.
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
