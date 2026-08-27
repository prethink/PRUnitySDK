using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public abstract class ExtendedEditorWindow : EditorWindow
{
    private GenericMenu menuBuilder;
    protected PRSDKDatabase database;

    protected void ToolbarMenu(string name, Action build, int width = 60)
    {
        if (GUILayout.Button(name, EditorStyles.toolbarPopup, GUILayout.Width(width)))
        {
            menuBuilder = new GenericMenu();

            build?.Invoke();

            menuBuilder.ShowAsContext();
            menuBuilder = null;
        }
    }

    protected void MenuItem(string path, Action action, bool enabled = true)
    {
        if (menuBuilder == null) 
            return;

        if (enabled)
            menuBuilder.AddItem(new GUIContent(path), false, () => action?.Invoke());
        else
            menuBuilder.AddDisabledItem(new GUIContent(path));
    }

    protected void MenuSeparator(string path = "")
    {
        menuBuilder?.AddSeparator(path);
    }

    protected void CreateHorizontalToolBar(Action action)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        action?.Invoke();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    //CreateHorizontalToolBar(() =>
    //    {
    //    ToolbarMenu("File", () =>
    //    {
    //        MenuItem("New", () => Debug.Log("New"));
    //        MenuItem("Save", () => { });
    //        MenuSeparator();
    //        MenuItem("Exit", Close);
    //    });

    //    ToolbarMenu("FileX", () =>
    //    {
    //        MenuItem("New", () => Debug.Log("New"));
    //        MenuItem("Save", () => { });
    //        MenuSeparator();
    //        MenuItem("Exit", Close);
    //    });
    //});

    protected void DrawRowSpliter(int padding, Color color, int lineSize = 1)
    {
        EditorGUILayout.Space(padding);
        var rect = EditorGUILayout.GetControlRect(false, lineSize);
        EditorGUI.DrawRect(rect, color);
        EditorGUILayout.Space(padding);
    }

    private int tabIndex;

    protected int SelectedTabIndex => tabIndex;

    protected void Tabs(params (string name, Action draw)[] tabs)
    {
        Tabs(false, tabs);
    }

    protected void Tabs(bool compact, params (string name, Action draw)[] tabs)
    {
        DrawTabsHeader(compact, tabs);
        DrawSelectedTab(tabs);
    }

    protected void DrawTabsHeader(bool compact, params (string name, Action draw)[] tabs)
    {
        string[] names = tabs.Select(t => t.name).ToArray();

        if (compact)
        {
            DrawTabsPopup(names);
            return;
        }

        DrawWrappedTabs(names);
    }

    /// <summary>
    /// Рисует выбор вкладки выпадающим списком независимо от ширины окна.
    /// </summary>
    /// <remarks>
    /// Подходит окнам с большим числом вкладок: ряд кнопок при десятке разделов
    /// либо переносится на несколько строк, либо сжимается до нечитаемого размера.
    /// </remarks>
    protected void DrawTabsDropdown(params (string name, Action draw)[] tabs)
    {
        DrawTabsPopup(tabs.Select(t => t.name).ToArray());
    }

    private void DrawTabsPopup(string[] names)
    {
        if (names.Length == 0)
            return;

        // Ширина по самой длинной подписи, но не во весь экран: список стоит слева,
        // остальное место остаётся под элементы тулбара.
        float width = 0f;
        foreach (string name in names)
            width = Mathf.Max(width, EditorStyles.toolbarPopup.CalcSize(new GUIContent(name)).x);

        width = Mathf.Clamp(width + 18f, 140f, 320f);

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        tabIndex = EditorGUILayout.Popup(tabIndex, names, EditorStyles.toolbarPopup,
            GUILayout.Width(width));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Рисует вкладки, перенося их на следующую строку, когда ряд не помещается по ширине.
    /// </summary>
    /// <remarks>
    /// <see cref="GUILayout.Toolbar(int, string[], GUIStyle, GUILayoutOption[])"/> всегда
    /// укладывается в один ряд и при нехватке места обрезает подписи, поэтому ряды
    /// набираются вручную по фактической ширине каждой кнопки.
    /// </remarks>
    private void DrawWrappedTabs(string[] names)
    {
        if (names.Length == 0)
            return;

        GUIStyle style = EditorStyles.toolbarButton;

        // Небольшой запас: полосы прокрутки и отступы окна.
        float available = Mathf.Max(120f, EditorGUIUtility.currentViewWidth - 24f);

        var widths = new float[names.Length];
        for (int i = 0; i < names.Length; i++)
            widths[i] = style.CalcSize(new GUIContent(names[i])).x;

        int rowStart = 0;
        while (rowStart < names.Length)
        {
            int rowLength = 0;
            float rowWidth = 0f;

            // В ряду всегда хотя бы одна вкладка, даже если она шире окна:
            // иначе цикл не сдвинулся бы с места.
            while (rowStart + rowLength < names.Length)
            {
                float next = widths[rowStart + rowLength];
                if (rowLength > 0 && rowWidth + next > available)
                    break;

                rowWidth += next;
                rowLength++;
            }

            DrawTabsRow(names, rowStart, rowLength, rowWidth, style);
            rowStart += rowLength;
        }
    }

    /// <summary>
    /// Рисует один ряд вкладок и переносит выбор в общий индекс.
    /// </summary>
    private void DrawTabsRow(string[] names, int start, int length, float width, GUIStyle style)
    {
        var row = new string[length];
        Array.Copy(names, start, row, 0, length);

        int localIndex = tabIndex - start;
        bool selectionInRow = localIndex >= 0 && localIndex < length;

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        int clicked = GUILayout.Toolbar(
            selectionInRow ? localIndex : -1,
            row,
            style,
            GUILayout.Width(width));

        // Ряд без выделения возвращает -1 до тех пор, пока по нему не кликнули.
        if (clicked >= 0 && clicked != localIndex)
            tabIndex = start + clicked;

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    protected void DrawSelectedTab(params (string name, Action draw)[] tabs)
    {
        if (tabIndex >= 0 && tabIndex < tabs.Length)
        {
            tabs[tabIndex].draw?.Invoke();
        }
    }

    #region Layout

    protected void DrawLayoutHorizontalBox(Action internalDraw)
    {
        internalDraw?.Invoke();
    }

    public void SplitRow(float leftWidth, Action left, Action right)
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical(GUILayout.Width(leftWidth));
        left?.Invoke();
        EditorGUILayout.EndVertical();

        GUILayout.Space(6);

        EditorGUILayout.BeginVertical();
        right?.Invoke();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region Properties

    protected void DrawSprite(SerializedProperty property, int height = 48, int width = 48)
    {
        if (property == null)
            return;

        EditorGUI.BeginChangeCheck();

        var sprite = property.objectReferenceValue as Sprite;

        sprite = (Sprite)EditorGUILayout.ObjectField(
            sprite,
            typeof(Sprite),
            false,
            GUILayout.Height(height),
            GUILayout.Width(width)
        );

        if (EditorGUI.EndChangeCheck())
        {
            property.objectReferenceValue = sprite;
        }
    }

    protected void DrawColor(SerializedProperty property, bool showLable = true)
    {
        if (showLable)
            EditorGUILayout.PropertyField(property);
        else
            EditorGUILayout.PropertyField(property, GUIContent.none);
    }

    protected void DrawGuidField(SerializedProperty guidProperty)
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("Guid", GUILayout.Width(150));

        EditorGUILayout.SelectableLabel(
            guidProperty.stringValue,
            GUILayout.Height(16)
        );

        if (GUILayout.Button("Copy", GUILayout.Width(50)))
            EditorGUIUtility.systemCopyBuffer = guidProperty.stringValue;

        if (GUILayout.Button("Refresh", GUILayout.Width(90)))
            guidProperty.stringValue = Guid.NewGuid().ToString();

        EditorGUILayout.EndHorizontal();
    }

    #endregion
}
