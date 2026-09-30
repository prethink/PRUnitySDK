using System;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Устанавливает DateTime-свойство в ProjectProperties из ISO-8601 строки.
/// </summary>
[CreateAssetMenu(fileName = "Add date time action", menuName = "PRUnitySDK/Actions/Properties/Add date time")]
public class AddDateTimeValueAction : ActionBase
{
    [SerializeField] protected string propertyName;

    [SerializeField, Tooltip("ISO-8601, например 2026-12-31T23:59:59Z")]
    protected string value;

    /// <inheritdoc />
    public override ActionResult CanExecute()
    {
        ActionResult availability = base.CanExecute();
        if (availability.IsFailed)
            return availability;
        if (string.IsNullOrWhiteSpace(propertyName))
            return ActionResult.Fail(ActionLabels.MissingPropertyName);
        return TryGetValue(out _)
            ? ActionResult.Success : ActionResult.Fail(ActionLabels.InvalidDateTime);
    }

    /// <inheritdoc />
    protected override ActionResult Action()
    {
        TryGetValue(out var dateTime);
        PRUnitySDK.Managers.ProjectProperties.SetDateTime(propertyName, dateTime);
        return ActionResult.Success;
    }

    private bool TryGetValue(out DateTime dateTime)
    {
        return DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out dateTime);
    }
}
