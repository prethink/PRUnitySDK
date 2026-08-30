using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Найденная проблема описания.
/// </summary>
public sealed class EntityDescriptionIssue
{
    public EntityDescriptionIssue(MessageType severity, string message, UnityEngine.Object target)
    {
        Severity = severity;
        Message = message;
        Target = target;
    }

    /// <summary>
    /// Насколько это серьёзно.
    /// </summary>
    public MessageType Severity { get; }

    /// <summary>
    /// Что не так.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// К чему относится: ассет описания или префаб сущности.
    /// </summary>
    public UnityEngine.Object Target { get; }
}

/// <summary>
/// Проверки описаний и сущностей.
/// </summary>
/// <remarks>
/// Описание нельзя проверить компилятором: пустое имя, отсутствующий вид или незаполненный
/// перевод - обычные значения полей, и узнать о них можно только в игре, по безымянной
/// строке в отладчике. Проверки собирают такие случаи заранее.
/// </remarks>
public static class EntityDescriptionValidator
{
    /// <summary>
    /// Проверяет один ассет описания.
    /// </summary>
    public static IReadOnlyList<EntityDescriptionIssue> Validate(ScriptableObject asset)
    {
        var issues = new List<EntityDescriptionIssue>();

        if (asset is not IEntityMetadata description)
            return issues;

        ValidateName(asset, description, issues);
        ValidateKind(asset, issues);
        ValidateIcon(asset, description, issues);
        ValidateLocalization(asset, description, issues);
        ValidateUsage(asset, issues);

        return issues;
    }

    /// <summary>
    /// Имя - самое дорогое поле: без него падает вычисление ключа перевода.
    /// </summary>
    private static void ValidateName(
        ScriptableObject asset,
        IEntityMetadata description,
        List<EntityDescriptionIssue> issues)
    {
        if (!string.IsNullOrWhiteSpace(description.Name))
            return;

        // EntityMetadataBase.LocalizationKey считает $"EntityInfo_{Name.ToLower()}" -
        // на пустом имени это NullReferenceException в рантайме, а не пустая строка.
        issues.Add(new EntityDescriptionIssue(
            MessageType.Error,
            "Не задано имя. Ключ перевода вычисляется из него, и обращение к нему упадёт.",
            asset));
    }

    /// <summary>
    /// Вид нужен только описаниям сущностей: у определений он объявлен в классе.
    /// </summary>
    private static void ValidateKind(ScriptableObject asset, List<EntityDescriptionIssue> issues)
    {
        if (asset is not EntityMetadataBase metadata)
            return;

        if (!string.IsNullOrWhiteSpace(metadata.EntityType?.Value))
            return;

        issues.Add(new EntityDescriptionIssue(
            MessageType.Warning,
            "Не задан вид. Сущность с этим описанием попадёт в трекер как Unknown.",
            asset));
    }

    private static void ValidateIcon(
        ScriptableObject asset,
        IEntityMetadata description,
        List<EntityDescriptionIssue> issues)
    {
        if (description.Icon != null)
            return;

        issues.Add(new EntityDescriptionIssue(
            MessageType.Warning,
            "Нет иконки. В UI и в отладчике будет пустое место.",
            asset));
    }

    /// <summary>
    /// Перевод проверяется по каждому языку отдельно.
    /// </summary>
    /// <remarks>
    /// Недостающий язык не ломает игру, но <c>PRLocalization</c> подставит вместо текста
    /// строку вида «ключ, NotFoundTranslate» - и она уедет прямо в интерфейс.
    /// </remarks>
    private static void ValidateLocalization(
        ScriptableObject asset,
        IEntityMetadata description,
        List<EntityDescriptionIssue> issues)
    {
        IReadOnlyDictionary<LangType, string> values = description.LocalizationValues;

        if (values == null || values.Count == 0)
        {
            issues.Add(new EntityDescriptionIssue(
                MessageType.Warning,
                "Нет переводов. В интерфейс уедет «NotFoundTranslate».",
                asset));
            return;
        }

        var missing = Enum.GetValues(typeof(LangType))
            .Cast<LangType>()
            .Where(lang => !values.TryGetValue(lang, out string value) || string.IsNullOrWhiteSpace(value))
            .ToArray();

        if (missing.Length == 0)
            return;

        issues.Add(new EntityDescriptionIssue(
            MessageType.Warning,
            $"Нет перевода: {string.Join(", ", missing)}.",
            asset));
    }

    /// <summary>
    /// Описание, на которое никто не ссылается.
    /// </summary>
    /// <remarks>
    /// Не ошибка: описание могло быть заведено заранее или использоваться из кода.
    /// Но чаще это остаток от удалённой сущности, и в сборку он не попадёт вовсе.
    /// </remarks>
    private static void ValidateUsage(ScriptableObject asset, List<EntityDescriptionIssue> issues)
    {
        if (EntityMetadataUsageIndex.GetUsages(asset).Count > 0)
            return;

        issues.Add(new EntityDescriptionIssue(
            MessageType.Info,
            "На описание не ссылается ни один префаб и ни одна сцена. В сборку оно не попадёт.",
            asset));
    }

    /// <summary>
    /// Ищет префабы сущностей, у которых описание не задано.
    /// </summary>
    /// <remarks>
    /// Проверять приходится загрузкой префаба: ссылки на описание у сущности нет, а «нет
    /// ссылки» по зависимостям не отличить от «нет сущности». Поэтому поиск запускается
    /// кнопкой, а не при открытии окна.
    /// </remarks>
    public static IReadOnlyList<EntityDescriptionIssue> FindEntitiesWithoutDescription()
    {
        var issues = new List<EntityDescriptionIssue>();
        string[] guids = AssetDatabase.FindAssets("t:Prefab");

        try
        {
            for (int index = 0; index < guids.Length; index++)
            {
                if (index % 50 == 0 &&
                    EditorUtility.DisplayCancelableProgressBar(
                        "Проверка сущностей",
                        "Поиск сущностей без описания...",
                        (float)index / guids.Length))
                {
                    break;
                }

                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab == null)
                    continue;

                foreach (EntityBase entity in prefab.GetComponentsInChildren<EntityBase>(true))
                {
                    if (entity == null || HasDescription(entity))
                        continue;

                    issues.Add(new EntityDescriptionIssue(
                        MessageType.Warning,
                        $"{prefab.name} → {entity.GetType().Name}: описание не задано.",
                        prefab));
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        return issues;
    }

    /// <summary>
    /// Ищет сущности без описания в открытых сценах.
    /// </summary>
    /// <remarks>
    /// Только в открытых: заглянуть в закрытую сцену можно лишь открыв её, а это меняет
    /// то, с чем человек сейчас работает - вплоть до потери несохранённых правок.
    /// Практический случай при этом покрыт: сцену проверяют, когда её и правят.
    /// <para>
    /// Ссылки со сцен на описания при этом видны всегда - их находит
    /// <see cref="EntityMetadataUsageIndex"/> по зависимостям, без открытия.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<EntityDescriptionIssue> FindSceneEntitiesWithoutDescription()
    {
        var issues = new List<EntityDescriptionIssue>();

        for (int index = 0; index < SceneManager.sceneCount; index++)
        {
            Scene scene = SceneManager.GetSceneAt(index);

            if (!scene.isLoaded)
                continue;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (EntityBase entity in root.GetComponentsInChildren<EntityBase>(true))
                {
                    if (entity == null || HasDescription(entity))
                        continue;

                    issues.Add(new EntityDescriptionIssue(
                        MessageType.Warning,
                        $"Сцена {scene.name}: {entity.name} → {entity.GetType().Name}, описание не задано.",
                        entity.gameObject));
                }
            }
        }

        return issues;
    }

    /// <summary>
    /// Есть ли у сущности источник описания.
    /// </summary>
    /// <remarks>
    /// Порядок тот же, что и в <see cref="EntityDescriptionSection"/>: определение,
    /// собственные поля, ассет описания. Ищется по типу поля, а не по имени - имена
    /// у разных сущностей свои.
    /// </remarks>
    private static bool HasDescription(EntityBase entity)
    {
        // Сущность, описывающая себя сама, поля описания носит на себе.
        if (entity is IEntityMetadata)
            return true;

        // Сущность без поля описания вовсе - не проблема этой проверки: у неё описание
        // берётся иначе, и жаловаться тут не на что.
        if (!EntityDescriptionSource.HasField(entity))
            return true;

        return EntityDescriptionSource.Resolve(entity) != null;
    }
}
