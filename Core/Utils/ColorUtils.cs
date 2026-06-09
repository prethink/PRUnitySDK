using UnityEngine;

public static class ColorUtils
{
    public static Color GetColor(float currentValue, float minValue, Color minValueColor, float maxValue, Color maxValueColor)
    {
        // дефолты (если не передали)
        if (minValueColor == default) minValueColor = Color.white;
        if (maxValueColor == default) maxValueColor = Color.red;

        // ниже минимума
        if (currentValue <= minValue)
            return minValueColor;

        // выше максимума
        if (currentValue >= maxValue)
            return maxValueColor;

        // нормализация 0..1
        float t = Mathf.InverseLerp(minValue, maxValue, currentValue);

        return Color.Lerp(minValueColor, maxValueColor, t);
    }
}