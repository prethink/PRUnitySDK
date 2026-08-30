using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Список описаний одного вида: сетка слева, свойства выбранного справа.
/// </summary>
/// <remarks>
/// Описание сущности живёт в двух видах ассетов: у сущностей без каталога - отдельным
/// <c>EntityMetadataBase</c>, у предметов каталога - в самом определении. Устроены они
/// одинаково - имя, иконка, качество, переводы, - поэтому список у них общий,
/// а различает их только тип поиска.
/// <para>
/// Ассеты ищутся по проекту, а не берутся из каталога: список в каталоге приходится вести
/// руками, и он расходится с проектом молча - к моменту, когда это заметили, в базе было
/// 6 описаний из 8 существующих.
/// </para>
/// </remarks>
public sealed class EntityDescriptionBrowser : ScriptableObject
{
    private const float InspectorWidth = 420f;
    private const float CardWidth = 124f;
    private const float CardHeight = 152f;
    private const float CardSpacing = 6f;
    private const float IconSize = 96f;

    [SerializeField] private string searchType = nameof(EntityMetadataBase);
    [SerializeField] private string countLabel = "Описаний";
    [SerializeField] private string search = string.Empty;
    [SerializeField] private int typeIndex;
    [SerializeField] private ScriptableObject selected;

    private ScriptableObject[] assets = Array.Empty<ScriptableObject>();
    private string[] typeNames = { "Все" };
    private Editor inspector;
    private EntityPrefabsGrid prefabs;
    private EntitySceneUsageList scenes;
    private readonly Dictionary<ScriptableObject, MessageType> severities = new();
    private GUIStyle badgeStyle;
    private Vector2 listScroll;
    private Vector2 inspectorScroll;
    private float lastWidth = 400f;
    private float measuredWidth;
    private GUIStyle nameStyle;
    private bool loaded;

    /// <summary>
    /// Настраивает, что искать.
    /// </summary>
    /// <param name="typeName">Базовый тип ассета для фильтра <c>t:</c>.</param>
    /// <param name="label">Подпись счётчика.</param>
    public void Configure(string typeName, string label)
    {
        if (string.Equals(searchType, typeName, StringComparison.Ordinal))
            return;

        searchType = typeName;
        countLabel = label;
        loaded = false;
    }

    /// <summary>
    /// Рисует список.
    /// </summary>
    public void Draw()
    {
        if (!loaded)
            Reload();

        AdoptMeasuredWidth();
        DrawToolbar();
        DrawValidationSummary();

        ScriptableObject[] visible = GetVisible();

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawGrid(visible);
            DrawInspector();
        }
    }

    /// <summary>
    /// Перечитывает ассеты проекта.
    /// </summary>
    /// <remarks>
    /// Ищется по базовому типу: наследники находятся сами, и новый вид описания попадает
    /// в список без правок здесь.
    /// </remarks>
    public void Reload()
    {
        loaded = true;

        // Импорт ассетов закрывает SerializedObject у открытых редакторов: прежний
        // инспектор после этого падает при первой же отрисовке.
        DestroyInspector();

        assets = AssetDatabase.FindAssets($"t:{searchType}")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Distinct(StringComparer.Ordinal)
            .Select(AssetDatabase.LoadAssetAtPath<ScriptableObject>)
            .Where(asset => asset is IEntityMetadata)
            .OrderBy(asset => asset.GetType().Name, StringComparer.Ordinal)
            .ThenBy(asset => asset.name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        typeNames = new[] { "Все" }
            .Concat(assets
                .Select(asset => asset.GetType().Name)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal))
            .ToArray();

        RebuildSeverities();

        if (selected != null && !assets.Contains(selected))
            Select(null);
    }

    private void OnDisable()
    {
        DestroyInspector();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            EditorGUILayout.LabelField($"{countLabel}: {assets.Length}", GUILayout.Width(150f));

            search = GUILayout.TextField(search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(120f));

            typeIndex = EditorGUILayout.Popup(
                Mathf.Clamp(typeIndex, 0, typeNames.Length - 1),
                typeNames,
                EditorStyles.toolbarPopup,
                GUILayout.Width(180f));

            if (GUILayout.Button("Обновить", EditorStyles.toolbarButton, GUILayout.Width(80f)))
            {
                Reload();
                GUIUtility.ExitGUI();
            }
        }
    }

    /// <summary>
    /// Строка с итогом проверок над сеткой.
    /// </summary>
    /// <remarks>
    /// Нужна, чтобы не пришлось перебирать карточки в поисках значка: по одной строке
    /// видно, есть ли в списке что чинить вообще.
    /// </remarks>
    private void DrawValidationSummary()
    {
        int errors = 0;
        int warnings = 0;

        foreach (MessageType worst in severities.Values)
        {
            if (worst == MessageType.Error)
                errors++;
            else if (worst == MessageType.Warning)
                warnings++;
        }

        if (errors == 0 && warnings == 0)
        {
            EditorGUILayout.HelpBox("Проблем не найдено.", MessageType.None);
            return;
        }

        string message = errors > 0
            ? $"С ошибками: {errors}. С предупреждениями: {warnings}."
            : $"С предупреждениями: {warnings}.";

        EditorGUILayout.HelpBox(message, errors > 0 ? MessageType.Error : MessageType.Warning);
    }

    /// <summary>
    /// Пересчитывает проверки по всем ассетам списка.
    /// </summary>
    /// <remarks>
    /// Считается на перечитывание списка, а не на каждый кадр: проверок немного, но их
    /// сотни, и часть лезет в индекс использований. Выбранный ассет обновляется отдельно -
    /// его правят прямо сейчас, и значок должен успевать за правкой.
    /// </remarks>
    private void RebuildSeverities()
    {
        severities.Clear();

        foreach (ScriptableObject asset in assets)
            severities[asset] = WorstSeverity(asset);
    }

    private static MessageType WorstSeverity(ScriptableObject asset)
    {
        var worst = MessageType.None;

        foreach (EntityDescriptionIssue issue in EntityDescriptionValidator.Validate(asset))
        {
            if (issue.Severity == MessageType.Error)
                return MessageType.Error;

            if (issue.Severity == MessageType.Warning)
                worst = MessageType.Warning;
        }

        return worst;
    }

    private ScriptableObject[] GetVisible()
    {
        IEnumerable<ScriptableObject> result = assets;

        if (typeIndex > 0 && typeIndex < typeNames.Length)
        {
            string typeName = typeNames[typeIndex];
            result = result.Where(asset =>
                string.Equals(asset.GetType().Name, typeName, StringComparison.Ordinal));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            result = result.Where(asset =>
                asset.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                || (((IEntityMetadata)asset).Name ?? string.Empty)
                    .IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        return result.ToArray();
    }

    private void DrawGrid(IReadOnlyList<ScriptableObject> visible)
    {
        using (new EditorGUILayout.VerticalScope())
        {
            if (visible.Count == 0)
            {
                EditorGUILayout.HelpBox("Ассетов с такими условиями не найдено.", MessageType.Info);
                GUILayout.FlexibleSpace();
                return;
            }

            using (var scroll = new EditorGUILayout.ScrollViewScope(listScroll))
            {
                int columns = Mathf.Max(1, Mathf.FloorToInt(MeasureWidth() / (CardWidth + CardSpacing)));

                for (int index = 0; index < visible.Count; index += columns)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        for (int column = 0; column < columns && index + column < visible.Count; column++)
                            DrawCard(visible[index + column]);

                        GUILayout.FlexibleSpace();
                    }
                }

                listScroll = scroll.scrollPosition;
            }
        }
    }

    /// <summary>
    /// Ширина колонки под сетку.
    /// </summary>
    /// <remarks>
    /// Настоящую ширину раскладка знает только на проходе <c>Repaint</c>, а на <c>Layout</c>
    /// отдаёт ноль. Взять свежее значение прямо здесь нельзя: число колонок разошлось бы
    /// между проходами, а вместе с ним и число нарисованных карточек - IMGUI отвечает на
    /// это исключением про «control N в группе из N».
    /// <para>
    /// Поэтому измеренное значение откладывается и вступает в силу с началом следующего
    /// <c>Layout</c>. Внутри одного кадра оба прохода пользуются одним и тем же числом.
    /// </para>
    /// </remarks>
    private float MeasureWidth()
    {
        Rect area = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true));

        if (Event.current.type == EventType.Repaint && area.width > 1f)
            measuredWidth = area.width;

        return lastWidth;
    }

    private void AdoptMeasuredWidth()
    {
        if (Event.current.type != EventType.Layout)
            return;

        if (measuredWidth > 1f)
            lastWidth = measuredWidth;
    }

    private void DrawCard(ScriptableObject asset)
    {
        var description = (IEntityMetadata)asset;
        bool isSelected = asset == selected;

        Rect card = GUILayoutUtility.GetRect(
            CardWidth,
            CardHeight,
            GUILayout.Width(CardWidth),
            GUILayout.Height(CardHeight));

        Color accent = QualityUtils.GetColor(description.Quality);
        Color background = Color.Lerp(new Color(0.13f, 0.14f, 0.17f, 1f), accent, 0.18f);

        EditorGUI.DrawRect(card, background);
        DrawBorder(card, isSelected ? accent : Color.Lerp(background, Color.white, 0.14f), isSelected ? 3f : 1f);

        if (GUI.Button(card, new GUIContent(string.Empty, AssetDatabase.GetAssetPath(asset)), GUIStyle.none))
        {
            Select(isSelected ? null : asset);

            // Раскладка кадра посчитана со старым выбором: продолжать её с новым нельзя.
            GUIUtility.ExitGUI();
        }

        var iconRect = new Rect(
            card.x + (card.width - IconSize) * 0.5f,
            card.y + 6f,
            IconSize,
            IconSize);

        if (description.Icon != null)
            DrawSprite(iconRect, description.Icon);
        else
            GUI.Label(iconRect, "Нет иконки", EditorStyles.centeredGreyMiniLabel);

        var labelRect = new Rect(card.x + 4f, iconRect.yMax + 2f, card.width - 8f, 44f);
        GUI.Label(labelRect, asset.name, GetNameStyle());

        DrawValidationBadge(card, asset);
    }

    /// <summary>
    /// Значок проблем в углу карточки.
    /// </summary>
    /// <remarks>
    /// Выбранный ассет проверяется заново на каждой отрисовке: его правят в инспекторе
    /// рядом, и значок должен гаснуть сразу, как заполнили поле. Остальные берутся
    /// из посчитанного при перечитывании списка.
    /// </remarks>
    private void DrawValidationBadge(Rect card, ScriptableObject asset)
    {
        MessageType worst;

        if (asset == selected)
        {
            worst = WorstSeverity(asset);
            severities[asset] = worst;
        }
        else if (!severities.TryGetValue(asset, out worst))
        {
            return;
        }

        if (worst != MessageType.Error && worst != MessageType.Warning)
            return;

        bool isError = worst == MessageType.Error;

        var badgeRect = new Rect(card.xMax - 24f, card.y + 4f, 20f, 20f);

        EditorGUI.DrawRect(
            badgeRect,
            isError ? new Color(0.78f, 0.16f, 0.16f, 1f) : new Color(0.78f, 0.55f, 0.12f, 1f));

        GUI.Label(
            badgeRect,
            new GUIContent("!", isError ? "Есть ошибки заполнения" : "Есть предупреждения"),
            GetBadgeStyle());
    }

    private GUIStyle GetBadgeStyle()
    {
        return badgeStyle ??= new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
    }

    private void DrawInspector()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(InspectorWidth)))
        {
            if (selected == null)
            {
                EditorGUILayout.HelpBox("Выберите карточку слева.", MessageType.Info);
                GUILayout.FlexibleSpace();
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(selected.name, EditorStyles.largeLabel);

                if (GUILayout.Button("Показать", GUILayout.Width(80f)))
                    EditorGUIUtility.PingObject(selected);
            }

            EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(selected), EditorStyles.miniLabel);

            DrawDeleteButtons();

            using (var scroll = new EditorGUILayout.ScrollViewScope(inspectorScroll))
            {
                DrawIssues();

                GetInspector()?.OnInspectorGUI();

                EditorGUILayout.Space(6f);

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    prefabs ??= new EntityPrefabsGrid();
                    prefabs.Draw(selected);
                }

                EditorGUILayout.Space(4f);

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    scenes ??= new EntitySceneUsageList();
                    scenes.Draw(selected);
                }

                inspectorScroll = scroll.scrollPosition;
            }
        }
    }

    /// <summary>
    /// Рисует проблемы выбранного описания.
    /// </summary>
    /// <remarks>
    /// Проверки считаются на каждый кадр отрисовки: они дешёвые - чтение полей одного
    /// ассета, - зато список не отстаёт от правок в инспекторе рядом.
    /// </remarks>
    private void DrawIssues()
    {
        IReadOnlyList<EntityDescriptionIssue> issues = EntityDescriptionValidator.Validate(selected);

        if (issues.Count == 0)
        {
            EditorGUILayout.HelpBox("Описание заполнено полностью.", MessageType.None);
            return;
        }

        foreach (EntityDescriptionIssue issue in issues)
            EditorGUILayout.HelpBox(issue.Message, issue.Severity);

        EditorGUILayout.Space(4f);
    }

    /// <summary>
    /// Рисует кнопки удаления.
    /// </summary>
    /// <remarks>
    /// Оба действия необратимы и спрашивают подтверждение со списком того, что исчезнет,
    /// - решение принимается там, а не здесь.
    /// </remarks>
    private void DrawDeleteButtons()
    {
        int usageCount = EntityMetadataUsageIndex.GetPrefabs(selected).Count;

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Удалить описание"))
            {
                if (EntityDescriptionDeleter.DeleteAsset(selected))
                {
                    Select(null);
                    Reload();
                }

                GUIUtility.ExitGUI();
            }

            using (new EditorGUI.DisabledScope(usageCount == 0))
            {
                if (GUILayout.Button($"Удалить с префабами ({usageCount})"))
                {
                    if (EntityDescriptionDeleter.DeleteWithPrefabs(selected))
                    {
                        Select(null);
                        Reload();
                    }

                    GUIUtility.ExitGUI();
                }
            }
        }
    }

    private void Select(ScriptableObject asset)
    {
        selected = asset;
        DestroyInspector();
    }

    private Editor GetInspector()
    {
        if (inspector != null && inspector.target == selected)
            return inspector;

        DestroyInspector();

        if (selected == null)
            return null;

        inspector = Editor.CreateEditor(selected);

        return inspector;
    }

    private void DestroyInspector()
    {
        if (inspector != null)
            DestroyImmediate(inspector);

        inspector = null;

        prefabs?.Dispose();
        prefabs = null;
        scenes = null;
    }

    private GUIStyle GetNameStyle()
    {
        return nameStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.UpperCenter,
            wordWrap = true
        };
    }

    /// <summary>
    /// Рисует спрайт, вписывая его в отведённое место.
    /// </summary>
    /// <remarks>
    /// Пропорции сохраняются: иконки предметов далеко не всегда квадратные, и растянутый
    /// в квадрат персонаж выглядит как ошибка импорта, а не как оформление.
    /// </remarks>
    private static void DrawSprite(Rect rect, Sprite sprite)
    {
        Texture2D texture = sprite.texture;

        if (texture == null)
            return;

        Rect textureRect = sprite.textureRect;
        var coordinates = new Rect(
            textureRect.x / texture.width,
            textureRect.y / texture.height,
            textureRect.width / texture.width,
            textureRect.height / texture.height);

        float spriteAspect = textureRect.width / textureRect.height;
        float rectAspect = rect.width / rect.height;

        if (spriteAspect > rectAspect)
        {
            float height = rect.width / spriteAspect;
            rect.y += (rect.height - height) * 0.5f;
            rect.height = height;
        }
        else
        {
            float width = rect.height * spriteAspect;
            rect.x += (rect.width - width) * 0.5f;
            rect.width = width;
        }

        GUI.DrawTextureWithTexCoords(rect, texture, coordinates, true);
    }

    private static void DrawBorder(Rect rect, Color color, float thickness)
    {
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }
}
