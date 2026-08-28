using System.Collections.Generic;

/// <summary>
/// Объекты, отдающие своё состояние в сохранение.
/// </summary>
/// <remarks>
/// Порядок сбора — порядок появления объектов. Множества хватило бы для проверки
/// членства, но не для повторяемости: если двое пишут в одно место, побеждать должен
/// всегда один и тот же, иначе сохранение зависит от того, как лёг хеш.
/// </remarks>
public sealed class SaveableRegistry
{
    private readonly List<ISaveable> ordered = new();
    private readonly HashSet<ISaveable> registered = new();

    /// <summary>
    /// Сколько объектов участвует в сохранении.
    /// </summary>
    public int Count => registered.Count;

    /// <summary>
    /// Ставит объект на учёт.
    /// </summary>
    public void Add(ISaveable saveable)
    {
        if (saveable != null && registered.Add(saveable))
            ordered.Add(saveable);
    }

    /// <summary>
    /// Снимает объект с учёта.
    /// </summary>
    /// <remarks>
    /// Из списка запись убирается не сразу: при выгрузке сцены объекты уходят пачками,
    /// и вычёркивать каждый по отдельности дороже, чем один раз уплотнить список перед
    /// следующим сбором.
    /// </remarks>
    public void Remove(ISaveable saveable)
    {
        registered.Remove(saveable);
    }

    /// <summary>
    /// Объекты в порядке появления.
    /// </summary>
    public IReadOnlyList<ISaveable> Collect()
    {
        if (ordered.Count != registered.Count)
            Compact();

        return ordered;
    }

    /// <summary>
    /// Убирает из списка снятые с учёта записи.
    /// </summary>
    private void Compact()
    {
        int target = 0;

        for (int index = 0; index < ordered.Count; index++)
        {
            ISaveable saveable = ordered[index];

            if (!registered.Contains(saveable))
                continue;

            ordered[target] = saveable;
            target++;
        }

        ordered.RemoveRange(target, ordered.Count - target);
    }
}
