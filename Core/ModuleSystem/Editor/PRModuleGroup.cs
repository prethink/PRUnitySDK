using UnityEngine;

/// <summary>
/// Подгруппа модуля в окне модулей.
/// </summary>
/// <remarks>
/// Сериализуется в манифестах: порядок значений менять нельзя.
/// </remarks>
public enum PRModuleGroup
{
    [InspectorName("Модули")] Modules,
    [InspectorName("Окна")] Windows,
    [InspectorName("Компоненты")] Components,
    [InspectorName("Сущности")] Entities,
    [InspectorName("Прочее")] Other
}
