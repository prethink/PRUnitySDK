using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Список классов, которые SDK находит по контракту, составленный при сборке билда.
/// </summary>
/// <remarks>
/// Часть классов SDK подключает сам: находит всех, кто реализует интерфейс или помечен атрибутом
/// (правила характеристик, фоновые задачи, миграции сохранения). Найти их можно только перебором
/// всех типов сборки, а первый такой перебор заставляет среду выполнения загрузить описание
/// каждого типа. Сборка у проекта одна, типов в ней тысячи: на слабом телефоне это полторы секунды
/// запуска ради нескольких классов.
/// <para>
/// Поэтому перебор делается заранее, в редакторе, где он мгновенный: перед сборкой билда
/// <c>ReflectionTypeRegistryGenerator</c> записывает найденное в текстовый ресурс
/// (<see cref="ResourcePath"/>). При запуске игра читает список и запрашивает каждый класс по имени —
/// загружаются описания только этих классов.
/// </para>
/// <para>
/// Список — ускорение, а не единственный источник. Нет файла, нет в нём контракта или имя не
/// нашлось — <see cref="TryGetTypes"/> возвращает <see langword="false"/>, и вызывающий делает
/// прежний перебор. В редакторе список не используется вовсе: там перебор быстрый, а новый класс
/// должен работать сразу, без пересборки списка.
/// </para>
/// </remarks>
public static class ReflectionTypeRegistry
{
    /// <summary>
    /// Где в <c>Resources</c> лежит список.
    /// </summary>
    public const string ResourcePath = "PRUnitySDK/ReflectionTypes";

    /// <summary>
    /// С этого знака в файле начинается строка контракта; строки под ней — его классы.
    /// </summary>
    public const string ContractPrefix = "#";

    /// <summary>
    /// С этих знаков начинается строка-комментарий.
    /// </summary>
    public const string CommentPrefix = "//";

    /// <summary>
    /// Пользоваться списком и в редакторе. Нужно проверкам: по умолчанию редактор ищет перебором.
    /// </summary>
    public static bool UseInEditor { get; set; }

    private static Dictionary<string, List<string>> names;
    private static bool loaded;

    /// <summary>
    /// Что уже найдено по контракту; <see langword="null"/> — по списку найти не вышло.
    /// </summary>
    private static readonly Dictionary<Type, Type[]> resolved = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnLoad()
    {
        Reset();
    }

    /// <summary>
    /// Забывает прочитанный список: следующий запрос прочитает файл заново.
    /// </summary>
    public static void Reset()
    {
        names = null;
        loaded = false;
        resolved.Clear();
    }

    /// <summary>
    /// Отдаёт классы контракта из списка.
    /// </summary>
    /// <remarks>
    /// В список попадают все неабстрактные и необобщённые классы контракта. Остальные свои условия —
    /// конструктор без параметров, включён ли атрибут — вызывающий проверяет сам, как и после перебора.
    /// </remarks>
    /// <param name="contract">Интерфейс, базовый класс или атрибут, по которому классы ищутся.</param>
    /// <param name="types">Классы контракта; пустой список — их нет.</param>
    /// <returns><see langword="false"/>, если по списку ответить нельзя и нужен перебор.</returns>
    public static bool TryGetTypes(Type contract, out IReadOnlyList<Type> types)
    {
        types = null;

        if (contract == null)
            return false;

#if UNITY_EDITOR
        if (!UseInEditor)
            return false;
#endif

        if (!resolved.TryGetValue(contract, out Type[] found))
        {
            found = Resolve(contract);
            resolved[contract] = found;
        }

        types = found;
        return found != null;
    }

    private static Type[] Resolve(Type contract)
    {
        Load();

        if (names == null)
            return null;

        if (!names.TryGetValue(contract.FullName, out List<string> typeNames))
        {
            PRLog.WriteWarning(typeof(ReflectionTypeRegistry),
                $"Контракта '{contract.FullName}' нет в списке типов — его классы ищутся перебором сборки. " +
                $"Пометьте контракт атрибутом {nameof(ReflectionContractAttribute)}.");
            return null;
        }

        var result = new Type[typeNames.Count];

        for (int index = 0; index < typeNames.Count; index++)
        {
            Type type = Type.GetType(typeNames[index]);

            if (type == null)
            {
                // Имя из списка не нашлось — список разошёлся с билдом. Отвечать наполовину нельзя:
                // потерянный класс молча выпал бы из игры. Пусть решит перебор.
                PRLog.WriteWarning(typeof(ReflectionTypeRegistry),
                    $"Класс '{typeNames[index]}' из списка типов не найден — контракт '{contract.FullName}' ищется перебором.");
                return null;
            }

            result[index] = type;
        }

        return result;
    }

    private static void Load()
    {
        if (loaded)
            return;

        loaded = true;

        var asset = Resources.Load<TextAsset>(ResourcePath);

        if (asset == null)
            return;

        names = Parse(asset.text);
        Resources.UnloadAsset(asset);
    }

    /// <summary>
    /// Разбирает текст списка: строка с <see cref="ContractPrefix"/> открывает контракт, строки под ней — его классы.
    /// </summary>
    public static Dictionary<string, List<string>> Parse(string text)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        List<string> current = null;

        foreach (string rawLine in text.Split('\n'))
        {
            string line = rawLine.Trim();

            if (line.Length == 0 || line.StartsWith(CommentPrefix, StringComparison.Ordinal))
                continue;

            if (line.StartsWith(ContractPrefix, StringComparison.Ordinal))
            {
                current = new List<string>();
                result[line.Substring(ContractPrefix.Length).Trim()] = current;
                continue;
            }

            current?.Add(line);
        }

        return result;
    }
}
