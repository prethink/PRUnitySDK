using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Раздел «Скорость времени» вкладки Time окна отладки: ускорение для проверки idle-механик (<see cref="PRDebugTime"/>).
/// </summary>
public static class PRDebugTimeSection
{
    private static readonly float[] Presets = { 1f, 2f, 5f, 10f, 20f };

    /// <summary>
    /// Рисует раздел.
    /// </summary>
    /// <param name="isPlaying">Идёт ли игра.</param>
    public static void Draw(bool isPlaying)
    {
        if (!isPlaying)
        {
            EditorGUILayout.HelpBox("Ускорение работает в Play Mode и сбрасывается при каждом запуске.", MessageType.Info);
            return;
        }

        EditorGUILayout.HelpBox("Вся игра идёт быстрее — движение, боты, доход, тренировка, ресурсы, созревание и " +
                                "перезарядки, как если бы время текло в N раз быстрее. Музыка и звуки — как обычно.", MessageType.None);

        float speed = PRDebugTime.Speed;
        float changed = EditorGUILayout.Slider("Во сколько раз быстрее", speed, 1f, PRDebugTime.MaxSpeed);
        if (!Mathf.Approximately(changed, speed))
            PRDebugTime.Speed = Mathf.Round(changed * 10f) / 10f;

        using (new EditorGUILayout.HorizontalScope())
        {
            for (int i = 0; i < Presets.Length; i++)
            {
                GUIStyle style = i == 0 ? EditorStyles.miniButtonLeft : i == Presets.Length - 1 ? EditorStyles.miniButtonRight : EditorStyles.miniButtonMid;
                string label = "x" + Presets[i].ToString("0", CultureInfo.InvariantCulture);
                bool active = Mathf.Approximately(PRDebugTime.Speed, Presets[i]);
                if (GUILayout.Toggle(active, label, style) && !active)
                    PRDebugTime.Speed = Presets[i];
            }
        }

        TimeSpan offset = PRDebugTime.Offset;
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Часы таймеров сдвинуты на", FormatOffset(offset));
            using (new EditorGUI.DisabledScope(offset == TimeSpan.Zero))
            {
                if (GUILayout.Button("Сбросить", EditorStyles.miniButton, GUILayout.Width(80f))
                    && (offset < TimeSpan.Zero || ConfirmResetClock(offset)))
                    PRDebugTime.ResetClock();
            }
        }
    }

    /// <summary>
    /// Вышли из Play — масштаб времени Unity снова обычный, иначе ускорение осталось бы в редакторе.
    /// </summary>
    [InitializeOnLoadMethod]
    private static void ResetOnEditMode()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) Time.timeScale = 1f;
        };
    }

    /// <summary>
    /// Сброс часов возвращает их назад, а таймеры, поставленные при ускорении, остаются ждать по ушедшим вперёд.
    /// </summary>
    /// <remarks>
    /// Спрашивается, только когда часы впереди: отставшие (после подвисаний) сброс двигает вперёд, таймерам это не вредит.
    /// </remarks>
    private static bool ConfirmResetClock(TimeSpan offset) =>
        EditorUtility.DisplayDialog("Сбросить часы таймеров?",
            $"Часы вернутся назад на {FormatOffset(offset).TrimStart('+')}. Таймеры, поставленные при ускорении (созревание, " +
            "перезарядки, подарки), затянутся на это же время. Сохранение при этом не меняется.\n\n" +
            "Чтобы просто замедлить игру, выберите x1 — часы при этом не сбрасываются.",
            "Сбросить", "Отмена");

    /// <summary>
    /// Сдвиг со знаком: «+» — часы впереди настоящих, «−» — отстают.
    /// </summary>
    private static string FormatOffset(TimeSpan offset)
    {
        string sign = offset < TimeSpan.Zero ? "−" : "+";
        TimeSpan span = offset.Duration();

        return span.TotalDays >= 1 ? $"{sign}{(int)span.TotalDays} д {span.Hours:00}:{span.Minutes:00}:{span.Seconds:00}"
            : $"{sign}{span.Hours:00}:{span.Minutes:00}:{span.Seconds:00}";
    }
}
