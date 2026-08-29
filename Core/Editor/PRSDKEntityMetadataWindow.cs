using UnityEditor;
using UnityEngine;

/// <summary>
/// Окно описаний сущностей: имя, иконка, качество и переводы.
/// </summary>
/// <remarks>
/// Описание — это то, чем сущность представляется игроку, и правят его отдельно от всего
/// остального: имена и иконки перебирают пачкой, а рядом с настройками звука или физики
/// они теряются.
/// </remarks>
public sealed class PRSDKEntityMetadataWindow : PRSDKDatabaseEditor
{
    /// <summary>
    /// Путь пункта меню: он же связывает окно с каталогом.
    /// </summary>
    public const string MenuPath = "PRUnitySDK/Windows/Entity metadata";

    /// <inheritdoc />
    protected override string OwnedEditorMenuPath => MenuPath;

    [MenuItem(MenuPath, false, 16)]
    private static void Open()
    {
        var window = GetWindow<PRSDKEntityMetadataWindow>();
        window.titleContent = new GUIContent("Описания сущностей");
        window.Show();
    }
}
