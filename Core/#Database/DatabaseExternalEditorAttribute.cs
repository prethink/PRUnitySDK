using System;

/// <summary>
/// Секция правится отдельным окном, а не общим окном базы.
/// </summary>
/// <remarks>
/// Часть каталогов переросла общий список: у переводов свои колонки языков и проверки,
/// у достижений — условия и прогресс. Показывать те же данные ещё и сырым списком
/// вредно: правки разъезжаются.
/// <para>
/// В окне базы остаётся строка с названием и кнопкой, открывающей нужное окно, иначе
/// раздел не был бы виден вообще.
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
