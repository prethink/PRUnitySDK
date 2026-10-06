using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Окно настройки экрана загрузки: поля слева, превью справа.
/// </summary>
/// <remarks>
/// Правит <see cref="LoadingScreenSettings"/> в настройках проекта. Превью — не схема, а тот же
/// экран, что увидит игрок: он собирается тем же методом (<see cref="BootstrapLoadingScreen.Build"/>)
/// во временной сцене и снимается камерой. Открытую сцену это не трогает.
/// </remarks>
public sealed class PRSDKLoadingScreenWindow : EditorWindow
{
    private const int PreviewWidth = 960;
    private const int PreviewHeight = 540;
    private const float SettingsWidth = 430f;

    private static readonly LangType[] Languages = { LangType.Russian, LangType.English, LangType.Turkey };
    private static readonly string[] LanguageNames = { "Русский", "English", "Türkçe" };

    private Vector2 scroll;
    private int previewLanguage;
    private float previewProgress = 0.6f;
    private Texture2D preview;
    private bool previewDirty = true;
    private GUIStyle hintStyle;

    [MenuItem(LoadingScreenSettings.MenuPath, false, 16)]
    private static void Open()
    {
        var window = GetWindow<PRSDKLoadingScreenWindow>();
        window.titleContent = new GUIContent("Экран загрузки");
        window.minSize = new Vector2(900f, 420f);
        window.Show();
    }

    private void OnEnable()
    {
        Undo.undoRedoPerformed += MarkPreviewDirty;
        previewDirty = true;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= MarkPreviewDirty;

        if (preview != null)
            DestroyImmediate(preview);

        preview = null;
    }

    private void MarkPreviewDirty()
    {
        previewDirty = true;
        Repaint();
    }

    /// <remarks>
    /// Превью снимается здесь, а не в <c>OnGUI</c>: рендер камеры посреди отрисовки окна сбивает
    /// состояние графики редактора.
    /// </remarks>
    private void Update()
    {
        if (!previewDirty || EditorApplication.isCompiling)
            return;

        PRSDKSettings settings = PRSDKSettings.Instance;

        if (settings == null || settings.LoadingScreen == null)
            return;

        previewDirty = false;
        RenderPreview(settings.LoadingScreen);
        Repaint();
    }

    private void OnGUI()
    {
        PRSDKSettings settings = PRSDKSettings.Instance;

        if (settings == null || settings.LoadingScreen == null)
        {
            EditorGUILayout.HelpBox("Настройки проекта не найдены. Выберите проект в PRUnitySDK → Windows → Project.", MessageType.Warning);
            return;
        }

        hintStyle ??= new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };

        LoadingScreenSettings screen = settings.LoadingScreen;

        DrawToolbar(settings);

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(SettingsWidth)))
            {
                scroll = EditorGUILayout.BeginScrollView(scroll);
                DrawSettings(settings, screen);
                EditorGUILayout.EndScrollView();
            }

            using (new EditorGUILayout.VerticalScope())
                DrawPreview(screen);
        }
    }

    private void DrawToolbar(PRSDKSettings settings)
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button(new GUIContent("Сохранить", "Записать настройки проекта на диск."),
                    EditorStyles.toolbarButton, GUILayout.Width(80f)))
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssetIfDirty(settings);
            }

            GUILayout.FlexibleSpace();

            GUILayout.Label("Превью:", EditorStyles.miniLabel);

            int language = GUILayout.Toolbar(previewLanguage, LanguageNames, EditorStyles.toolbarButton, GUILayout.Width(210f));
            if (language != previewLanguage)
            {
                previewLanguage = language;
                previewDirty = true;
            }

            GUILayout.Label("прогресс", EditorStyles.miniLabel);

            float progress = GUILayout.HorizontalSlider(previewProgress, 0f, 1f, GUILayout.Width(110f));
            if (!Mathf.Approximately(progress, previewProgress))
            {
                previewProgress = progress;
                previewDirty = true;
            }
        }
    }

    #region Поля

    private void DrawSettings(PRSDKSettings settings, LoadingScreenSettings screen)
    {
        Header("Экран загрузки");
        ToggleField(settings, "Показывать экран загрузки",
            "Выключено — запуск идёт по-старому: SDK собирается одним вызовом, сцена меняется под затемнением.",
            screen.Enabled, value => screen.Enabled = value);

        if (!screen.Enabled)
            EditorGUILayout.HelpBox("Экран выключен: игрок его не увидит. Превью показывает, каким он был бы.", MessageType.Info);

        Header("Фон");
        ColorField(settings, "Цвет", screen.BackgroundColor, value => screen.BackgroundColor = value);
        ObjectField(settings, "Картинка", "Пусто — один цвет.", screen.BackgroundImage, value => screen.BackgroundImage = value);

        using (new EditorGUI.DisabledScope(screen.BackgroundImage == null))
        {
            EnumField(settings, "Вписывание",
                "Stretch — растянуть без пропорций. Cover — закрыть экран целиком, лишнее уйдёт за края. Fit — вписать целиком, по краям цвет фона.",
                screen.BackgroundImageMode, value => screen.BackgroundImageMode = value);
            ColorField(settings, "Оттенок картинки", screen.BackgroundImageColor, value => screen.BackgroundImageColor = value);
        }

        Header("Текст");
        ObjectField(settings, "Шрифт", "Пусто — шрифт TextMeshPro по умолчанию.", screen.Font, value => screen.Font = value);

        EditorGUILayout.Space(4f);
        ToggleField(settings, "Заголовок", "Показывать заголовок.", screen.ShowTitle, value => screen.ShowTitle = value);

        using (new EditorGUI.DisabledScope(!screen.ShowTitle))
        {
            DrawTexts(settings, screen.GetTitle, screen.SetTitle, LoadingScreenLabels.Title, multiline: false);
            ColorField(settings, "Цвет", screen.TitleColor, value => screen.TitleColor = value);
            FloatField(settings, "Размер", screen.TitleSize, value => screen.TitleSize = value);
        }

        EditorGUILayout.Space(4f);
        ToggleField(settings, "Пояснение", "Показывать пояснение под заголовком.", screen.ShowDescription, value => screen.ShowDescription = value);

        using (new EditorGUI.DisabledScope(!screen.ShowDescription))
        {
            DrawTexts(settings, screen.GetDescription, screen.SetDescription, LoadingScreenLabels.Description, multiline: true);
            ColorField(settings, "Цвет", screen.DescriptionColor, value => screen.DescriptionColor = value);
            FloatField(settings, "Размер", screen.DescriptionSize, value => screen.DescriptionSize = value);
        }

        Header("Полоса прогресса");
        ToggleField(settings, "Показывать полосу", "Показывать полосу прогресса.", screen.ShowProgressBar, value => screen.ShowProgressBar = value);

        using (new EditorGUI.DisabledScope(!screen.ShowProgressBar))
        {
            Vector2 size = EditorGUILayout.Vector2Field(
                new GUIContent("Размер", "Ширина и высота в единицах холста 1920×1080."), screen.ProgressSize);
            if (size != screen.ProgressSize)
                Change(settings, "Размер полосы", () => screen.ProgressSize = size);

            ColorField(settings, "Цвет заполнения", screen.ProgressFillColor, value => screen.ProgressFillColor = value);
            ColorField(settings, "Цвет подложки", screen.ProgressBackColor, value => screen.ProgressBackColor = value);
            ObjectField(settings, "Картинка заполнения", "Пусто — прямоугольник.", screen.ProgressFillSprite, value => screen.ProgressFillSprite = value);
            ObjectField(settings, "Картинка подложки",
                "Пусто — прямоугольник. Картинка с границами нарезки растягивается без искажения углов.",
                screen.ProgressBackSprite, value => screen.ProgressBackSprite = value);
            FloatField(settings, "Плотность картинок", screen.ProgressPixelsPerUnit, value => screen.ProgressPixelsPerUnit = value,
                "Больше — углы нарезанной картинки меньше.");
        }

        Header("Значок");
        ToggleField(settings, "Показывать значок", "Показывать вращающийся значок.", screen.ShowSpinner, value => screen.ShowSpinner = value);

        using (new EditorGUI.DisabledScope(!screen.ShowSpinner))
        {
            ObjectField(settings, "Картинка", "Пусто — значка нет.", screen.SpinnerIcon, value => screen.SpinnerIcon = value);
            FloatField(settings, "Размер", screen.SpinnerSize, value => screen.SpinnerSize = value, "Сторона в единицах холста 1920×1080.");
            FloatField(settings, "Скорость", screen.SpinnerSpeed, value => screen.SpinnerSpeed = value,
                "Градусов в секунду. Ноль — значок стоит. В превью значок неподвижен.");

            if (screen.ShowSpinner && screen.SpinnerIcon == null)
                EditorGUILayout.HelpBox("Картинка значка не задана — значка на экране не будет.", MessageType.Info);
        }

        EditorGUILayout.Space(8f);
    }

    /// <summary>
    /// Поля текста по одному на язык. Пустое поле показывает стандартный текст серым.
    /// </summary>
    private void DrawTexts(PRSDKSettings settings, Func<LangType, string> get, Action<LangType, string> set,
        ILocalizationProvider fallback, bool multiline)
    {
        for (int index = 0; index < Languages.Length; index++)
        {
            LangType language = Languages[index];
            string current = get(language);

            string value = multiline
                ? TextArea(LanguageNames[index], current)
                : EditorGUILayout.DelayedTextField(LanguageNames[index], current);

            if (value != current)
                Change(settings, "Текст экрана загрузки", () => set(language, value));

            if (string.IsNullOrWhiteSpace(get(language))
                && fallback.LocalizationValues.TryGetValue(language, out string standard))
            {
                using (new EditorGUI.IndentLevelScope())
                    EditorGUILayout.LabelField(" ", "пусто — стандартный: " + standard, hintStyle);
            }
        }
    }

    private static string TextArea(string label, string value)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PrefixLabel(label);
            return EditorGUILayout.TextArea(value, GUILayout.MinHeight(34f));
        }
    }

    private static void Header(string text)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
    }

    private void ToggleField(PRSDKSettings settings, string label, string tooltip, bool current, Action<bool> set)
    {
        bool value = EditorGUILayout.ToggleLeft(new GUIContent(label, tooltip), current);

        if (value != current)
            Change(settings, label, () => set(value));
    }

    private void ColorField(PRSDKSettings settings, string label, Color current, Action<Color> set)
    {
        Color value = EditorGUILayout.ColorField(label, current);

        if (value != current)
            Change(settings, label, () => set(value));
    }

    private void FloatField(PRSDKSettings settings, string label, float current, Action<float> set, string tooltip = null)
    {
        float value = EditorGUILayout.DelayedFloatField(new GUIContent(label, tooltip), current);

        if (!Mathf.Approximately(value, current))
            Change(settings, label, () => set(value));
    }

    private void EnumField<T>(PRSDKSettings settings, string label, string tooltip, T current, Action<T> set)
        where T : Enum
    {
        var value = (T)EditorGUILayout.EnumPopup(new GUIContent(label, tooltip), current);

        if (!Equals(value, current))
            Change(settings, label, () => set(value));
    }

    private void ObjectField<T>(PRSDKSettings settings, string label, string tooltip, T current, Action<T> set)
        where T : UnityEngine.Object
    {
        var value = (T)EditorGUILayout.ObjectField(new GUIContent(label, tooltip), current, typeof(T), false);

        if (value != current)
            Change(settings, label, () => set(value));
    }

    /// <summary>
    /// Меняет настройки с записью в историю отмены и просит перерисовать превью.
    /// </summary>
    private void Change(PRSDKSettings settings, string name, Action change)
    {
        Undo.RecordObject(settings, name);
        change();
        EditorUtility.SetDirty(settings);
        previewDirty = true;
        Repaint();
    }

    #endregion

    #region Превью

    private void DrawPreview(LoadingScreenSettings screen)
    {
        Rect area = GUILayoutUtility.GetRect(100f, 100f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        area = new Rect(area.x + 8f, area.y + 8f, area.width - 16f, area.height - 16f);

        if (area.width <= 0f || area.height <= 0f)
            return;

        // Кадр 16:9, вписанный в отведённое место.
        float scale = Mathf.Min(area.width / PreviewWidth, area.height / PreviewHeight);
        var frame = new Rect(
            area.x + (area.width - PreviewWidth * scale) * 0.5f, area.y,
            PreviewWidth * scale, PreviewHeight * scale);

        if (preview != null)
            GUI.DrawTexture(frame, preview, ScaleMode.StretchToFill);
        else
            EditorGUI.DrawRect(frame, new Color(0f, 0f, 0f, 0.3f));

        var note = new Rect(frame.x, frame.yMax + 4f, frame.width, 34f);
        GUI.Label(note,
            "Так экран выглядит на 16:9. Значок в превью неподвижен; в браузере его крутит страница, и он не замирает вместе с игрой.",
            hintStyle);
    }

    /// <summary>
    /// Собирает экран во временной сцене и снимает его камерой.
    /// </summary>
    private void RenderPreview(LoadingScreenSettings screen)
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        RenderTexture target = null;

        try
        {
            var cameraObject = new GameObject("Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);

            var camera = cameraObject.AddComponent<Camera>();
            camera.scene = scene;
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            target = new RenderTexture(PreviewWidth, PreviewHeight, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;

            BootstrapLoadingScreen built = BootstrapLoadingScreen.Build(screen, scene);

            // Холст поверх экрана в текстуру не попадает: в превью его рисует камера.
            var canvas = built.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 5f;

            built.SetLanguage(Languages[Mathf.Clamp(previewLanguage, 0, Languages.Length - 1)]);
            built.SetProgressImmediately(previewProgress);

            Canvas.ForceUpdateCanvases();

            foreach (TMP_Text text in built.GetComponentsInChildren<TMP_Text>(true))
                text.ForceMeshUpdate();

            Canvas.ForceUpdateCanvases();
            camera.Render();

            if (preview == null)
            {
                preview = new Texture2D(PreviewWidth, PreviewHeight, TextureFormat.RGB24, false)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            preview.ReadPixels(new Rect(0, 0, PreviewWidth, PreviewHeight), 0, 0);
            preview.Apply();
            RenderTexture.active = previous;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            if (target != null)
                target.Release();

            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    #endregion
}
