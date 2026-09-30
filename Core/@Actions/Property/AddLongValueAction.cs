using UnityEngine;

/// <summary>
/// Прибавляет значение к long-свойству в ProjectProperties.
/// </summary>
[CreateAssetMenu(fileName = "Add long action", menuName = "PRUnitySDK/Actions/Properties/Add long")]
public class AddLongValueAction : ActionBase
{
    [SerializeField] protected string propertyName;
    [SerializeField] protected long count;

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
        PRUnitySDK.Managers.ProjectProperties.AddLong(propertyName, count, save: false);
        return ActionResult.Success;
    }
}
