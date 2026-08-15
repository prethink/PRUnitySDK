/// <summary>
/// Способ отображения секции базы в окне PRSDKDatabase.
/// </summary>
public enum DatabaseEditorPresentation
{
    /// <summary>
    /// Автоматически использует сетку для наследников <see cref="ItemDefinitionBase"/>.
    /// </summary>
    Auto,

    /// <summary>
    /// Использует стандартное отображение SerializedProperty.
    /// </summary>
    Default,

    /// <summary>
    /// Использует сетку карточек и редактор выбранного asset.
    /// </summary>
    Grid
}
