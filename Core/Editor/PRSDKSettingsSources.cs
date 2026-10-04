using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

/// <summary>
/// Определяет, откуда пришёл раздел настроек: из какой папки верхнего уровня его объявили.
/// </summary>
/// <remarks>
/// <see cref="PRSDKSettings"/> собирается из partial-файлов: общая часть SDK кладёт свои
/// разделы, закрытая часть — свои, проект игры — свои. В скомпилированном типе этого уже
/// не видно, поэтому источник ищется по исходникам: в каком файле объявлено поле раздела.
/// <para>
/// Папкой-источником считается первая после <c>Assets</c>: <c>PRUnitySDK</c>,
/// <c>PRUnitySDKPrivate</c>, папка игры. Имена нигде не зашиты — новая часть SDK или
/// проект появятся в окне отдельной группой сами.
/// </para>
/// <para>
/// Таблица строится один раз на загрузку домена: правка скриптов домен перезагружает,
/// и таблица пересоберётся вместе с ним.
/// </para>
/// </remarks>
internal static class PRSDKSettingsSources
{
    /// <summary>
    /// Группа для раздела, чьё объявление найти не удалось.
    /// </summary>
    public const string Unknown = "Прочее";

    private const string BackingFieldSuffix = ">k__BackingField";

    private static Dictionary<string, string> sourceByMember;
    private static string coreSource;

    /// <summary>
    /// Папка, в которой объявлен сам <see cref="PRSDKSettings"/>: её группа идёт первой.
    /// </summary>
    public static string CoreSource
    {
        get
        {
            EnsureBuilt();
            return coreSource;
        }
    }

    /// <summary>
    /// Папка-источник раздела верхнего уровня.
    /// </summary>
    /// <param name="rootProperty">Сериализованное свойство раздела.</param>
    public static string GetSource(SerializedProperty rootProperty)
    {
        EnsureBuilt();

        return rootProperty != null && sourceByMember.TryGetValue(GetMemberName(rootProperty.name), out string source)
            ? source
            : Unknown;
    }

    /// <summary>
    /// Порядок групп: сначала общая часть SDK, потом остальные по алфавиту, «Прочее» в конце.
    /// </summary>
    public static int Compare(string left, string right)
    {
        return GetRank(left) != GetRank(right)
            ? GetRank(left).CompareTo(GetRank(right))
            : string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private static int GetRank(string source)
    {
        if (source == CoreSource)
            return 0;

        return source == Unknown ? 2 : 1;
    }

    private static void EnsureBuilt()
    {
        if (sourceByMember != null)
            return;

        sourceByMember = new Dictionary<string, string>(StringComparer.Ordinal);

        var members = new List<string>();

        foreach (System.Reflection.FieldInfo field in typeof(PRSDKSettings).GetFields(
                     System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                     System.Reflection.BindingFlags.NonPublic))
        {
            members.Add(GetMemberName(field.Name));
        }

        // Сначала файлы с именем настроек в названии — так названы все partial-файлы SDK.
        Scan(AssetDatabase.FindAssets($"{nameof(PRSDKSettings)} t:MonoScript"), members);

        // Файл назвали иначе — ищем по всем скриптам. Дороже, поэтому только для того,
        // что не нашлось.
        if (sourceByMember.Count < members.Count)
            Scan(AssetDatabase.FindAssets("t:MonoScript"), members);

        coreSource = FindCoreSource();
    }

    private static void Scan(string[] guids, List<string> members)
    {
        string partialMarker = $"partial class {nameof(PRSDKSettings)}";

        foreach (string guid in guids)
        {
            if (sourceByMember.Count >= members.Count)
                return;

            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
                continue;

            string text = File.ReadAllText(path);

            if (!text.Contains(partialMarker))
                continue;

            string source = GetTopFolder(path);

            foreach (string member in members)
            {
                if (sourceByMember.ContainsKey(member))
                    continue;

                // Автосвойство «Name { get» либо обычное поле «Name =» / «Name;».
                if (Regex.IsMatch(text, $@"\b{Regex.Escape(member)}\s*(\{{\s*get\b|=|;)"))
                    sourceByMember[member] = source;
            }
        }
    }

    private static string FindCoreSource()
    {
        foreach (string guid in AssetDatabase.FindAssets($"{nameof(PRSDKSettings)} t:MonoScript"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (Path.GetFileNameWithoutExtension(path) == nameof(PRSDKSettings))
                return GetTopFolder(path);
        }

        return string.Empty;
    }

    /// <summary>
    /// Первая папка после <c>Assets</c>; для пакета — его имя.
    /// </summary>
    private static string GetTopFolder(string path)
    {
        string[] parts = path.Replace('\\', '/').Split('/');

        return parts.Length > 2 ? parts[1] : Unknown;
    }

    private static string GetMemberName(string serializedName)
    {
        return serializedName.StartsWith("<", StringComparison.Ordinal) &&
               serializedName.EndsWith(BackingFieldSuffix, StringComparison.Ordinal)
            ? serializedName.Substring(1, serializedName.Length - BackingFieldSuffix.Length - 1)
            : serializedName;
    }
}
