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
    public override bool CanExecute()
    {
        return base.CanExecute() && !string.IsNullOrWhiteSpace(propertyName);
    }

    /// <inheritdoc />
    protected override void Action()
    {
        PRUnitySDK.Managers.ProjectProperties.SetString(propertyName, value);
    }
}
