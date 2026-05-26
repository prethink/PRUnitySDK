using System.Collections.Generic;

/// <summary>
/// Интерфейс для объектов, поддерживающих локализацию.
/// Позволяет получать переводы текста по языковому ключу.
/// </summary>
public interface ITranslateble
{
    /// <summary>
    /// Ключ перевода, который используется в системе локализации.
    /// Например, "item.sword.name" или "ui.start_button".
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Получить перевод для указанного языка.
    /// </summary>
    /// <param name="lang">Язык, для которого требуется перевод.</param>
    /// <returns>Переведённая строка или <see cref="Key"/>, если перевод не найден.</returns>
    string GetTranslateByLang(string lang);

    /// <summary>
    /// Получить перевод для текущего активного языка.
    /// </summary>
    /// <returns>Переведённая строка или <see cref="Key"/>, если перевод не найден.</returns>
    string GetTranslate();

    /// <summary>
    /// Возвращает все доступные переводы в виде словаря.
    /// </summary>
    /// <returns>
    /// Словарь, где ключ — тип языка, а значение — перевод.
    /// </returns>
    IReadOnlyDictionary<LangType, string> GetTranslateDictionary();
}
