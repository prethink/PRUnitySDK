using UnityEngine;

public abstract class ObjectDefinitionBase<T> : ItemVisualDefinition where T : Object
{
    [field: SerializeField, PrefabPreview(140)] public T Prefab { get; protected set; }
}
