using System.Collections.Generic;

/// <summary>
/// Сохранение игрока, которое площадка пишет на диск при запуске из редактора.
/// </summary>
/// <remarks>
/// У каждой площадки сохранение своё: у Яндекса (YG2) — файл <c>SavesEditorYG2.json</c>, у VK, Пикабу и Steam —
/// <c>UserSettings/PRSDKEditorSaves/&lt;площадка&gt;/storage.json</c> (<see cref="PRSDKEditorStorage"/>).
/// Площадка объявляет свои файлы этим интерфейсом, а <see cref="PRSDKProjectSaves"/> при смене проекта SDK
/// убирает их в папку прежнего проекта и возвращает файлы нового. Реализации находятся сами
/// (<c>TypeCache</c>): нужен открытый класс с конструктором без параметров. Встроенные — в ядре
/// (<c>PRSDKPlatformSaveStores.cs</c>), без кода плагинов: отключённая площадка тоже переключается.
/// </remarks>
public interface IPRSDKEditorSaveStore
{
    /// <summary>
    /// Имя площадки: папка, в которой лежат сохранения этой площадки у проекта.
    /// </summary>
    string Platform { get; }

    /// <summary>
    /// Полные пути файлов сохранения, которые пишет площадка.
    /// </summary>
    IEnumerable<string> Files { get; }
}
