using System.Collections.Generic;

/// <summary>
/// Учёт состояний объектов: что есть на сцене сейчас и что сохранено в проекте.
/// </summary>
/// <remarks>
/// Срез сцены содержит живые компоненты текущего уровня. По нему считают «собрано 47
/// из 400»: и числитель, и знаменатель берутся из того, что реально расставлено.
/// <para>
/// Срез проекта содержит записи в сохранении, включая незагруженные уровни. По нему
/// считают общий прогресс и проверяют, трогали ли объект.
/// </para>
/// <para>
/// Запись есть только у объектов, чьё состояние отличается от заданного в инспекторе,
/// поэтому нетронутый объект в срезе проекта не виден.
/// </para>
/// </remarks>
public sealed class ObjectStateTracker
{
    private readonly List<SaveableObjectState> loaded = new();
    private readonly HashSet<SaveableObjectState> registered = new();
    private readonly Dictionary<string, SaveableObjectState> byKey = new();

    #region Сцена

    /// <summary>
    /// Состояния, живущие на сцене сейчас.
    /// </summary>
    public IReadOnlyList<SaveableObjectState> Loaded => loaded;

    /// <summary>
    /// Сколько состояний на сцене.
    /// </summary>
    public int LoadedCount => loaded.Count;

    /// <summary>
    /// Ставит состояние на учёт.
    /// </summary>
    /// <remarks>
    /// Объект без ключа не учитывается по ключу, но в общий список попадает: его всё
    /// равно видно в отладке, а ненастроенный компонент лучше показать, чем спрятать.
    /// </remarks>
    public void Add(SaveableObjectState state)
    {
        // Проверка членства по множеству, а не по списку: состояния встают на учёт
        // из Awake каждого компонента, и линейный поиск дал бы O(N²) на загрузке
        // уровня с россыпью предметов. Список нужен только ради порядка.
        if (state == null || !registered.Add(state))
            return;

        loaded.Add(state);

        string key = state.StateId;

        if (!string.IsNullOrEmpty(key))
            byKey[key] = state;
    }

    /// <summary>
    /// Снимает состояние с учёта.
    /// </summary>
    public void Remove(SaveableObjectState state)
    {
        if (state == null || !registered.Remove(state))
            return;

        loaded.Remove(state);

        string key = state.StateId;

        if (!string.IsNullOrEmpty(key) && byKey.TryGetValue(key, out SaveableObjectState owner) && owner == state)
            byKey.Remove(key);
    }

    /// <summary>
    /// Ищет состояние на сцене по ключу.
    /// </summary>
    public bool TryGetLoaded(string key, out SaveableObjectState state)
    {
        state = null;
        return !string.IsNullOrEmpty(key) && byKey.TryGetValue(key, out state);
    }

    /// <summary>
    /// Прогресс по сцене: сколько показано, сколько спрятано, сколько всего.
    /// </summary>
    /// <param name="group">
    /// Считать только эту группу. Без неё в счёт идут все объекты уровня, а это обычно
    /// не то, что нужно: кристаллы, двери и постройки сложатся в одно число.
    /// </param>
    /// <remarks>
    /// Считается по живым компонентам, поэтому «всего» берётся из самой сцены. Спрятанный
    /// объект из подсчёта не выпадает: он выключил себя сам и остаётся на учёте.
    /// </remarks>
    public ObjectStateProgress GetSceneProgress(Enumeration group = null)
    {
        int opened = 0;
        int hidden = 0;

        foreach (SaveableObjectState state in loaded)
        {
            if (state == null)
                continue;

            if (group != null && state.Group != group)
                continue;

            if (state.IsOpened)
                opened++;
            else
                hidden++;
        }

        return new ObjectStateProgress(opened, hidden);
    }

    #endregion

    #region Проект

    /// <summary>
    /// Все сохранённые состояния проекта.
    /// </summary>
    /// <remarks>
    /// Пустой словарь, пока данные проекта не прочитаны, — обращаться к трекеру
    /// до готовности можно, он просто ничего не найдёт.
    /// </remarks>
    public IReadOnlyDictionary<string, SceneObjectState> Saved =>
        TryGetProjectData(out ProjectData projectData)
            ? (IReadOnlyDictionary<string, SceneObjectState>)projectData.SceneObjects
            : Empty;

    /// <summary>
    /// Сколько объектов записано в сохранении.
    /// </summary>
    public int SavedCount => Saved.Count;

    /// <summary>
    /// Состояние этого объекта уже сохранялось.
    /// </summary>
    public bool IsSaved(string key)
    {
        return !string.IsNullOrEmpty(key) && Saved.ContainsKey(key);
    }

    /// <summary>
    /// Читает сохранённое состояние по ключу.
    /// </summary>
    /// <remarks>
    /// Работает и для объектов с других уровней: запись живёт в данных проекта,
    /// а не в сцене.
    /// </remarks>
    public bool TryGetSaved(string key, out SceneObjectState state)
    {
        state = null;
        return !string.IsNullOrEmpty(key) && Saved.TryGetValue(key, out state);
    }

    /// <summary>
    /// Сколько сохранённых объектов показано или спрятано.
    /// </summary>
    public int CountSaved(bool opened)
    {
        int count = 0;

        foreach (KeyValuePair<string, SceneObjectState> pair in Saved)
        {
            if (pair.Value != null && pair.Value.IsActive == opened)
                count++;
        }

        return count;
    }

    /// <summary>
    /// Забывает сохранённое состояние объекта.
    /// </summary>
    /// <remarks>
    /// Объект вернётся к значениям по умолчанию при следующей загрузке уровня.
    /// Пригодится для сброса прогресса и уборки записей от объектов, которых на уровнях
    /// больше нет: сами они не исчезают.
    /// </remarks>
    public bool Forget(string key)
    {
        return !string.IsNullOrEmpty(key)
            && TryGetProjectData(out ProjectData projectData)
            && projectData.SceneObjects.Remove(key);
    }

    #endregion

    private static readonly Dictionary<string, SceneObjectState> Empty = new();

    /// <summary>
    /// Данные проекта, если они уже прочитаны.
    /// </summary>
    /// <remarks>
    /// Публичный: тем же вопросом задаётся <see cref="SaveableObjectState"/>, и правило
    /// «когда к данным можно обращаться» должно быть одно.
    /// </remarks>
    public static bool TryGetProjectData(out ProjectData projectData)
    {
        projectData = null;

        if (!GameManager.HasInstance || !GameManager.Instance.ReadySignal.IsReady)
            return false;

        projectData = GameManager.Instance.GetProjectData();
        return projectData != null;
    }
}
