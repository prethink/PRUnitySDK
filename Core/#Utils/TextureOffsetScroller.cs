using UnityEngine;

/// <summary>
/// Двигает текстуру по материалу: бегущая лента, вода, полоса конвейера.
/// </summary>
/// <remarks>
/// Сдвиг задаётся через <c>MaterialPropertyBlock</c>: обращение к <c>renderer.material</c>
/// создало бы объекту личную копию материала, которая живёт до конца сцены и выбивает
/// объект из общей отрисовки.
/// <para>
/// Имя свойства настраивается: в URP основная текстура называется <c>_BaseMap</c>,
/// во встроенном конвейере — <c>_MainTex</c>. Смещение и масштаб лежат в одном векторе
/// <c>_ST</c>, поэтому масштаб берётся из материала и передаётся вместе со сдвигом.
/// </para>
/// </remarks>
[RequireComponent(typeof(Renderer))]
public class TextureOffsetScroller : PRMonoBehaviour
{
    [Tooltip("Скорость сдвига по осям, за игровую секунду.")]
    [SerializeField] private Vector2 speed = new Vector2(0f, 1f);

    [Tooltip("Имя текстуры в шейдере. URP — _BaseMap, встроенный конвейер — _MainTex.")]
    [SerializeField] private string texturePropertyName = "_BaseMap";

    private new Renderer renderer;
    private MaterialPropertyBlock block;
    private Vector2 offset;
    private Vector2 scale = Vector2.one;
    private int propertyId;
    private bool hasProperty;

    protected override void InitializationComponents()
    {
        base.InitializationComponents();

        renderer = GetComponent<Renderer>();
        block = new MaterialPropertyBlock();
        propertyId = Shader.PropertyToID($"{texturePropertyName}_ST");

        Material material = renderer.sharedMaterial;
        hasProperty = material != null && material.HasProperty(texturePropertyName);

        if (!hasProperty)
        {
            PRLog.WriteWarning(this,
                $"В материале объекта [{name}] нет текстуры [{texturePropertyName}]: сдвигать нечего.");

            return;
        }

        // Масштаб берём из материала и дальше не трогаем: он лежит в том же векторе,
        // что и смещение, и без него текстура растянулась бы при первом же сдвиге.
        scale = material.GetTextureScale(texturePropertyName);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Живёт в <c>PRUpdate</c>, поэтому на логической паузе лента останавливается
    /// вместе с игрой, а не продолжает ехать под открытым окном.
    /// </remarks>
    protected override void PRUpdate()
    {
        if (!hasProperty)
            return;

        offset += speed * PRTime.Instance.GameDeltaTime;

        // Сворачиваем в один период: у текстуры сдвиг на 1 и на 1001 выглядит одинаково,
        // но растущее без предела число теряет точность, и через час игры лента начинает
        // дёргаться.
        offset.x = Mathf.Repeat(offset.x, 1f);
        offset.y = Mathf.Repeat(offset.y, 1f);

        renderer.GetPropertyBlock(block);
        block.SetVector(propertyId, new Vector4(scale.x, scale.y, offset.x, offset.y));
        renderer.SetPropertyBlock(block);
    }
}
