using System.Collections.Generic;
using UnityEngine;

public abstract class TextureDefinition : ItemVisualDefinition
{
    [field: SerializeField] public Texture2D Prefab { get; protected set; }

    public override CategoryPath Category => throw new System.NotImplementedException();

}
