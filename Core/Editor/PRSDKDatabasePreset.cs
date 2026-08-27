using System;
using System.Collections.Generic;

/// <summary>
/// Сохранённый состав базы: какие определения входят в сборку игры.
/// </summary>
/// <remarks>
/// Один проект SDK обслуживает несколько игр, и набор доступных шапок, эффектов и прочих
/// предметов у них разный — это прямо влияет на размер билда. Набор описывает состав
/// каталогов, чтобы переключать его целиком, а не собирать заново руками.
/// <para>
/// Хранится в JSON рядом с проектом, а не ассетом в <c>Assets</c>: набор нужен только
/// редактору, и попадать в билд ему незачем.
/// </para>
/// </remarks>
[Serializable]
public class PRSDKDatabasePreset
{
    /// <summary>
    /// Версия формата.
    /// </summary>
    /// <remarks>
    /// Набор переживает правки SDK, поэтому при чтении нужно понимать, с чем имеешь дело.
    /// </remarks>
    public int version = 1;

    /// <summary>
    /// Имя набора.
    /// </summary>
    public string name = string.Empty;

    /// <summary>
    /// Когда набор сохранён, в формате ISO 8601.
    /// </summary>
    public string savedAt = string.Empty;

    /// <summary>
    /// Проект, в котором набор сделан.
    /// </summary>
    /// <remarks>
    /// Только для чтения человеком: помогает понять, откуда приехал файл.
    /// </remarks>
    public string project = string.Empty;

    /// <summary>
    /// Каталоги базы.
    /// </summary>
    public List<PRSDKDatabasePresetSection> sections = new();
}

/// <summary>
/// Один каталог базы внутри набора.
/// </summary>
[Serializable]
public class PRSDKDatabasePresetSection
{
    /// <summary>
    /// Путь сериализованного свойства — по нему каталог находится при загрузке.
    /// </summary>
    public string path = string.Empty;

    /// <summary>
    /// Читаемое название каталога.
    /// </summary>
    public string label = string.Empty;

    /// <summary>
    /// Тип элементов каталога.
    /// </summary>
    /// <remarks>
    /// Проверяется при загрузке: ассет с подходящим GUID может оказаться совсем не тем,
    /// если файлы переносили между проектами.
    /// </remarks>
    public string elementType = string.Empty;

    /// <summary>
    /// Элементы каталога.
    /// </summary>
    public List<PRSDKDatabasePresetItem> items = new();
}

/// <summary>
/// Ссылка на один ассет внутри набора.
/// </summary>
/// <remarks>
/// Хранится сразу тремя способами. GUID — основной: он переживает переименование и
/// перемещение файла. Путь и имя нужны, когда GUID не совпал: ассеты, скопированные между
/// проектами без <c>.meta</c>, получают новые GUID, и без запасного поиска набор пришлось бы
/// собирать заново.
/// </remarks>
[Serializable]
public class PRSDKDatabasePresetItem
{
    /// <summary>
    /// GUID ассета.
    /// </summary>
    public string guid = string.Empty;

    /// <summary>
    /// Локальный идентификатор внутри ассета.
    /// </summary>
    /// <remarks>
    /// Нужен вложенным объектам: у ассета их может быть несколько, и GUID у них общий.
    /// </remarks>
    public long localId;

    /// <summary>
    /// Путь ассета на момент сохранения.
    /// </summary>
    public string path = string.Empty;

    /// <summary>
    /// Имя ассета на момент сохранения.
    /// </summary>
    public string name = string.Empty;

    /// <summary>
    /// Тип объекта на момент сохранения.
    /// </summary>
    public string type = string.Empty;
}
