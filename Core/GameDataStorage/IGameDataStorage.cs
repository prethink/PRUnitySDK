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
    /// Установить значение.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="category"></param>
    /// <param name="enumeration"></param>
    /// <param name="value"></param>
    /// <param name="isRequiredSave"></param>
    void SetValue<T>(Enumeration category, Enumeration<T> enumeration, T value, bool isRequiredSave = true);

    /// <summary>
    /// Получить значение.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="category"></param>
    /// <param name="enumeration"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    T GetValue<T>(Enumeration category, Enumeration<T> enumeration, T defaultValue);

    /// <summary>
    /// Обновляет данные проекта с возможностью немедленного сохранения.
    /// </summary>
    /// <param name="projectContext">Новые данные проекта.</param>
    /// <param name="requiredSave">Если true, то данные будут сохранены сразу после обновления.</param>
    void UpdateProjectData(ProjectData projectContext, bool requiredSave = false);
}
