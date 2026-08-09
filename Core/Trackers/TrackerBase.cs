using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Базовый реестр объектов с единым контрактом регистрации и удаления.
/// </summary>
public abstract class TrackerBase<T>
{
    /// <summary>
    /// Внутренняя изменяемая коллекция зарегистрированных элементов.
    /// </summary>
    protected List<T> elements = new List<T>();

    /// <summary>
    /// Возвращает снимок зарегистрированных элементов.
    /// </summary>
    public IReadOnlyList<T> Elements => elements.ToList();

    /// <summary>
    /// Регистрирует элемент, если он допустим и ещё не зарегистрирован.
    /// </summary>
    public abstract bool Register(T element);

    /// <summary>
    /// Удаляет ранее зарегистрированный элемент.
    /// </summary>
    public abstract bool Unregister(T element);

    public virtual bool Contains(T element)
    {
        return elements.Contains(element);
    }
}
