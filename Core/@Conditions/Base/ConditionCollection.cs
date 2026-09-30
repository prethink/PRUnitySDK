using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Условие-ассет, собранное из других условий-ассетов.
/// </summary>
/// <remarks>
/// Именованный набор правил: «открыто во втором мире» — это и уровень, и пройденный
/// туториал, и купленный ключ. Собрав их один раз, дальше ссылаются на набор, а не
/// перечисляют составляющие в каждом месте. Правка тогда делается в одном файле.
/// <para>
/// Наборы вкладываются друг в друга: набор — это тоже <see cref="ConditionBase"/>,
/// поэтому его кладут в список другого набора.
/// </para>
/// <para>
/// Для условий, которые собираются прямо в инспекторе владельца и ассетами быть
/// не должны, есть встроенные <see cref="AllCondition"/> и <see cref="AnyCondition"/>.
/// </para>
/// </remarks>
[CreateAssetMenu(fileName = "Condition collection", menuName = "PRUnitySDK/Conditions/Condition collection")]
public class ConditionCollection : ConditionBase
{
    [SerializeField]
    [Tooltip("Сколько вложенных условий должно выполниться.")]
    private ConditionMatch match = ConditionMatch.All;

    [SerializeField]
    [Tooltip("Условия набора. Набор можно вложить в набор.")]
    private List<ConditionBase> conditions = new();

    /// <summary>
    /// Сколько вложенных условий должно выполниться.
    /// </summary>
    public ConditionMatch Match => match;

    /// <summary>
    /// Условия набора.
    /// </summary>
    public IReadOnlyList<ConditionBase> Conditions => conditions;

    /// <summary>
    /// Наборы, которые сейчас в процессе проверки.
    /// </summary>
    /// <remarks>
    /// Общий на всех и переиспользуемый: проверка идёт в главном потоке и по одной
    /// за раз, а заводить набор на каждый вызов нельзя — <c>Evaluate</c> зовут на каждый
    /// удар и на каждый кадр.
    /// </remarks>
    private static readonly HashSet<ConditionCollection> Visiting = new();

    /// <inheritdoc />
    /// <remarks>
    /// Пустой набор выполнен при любом режиме: отсутствие правил — это разрешение.
    /// Пустые строки списка пропускаются по той же причине.
    /// </remarks>
    public override ConditionResult Evaluate(ConditionContextBase context)
    {
        // Набор может содержать набор, поэтому мышкой собирается кольцо: A ссылается
        // на B, B обратно на A. Без этой проверки такое кольцо уходит в бесконечную
        // рекурсию и роняет игру переполнением стека — без единого внятного слова о том,
        // что случилось.
        if (!Visiting.Add(this))
        {
            PRLog.WriteError(this, $"Набор условий [{name}] ссылается сам на себя по кругу. " +
                                   "Круг разорван, набор считается выполненным.");

            return ConditionResult.Success;
        }

        try
        {
            return EvaluateConditions(context);
        }
        finally
        {
            Visiting.Remove(this);
        }
    }

    private ConditionResult EvaluateConditions(ConditionContextBase context)
    {
        if (conditions == null)
            return ConditionResult.Success;

        ConditionResult? firstFailure = null;
        bool hasAny = false;
        foreach (ConditionBase condition in conditions)
        {
            if (condition == null)
                continue;

            hasAny = true;
            ConditionResult result = condition.Evaluate(context);
            if (match == ConditionMatch.All && result.IsFailed)
                return result;
            if (match == ConditionMatch.Any && result.IsSuccess)
                return result;

            if (result.IsFailed)
                firstFailure ??= result;
        }

        return match == ConditionMatch.All || !hasAny
            ? ConditionResult.Success
            : firstFailure ?? ConditionResult.Fail(ConditionLabels.Unavailable);
    }
}
