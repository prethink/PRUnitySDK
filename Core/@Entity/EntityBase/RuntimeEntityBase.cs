using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using UnityEngine;

public abstract class RuntimeEntityBase : EntityBase, IEntityMetadata, IEntityMetadataProvider
{
    [field: SerializeField] public Sprite Icon { get; protected set; }

    [field: SerializeField] public string LocalizationKey { get; protected set; }

    public IReadOnlyDictionary<LangType, string> LocalizationValues => localization;
    [field: SerializeField, SerializedDictionary("Lang", "Value")] public SerializedDictionary<LangType, string> localization { get; private set; }
    [field: SerializeField] public QualityType Quality { get; protected set; }

    public IEntityMetadata EntityMetadata => Description.GetMetadata();

    protected IEntityMetadata baseEntityMetadata;
    protected IEntityMetadata overrideEntityMetadata;

    protected override void InitializeEntityMetadata()
    {
        baseEntityMetadata = this;
        overrideEntityMetadata = GetOverrideMetadata();

        Description = overrideEntityMetadata != null
            ? new EntityDescription(baseEntityMetadata, overrideEntityMetadata)
            : new EntityDescription(baseEntityMetadata);
    }

    /// <summary>
    /// Переопределяющее описание с того же объекта.
    /// </summary>
    /// <remarks>
    /// Себя пропускаем: сущность сама <see cref="IEntityMetadataProvider"/>, а её
    /// <see cref="EntityMetadata"/> читает <c>Description</c>, которого на этом шаге ещё
    /// нет. <c>GetComponent</c> возвращал именно её, и сущность без отдельного
    /// <see cref="EntityMetadataProvider"/> падала в <c>Awake</c>.
    /// </remarks>
    /// <returns>Описание соседнего провайдера либо <c>null</c>.</returns>
    private IEntityMetadata GetOverrideMetadata()
    {
        foreach (IEntityMetadataProvider provider in GetComponents<IEntityMetadataProvider>())
        {
            if (!ReferenceEquals(provider, this))
                return provider.EntityMetadata;
        }

        return null;
    }
}
