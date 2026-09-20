using UnityEngine;

/// <summary>
/// Подробный результат одной попытки нанести урон.
/// </summary>
public sealed class DamageOutcome
{
    private readonly DamageData damageData;

    /// <summary>
    /// Здоровье действительно уменьшилось.
    /// </summary>
    private readonly bool healthReduced;

    /// <summary>
    /// Результат обработки попытки нанесения урона.
    /// </summary>
    public DamageResult Result { get; }

    /// <summary>
    /// Итог попытки, которая не дошла до здоровья.
    /// </summary>
    /// <remarks>
    /// Чтобы <c>TakeDamage</c> никогда не возвращал <c>null</c>: тому, кто не нашёл
    /// здоровья, ответить нечем, а проверка на <c>null</c> у каждого вызова стоила бы
    /// дороже общего пустого итога.
    /// </remarks>
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
    /// Фактически снятое здоровье с учётом ограничения диапазоном здоровья.
    /// </summary>
    public float AppliedDamage => HealthBefore - HealthAfter;

    /// <summary>
    /// Количество урона, поглощённое сопротивлениями и защитными обработчиками.
    /// </summary>
    public float AbsorbedDamage => damageData?.AbsorbedDamage ?? 0f;

    /// <summary>
    /// Здоровье цели действительно уменьшилось.
    /// </summary>
    /// <remarks>
    /// Второй вопрос после <c>Result.IsApplied()</c> и отличается от него ровно одним
    /// случаем: у сущности с бесконечным здоровьем (<c>HealthComponent.IsImmortal</c>)
    /// удар засчитывается целиком — результат <see cref="DamageResult.Damaged"/>,
    /// <see cref="AppliedDamage"/> настоящий, эффекты срабатывают, — а тратить нечего.
    /// <para>
    /// По самому <see cref="DamageResult"/> это не узнать: груша и обычная цель
    /// возвращают одно и то же значение. Поэтому ответ живёт здесь, а не расширением
    /// перечисления.
    /// </para>
    /// <para>
    /// Спрашивают об этом те, кому важна убыль, а не факт удара: прогресс «снесено
    /// здоровья», достижения, статистика боя. Эффектам и опыту хватает
    /// <c>IsApplied()</c> — иначе груша перестала бы отзываться.
    /// </para>
    /// </remarks>
    public bool WasHealthReduced => healthReduced && AppliedDamage > 0f;

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
    /// <param name="healthReduced">
    /// Здоровье действительно уменьшилось. Ложно у сущности с бесконечным здоровьем:
    /// урон ей засчитывается целиком, а тратить нечего.
    /// </param>
    public DamageOutcome(
        DamageResult result,
        DamageData damageData,
        float healthBefore,
        float healthAfter,
        Vector3? hitPoint = null,
        Collider hitCollider = null,
        bool healthReduced = true)
    {
        Result = result;
        this.damageData = damageData?.Clone();
        HealthBefore = healthBefore;
        HealthAfter = healthAfter;
        HitPoint = hitPoint;
        HitCollider = hitCollider;
        this.healthReduced = healthReduced;
    }
}
