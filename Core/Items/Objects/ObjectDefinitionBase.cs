using UnityEngine;

public abstract class ObjectDefinitionBase<T> : ItemVisualDefinition, IEntityMetadata where T : Object
{
    [field: SerializeField, PrefabPreview(140)] public T Prefab { get; protected set; }
}
