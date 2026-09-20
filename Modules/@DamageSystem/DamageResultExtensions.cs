/// <summary>
/// Чтение результата попытки нанести урон.
/// </summary>
public static class DamageResultExtensions
{
    /// <summary>
    /// Урон прошёл: цель его приняла, жива она после этого или нет.
    /// </summary>
    /// <remarks>
    /// Почти всем, кто смотрит на результат, нужен именно этот ответ, а не конкретное
    /// значение: эффекты попадания, начисление опыта, счётчики ударов одинаково
    /// относятся к <see cref="DamageResult.Damaged"/> и <see cref="DamageResult.Killed"/>.
    /// Смертельным удар делает вторую половину дела, но первую он всё равно сделал.
    /// <para>
    /// Пару «или убил, или ранил» раньше писали в каждом таком месте руками, и забытая
    /// половина означала бы эффект, который не срабатывает на добивающем ударе.
    /// </para>
    /// <para>
    /// Отвечает на вопрос «удар засчитан», а не «здоровье уменьшилось»: у сущности
    /// с бесконечным здоровьем (<c>HealthComponent.IsImmortal</c>) урон проходит целиком,
    /// а здоровье остаётся прежним. Кому важно именно списанное, смотрит
    /// <see cref="DamageOutcome.AppliedDamage"/>.
    /// </para>
    /// </remarks>
    /// <param name="result">Результат попытки.</param>
    /// <returns><c>true</c> для <c>Damaged</c> и <c>Killed</c>.</returns>
    public static bool IsApplied(this DamageResult result)
    {
        return result == DamageResult.Damaged || result == DamageResult.Killed;
    }

    /// <summary>
    /// Урон прошёл: то же, что у результата, но спрашивается прямо у итога.
    /// </summary>
    /// <remarks>
    /// <c>TakeDamage</c> возвращает <see cref="DamageOutcome"/>, и без этой перегрузки
    /// у каждого вызова появлялось бы лишнее звено: <c>outcome.Result.IsApplied()</c>.
    /// </remarks>
    public static bool IsApplied(this DamageOutcome outcome)
    {
        return outcome != null && outcome.Result.IsApplied();
    }
}
