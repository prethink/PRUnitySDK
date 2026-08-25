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
    public override bool CanExecute()
    {
        return base.CanExecute() && !string.IsNullOrWhiteSpace(propertyName);
    }

    /// <inheritdoc />
    protected override void Action()
    {
        PRUnitySDK.Managers.ProjectProperties.AddLong(propertyName, count, save: false);
    }
}
