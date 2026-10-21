using UnityEngine;

/// <summary>
/// Проект SDK: база, настройки и префабы одной игры.
/// </summary>
/// <remarks>
/// Один SDK обслуживает несколько игр, у каждой свой состав предметов, настройки
/// и префабы. Состав лежит прямо в ассете проекта: применять ничего не нужно, чужую
/// игру не перезаписать, а разница видна в истории изменений.
/// <para>
/// Ассеты лежат вне <c>Resources</c>: в сборку попадает только то, до чего есть ссылка,
/// поэтому чужие игры туда не тянутся. Активный проект указывает
/// <see cref="PRSDKActiveProject"/>, единственный ассет в ресурсах.
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

    [SerializeField, SceneGuid]
    [Tooltip("Необязательно. Сцена игры: когда проект становится активным, в списке сборки первой встаёт стартовая сцена, " +
             "за ней эта, остальные выключаются. Пусто — список сборки не трогается.")]
    private string gameScene;

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
    /// Обе части на месте.
    /// </summary>
    /// <remarks>
    /// Неполный проект — не ошибка: недостающее берётся прежним путём, из ресурсов.
    /// Так проект можно собирать по частям, не ломая работающую игру.
    /// </remarks>
    public bool IsComplete => database != null && settings != null;

    /// <summary>
    /// GUID сцены игры; пусто — сцена к проекту не привязана.
    /// </summary>
    /// <remarks>
    /// Хранится GUID, а не путь: перенос сцены в другую папку привязку не ломает. В сборке поле не читается —
    /// по нему в редакторе собирается список сцен сборки (<c>PRSDKProjectScenes</c>).
    /// </remarks>
    public string GameSceneGuid => gameScene;

    /// <summary>
    /// Ассет проекта нужного вида либо <see langword="null"/>.
    /// </summary>
    public T Resolve<T>() where T : ScriptableObject
    {
        if (typeof(T) == typeof(PRSDKDatabase))
            return database as T;

        if (typeof(T) == typeof(PRSDKSettings))
            return settings as T;

        return null;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Состав или описание проекта изменили в редакторе: инспектором, Undo или <see cref="SetContent"/>.
    /// </summary>
    /// <remarks>
    /// Приходит отложенно, после проверки значений, как и <see cref="PRSDKActiveProject.EditedInEditor"/>. Слушает
    /// редакторский наблюдатель: заменённые в активном проекте база или настройки требуют сброса синглтона.
    /// </remarks>
    public static event System.Action EditedInEditor;

    private void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall -= RaiseEdited;
        UnityEditor.EditorApplication.delayCall += RaiseEdited;
    }

    private static void RaiseEdited() => EditedInEditor?.Invoke();

    /// <summary>
    /// Задаёт состав проекта.
    /// </summary>
    /// <remarks>
    /// Только для редактора: в игре состав проекта не меняется.
    /// </remarks>
    public void SetContent(PRSDKDatabase newDatabase, PRSDKSettings newSettings)
    {
        database = newDatabase;
        settings = newSettings;
        UnityEditor.EditorUtility.SetDirty(this);
        OnValidate();
    }

    /// <summary>
    /// Задаёт название и заметку.
    /// </summary>
    public void SetInfo(string newTitle, string newDescription)
    {
        title = newTitle;
        description = newDescription;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
