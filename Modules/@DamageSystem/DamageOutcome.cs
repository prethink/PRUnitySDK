using UnityEngine;

/// <summary>
/// Подробный результат одной попытки нанести урон.
/// </summary>
public sealed class DamageOutcome
{
    private readonly DamageData damageData;

    /// <summary>
    /// Результат обработки попытки нанесения урона.
    /// </summary>
    public DamageResult Result { get; }

    /// <summary>
    /// Итог попытки, которая не дошла до здоровья.
    /// </summary>
    public static DamageOutcome NotHandled { get; } = new(DamageResult.NotHandled, null, 0f, 0f);

    /// <summary>
    /// Снимок итоговых данных урона после всех модификаторов.
    /// </summary>
    public DamageData DamageData => damageData?.Clone();

    /// <summary>
    /// Здоровье жертвы перед обработкой урона.
    /// </summary>
    public float HealthBefore { get; }

    /// <summary>
    /// Здоровье жертвы после обработки урона.
    /// </summary>
    public float HealthAfter { get; }

    /// <summary>
    /// Засчитанный урон: потерянные HP у обычной цели, полный урон у бессмертной.
    /// </summary>
    public float AppliedDamage { get; }

    /// <summary>
    /// Фактическая потеря HP; у бессмертной цели равна нулю.
    /// </summary>
    public float HealthLost => Mathf.Max(0f, HealthBefore - HealthAfter);

    /// <summary>
    /// Количество урона, поглощённое сопротивлениями и защитными обработчиками.
    /// </summary>
    public float AbsorbedDamage => damageData?.AbsorbedDamage ?? 0f;

    /// <summary>
    /// Попадание принято, включая смертельное, нулевой урон и удар по бессмертной цели.
    /// </summary>
    public bool WasApplied => Result.IsApplied();

    /// <summary>
    /// Здоровье цели действительно уменьшилось.
    /// </summary>
    public bool WasHealthReduced => HealthLost > 0f;

    /// <summary>
    /// Содержит ли итоговый тип урона флаг <see cref="DamageType.Critical"/>.
    /// </summary>
    public bool WasCritical => damageData != null &&
                               (damageData.DamageType & DamageType.Critical) != 0;

    /// <summary>
    /// Мировая точка попадания, если она была передана.
    /// </summary>
    public Vector3? HitPoint { get; }

    /// <summary>
    /// Коллайдер попадания, если он был передан.
    /// </summary>
    public Collider HitCollider { get; }

    /// <summary>
    /// Создаёт неизменяемое описание результата обработки урона.
    /// </summary>
    /// <param name="result">Результат обработки.</param>
    /// <param name="damageData">Итоговые данные урона; внутри сохраняется их копия.</param>
    /// <param name="healthBefore">Здоровье до обработки.</param>
    /// <param name="healthAfter">Здоровье после обработки.</param>
    /// <param name="hitPoint">Необязательная мировая точка попадания.</param>
    /// <param name="hitCollider">Необязательный коллайдер попадания.</param>
    /// <param name="appliedDamage">
    /// Засчитанный урон; если не задан, используется фактическая потеря HP.
    /// </param>
    public DamageOutcome(
        DamageResult result,
        DamageData damageData,
        float healthBefore,
        float healthAfter,
        Vector3? hitPoint = null,
        Collider hitCollider = null,
        float? appliedDamage = null)
    {
        Result = result;
        this.damageData = damageData?.Clone();
        HealthBefore = healthBefore;
        HealthAfter = healthAfter;
        HitPoint = hitPoint;
        HitCollider = hitCollider;
        AppliedDamage = appliedDamage ?? HealthLost;
    }
}
