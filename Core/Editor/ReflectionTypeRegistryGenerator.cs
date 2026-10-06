using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEngine;

/// <summary>
/// Собирает список классов, которые SDK находит по контракту (<see cref="ReflectionTypeRegistry"/>).
/// </summary>
/// <remarks>
/// Тот же поиск, что игра делала при запуске перебором всей сборки, только в редакторе, где он
/// мгновенный. Контракты — всё, что помечено <see cref="ReflectionContractAttribute"/>.
/// <para>
/// Список пересобирается перед каждой сборкой билда: класс, добавленный вчера, попадёт в него без
/// отдельного напоминания, а устаревшим список в билде быть не может.
/// </para>
/// </remarks>
public sealed class ReflectionTypeRegistryGenerator : IPreprocessBuildWithReport
{
    /// <summary>
    /// Куда пишется файл: ресурс проекта, читается по <see cref="ReflectionTypeRegistry.ResourcePath"/>.
    /// </summary>
    private const string OutputPath =
        "Assets/PRUnityData/Resources/" + ReflectionTypeRegistry.ResourcePath + ".txt";

    private const string GeneratedNote =
        "Создан автоматически: PRUnitySDK/Обновить список типов. Правки будут перезаписаны.";

    /// <inheritdoc />
    public int callbackOrder => 0;

    /// <inheritdoc />
    public void OnPreprocessBuild(BuildReport report)
    {
        Generate(silent: true);
    }

    [MenuItem("PRUnitySDK/Обновить список типов")]
    private static void GenerateFromMenu()
    {
        Generate(silent: false);
    }

    /// <summary>
    /// Пересобирает файл.
    /// </summary>
    public static void Generate(bool silent)
    {
        string content = Build(Collect());
        string fullPath = Path.GetFullPath(OutputPath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? string.Empty);

        // Файл переписывается только при изменениях: иначе каждая сборка дёргала бы переимпорт.
        if (File.Exists(fullPath) && File.ReadAllText(fullPath) == content)
        {
            if (!silent)
                PRLog.WriteDebug(typeof(ReflectionTypeRegistryGenerator), $"{OutputPath} уже актуален.");

            return;
        }

        File.WriteAllText(fullPath, content, new UTF8Encoding(false));
        AssetDatabase.ImportAsset(OutputPath);
        PRLog.WriteDebug(typeof(ReflectionTypeRegistryGenerator), $"{OutputPath} обновлён.");
    }

    /// <summary>
    /// Находит классы каждого контракта.
    /// </summary>
    /// <remarks>
    /// Берутся только сборки, которые попадают в билд: класс из редакторской сборки игра по имени
    /// не найдёт, и весь контракт ушёл бы на перебор.
    /// </remarks>
    public static SortedDictionary<string, List<Type>> Collect()
    {
        var playerAssemblies = new HashSet<string>(
            CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies).Select(assembly => assembly.name),
            StringComparer.Ordinal);

        var result = new SortedDictionary<string, List<Type>>(StringComparer.Ordinal);

        foreach (Type contract in TypeCache.GetTypesWithAttribute<ReflectionContractAttribute>())
        {
            IEnumerable<Type> candidates = typeof(Attribute).IsAssignableFrom(contract)
                ? TypeCache.GetTypesWithAttribute(contract)
                : TypeCache.GetTypesDerivedFrom(contract);

            result[contract.FullName] = candidates
                .Where(type => !type.IsAbstract && !type.IsInterface && !type.ContainsGenericParameters)
                .Where(type => playerAssemblies.Contains(type.Assembly.GetName().Name))
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToList();
        }

        return result;
    }

    private static string Build(SortedDictionary<string, List<Type>> byContract)
    {
        var builder = new StringBuilder();
        builder.Append(ReflectionTypeRegistry.CommentPrefix).Append(' ').Append(GeneratedNote).Append('\n');

        foreach (KeyValuePair<string, List<Type>> pair in byContract)
        {
            builder.Append(ReflectionTypeRegistry.ContractPrefix).Append(pair.Key).Append('\n');

            // Имя со сборкой, без версии: так его находит Type.GetType в любой сборке билда.
            foreach (Type type in pair.Value)
                builder.Append(type.FullName).Append(", ").Append(type.Assembly.GetName().Name).Append('\n');
        }

        return builder.ToString();
    }
}
