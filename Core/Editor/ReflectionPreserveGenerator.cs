using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEngine;

/// <summary>
/// Собирает <c>link.xml</c> для кода, до которого игра добирается рефлексией.
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
        typeof(ISaveMigration)
    };

    /// <summary>
    /// Атрибуты методов, которые ядро находит перебором и вызывает само: хуки, partial-вызовы, подмены.
    /// </summary>
    /// <remarks>
    /// Наследники атрибута тоже учитываются: так попадают <c>OverrideProperty</c> и <c>OverrideBootstrap</c>.
    /// </remarks>
    private static readonly Type[] MethodMarkers =
    {
        typeof(MethodHookAttribute),
        typeof(InvokePartialAttribute),
        typeof(OverrideBaseAttribute)
    };

    /// <summary>
    /// Сборки, которые попадают в билд. Обновляются при каждом сборе списка.
    /// </summary>
    private static HashSet<string> playerAssemblies = new(StringComparer.Ordinal);

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
    /// Обновляет файл после каждой перекомпиляции, чтобы он не отставал от кода.
    /// </summary>
    /// <remarks>
    /// Во время перезагрузки домена импортировать ассеты нельзя, поэтому запись откладывается
    /// на следующий тик редактора. При входе в Play Mode домен тоже перезагружается, тогда файл не трогаем.
    /// </remarks>
    [DidReloadScripts]
    private static void GenerateAfterReload()
    {
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                Generate(silent: true);
        };
    }

    /// <summary>
    /// Пересобирает файл и сообщает, что в него попало.
    /// </summary>
    private static void Generate(bool silent)
    {
        Dictionary<string, SortedDictionary<string, PreservedType>> byAssembly = Collect();
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
    /// Находит код, который нельзя вырезать, и раскладывает его по сборкам.
    /// </summary>
    private static Dictionary<string, SortedDictionary<string, PreservedType>> Collect()
    {
        var byAssembly = new Dictionary<string, SortedDictionary<string, PreservedType>>(StringComparer.Ordinal);

        playerAssemblies = new HashSet<string>(
            CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies).Select(assembly => assembly.name),
            StringComparer.Ordinal);

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

        // Классы из списка типов игра запрашивает по имени: вырезанный класс она бы не нашла.
        foreach (List<Type> types in ReflectionTypeRegistryGenerator.Collect().Values)
        {
            foreach (Type type in types)
                Add(byAssembly, type);
        }

        foreach (Type marker in MethodMarkers)
        {
            foreach (Type attribute in TypeCache.GetTypesDerivedFrom(marker).Append(marker))
            {
                foreach (MethodInfo method in TypeCache.GetMethodsWithAttribute(attribute))
                    AddMethod(byAssembly, method);
            }
        }

        CollectJsonTypes(byAssembly);
        CollectDatabaseCatalogs(byAssembly);

        return byAssembly;
    }

    /// <summary>
    /// Добавляет классы, которые Newtonsoft создаёт и заполняет сам.
    /// </summary>
    /// <remarks>
    /// Конструктор и сеттеры, которые вызывает только десериализатор, сборщик считает лишними.
    /// <c>PRJsonUtils</c> гасит ошибки чтения, поэтому без них прогресс игрока обнулился бы молча.
    /// Обход идёт от корня сохранения и от классов с разметкой Newtonsoft по полям и свойствам,
    /// для абстрактных полей берутся все наследники.
    /// </remarks>
    private static void CollectJsonTypes(Dictionary<string, SortedDictionary<string, PreservedType>> byAssembly)
    {
        var queue = new Queue<Type>();
        queue.Enqueue(typeof(PRSaveData));

        foreach (FieldInfo field in TypeCache.GetFieldsWithAttribute<JsonPropertyAttribute>())
            queue.Enqueue(field.DeclaringType);

        // Конвертеры Newtonsoft создаёт по атрибуту [JsonConverter].
        foreach (Type converter in TypeCache.GetTypesDerivedFrom<JsonConverter>())
            Add(byAssembly, converter);

        var visited = new HashSet<Type>();

        while (queue.Count > 0)
        {
            Type type = queue.Dequeue();

            if (type == null || !visited.Add(type))
                continue;

            if (type.IsArray)
            {
                queue.Enqueue(type.GetElementType());
                continue;
            }

            if (type.IsGenericType)
            {
                foreach (Type argument in type.GetGenericArguments())
                    queue.Enqueue(argument);
            }

            if (!IsProjectAssembly(type.Assembly) || typeof(UnityEngine.Object).IsAssignableFrom(type))
                continue;

            if (type.IsAbstract || type.IsInterface)
            {
                foreach (Type derived in TypeCache.GetTypesDerivedFrom(type))
                    queue.Enqueue(derived);
            }

            Add(byAssembly, type);
            queue.Enqueue(type.BaseType);

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            foreach (FieldInfo field in type.GetFields(flags))
            {
                if (field.IsPublic || field.IsDefined(typeof(JsonPropertyAttribute)))
                    queue.Enqueue(field.FieldType);
            }

            foreach (PropertyInfo property in type.GetProperties(flags))
            {
                if (property.GetMethod?.IsPublic == true || property.IsDefined(typeof(JsonPropertyAttribute)))
                    queue.Enqueue(property.PropertyType);
            }
        }
    }

    /// <summary>
    /// Добавляет каталоги базы проекта и контейнеры, в которых они лежат.
    /// </summary>
    /// <remarks>
    /// Магазин собирает состав базы обходом её свойств. Свойство, которое читает только этот
    /// обход, сборщик вырезал бы, и товары каталога пропали бы из магазина.
    /// </remarks>
    private static void CollectDatabaseCatalogs(Dictionary<string, SortedDictionary<string, PreservedType>> byAssembly)
    {
        Add(byAssembly, typeof(PRSDKDatabase), allowUnityObjects: true);
        Add(byAssembly, typeof(Database<>));

        var queue = new Queue<Type>();
        queue.Enqueue(typeof(PRSDKDatabase));

        var visited = new HashSet<Type>();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        while (queue.Count > 0)
        {
            Type owner = queue.Dequeue();

            if (!visited.Add(owner))
                continue;

            foreach (PropertyInfo property in owner.GetProperties(flags))
            {
                Type type = property.PropertyType;

                if (IsDatabase(type))
                {
                    Add(byAssembly, type);
                }
                else if (type.IsClass && type.Namespace == null && !typeof(UnityEngine.Object).IsAssignableFrom(type))
                {
                    Add(byAssembly, type);
                    queue.Enqueue(type);
                }
            }
        }
    }

    private static bool IsDatabase(Type type)
    {
        for (Type current = type; current != null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Database<>))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Добавляет тип в список сохраняемых целиком.
    /// </summary>
    /// <remarks>
    /// Компоненты сцены и ассеты пропускаются: их удерживает ссылка из префаба.
    /// Закрытый обобщённый тип сохраняется по определению.
    /// </remarks>
    private static void Add(
        Dictionary<string, SortedDictionary<string, PreservedType>> byAssembly,
        Type type,
        bool allowUnityObjects = false)
    {
        if (type == null || type.IsInterface || type.IsGenericParameter)
            return;

        if (type.IsConstructedGenericType)
            type = type.GetGenericTypeDefinition();

        if (!allowUnityObjects && typeof(UnityEngine.Object).IsAssignableFrom(type))
            return;

        PreservedType entry = GetEntry(byAssembly, type);

        if (entry != null)
            entry.All = true;
    }

    /// <summary>
    /// Добавляет в список один метод типа.
    /// </summary>
    private static void AddMethod(Dictionary<string, SortedDictionary<string, PreservedType>> byAssembly, MethodInfo method)
    {
        Type type = method?.DeclaringType;

        if (type == null)
            return;

        if (type.IsConstructedGenericType)
            type = type.GetGenericTypeDefinition();

        GetEntry(byAssembly, type)?.Methods.Add(method.Name);
    }

    private static PreservedType GetEntry(Dictionary<string, SortedDictionary<string, PreservedType>> byAssembly, Type type)
    {
        if (!IsProjectAssembly(type.Assembly))
            return null;

        string assembly = type.Assembly.GetName().Name;

        if (!byAssembly.TryGetValue(assembly, out SortedDictionary<string, PreservedType> types))
        {
            types = new SortedDictionary<string, PreservedType>(StringComparer.Ordinal);
            byAssembly[assembly] = types;
        }

        // Вложенный тип link.xml записывает через «/», а не через «+», как его печатает .NET.
        string name = type.FullName?.Replace('+', '/');

        if (name == null)
            return null;

        if (!types.TryGetValue(name, out PreservedType entry))
        {
            entry = new PreservedType();
            types[name] = entry;
        }

        return entry;
    }

    /// <summary>
    /// Сборки Unity и пакетов трогать незачем: их состав сборщик знает и без нас.
    /// </summary>
    /// <remarks>
    /// Редакторских сборок в билде нет, и ссылка на них в <c>link.xml</c> была бы ошибкой.
    /// </remarks>
    private static bool IsProjectAssembly(System.Reflection.Assembly assembly)
    {
        string name = assembly.GetName().Name;

        return playerAssemblies.Contains(name)
            && !name.StartsWith("Unity", StringComparison.Ordinal)
            && !name.StartsWith("System", StringComparison.Ordinal)
            && !name.StartsWith("Newtonsoft", StringComparison.Ordinal)
            && name != "mscorlib"
            && name != "netstandard";
    }

    /// <summary>
    /// Собирает текст файла.
    /// </summary>
    private static string Build(Dictionary<string, SortedDictionary<string, PreservedType>> byAssembly)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!--");
        builder.AppendLine($"  {GeneratedNote}");
        builder.AppendLine("-->");
        builder.AppendLine("<linker>");

        foreach (KeyValuePair<string, SortedDictionary<string, PreservedType>> pair in byAssembly.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            builder.AppendLine($"  <assembly fullname=\"{pair.Key}\">");

            foreach (KeyValuePair<string, PreservedType> type in pair.Value)
            {
                string name = SecurityElement.Escape(type.Key);

                if (type.Value.All)
                {
                    builder.AppendLine($"    <type fullname=\"{name}\" preserve=\"all\" />");
                    continue;
                }

                builder.AppendLine($"    <type fullname=\"{name}\">");

                foreach (string method in type.Value.Methods)
                    builder.AppendLine($"      <method name=\"{SecurityElement.Escape(method)}\" />");

                builder.AppendLine("    </type>");
            }

            builder.AppendLine("  </assembly>");
        }

        builder.AppendLine("</linker>");
        return builder.ToString();
    }

    /// <summary>
    /// Что сохранить у одного типа: весь тип или отдельные методы.
    /// </summary>
    private sealed class PreservedType
    {
        public bool All;
        public readonly SortedSet<string> Methods = new(StringComparer.Ordinal);
    }
}
