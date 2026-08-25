using System;

/// <summary>
/// Базовая логика награды, активной до сохранённого момента времени.
/// <para>
/// Состояние хранит <see cref="TimeLimitedRewardService"/> в отдельном наборе данных
/// проекта. Раньше момент окончания лежал среди произвольных DateTime-свойств:
/// награды нельзя было перечислить, они не сообщали об истечении, а ключ мог
/// совпасть с чужим свойством.
/// </para>
/// </summary>
public abstract class TimeLimitedRewardBase
{
    /// <summary>
    /// Ключ награды по умолчанию.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Проверяет, активна ли награда с ключом по умолчанию.
    /// </summary>
    public bool IsActive(out DateTime endTime)
    {
        return IsActive(Name, out endTime);
    }

    /// <summary>
    /// Сколько осталось до окончания награды с ключом по умолчанию.
    /// </summary>
    public TimeSpan GetRemaining()
    {
        return GetRemaining(Name);
    }

    /// <summary>
    /// Добавляет время награде с ключом по умолчанию.
    /// </summary>
    public void AddTime(TimeSpan addTime)
    {
        AddTime(Name, addTime);
    }

    /// <summary>
    /// Снимает награду с ключом по умолчанию.
    /// </summary>
    public bool Remove()
    {
        return Remove(Name);
    }

    /// <summary>
    /// Проверяет награду с указанным логическим именем.
    /// </summary>
    protected virtual bool IsActive(string name, out DateTime endTime)
    {
        return TimeLimitedRewardService.Instance.IsActive(GetName(name), out endTime);
    }

    /// <summary>
    /// Сколько осталось до окончания награды с указанным логическим именем.
    /// </summary>
    protected virtual TimeSpan GetRemaining(string name)
    {
        return TimeLimitedRewardService.Instance.GetRemaining(GetName(name));
    }

    /// <summary>
    /// Добавляет время награде с указанным логическим именем.
    /// Активная награда продлевается от своего конца, истёкшая - от текущего момента.
    /// </summary>
    protected virtual void AddTime(string name, TimeSpan addTime)
    {
        TimeLimitedRewardService.Instance.AddTime(GetName(name), addTime);
    }

    /// <summary>
    /// Снимает награду с указанным логическим именем.
    /// </summary>
    protected virtual bool Remove(string name)
    {
        return TimeLimitedRewardService.Instance.Remove(GetName(name));
    }

    /// <summary>
    /// Преобразует логическое имя награды в ключ хранилища.
    /// </summary>
    public virtual string GetName(string name)
    {
        return name;
    }
}
