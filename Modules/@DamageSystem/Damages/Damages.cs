/// <summary>
/// Создаёт урон нужного вида одной строкой.
/// </summary>
/// <remarks>
/// Наследников на каждый вид заводить нечем и незачем. По классу урона в системе никто
/// не ветвится — сопротивления, зональные множители и события читают флаги
/// <see cref="DamageType"/>, — а сам <see cref="DamageType"/> комбинируется: один удар
/// бывает разом огненным, взрывным и по площади. Класс на вид такое не выражает,
/// а именованный метод выражает: составной урон собирается через <see cref="Of"/>.
/// <para>
/// Выигрыш против голого конструктора в том, что вид нельзя забыть и нельзя перепутать
/// местами с силой отдачи: <c>new CommonDamage(10f, 0f, DamageType.Fire)</c> против
/// <c>Damages.Fire(10f)</c>.
/// </para>
/// </remarks>
public static class Damages
{
    /// <summary>
    /// Урон заданного вида; виды комбинируются флагами.
    /// </summary>
    /// <remarks>
    /// Общий случай, к которому сводятся остальные методы. Нужен там, где видов сразу
    /// несколько: <c>Damages.Of(30f, DamageType.Fire | DamageType.Explosion, 5f)</c>.
    /// <para>
    /// Через него же ставятся пометки, которые видом урона не являются, —
    /// <see cref="DamageType.AreaOfEffect"/>, <see cref="DamageType.TimeBased"/>.
    /// <see cref="DamageType.Critical"/> сюда обычно не пишут: его выставляет зональный
    /// декоратор при попадании в критическую зону.
    /// </para>
    /// </remarks>
    /// <param name="amount">Величина урона.</param>
    /// <param name="type">Вид урона; флаги складываются через <c>|</c>.</param>
    /// <param name="knockBack">Сила отбрасывания.</param>
    public static IDamageProvider Of(float amount, DamageType type = DamageType.Generic, float knockBack = 0f)
    {
        return new CommonDamage(amount, knockBack, type);
    }

    /// <summary>
    /// Урон из готовых данных.
    /// </summary>
    /// <remarks>
    /// Так пересобирают урон те, кто его правит по дороге: сопротивления считают новое
    /// число и заворачивают данные обратно в провайдер.
    /// </remarks>
    /// <param name="data">Данные урона.</param>
    public static IDamageProvider From(DamageData data)
    {
        return new CommonDamage(data);
    }

    /// <summary>
    /// Урон без указанного вида.
    /// </summary>
    /// <remarks>
    /// <see cref="DamageType.Generic"/> совпадает только с правилами сопротивления,
    /// где тоже указан <c>Generic</c>: «никакой» вид не означает «любой».
    /// </remarks>
    public static IDamageProvider Common(float amount, float knockBack = 0f)
    {
        return Of(amount, DamageType.Generic, knockBack);
    }

    /// <summary>
    /// Урон от падения с высоты.
    /// </summary>
    public static IDamageProvider Fall(float amount, float knockBack = 0f)
    {
        return Of(amount, DamageType.Fall, knockBack);
    }

    /// <summary>
    /// Огнестрельное попадание.
    /// </summary>
    public static IDamageProvider Bullet(float amount, float knockBack = 0f)
    {
        return Of(amount, DamageType.Bullet, knockBack);
    }

    /// <summary>
    /// Огонь и горение.
    /// </summary>
    public static IDamageProvider Fire(float amount, float knockBack = 0f)
    {
        return Of(amount, DamageType.Fire, knockBack);
    }

    /// <summary>
    /// Холод и обморожение.
    /// </summary>
    public static IDamageProvider Ice(float amount, float knockBack = 0f)
    {
        return Of(amount, DamageType.Ice, knockBack);
    }

    /// <summary>
    /// Электричество.
    /// </summary>
    public static IDamageProvider Electric(float amount, float knockBack = 0f)
    {
        return Of(amount, DamageType.Electric, knockBack);
    }

    /// <summary>
    /// Яд.
    /// </summary>
    public static IDamageProvider Poison(float amount, float knockBack = 0f)
    {
        return Of(amount, DamageType.Poison, knockBack);
    }

    /// <summary>
    /// Радиация.
    /// </summary>
    public static IDamageProvider Radiation(float amount, float knockBack = 0f)
    {
        return Of(amount, DamageType.Radiation, knockBack);
    }

    /// <summary>
    /// Взрыв.
    /// </summary>
    /// <remarks>
    /// Пометку <see cref="DamageType.AreaOfEffect"/> сюда не подмешиваем: взрыв бывает
    /// и прямым попаданием, а зона поражения — отдельное свойство удара.
    /// Нужна обе — <c>Damages.Of(amount, DamageType.Explosion | DamageType.AreaOfEffect)</c>.
    /// </remarks>
    public static IDamageProvider Explosion(float amount, float knockBack = 0f)
    {
        return Of(amount, DamageType.Explosion, knockBack);
    }

    /// <summary>
    /// Кислота.
    /// </summary>
    public static IDamageProvider Acid(float amount, float knockBack = 0f)
    {
        return Of(amount, DamageType.Acid, knockBack);
    }

    /// <summary>
    /// Урон по рассудку, не связанный с физическим воздействием.
    /// </summary>
    public static IDamageProvider Mental(float amount, float knockBack = 0f)
    {
        return Of(amount, DamageType.Mental, knockBack);
    }
}
