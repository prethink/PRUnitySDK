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
    public override bool CanExecute()
    {
        return base.CanExecute()
            && !string.IsNullOrWhiteSpace(propertyName)
            && TryGetValue(out _);
    }

    /// <inheritdoc />
    protected override void Action()
    {
        TryGetValue(out var dateTime);
        PRUnitySDK.Managers.ProjectProperties.SetDateTime(propertyName, dateTime);
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
