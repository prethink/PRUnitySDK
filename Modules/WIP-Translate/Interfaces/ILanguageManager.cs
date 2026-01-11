using System;

/// <summary>
/// Интерфейс менеджера языков.
/// </summary>
public interface ILanguageManager
{
    /// <summary>
    /// Событие изменения языка.
    /// </summary>
    public event Action<string> OnChangeLangEvent;

    /// <summary>
    /// Инициализация системы.
    /// </summary>
    public void InitSystem();

    /// <summary>
    /// Получить текущий язык.
    /// </summary>
    /// <param name="lang">Язык.</param>
    public void SwitchLang(string lang);

    /// <summary>
    /// Получить текущий язык.
    /// </summary>
    /// <returns>Язык.</returns>
    public string GetCurrentLang();

    /// <summary>
    /// Инициализация языка.
    /// </summary>
    /// <param name="lang"></param>
    public void InitLang(string lang);
}
