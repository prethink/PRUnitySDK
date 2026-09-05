using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Собирает <c>link.xml</c> для типов, которые создаются рефлексией.
/// </summary>
/// <remarks>
/// Сборщик билда вырезает код, до которого не дотягивается явный вызов, поэтому классы,
/// которые SDK находит рефлексией (фоновые задачи, источники резерва, обработчики
/// эффектов), в билде исчезают, хотя в редакторе всё работает.
/// <para>
/// Список собирается по атрибутам и интерфейсам, с которыми SDK работает через
/// рефлексию, чтобы не расставлять <c>[Preserve]</c> руками. Наследовать свой атрибут
/// от <c>PreserveAttribute</c> бесполезно: сборщик узнаёт его по имени, а не по типу.
/// </para>
/// </remarks>
public sealed class ReflectionPreserveGenerator : IPreprocessBuildWithReport
{
    /// <summary>
    /// Куда пишется файл. Unity подхватывает любой <c>link.xml</c> внутри Assets.
    /// </summary>
    private const string OutputPath = "Assets/PRUnitySDK/link.xml";

    private const string GeneratedNote =
        "Создан автоматически: PRUnitySDK/Обновить link.xml. Правки будут перезаписаны.";

    /// <inheritdoc />
    public int callbackOrder => 0;

    /// <summary>
    /// Атрибуты, которыми помечают классы для автоматической регистрации.
    /// </summary>
    private static readonly Type[] MarkerAttributes =
    {
        typeof(AutoBackgroundTaskAttribute),
        typeof(AutoReservedItemsProviderAttribute)
    };

    /// <summary>
    /// Контракты, реализации которых SDK ищет и создаёт сам.
    /// </summary>
    private static readonly Type[] ReflectionContracts =
    {
        typeof(IReservedItemsProvider),
        typeof(IBackgroundTask),
        typeof(IEnumerationProvider),
        typeof(IStatRuleProvider)
    };

    /// <inheritdoc />
    /// <remarks>
    /// Список обновляется перед каждой сборкой: класс, добавленный вчера, попадёт в билд
    /// без отдельного напоминания.
    /// </remarks>
    public void OnPreprocessBuild(BuildReport report)
    {
        Generate(silent: true);
    }

    [MenuItem("PRUnitySDK/Обновить link.xml")]
    private static void GenerateFromMenu()
    {
        Generate(silent: false);
    }

    /// <summary>
    /// Пересобирает файл и сообщает, что в него попало.
    /// </summary>
    private static void Generate(bool silent)
    {
        Dictionary<string, SortedSet<string>> byAssembly = Collect();
        string content = Build(byAssembly);
        string fullPath = Path.GetFullPath(OutputPath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? string.Empty);

        // Файл переписывается только при изменениях: иначе каждая сборка дёргала бы
        // переимпорт и версионирование ради одинакового содержимого.
        if (File.Exists(fullPath) && File.ReadAllText(fullPath) == content)
        {
            if (!silent)
                PRLog.WriteDebug(typeof(ReflectionPreserveGenerator), $"{OutputPath} уже актуален.");

            return;
        }

        File.WriteAllText(fullPath, content, new UTF8Encoding(false));
        AssetDatabase.ImportAsset(OutputPath);

        int count = byAssembly.Sum(pair => pair.Value.Count);
        PRLog.WriteDebug(typeof(ReflectionPreserveGenerator), $"{OutputPath} обновлён: типов {count}.");
    }

    /// <summary>
    /// Находит типы, которые нельзя вырезать, и раскладывает их по сборкам.
    /// </summary>
    private static Dictionary<string, SortedSet<string>> Collect()
    {
        var byAssembly = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);

        foreach (Type attribute in MarkerAttributes)
        {
            foreach (Type type in TypeCache.GetTypesWithAttribute(attribute))
                Add(byAssembly, type);
        }

        foreach (Type contract in ReflectionContracts)
        {
            foreach (Type type in TypeCache.GetTypesDerivedFrom(contract))
                Add(byAssembly, type);
        }

        return byAssembly;
    }

    /// <summary>
    /// Добавляет тип в список сохраняемых.
    /// </summary>
    /// <remarks>
    /// Абстрактные и обобщённые типы пропускаются: создать их нельзя, а сохранять
    /// незачем. Компоненты сцены тоже — их удерживает ссылка из префаба.
    /// </remarks>
    private static void Add(Dictionary<string, SortedSet<string>> byAssembly, Type type)
    {
        if (type == null || type.IsAbstract || type.IsInterface || type.ContainsGenericParameters)
            return;

        if (typeof(Component).IsAssignableFrom(type) || typeof(ScriptableObject).IsAssignableFrom(type))
            return;

        string assembly = type.Assembly.GetName().Name;

        // Сборки Unity и пакетов трогать незачем: их состав сборщик знает и без нас.
        if (assembly.StartsWith("Unity", StringComparison.Ordinal)
            || assembly.StartsWith("System", StringComparison.Ordinal)
            || assembly == "mscorlib")
            return;

        if (!byAssembly.TryGetValue(assembly, out SortedSet<string> types))
        {
            types = new SortedSet<string>(StringComparer.Ordinal);
            byAssembly[assembly] = types;
        }

        types.Add(type.FullName);
    }

    /// <summary>
    /// Собирает текст файла.
    /// </summary>
    private static string Build(Dictionary<string, SortedSet<string>> byAssembly)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!--");
        builder.AppendLine($"  {GeneratedNote}");
        builder.AppendLine("-->");
        builder.AppendLine("<linker>");

        foreach (KeyValuePair<string, SortedSet<string>> pair in byAssembly.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            builder.AppendLine($"  <assembly fullname=\"{pair.Key}\">");

            foreach (string type in pair.Value)
                builder.AppendLine($"    <type fullname=\"{type}\" preserve=\"all\" />");

            builder.AppendLine("  </assembly>");
        }

        builder.AppendLine("</linker>");
        return builder.ToString();
    }
}
