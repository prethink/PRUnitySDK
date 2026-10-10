using UnityEngine;

public abstract class ObjectDefinitionBase<T> : ItemVisualDefinition, IEntityMetadata, IEntityPrefabProvider where T : Object
{
    [field: SerializeField, PrefabPreview(140)] public T Prefab { get; protected set; }

    /// <inheritdoc />
    public EntityBase EntityPrefab => Prefab as EntityBase;
}
