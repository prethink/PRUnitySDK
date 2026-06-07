using UnityEngine;

public class SpritePreviewAttribute : PropertyAttribute
{
    public float Height { get; }

    public SpritePreviewAttribute(float height = 64f)
    {
        Height = height;
    }
}