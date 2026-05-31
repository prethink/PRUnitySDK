using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class TextureOffsetScroller : PRMonoBehaviour
{
    [SerializeField] private Vector2 speed = new Vector2(0f, 1f);

    private Renderer renderer;
    private Vector2 offset;


    protected override void InitializationComponents()
    {
        base.InitializationComponents();
        renderer = GetComponent<Renderer>();
    }

    protected override void PRUpdate()
    {
        offset += speed * Time.deltaTime;
        renderer.material.mainTextureOffset = offset;
    }
}