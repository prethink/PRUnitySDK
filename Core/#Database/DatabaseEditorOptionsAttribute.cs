using System;

/// <summary>
/// Управляет доступными действиями секции в окне PRSDKDatabase.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class DatabaseEditorOptionsAttribute : Attribute
{
    /// <summary>
    /// Способ отображения содержимого базы.
    /// </summary>
    public DatabaseEditorPresentation Presentation { get; set; } = DatabaseEditorPresentation.Auto;

    /// <summary>
    /// Показывать кнопку добавления всех найденных assets.
    /// </summary>
    public bool ShowAddAll { get; set; } = true;

    /// <summary>
    /// Показывать кнопку удаления пустых ссылок.
    /// </summary>
    public bool ShowRemoveNull { get; set; } = true;

    /// <summary>
    /// Показывать кнопку полной очистки списка.
    /// </summary>
    public bool ShowClear { get; set; } = true;

    /// <summary>
    /// Показывать результаты <see cref="IDatabaseValidationProvider.Validate"/>.
    /// </summary>
    public bool ShowValidation { get; set; } = true;
}
