/// <summary>
/// Чем занят загрузчик (<see cref="Bootstrap"/>).
/// </summary>
/// <remarks>
/// Нужна диагностике: по виду загрузочной сцены стадии не различить — экран один и тот же.
/// </remarks>
public enum BootstrapStage
{
    /// <summary>
    /// Ждёт площадку: данные её SDK ещё не пришли. Без площадки стадия проходит мгновенно.
    /// </summary>
    WaitingPlatform,

    /// <summary>
    /// Собирает SDK: менеджеры, база, сохранение.
    /// </summary>
    InitializingSDK,

    /// <summary>
    /// SDK готов, идёт смена сцены на игровую.
    /// </summary>
    LoadingScene
}
