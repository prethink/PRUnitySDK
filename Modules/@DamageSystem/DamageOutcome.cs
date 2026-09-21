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
    /// Кто нанёс урон. У урона от окружения отсутствует.
    /// </summary>
    public IEntity Attacker { get; }

    /// <summary>
    /// По кому пришёлся удар.
    /// </summary>
    public IEntity Victim { get; }

    /// <summary>
    /// Чем нанесён урон. Отсутствует у урона без оружия.
    /// </summary>
    public IWeapon Weapon { get; }

    /// <summary>
    /// Провайдер урона в том виде, в каком он дошёл до здоровья, уже с декораторами.
    /// </summary>
    /// <remarks>
    /// У отказов <see cref="DamageData"/> пустой, и провайдер остаётся единственным
    /// источником сведений о том, чем именно пытались ударить.
    /// </remarks>
    public IDamageProvider DamageProvider { get; }

    /// <summary>
    /// Итог попытки, которая не дошла до здоровья.
    /// </summary>
    /// <param name="attacker">Кто наносил урон.</param>
    /// <param name="victim">По кому пытались попасть, если цель известна.</param>
    /// <param name="weapon">Чем наносился урон.</param>
    /// <param name="damageProvider">Провайдер урона, который не был обработан.</param>
    public static DamageOutcome NotHandled(
        IEntity attacker = null,
        IEntity victim = null,
        IWeapon weapon = null,
        IDamageProvider damageProvider = null)
    {
        return new DamageOutcome(
            DamageResult.NotHandled, attacker, victim, weapon, damageProvider, null, 0f, 0f);
    }

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
    /// Где стоял атакующий в момент удара.
    /// </summary>
    /// <remarks>
    /// Снимок, а не позиция живого объекта: к моменту, когда итог дочитают, атакующий
    /// бывает уже уничтожен или убран в пул. Пусто у урона от окружения - у игрового
    /// события своего места на сцене нет.
    /// </remarks>
    public Vector3? AttackerPosition { get; }

    /// <summary>
    /// Где стояла жертва в момент удара.
    /// </summary>
    /// <remarks>
    /// После смертельного удара сущность прячут или отдают в пул, и спрашивать место
    /// у неё самой уже поздно. Отсюда же берут точку для посмертных эффектов и разбора
    /// того, где игроков убивают.
    /// </remarks>
    public Vector3? VictimPosition { get; }

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
    /// <param name="attacker">Кто нанёс урон.</param>
    /// <param name="victim">По кому пришёлся удар.</param>
    /// <param name="weapon">Чем нанесён урон.</param>
    /// <param name="damageProvider">Провайдер урона, дошедший до здоровья.</param>
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
        IEntity attacker,
        IEntity victim,
        IWeapon weapon,
        IDamageProvider damageProvider,
        DamageData damageData,
        float healthBefore,
        float healthAfter,
        Vector3? hitPoint = null,
        Collider hitCollider = null,
        float? appliedDamage = null)
    {
        Result = result;
        Attacker = attacker;
        Victim = victim;
        Weapon = weapon;
        DamageProvider = damageProvider;
        AttackerPosition = GetPosition(attacker);
        VictimPosition = GetPosition(victim);
        this.damageData = damageData?.Clone();
        HealthBefore = healthBefore;
        HealthAfter = healthAfter;
        HitPoint = hitPoint;
        HitCollider = hitCollider;
        AppliedDamage = appliedDamage ?? HealthLost;
    }

    /// <summary>
    /// Место сущности на сцене, если оно у неё есть.
    /// </summary>
    /// <remarks>
    /// Только <see cref="EntityBase"/>: у игрового события объект создаётся фабрикой
    /// по первому обращению, и чтение позиции завело бы пустой объект ради числа,
    /// которого у события всё равно нет.
    /// </remarks>
    private static Vector3? GetPosition(IEntity entity)
    {
        return entity is EntityBase entityBase && entityBase != null ? entityBase.Position : null;
    }
}
