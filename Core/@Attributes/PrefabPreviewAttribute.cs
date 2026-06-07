using UnityEngine;

public class PrefabPreviewAttribute : PropertyAttribute
{
    public float Height { get; }

    public PrefabPreviewAttribute(float height = 80f)
    {
        Height = height;
    }
}