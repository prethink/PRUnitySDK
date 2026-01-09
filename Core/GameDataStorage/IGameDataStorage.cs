public interface IGameDataStorage
{
    /// <summary>
    /// Загружает данные игры (например, настройки или прогресс).
    /// </summary>
    void Load();

    /// <summary>
    /// Сохраняет текущие данные игры (например, настройки или прогресс).
    /// </summary>
    void Save();

    /// <summary>
    /// Получает текущие настройки игры.
    /// </summary>
    /// <returns>Объект GameSettings, содержащий настройки игры.</returns>
    GameSettings GetGameSettings();

    /// <summary>
    /// Получает текущие данные проекта.
    /// </summary>
    /// <returns>Объект ProjectData, содержащий данные проекта.</returns>
    ProjectData GetProjectData();

    /// <summary>
    /// Обновляет настройки игры с возможностью немедленного сохранения.
    /// </summary>
    /// <param name="gameSettings">Новые настройки игры.</param>
    /// <param name="requiredSave">Если true, то данные будут сохранены сразу после обновления.</param>
    void UpdateGameSettings(GameSettings gameSettings, bool requiredSave = false);

    /// <summary>
    /// Обновляет данные проекта с возможностью немедленного сохранения.
    /// </summary>
    /// <param name="projectContext">Новые данные проекта.</param>
    /// <param name="requiredSave">Если true, то данные будут сохранены сразу после обновления.</param>
    void UpdateProjectData(ProjectData projectContext, bool requiredSave = false);
}
