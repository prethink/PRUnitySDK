using System;
using DG.Tweening;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Рисует поле сглаживания вместе с графиком его кривой.
/// </summary>
/// <remarks>
/// Кривая считается тем же способом, каким её считает сам DOTween
/// (<see cref="DOVirtual.EasedValue(float, float, float, Ease)"/>), поэтому превью
/// не разъедется с поведением после обновления плагина.
/// </remarks>
[CustomPropertyDrawer(typeof(EasePreviewAttribute))]
public class EasePreviewDrawer : PropertyDrawer
{
    private const float Gap = 4f;

    /// <summary>
    /// Сколько точек считается по кривой.
    /// </summary>
    /// <remarks>
    /// Хватает, чтобы отскок у <c>OutElastic</c> читался как отскок, а не как угловатая
    /// пила, и при этом график пересчитывается на каждую перерисовку незаметно.
    /// </remarks>
    private const int Samples = 64;

    /// <summary>
    /// Запас над и под графиком в долях от размаха.
    /// </summary>
    /// <remarks>
    /// <c>OutBack</c> и <c>OutElastic</c> выходят за пределы отрезка, и без запаса кривая
    /// упиралась бы в край рамки ровно там, где интереснее всего.
    /// </remarks>
    private const float Padding = 0.12f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var fieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        EditorGUI.PropertyField(fieldRect, property, label);

        if (!TryResolveEase(property, out Ease ease))
            return;

        var settings = (EasePreviewAttribute)attribute;

        // По ширине поля, а не всей строки: график встаёт ровно под выпадающим списком,
        // и колонка подписей остаётся ровной.
        float indent = EditorGUIUtility.labelWidth + Gap;

        var graphRect = new Rect(
            position.x + indent,
            fieldRect.yMax + Gap,
            Mathf.Max(60f, position.width - indent),
            settings.Height);

        DrawGraph(graphRect, ease);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var settings = (EasePreviewAttribute)attribute;

        if (!TryResolveEase(property, out _))
            return EditorGUIUtility.singleLineHeight;

        return EditorGUIUtility.singleLineHeight + Gap + settings.Height;
    }

    /// <summary>
    /// Достаёт из поля сглаживание, которое можно посчитать.
    /// </summary>
    /// <remarks>
    /// По имени, а не по <c>enumValueIndex</c>: индекс совпадает со значением только пока
    /// в перечислении нет пропусков, а это свойство чужого плагина, и держать его
    /// за обещание нельзя.
    /// <para>
    /// <c>Unset</c> подменяется тем сглаживанием, которое DOTween подставит сам, — иначе
    /// превью показывало бы прямую там, где на деле будет разгон.
    /// </para>
    /// </remarks>
    private static bool TryResolveEase(SerializedProperty property, out Ease ease)
    {
        ease = Ease.Linear;

        if (property.propertyType != SerializedPropertyType.Enum)
            return false;

        string[] names = property.enumNames;
        int index = property.enumValueIndex;

        if (index < 0 || index >= names.Length || !Enum.TryParse(names[index], out ease))
            return false;

        if (ease == Ease.Unset)
            ease = DOTween.defaultEaseType;

        // Служебные значения считать нечем: у одного нет кривой вовсе, у другого она
        // задаётся в коде и в инспекторе недоступна.
        return ease != Ease.INTERNAL_Zero && ease != Ease.INTERNAL_Custom;
    }

    /// <summary>
    /// Рисует рамку, линию старта с финишем и саму кривую.
    /// </summary>
    private static void DrawGraph(Rect rect, Ease ease)
    {
        EditorGUI.DrawRect(rect, new Color(0.16f, 0.16f, 0.16f, 1f));

        if (Event.current.type != EventType.Repaint)
            return;

        if (!TrySample(ease, out float[] values))
            return;

        float min = 0f;
        float max = 1f;

        foreach (float value in values)
        {
            min = Mathf.Min(min, value);
            max = Mathf.Max(max, value);
        }

        float range = Mathf.Max(0.0001f, max - min);
        float pad = range * Padding;

        min -= pad;
        max += pad;
        range = max - min;

        DrawLevel(rect, 0f, min, range, new Color(1f, 1f, 1f, 0.12f));
        DrawLevel(rect, 1f, min, range, new Color(1f, 1f, 1f, 0.12f));

        var points = new Vector3[values.Length];

        for (var i = 0; i < values.Length; i++)
        {
            float x = rect.x + rect.width * i / (values.Length - 1f);
            float y = rect.yMax - rect.height * (values[i] - min) / range;

            points[i] = new Vector3(x, y, 0f);
        }

        Handles.color = new Color(0.45f, 0.85f, 1f);
        Handles.DrawAAPolyLine(2f, points);
        Handles.color = Color.white;
    }

    /// <summary>
    /// Считает кривую теми же формулами, что и сам DOTween.
    /// </summary>
    /// <remarks>
    /// В <c>try</c>, потому что перечисление принадлежит плагину: неизвестное значение
    /// из будущей версии уронило бы <c>OnGUI</c>, а исключение оттуда ломает отрисовку
    /// всего инспектора.
    /// </remarks>
    private static bool TrySample(Ease ease, out float[] values)
    {
        values = new float[Samples];

        try
        {
            for (var i = 0; i < Samples; i++)
                values[i] = DOVirtual.EasedValue(0f, 1f, i / (Samples - 1f), ease);
        }
        catch
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Рисует горизонтальную отметку: начало и конец движения.
    /// </summary>
    private static void DrawLevel(Rect rect, float value, float min, float range, Color color)
    {
        float y = rect.yMax - rect.height * (value - min) / range;

        EditorGUI.DrawRect(new Rect(rect.x, y, rect.width, 1f), color);
    }
}
