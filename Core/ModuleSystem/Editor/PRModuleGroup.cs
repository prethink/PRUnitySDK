using UnityEngine;

/// <summary>
/// Подгруппа модуля в окне модулей.
/// </summary>
/// <remarks>
/// Сериализуется в манифестах: порядок значений менять нельзя.
/// <see cref="Integrations"/> в окне идёт отдельной вкладкой.
/// </remarks>
public enum PRModuleGroup
{
    [InspectorName("Модули")] Modules,
    [InspectorName("Окна")] Windows,
    [InspectorName("Компоненты")] Components,
    [InspectorName("Сущности")] Entities,
    [InspectorName("Прочее")] Other,
    [InspectorName("Интеграции")] Integrations
}
