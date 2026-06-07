using UnityEngine;

public abstract class ObjectDefinitionBase : ItemVisualDefinition
{
    [field: SerializeField, PrefabPreview(140)] public GameObject Prefab { get; protected set; }
}
