using System;

/// <summary>
/// Секция правится отдельным окном, а не общим окном базы.
/// </summary>
/// <remarks>
/// Часть каталогов переросла общий список: у переводов свои колонки языков и проверки,
/// у достижений — условия и прогресс. Такому разделу нужен свой редактор, а показывать
/// те же данные ещё и сырым списком вредно: правки разъезжаются, а настройщик не знает,
/// какое из двух мест главное.
/// <para>
/// Секция не исчезает совсем: в окне базы остаётся строка с названием и кнопкой,
/// открывающей нужное окно. Иначе раздел выглядел бы потерянным — есть в данных, но
/// нигде не виден.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class DatabaseExternalEditorAttribute : Attribute
{
    /// <summary>
    /// Путь пункта меню, открывающего окно.
    /// </summary>
    public string MenuPath { get; }

    /// <summary>
    /// Название окна для подписи.
    /// </summary>
    public string WindowName { get; set; } = string.Empty;

    /// <summary>
    /// Чем именно занимается это окно.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="menuPath">Путь пункта меню, например <c>PRUnitySDK/Windows/Localization</c>.</param>
    public DatabaseExternalEditorAttribute(string menuPath)
    {
        MenuPath = menuPath;
    }
}
