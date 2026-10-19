using UnityEditor;

/// <summary>
/// Окно предметов: каталоги вещей, которые видит игрок.
/// </summary>
/// <remarks>
/// Каталогов предметов больше десятка, и в общем списке базы они тонули среди наград,
/// звуков и настроек. Разделы те же самые — окно лишь показывает их отдельно.
/// </remarks>
public sealed class PRSDKItemsWindow : PRSDKDatabaseEditor, IPRSDKGameWindow
{
    /// <summary>
    /// Путь пункта меню: он же связывает окно с каталогами.
    /// </summary>
    public const string MenuPath = "PRUnitySDK/Game/Items";

    /// <inheritdoc />
    protected override string OwnedEditorMenuPath => MenuPath;

    [MenuItem(MenuPath, false, 20)]
    private static void Open()
    {
        PRSDKGameWindows.Open<PRSDKItemsWindow>("Items");
    }
}
