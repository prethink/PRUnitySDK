/// <summary>
/// Что делать с сохранением, версия которого старше <see cref="GameStorageSettings.SaveVersion"/>.
/// </summary>
/// <remarks>
/// Сохранение новее текущей версии всегда загружается как есть. Его записала более свежая
/// сборка, и стирать прогресс из-за старой сборки из кэша нельзя.
/// </remarks>
public enum SaveVersionMismatchAction
{
    /// <summary>
    /// Загрузить как есть, без шагов <see cref="ISaveMigration"/>. Поля, которых нет в
    /// <see cref="PRSaveData"/>, пропускаются, недостающие остаются по умолчанию.
    /// </summary>
    Keep = 0,

    /// <summary>
    /// Прогнать через <see cref="ISaveMigration"/> до текущей версии. Если шаг упал, начать новое.
    /// </summary>
    Convert = 1,

    /// <summary>
    /// Отбросить сохранение и начать новое.
    /// </summary>
    StartNew = 2
}
