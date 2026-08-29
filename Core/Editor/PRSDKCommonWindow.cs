using UnityEditor;
using UnityEngine;

/// <summary>
/// Общие каталоги: действия, спрайты и звуки.
/// </summary>
/// <remarks>
/// Сюда попадает то, что не относится ни к предметам, ни к наградам, ни к описаниям
/// сущностей: реестры ассетов, к которым обращаются по ключу. Окно временное по смыслу —
/// как только у любого из этих каталогов появится своё содержательное окно, он уедет
/// туда, а это исчезнет само.
/// </remarks>
public sealed class PRSDKCommonWindow : PRSDKDatabaseEditor
{
    /// <summary>
    /// Путь пункта меню: он же связывает окно с каталогами.
    /// </summary>
    public const string MenuPath = "PRUnitySDK/Windows/Common";

    /// <inheritdoc />
    protected override string OwnedEditorMenuPath => MenuPath;

    [MenuItem(MenuPath, false, 17)]
    private static void Open()
    {
        var window = GetWindow<PRSDKCommonWindow>();
        window.titleContent = new GUIContent("Общее");
        window.Show();
    }
}
