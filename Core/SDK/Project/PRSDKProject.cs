using UnityEngine;

/// <summary>
/// Проект SDK: база, настройки и префабы одной игры.
/// </summary>
/// <remarks>
/// Один SDK обслуживает несколько игр, и у каждой свой состав предметов, свои настройки
/// и свой набор префабов. Прежде это решалось наборами: состав выгружался в JSON и
/// применялся поверх общей базы. Хранить состав прямо в своём ассете надёжнее — ничего
/// не нужно «применять», нельзя перезаписать чужую игру, а разница видна в истории
/// изменений по-человечески.
/// <para>
/// Сами ассеты лежат вне <c>Resources</c> намеренно: в сборку попадает только то, до чего
/// есть ссылка, поэтому чужие игры туда не тянутся. Активный проект указывается
/// в <see cref="PRSDKActiveProject"/> — единственном, что лежит в ресурсах.
/// </para>
/// </remarks>
[CreateAssetMenu(fileName = "PRUnitySDK Project", menuName = "PRUnitySDK/Project", order = 0)]
public class PRSDKProject : ScriptableObject
{
    [SerializeField]
    [Tooltip("Название проекта для окна переключения.")]
    private string title;

    [SerializeField]
    [TextArea]
    [Tooltip("Заметка: чем этот проект отличается от остальных.")]
    private string description;

    [SerializeField]
    [Tooltip("Каталоги: предметы, награды, описания сущностей.")]
    private PRSDKDatabase database;

    [SerializeField]
    [Tooltip("Настройки модулей SDK.")]
    private PRSDKSettings settings;

    [SerializeField]
    [Tooltip("Префабы ядра, модулей и окон.")]
    private PrefabContainer prefabs;

    /// <summary>
    /// Название проекта; пусто — имя ассета.
    /// </summary>
    public string Title => string.IsNullOrWhiteSpace(title) ? name : title;

    /// <summary>
    /// Заметка о проекте.
    /// </summary>
    public string Description => description;

    /// <summary>
    /// Каталоги проекта.
    /// </summary>
    public PRSDKDatabase Database => database;

    /// <summary>
    /// Настройки проекта.
    /// </summary>
    public PRSDKSettings Settings => settings;

    /// <summary>
    /// Префабы проекта.
    /// </summary>
    public PrefabContainer Prefabs => prefabs;

    /// <summary>
    /// Все три части на месте.
    /// </summary>
    /// <remarks>
    /// Неполный проект — не ошибка: недостающее берётся прежним путём, из ресурсов.
    /// Так проект можно собирать по частям, не ломая работающую игру.
    /// </remarks>
    public bool IsComplete => database != null && settings != null && prefabs != null;

    /// <summary>
    /// Ассет проекта нужного вида либо <see langword="null"/>.
    /// </summary>
    public T Resolve<T>() where T : ScriptableObject
    {
        if (typeof(T) == typeof(PRSDKDatabase))
            return database as T;

        if (typeof(T) == typeof(PRSDKSettings))
            return settings as T;

        if (typeof(T) == typeof(PrefabContainer))
            return prefabs as T;

        return null;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Задаёт состав проекта.
    /// </summary>
    /// <remarks>
    /// Только для редактора: в игре состав проекта не меняется.
    /// </remarks>
    public void SetContent(PRSDKDatabase newDatabase, PRSDKSettings newSettings, PrefabContainer newPrefabs)
    {
        database = newDatabase;
        settings = newSettings;
        prefabs = newPrefabs;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
