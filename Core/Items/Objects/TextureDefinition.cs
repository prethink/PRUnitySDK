using UnityEngine;

public abstract class TextureDefinition : ItemVisualDefinition
{
    [field: SerializeField] public Texture2D Prefab { get; protected set; }

}
