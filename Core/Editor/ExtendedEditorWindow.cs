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

    protected void Tabs(params (string name, Action draw)[] tabs)
    {
        // header
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        string[] names = tabs.Select(t => t.name).ToArray();

        tabIndex = GUILayout.Toolbar(
            tabIndex,
            names,
            EditorStyles.toolbarButton
        );

        GUILayout.FlexibleSpace();

        EditorGUILayout.EndHorizontal();

        // content
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
