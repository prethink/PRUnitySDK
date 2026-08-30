using UnityEditor;
using UnityEngine;

/// <summary>
/// Окно описаний сущностей: имя, иконка, качество и переводы.
/// </summary>
/// <remarks>
/// Описание — это то, чем сущность представляется игроку, и правят его отдельно от всего
/// остального: имена и иконки перебирают пачкой, а рядом с настройками звука или физики
/// они теряются.
/// <para>
/// Вкладок две, потому что хранилища два: у сущностей без каталога описание лежит
/// отдельным ассетом, у предметов каталога — в самом определении. Показывать только
/// первое значило бы показывать восемь ассетов и молчать про четыре сотни.
/// </para>
/// <para>
/// Ассеты ищутся по проекту, а не берутся из каталога SDK: каталог приходилось вести
/// руками, и он расходился с проектом молча.
/// </para>
/// </remarks>
public sealed class PRSDKEntityMetadataWindow : EditorWindow
{
    private const string MenuPath = "PRUnitySDK/Windows/Entity metadata";

    private static readonly string[] TabTitles = { "Описания", "Определения", "Проверка" };

    [SerializeField] private int selectedTab;

    private EntityDescriptionBrowser metadata;
    private EntityDescriptionBrowser definitions;
    private EntityDescriptionAuditView audit;

    [MenuItem(MenuPath, false, 16)]
    private static void Open()
    {
        var window = GetWindow<PRSDKEntityMetadataWindow>();
        window.titleContent = new GUIContent("Описания сущностей");
        window.minSize = new Vector2(720f, 480f);
        window.Show();
    }

    private void OnGUI()
    {
        selectedTab = GUILayout.Toolbar(
            Mathf.Clamp(selectedTab, 0, TabTitles.Length - 1),
            TabTitles,
            GUILayout.Height(24f));

        switch (selectedTab)
        {
            case 0:
                Ensure(ref metadata, nameof(EntityMetadataBase), "Описаний").Draw();
                break;

            case 1:
                Ensure(ref definitions, nameof(ItemDefinitionBase), "Определений").Draw();
                break;

            default:
                EnsureAudit().Draw();
                break;
        }
    }

    /// <summary>
    /// Создаёт состояние вкладки при первом показе.
    /// </summary>
    /// <remarks>
    /// Состояние держится <c>ScriptableObject</c>, а не полем окна: так выбранный ассет
    /// и фильтры переживают перекомпиляцию скриптов.
    /// </remarks>
    private EntityDescriptionBrowser Ensure(
        ref EntityDescriptionBrowser browser,
        string typeName,
        string label)
    {
        if (browser == null)
        {
            browser = CreateInstance<EntityDescriptionBrowser>();
            browser.hideFlags = HideFlags.HideAndDontSave;
        }

        browser.Configure(typeName, label);

        return browser;
    }

    private EntityDescriptionAuditView EnsureAudit()
    {
        if (audit == null)
        {
            audit = CreateInstance<EntityDescriptionAuditView>();
            audit.hideFlags = HideFlags.HideAndDontSave;
        }

        return audit;
    }

    private void OnDestroy()
    {
        Release(ref metadata);
        Release(ref definitions);

        if (audit != null)
            DestroyImmediate(audit);

        audit = null;
    }

    private static void Release(ref EntityDescriptionBrowser browser)
    {
        if (browser != null)
            DestroyImmediate(browser);

        browser = null;
    }
}
