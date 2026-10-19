#if !PRSDK_DISABLE_PIKABU
using System;
using System.Diagnostics;

/// <summary>
/// Сохранение игры в облачном хранилище Пикабу: весь <see cref="PRSaveData"/> одной строкой под ключом <see cref="SaveKey"/>.
/// </summary>
/// <remarks>
/// Хранилище приходит вместе с запуском SDK, чтение и запись синхронные. На сервер изменения SDK отправляет сам,
/// с небольшой задержкой; лимит площадки (100 запросов за 5 минут) считает только явные отправки, поэтому
/// обычное сохранение их не делает. Кому нужно подтверждение сервера — списанию покупок, — тот зовёт <see cref="Flush"/>.
/// <para>
/// Всё хранилище игрока — не больше 200 КБ. Сверх лимита данные в игре остаются, но на сервер уже не уходят.
/// </para>
/// <para>
/// Игра, открытая сразу на двух устройствах или вкладках, получает от площадки <c>REVISION_CONFLICT</c>, и до
/// перезагрузки страницы облако её сохранения не принимает. Игроку об этом говорит приватная часть интеграции.
/// </para>
/// <para>
/// Стратегия сохранения из настроек здесь не важна: данные всегда идут строкой, как при <see cref="SaveStrategy.Serialize"/>.
/// </para>
/// </remarks>
public class PikabuGameDataStorager : IGameDataStorage, IGameDataStorageSaveInfo
{
    /// <summary>
    /// Ключ облачного хранилища, под которым лежит сохранение.
    /// </summary>
    public const string SaveKey = "prsave";

    #region Поля и свойства

    private PRSaveData saveData;

    public DateTime? CreationDate => saveData == null || saveData.SaveDate == default
        ? null
        : saveData.SaveDate;

    public DateTime? LastUpdateDate => saveData == null
        ? null
        : saveData.UpdateDate != default
            ? saveData.UpdateDate
            : CreationDate;

    #endregion

    /// <summary>
    /// Отправляет записанное на сервер и сообщает, принял ли он его.
    /// </summary>
    public static void Flush(Action<bool> done)
    {
        PikabuSDK.StorageFlush(result =>
        {
            if (!result.success)
                PRLog.WriteWarning(typeof(PikabuGameDataStorager), $"Облако Пикабу не приняло сохранение: {result.reason}.");
            done?.Invoke(result.success);
        });
    }

    #region IGameDataStorage

    /// <remarks>
    /// Загрузчик собирает SDK после запуска площадки, и хранилище к этому времени уже на месте.
    /// Площадка не поднялась — игра начинается с нуля.
    /// </remarks>
    public bool TryLoad()
    {
        var stopwatch = Stopwatch.StartNew();

        if (!PikabuSDK.IsReady)
            PRLog.WriteWarning(this, "SDK Пикабу не запущен: сохранение не прочитано, игра начнётся с нуля.");

        bool loaded = SaveDataVersioning.TryReadRaw(PikabuSDK.StorageGet(SaveKey), out saveData);

        stopwatch.Stop();
        readySignal.SetReady();
        PRLog.WriteDebug(this, $"Loading end. in {stopwatch.Elapsed.TotalMilliseconds:F2} ms.");
        return loaded;
    }

    public void Save()
    {
        var stopwatch = Stopwatch.StartNew();

        saveData.UpdateDate = PRUnitySDK.ServerTime.GetNow();

        if (!PikabuSDK.IsReady)
        {
            PRLog.WriteWarning(this, "SDK Пикабу не запущен: сохранение не отправлено.");
            return;
        }

        PikabuSDK.StorageSet(SaveKey, SaveDataVersioning.Serialize(saveData));

        stopwatch.Stop();
        PRLog.WriteDebug(this, $"Save end. in {stopwatch.Elapsed.TotalMilliseconds:F2} ms.");
    }

    public GameSettings GetGameSettings()
    {
        return saveData.GameSettings.Clone() as GameSettings;
    }

    public ProjectData GetProjectData()
    {
        return saveData.ProjectData.Clone() as ProjectData;
    }

    public void UpdateGameSettings(GameSettings gameSettings, bool requiredSave = false)
    {
        saveData.GameSettings = gameSettings.Clone() as GameSettings;

        if (requiredSave)
            Save();
    }

    public void UpdateProjectData(ProjectData projectData, bool requiredSave = false)
    {
        saveData.ProjectData = projectData.Clone() as ProjectData;

        if (requiredSave)
            Save();
    }

    #endregion

    #region IReadySignalProvider

    protected readonly ReadySignal readySignal = new ReadySignal(typeof(PikabuGameDataStorager));

    public IReadySignal ReadySignal => readySignal;

    #endregion
}
#endif
