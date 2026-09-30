using UnityEngine;

/// <summary>
/// Устанавливает bool-свойство в ProjectProperties.
/// </summary>
[CreateAssetMenu(fileName = "Add bool action", menuName = "PRUnitySDK/Actions/Properties/Set bool")]
public class AddBoolValueAction : ActionBase
{
    [SerializeField] protected string propertyName;
    [SerializeField] protected bool value;

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
        PRUnitySDK.Managers.ProjectProperties.SetBool(propertyName, value);
        return ActionResult.Success;
    }
}
