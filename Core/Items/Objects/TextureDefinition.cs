using System.Collections.Generic;
using UnityEngine;

public class TextureDefinition : ItemVisualDefinition
{
    [field: SerializeField] public Texture2D Prefab { get; protected set; }

    public override string Id => throw new System.NotImplementedException();

    public override CategoryPath Category => throw new System.NotImplementedException();

    public override string LocalizationKey => throw new System.NotImplementedException();

    public override IReadOnlyDictionary<LangType, string> LocalizationValues => throw new System.NotImplementedException();
}
