using UnityEngine;

/// <summary>
/// Устанавливает string-свойство в ProjectProperties.
/// </summary>
[CreateAssetMenu(fileName = "Add string action", menuName = "PRUnitySDK/Actions/Properties/Add string")]
public class AddStringValueAction : ActionBase
{
    [SerializeField] protected string propertyName;
    [SerializeField] protected string value;

    /// <inheritdoc />
    public override ActionResult CanExecute()
    {
        ActionResult availability = base.CanExecute();
        if (availability.IsFailed)
            return availability;
        return !string.IsNullOrWhiteSpace(propertyName)
            ? ActionResult.Success : ActionResult.Fail(ActionLabels.MissingPropertyName);
    }

    /// <inheritdoc />
    protected override ActionResult Action()
    {
        PRUnitySDK.Managers.ProjectProperties.SetString(propertyName, value);
        return ActionResult.Success;
    }
}
