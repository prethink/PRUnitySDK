using UnityEditor;

/// <summary>
/// Окно наград: тот же редактор базы, но только со своими разделами.
/// </summary>
/// <remarks>
/// Наследник, а не отдельная реализация: сетка, поиск, проверки, наборы состава и удаление
/// уже написаны один раз. Окну остаётся сказать, какие разделы считает своими, — их
/// определяет атрибут <see cref="DatabaseExternalEditorAttribute"/> на каталоге.
/// </remarks>
public sealed class PRSDKRewardsWindow : PRSDKDatabaseEditor
{
    /// <summary>
    /// Путь пункта меню: он же связывает окно с каталогами.
    /// </summary>
    public const string MenuPath = "PRUnitySDK/Windows/Rewards";

    /// <inheritdoc />
    protected override string OwnedEditorMenuPath => MenuPath;

    [MenuItem(MenuPath, false, 12)]
    private static void Open()
    {
        var window = GetWindow<PRSDKRewardsWindow>();
        window.titleContent = new UnityEngine.GUIContent("Награды");
        window.Show();
    }
}
