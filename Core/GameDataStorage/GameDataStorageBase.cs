using System;
using System.Diagnostics;

/// <summary>
/// Общая часть хранилищ, которые держат сохранение одной строкой.
/// </summary>
/// <remarks>
/// Хранилище площадки отличается только тем, откуда строка берётся и куда уходит
/// (<see cref="ReadRaw"/>, <see cref="WriteRaw"/>). Всё остальное одинаково: версия и шифрование
/// (<see cref="SaveDataVersioning"/>), даты, копии данных для игры, сигнал готовности.
/// <para>
/// Строка сохранения есть, но не прочиталась (в том числе сорвался шаг преобразования версии) — хранилище до конца запуска не пишет
/// (<see cref="IsWriteBlocked"/>). Игра при этом идёт с новыми данными, как при первом запуске, а сохранение
/// игрока остаётся нетронутым: прочитать его сможет исправленная сборка. Без этого первая же запись
/// заменила бы прогресс пустым.
/// </para>
/// <para>
/// То же, когда при загрузке хранилище было недоступно (<see cref="MarkUnavailable"/>): площадка не поднялась,
/// сохранение не прочитано, и если она ответит позже, запись этого запуска легла бы поверх настоящего прогресса.
/// </para>
/// <para>
/// Хранилище, которое держит сохранение не строкой или в нескольких местах, переопределяет
/// <see cref="ReadSave"/> и <see cref="WriteSave"/>.
/// </para>
/// </remarks>
public abstract class GameDataStorageBase : IGameDataStorage, IGameDataStorageSaveInfo
{
    private PRSaveData saveData;

    private bool blockedSaveReported;

    private bool unavailableOnLoad;

    /// <summary>
    /// Запись выключена до конца запуска: сохранение игрока не прочиталось или хранилище при загрузке было
    /// недоступно, и запись заменила бы прогресс игрока пустым.
    /// </summary>
    public bool IsWriteBlocked { get; private set; }

    /// <summary>
    /// Последний вызов <see cref="Save"/> отдал данные хранилищу, и оно их приняло.
    /// </summary>
    /// <remarks>
    /// <see cref="GameManager"/> по этому признаку решает, состоялась ли запись: о несостоявшейся подписчики
    /// события сохранения узнать не должны — списание покупок ждёт именно записанного.
    /// <para>
    /// «Приняло» — данные переданы хранилищу или SDK площадки, а не «сервер подтвердил». Подтверждение облака —
    /// отдельный вопрос каждой площадки (очередь синхронизации GamePush, <c>Flush</c> Пикабу); у Яндекса его нет вовсе:
    /// плагин итог отправки не сообщает.
    /// </para>
    /// </remarks>
    public bool LastSaveWritten { get; private set; }

    /// <inheritdoc />
    public DateTime? CreationDate => saveData == null || saveData.SaveDate == default
        ? null
        : saveData.SaveDate;

    /// <inheritdoc />
    public DateTime? LastUpdateDate => saveData == null
        ? null
        : saveData.UpdateDate != default
            ? saveData.UpdateDate
            : CreationDate;

    /// <summary>
    /// Сохранение лежит у площадки: в запуске, не дождавшемся её (<see cref="Bootstrap.StartedWithoutPlatform"/>),
    /// хранилище не читает и не пишет.
    /// </summary>
    /// <remarks>
    /// Площадка могла ответить уже после сторожа загрузчика. Записать в неё теперь значило бы положить прогресс
    /// этого запуска поверх настоящего. Хранилище, которому площадка не нужна (диск, PlayerPrefs), отвечает
    /// <see langword="false"/>.
    /// </remarks>
    protected virtual bool NeedsPlatform => true;

    /// <summary>
    /// Строка сохранения из хранилища; пусто, если сохранения ещё нет или хранилище недоступно.
    /// </summary>
    protected abstract string ReadRaw();

    /// <summary>
    /// Отдаёт строку сохранения хранилищу.
    /// </summary>
    /// <returns><see langword="false"/>, если хранилище строку не приняло: площадка не запущена, нет места.</returns>
    protected abstract bool WriteRaw(string raw);

    /// <summary>
    /// Читает сохранение. По умолчанию — из строки <see cref="ReadRaw"/>.
    /// </summary>
    /// <param name="data">Сохранение; если его нет или оно не прочиталось, новое.</param>
    /// <param name="unreadable"><c>true</c>, если сохранение есть, но не прочиталось: запись будет выключена.</param>
    /// <returns><c>true</c>, если прочитано существующее сохранение.</returns>
    protected virtual bool ReadSave(out PRSaveData data, out bool unreadable)
    {
        return SaveDataVersioning.TryReadRaw(ReadRaw() ?? string.Empty, out data, out unreadable);
    }

    /// <summary>
    /// Отдаёт сохранение хранилищу. По умолчанию — строкой через <see cref="WriteRaw"/>.
    /// </summary>
    /// <returns><see langword="false"/>, если хранилище сохранение не приняло.</returns>
    protected virtual bool WriteSave(PRSaveData data)
    {
        return WriteRaw(SaveDataVersioning.Serialize(data));
    }

    /// <summary>
    /// Сообщает из <see cref="ReadRaw"/> или <see cref="ReadSave"/>, что хранилище недоступно и сохранение не прочитано:
    /// запись до конца запуска выключается.
    /// </summary>
    protected void MarkUnavailable()
    {
        unavailableOnLoad = true;
    }

    #region IGameDataStorage

    /// <inheritdoc />
    public bool TryLoad()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        unavailableOnLoad = false;
        bool unreadable = false;
        bool loaded = false;

        if (NeedsPlatform && Bootstrap.StartedWithoutPlatform)
            MarkUnavailable();
        else
            loaded = ReadSave(out saveData, out unreadable);

        // В сохранении эти части могли записаться пустыми; игра ждёт их всегда.
        saveData ??= new PRSaveData();
        saveData.GameSettings ??= new GameSettings();
        saveData.ProjectData ??= new ProjectData();

        IsWriteBlocked = unreadable || unavailableOnLoad;
        blockedSaveReported = false;
        LastSaveWritten = false;

        if (unreadable)
        {
            PRLog.WriteError(this, "Сохранение игрока есть, но не прочиталось. Игра начнётся с новых данных, а запись " +
                                   "до конца запуска выключена, чтобы не затереть его.");
        }
        else if (unavailableOnLoad)
        {
            PRLog.WriteWarning(this, "Хранилище при загрузке недоступно: игра начнётся с новых данных, запись до конца " +
                                     "запуска выключена.");
        }

        stopwatch.Stop();
        readySignal.SetReady();
        PRLog.WriteDebug(this, $"Loading end. in {stopwatch.Elapsed.TotalMilliseconds:F2} ms.");

        return loaded;
    }

    /// <inheritdoc />
    public void Save()
    {
        LastSaveWritten = false;

        if (IsWriteBlocked)
        {
            // Один раз за запуск: сохранение зовут часто, и журнал заполнился бы одной строкой.
            if (!blockedSaveReported)
            {
                blockedSaveReported = true;
                PRLog.WriteWarning(this, "Сохранение не записано: запись выключена при загрузке до конца запуска.");
            }

            return;
        }

        Stopwatch stopwatch = Stopwatch.StartNew();

        saveData.UpdateDate = PRUnitySDK.ServerTime.GetNow();

        LastSaveWritten = WriteSave(saveData);

        if (!LastSaveWritten)
            PRLog.WriteWarning(this, "Хранилище не приняло сохранение.");

        stopwatch.Stop();
        PRLog.WriteDebug(this, $"Save end. in {stopwatch.Elapsed.TotalMilliseconds:F2} ms.");
    }

    /// <inheritdoc />
    public GameSettings GetGameSettings()
    {
        return saveData?.GameSettings?.Clone() as GameSettings;
    }

    /// <inheritdoc />
    public ProjectData GetProjectData()
    {
        return saveData?.ProjectData?.Clone() as ProjectData;
    }

    /// <inheritdoc />
    public void UpdateGameSettings(GameSettings gameSettings, bool requiredSave = false)
    {
        saveData.GameSettings = gameSettings.Clone() as GameSettings;

        if (requiredSave)
            Save();
    }

    /// <inheritdoc />
    public void UpdateProjectData(ProjectData projectData, bool requiredSave = false)
    {
        saveData.ProjectData = projectData.Clone() as ProjectData;

        if (requiredSave)
            Save();
    }

    #endregion

    #region IReadySignalProvider

    private readonly ReadySignal readySignal;

    protected GameDataStorageBase()
    {
        // Имя сигнала — тип самого хранилища, как было у каждого из них до общей базы.
        readySignal = new ReadySignal(GetType());
    }

    /// <inheritdoc />
    public IReadySignal ReadySignal => readySignal;

    #endregion
}
