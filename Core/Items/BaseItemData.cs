using UnityEngine;

public abstract partial class BaseItemData : ScriptableObject, IItemData
{
    public abstract string Id { get; }
    public abstract string Name { get; }
    [field: SerializeField] public Sprite Icon { get; protected set; }
    [field: SerializeField] public QualityType Quality { get; protected set; }

    public abstract ItemCategory Category { get; }
}
