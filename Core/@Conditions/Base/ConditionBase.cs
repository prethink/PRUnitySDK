using UnityEngine;

/// <summary>
/// Основа условий-ассетов.
/// </summary>
/// <remarks>
/// Ассет удобен, когда одна и та же настройка нужна в нескольких местах: «открыто после
/// сотни кубков» лежит одним файлом, и на него ссылаются все, кому это правило подходит.
/// Правку тогда делают в одном месте.
/// <para>
/// Для условия, уникального для конкретного объекта, ассет не нужен — его описывают
/// встроенным объектом прямо в инспекторе владельца.
/// </para>
/// <para>
/// Чтобы ассет попал туда, где ждут встроенное условие, есть <see cref="AssetCondition"/>.
/// </para>
/// </remarks>
public abstract class ConditionBase : ScriptableObject, ICondition
{
    /// <inheritdoc />
    public abstract bool Evaluate(ConditionContextBase context);

    /// <inheritdoc cref="ICondition.Evaluate()" />
    /// <remarks>
    /// Повторяет метод интерфейса по умолчанию: тот виден только через
    /// <see cref="ICondition"/>, а ассет чаще держат полем своего типа.
    /// </remarks>
    public bool Evaluate()
    {
        return Evaluate(ConditionContextEmpty.Instance);
    }
}
