using UnityEditor;

/// <summary>
/// Окно предметов: каталоги вещей, которые видит игрок.
/// </summary>
/// <remarks>
/// Каталогов предметов больше десятка, и в общем списке базы они тонули среди наград,
/// звуков и настроек. Разделы те же самые — окно лишь показывает их отдельно.
/// </remarks>
public sealed class PRSDKItemsWindow : PRSDKDatabaseEditor
{
    /// <summary>
    /// Путь пункта меню: он же связывает окно с каталогами.
    /// </summary>
    public const string MenuPath = "PRUnitySDK/Windows/Items";

    /// <inheritdoc />
    protected override string OwnedEditorMenuPath => MenuPath;

    [MenuItem(MenuPath, false, 11)]
    private static void Open()
    {
        var window = GetWindow<PRSDKItemsWindow>();
        window.titleContent = new UnityEngine.GUIContent("Предметы");
        window.Show();
    }
}
