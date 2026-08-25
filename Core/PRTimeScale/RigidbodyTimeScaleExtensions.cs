using UnityEngine;

/// <summary>
/// Приложение сил и скоростей к телу с учётом его личного масштаба времени.
/// <para>
/// Если на объекте нет <see cref="RigidbodyTimeScaleDriverBase"/>, методы работают как
/// обычные вызовы Rigidbody - тело живёт в глобальном темпе, который уже учтён
/// шагом симуляции.
/// </para>
/// </summary>
public static class RigidbodyTimeScaleExtensions
{
    /// <summary>
    /// Масштаб времени тела относительно остальной сцены. Единица, если драйвера нет.
    /// </summary>
    public static float GetRelativeTimeScale(this Rigidbody body)
    {
        if (body == null)
            return 1f;

        var driver = body.GetComponent<RigidbodyTimeScaleDriverBase>();

        return driver != null ? driver.RelativeTimeScale : 1f;
    }

    /// <summary>
    /// Приложить силу с учётом масштаба времени тела.
    /// </summary>
    public static void AddScaledForce(this Rigidbody body, Vector3 force, ForceMode mode)
    {
        if (body == null)
            return;

        var driver = body.GetComponent<RigidbodyTimeScaleDriverBase>();

        if (driver != null)
        {
            driver.AddScaledForce(force, mode);
            return;
        }

        body.AddForce(force, mode);
    }

    /// <summary>
    /// Задать скорость с учётом масштаба времени тела.
    /// <para>
    /// Скорость масштабируется линейно: замедленный вдвое персонаж должен
    /// отпрыгнуть от лестницы вдвое медленнее, пройдя то же расстояние.
    /// </para>
    /// </summary>
    public static void SetScaledVelocity(this Rigidbody body, Vector3 velocity)
    {
        if (body == null)
            return;

        body.velocity = velocity * body.GetRelativeTimeScale();
    }
}
