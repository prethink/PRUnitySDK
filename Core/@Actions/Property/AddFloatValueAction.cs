using UnityEngine;

/// <summary>
/// Прибавляет значение к float-свойству в ProjectProperties.
/// </summary>
[CreateAssetMenu(fileName = "Add float action", menuName = "PRUnitySDK/Actions/Properties/Add float")]
public class AddFloatValueAction : ActionBase
{
    [SerializeField] protected string propertyName;
    [SerializeField] protected float count;

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
        PRUnitySDK.Managers.ProjectProperties.AddFloat(propertyName, count, save: false);
        return ActionResult.Success;
    }
}
