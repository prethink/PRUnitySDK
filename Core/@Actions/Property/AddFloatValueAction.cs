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
    public override bool CanExecute()
    {
        return base.CanExecute() && !string.IsNullOrWhiteSpace(propertyName);
    }

    /// <inheritdoc />
    protected override void Action()
    {
        PRUnitySDK.Managers.ProjectProperties.AddFloat(propertyName, count, save: false);
    }
}
