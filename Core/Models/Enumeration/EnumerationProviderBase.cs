using System.Collections.Generic;
using System.Linq;

public abstract class EnumerationProviderBase : IEnumerationProvider
{
    public abstract bool IncludeInherited { get; }

    /// <summary>
    /// Значение, которое подразумевается, пока не выбрано другое.
    /// </summary>
    /// <remarks>
    /// Свойство абстрактное: чем заменить незаполненную ссылку, решает сам набор.
    /// Обычный ответ — <see cref="FirstOption"/>; если умолчания нет, верните <c>null</c>.
    /// </remarks>
    public abstract Enumeration Default { get; }

    /// <summary>
    /// Первое объявленное значение набора.
    /// </summary>
    /// <remarks>
    /// Порядок задаёт <see cref="EnumerationOrderAttribute"/>, а без него — объявление
    /// в коде; при <see cref="IncludeInherited"/> сначала идёт базовый набор.
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
