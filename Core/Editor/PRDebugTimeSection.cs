using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Раздел «Скорость времени» вкладки Overview: ускорение для проверки idle-механик (<see cref="PRDebugTime"/>).
/// </summary>
public class PRDebugTimeSection : IPRDebugOverviewSection
{
    private static readonly float[] Presets = { 1f, 2f, 5f, 10f, 20f };

    public string Title => "Скорость времени";

    public int Order => -10;

    public void Draw(PRDebugTabContext context)
    {
        if (!context.IsPlaying)
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
            EditorGUILayout.LabelField("Часы таймеров впереди на", FormatOffset(offset));
            using (new EditorGUI.DisabledScope(offset == TimeSpan.Zero))
            {
                if (GUILayout.Button("Сбросить", EditorStyles.miniButton, GUILayout.Width(80f)))
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

    private static string FormatOffset(TimeSpan offset) =>
        offset.TotalDays >= 1 ? $"{(int)offset.TotalDays} д {offset.Hours:00}:{offset.Minutes:00}:{offset.Seconds:00}"
            : $"{offset.Hours:00}:{offset.Minutes:00}:{offset.Seconds:00}";
}
