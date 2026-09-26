using UnityEngine;

public partial class PRSDKSettings
{
    /// <summary>
    /// Свой-чужой: стороны сущностей, матрица ударов и команды.
    /// </summary>
    [field: SerializeField]
    public EntitySidesSettings Sides { get; private set; } = new();
}
