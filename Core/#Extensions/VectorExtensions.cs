using UnityEngine;

public static class VectorExtensions
{
    /// <summary>
    /// ¬озвращает случайное число в диапазоне x (min) и y (max).
    /// </summary>
    public static float GetRandom(this Vector2 range)
    {
        return Random.Range(range.x, range.y);
    }

    /// <summary>
    /// ¬озвращает случайное число в диапазоне x (min) и y (max).
    /// </summary>
    public static long GetRandomLong(this Vector2 range)
    {
        return (long)Random.Range(range.x, range.y);
    }

    public static Vector3 Clamp(this Vector3 value, Vector3 min, Vector3 max)
    {
        return new Vector3(
            Mathf.Clamp(value.x, min.x, max.x),
            Mathf.Clamp(value.y, min.y, max.y),
            Mathf.Clamp(value.z, min.z, max.z)
        );
    }

    public static Vector3 Clamp(this Vector3 value, float min, float max)
    {
        return new Vector3(
            Mathf.Clamp(value.x, min, max),
            Mathf.Clamp(value.y, min, max),
            Mathf.Clamp(value.z, min, max)
        );
    }
}
