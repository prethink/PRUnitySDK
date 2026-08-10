using UnityEngine;

/// <summary>
/// Подробный результат одной попытки нанести урон.
/// </summary>
public sealed class DamageOutcome
{
    /// <summary>
    /// Результат обработки попытки нанесения урона.
    /// </summary>
    public DamageResult Result { get; }

    /// <summary>
    /// Снимок итоговых данных урона после всех модификаторов.
    /// </summary>
    public DamageData DamageData { get; }

    /// <summary>
    /// Здоровье жертвы перед обработкой урона.
    /// </summary>
    public float HealthBefore { get; }

    /// <summary>
    /// Здоровье жертвы после обработки урона.
    /// </summary>
    public float HealthAfter { get; }

    /// <summary>
    /// Фактически снятое здоровье с учётом ограничения диапазоном здоровья.
    /// </summary>
    public float AppliedDamage => HealthBefore - HealthAfter;

    /// <summary>
    /// Количество урона, поглощённое сопротивлениями и защитными обработчиками.
    /// </summary>
    public float AbsorbedDamage => DamageData?.AbsorbedDamage ?? 0f;

    /// <summary>
    /// Содержит ли итоговый тип урона флаг <see cref="DamageType.Critical"/>.
    /// </summary>
    public bool WasCritical => DamageData != null &&
                               (DamageData.DamageType & DamageType.Critical) != 0;

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
    public DamageOutcome(
        DamageResult result,
        DamageData damageData,
        float healthBefore,
        float healthAfter,
        Vector3? hitPoint = null,
        Collider hitCollider = null)
    {
        Result = result;
        DamageData = damageData?.Clone();
        HealthBefore = healthBefore;
        HealthAfter = healthAfter;
        HitPoint = hitPoint;
        HitCollider = hitCollider;
    }
}
