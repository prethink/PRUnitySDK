using System.Collections.Generic;
using System.Linq;

public abstract class EnumerationProviderBase : IEnumerationProvider
{
    public abstract bool IncludeInherited { get; }

    /// <summary>
    /// Значение, которое подразумевается, пока не выбрано другое.
    /// </summary>
    /// <remarks>
    /// Объявляется обязательным, а не берётся по умолчанию: незаполненная ссылка приходит
    /// в код как <c>null</c>, и чем его заменить — решение набора, а не общее правило.
    /// Обычный ответ — <see cref="FirstOption"/>; набору, у которого разумного умолчания
    /// нет, честнее вернуть <c>null</c>.
    /// </remarks>
    public abstract Enumeration Default { get; }

    /// <summary>
    /// Первое объявленное значение набора.
    /// </summary>
    /// <remarks>
    /// Порядок не случайный: поля сортируются по <c>MetadataToken</c>, то есть идут
    /// в порядке объявления в коде. При <see cref="IncludeInherited"/> первыми идут
    /// значения базового набора.
    /// </remarks>
    protected Enumeration FirstOption
    {
        get
        {
            return GetOptions().FirstOrDefault();
        }
    }

    public virtual IEnumerable<Enumeration> GetOptions()
    {
        return GetType().GetEnumerations(this.IncludeInherited);
    }
}
