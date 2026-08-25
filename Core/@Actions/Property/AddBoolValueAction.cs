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
    public override bool CanExecute()
    {
        return base.CanExecute() && !string.IsNullOrWhiteSpace(propertyName);
    }

    /// <inheritdoc />
    protected override void Action()
    {
        PRUnitySDK.Managers.ProjectProperties.SetBool(propertyName, value);
    }
}
