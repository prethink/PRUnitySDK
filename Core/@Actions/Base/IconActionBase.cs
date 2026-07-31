using UnityEngine;

public abstract class IconActionBase : ActionBase, IIconProvider
{
    /// <summary>
    /// Иконка действия.
    /// </summary>
    [field: SerializeField] public Sprite Icon { get; protected set; }
}
