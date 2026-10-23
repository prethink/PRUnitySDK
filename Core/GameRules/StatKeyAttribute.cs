using UnityEngine;

/// <summary>
/// Строковое поле с ключом характеристики: в инспекторе — выпадающий список характеристик сущностей.
/// </summary>
/// <remarks>
/// Варианты собираются из наборов, на которых построены ассеты характеристик (<c>EntityStatsBase&lt;T&gt;</c>).
/// Неизвестное сохранённое значение показывается как <c>Missing</c> и не стирается.
/// </remarks>
public class StatKeyAttribute : PropertyAttribute
{
}
