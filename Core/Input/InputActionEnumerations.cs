using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>
/// Все ключи ввода игры одним списком.
/// </summary>
/// <remarks>
/// Сам ключей не объявляет: собирает значения всех наследников
/// <see cref="InputEnumerationProviderBase"/>. Окну управления нужен один выпадающий список
/// «что делает клавиша», а ключи живут по модулям — у персонажа свои, у панели быстрого
/// доступа свои, у проекта могут появиться третьи. Перечислять их здесь вручную значило бы
/// править ядро на каждый новый модуль.
/// <para>
/// Набор наследника с <c>IncludeInherited</c> повторяет базовые ключи — повторы убираются,
/// остаётся первое вхождение. Базовые наборы идут раньше производных, поэтому ключи
/// персонажа стоят в начале списка, а добавленные проектом — после них.
/// </para>
/// </remarks>
public sealed class InputActionEnumerations : EnumerationProviderBase
{
    private Enumeration[] options;

    /// <inheritdoc />
    public override bool IncludeInherited => true;

    /// <inheritdoc />
    public override Enumeration Default => FirstOption;

    /// <inheritdoc />
    /// <remarks>
    /// Считается один раз: экземпляр набора кеширует <see cref="EnumerationExtensions"/>,
    /// и кеш сбрасывается вместе с перезагрузкой домена — новые наборы после
    /// перекомпиляции попадут в список.
    /// </remarks>
    public override IEnumerable<Enumeration> GetOptions()
    {
        return options ??= CollectOptions();
    }

    private static Enumeration[] CollectOptions()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<Enumeration>();

        foreach (Type type in GetProviderTypes())
        {
            foreach (Enumeration option in type.GetEnumerationsSmart(true))
            {
                if (option != null && seen.Add(option.Value))
                    result.Add(option);
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// Наборы ключей ввода: сначала базовые, внутри одного уровня — по имени.
    /// </summary>
    private static IEnumerable<Type> GetProviderTypes()
    {
        return FindDerivedTypes()
            .Where(type => !type.IsAbstract && !type.IsGenericTypeDefinition
                && type.GetConstructor(Type.EmptyTypes) != null)
            .OrderBy(GetDepth)
            .ThenBy(type => type.FullName, StringComparer.Ordinal);
    }

    private static IEnumerable<Type> FindDerivedTypes()
    {
#if UNITY_EDITOR
        return UnityEditor.TypeCache.GetTypesDerivedFrom<InputEnumerationProviderBase>();
#else
        var types = new List<Type>();

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] assemblyTypes;

            try
            {
                assemblyTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                assemblyTypes = exception.Types;
            }

            foreach (Type type in assemblyTypes)
            {
                if (type != null && typeof(InputEnumerationProviderBase).IsAssignableFrom(type))
                    types.Add(type);
            }
        }

        return types;
#endif
    }

    private static int GetDepth(Type type)
    {
        int depth = 0;

        for (Type current = type; current != null; current = current.BaseType)
            depth++;

        return depth;
    }
}
