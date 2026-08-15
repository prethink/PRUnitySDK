using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class Database<T> : IDatabaseValidationProvider
{
    [SerializeField] private List<T> data = new();

    public IEnumerable<T> Data => data.ToList();

    /// <summary>
    /// Выполняет общую проверку пустых элементов, дубликатов и стабильных Id.
    /// Наследник может дополнить правила, перечислив сначала результаты базовой реализации.
    /// </summary>
    public virtual IEnumerable<DatabaseValidationIssue> Validate()
    {
        var elements = new HashSet<T>();
        var ids = new HashSet<string>(StringComparer.Ordinal);

        for (int index = 0; index < data.Count; index++)
        {
            T item = data[index];
            if (item is null)
            {
                yield return new DatabaseValidationIssue(
                    "null-item",
                    $"Элемент с индексом {index} не задан.",
                    DatabaseValidationSeverity.Error,
                    index);
                continue;
            }

            if (!elements.Add(item))
            {
                yield return new DatabaseValidationIssue(
                    "duplicate-item",
                    $"Элемент с индексом {index} уже присутствует в базе.",
                    DatabaseValidationSeverity.Warning,
                    index);
            }

            if (item is not IIdentifiable identifiable)
                continue;

            if (string.IsNullOrWhiteSpace(identifiable.Id))
            {
                yield return new DatabaseValidationIssue(
                    "empty-id",
                    $"У элемента с индексом {index} не задан Id.",
                    DatabaseValidationSeverity.Error,
                    index);
                continue;
            }

            if (!ids.Add(identifiable.Id))
            {
                yield return new DatabaseValidationIssue(
                    "duplicate-id",
                    $"Id '{identifiable.Id}' повторяется у элемента с индексом {index}.",
                    DatabaseValidationSeverity.Error,
                    index);
            }
        }
    }
}
