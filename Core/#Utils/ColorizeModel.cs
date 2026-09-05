using UnityEngine;

/// <summary>
/// Перекрашивает все меши объекта разом и умеет вернуть исходные цвета.
/// </summary>
/// <remarks>
/// Нужен там, где модель временно показывают в другом виде: силуэт неизвестной награды,
/// подсветка выбранного, обозначение недоступного. Исходные цвета снимаются при запуске.
/// <para>
/// Цвет задаётся через <c>MaterialPropertyBlock</c>, чтобы не создавать объекту личную
/// копию материала: копия живёт до конца сцены и выбивает объект из общей отрисовки.
/// Имя свойства настраивается, в URP это <c>_BaseColor</c>, во встроенном конвейере
/// <c>_Color</c>.
/// </para>
/// </remarks>
public class ColorizeModel : PRMonoBehaviour
{
    [Tooltip("Имя цвета в шейдере. URP — _BaseColor, встроенный конвейер — _Color.")]
    [SerializeField] protected string colorPropertyName = "_BaseColor";

    [Tooltip("Цвет, который ставит Apply.")]
    [SerializeField] protected Color color = Color.white;

    private Renderer[] renderers;
    private Color[][] originalColors;
    private MaterialPropertyBlock block;
    private int propertyId;

    /// <summary>
    /// Цвет, заданный в инспекторе.
    /// </summary>
    public Color Color => color;

    protected override void InitializationComponents()
    {
        base.InitializationComponents();

        renderers = GetComponentsInChildren<Renderer>(true);
        block = new MaterialPropertyBlock();
        propertyId = Shader.PropertyToID(colorPropertyName);

        CacheOriginalColors();
    }

    /// <summary>
    /// Красит модель цветом из инспектора.
    /// </summary>
    /// <remarks>
    /// Без параметров, поэтому вешается на <c>UnityEvent</c>.
    /// </remarks>
    public void Apply()
    {
        SetColor(color);
    }

    /// <summary>
    /// Красит модель в чёрный силуэт.
    /// </summary>
    public void SetBlack()
    {
        SetColor(Color.black);
    }

    /// <summary>
    /// Красит модель указанным цветом.
    /// </summary>
    public void SetColor(Color value)
    {
        if (renderers == null)
            return;

        foreach (Renderer target in renderers)
        {
            if (target == null)
                continue;

            // Материалы берём один раз: свойство отдаёт новую копию массива
            // на каждое обращение, а в условии цикла оно вычислялось бы каждый шаг.
            int materialCount = target.sharedMaterials.Length;

            for (int index = 0; index < materialCount; index++)
                SetColor(target, index, value);
        }
    }

    /// <summary>
    /// Возвращает цвета, которые были у модели при запуске.
    /// </summary>
    public void ResetColor()
    {
        if (renderers == null || originalColors == null)
            return;

        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer target = renderers[rendererIndex];
            Color[] colors = originalColors[rendererIndex];

            if (target == null || colors == null)
                continue;

            for (int materialIndex = 0; materialIndex < colors.Length; materialIndex++)
                SetColor(target, materialIndex, colors[materialIndex]);
        }
    }

    /// <summary>
    /// Снимает исходные цвета, чтобы было к чему возвращаться.
    /// </summary>
    /// <remarks>
    /// Массив рядом с <c>renderers</c>, а не словарь по ним: список мешей фиксируется
    /// при запуске и дальше не меняется, поэтому индекс — надёжный и более дешёвый ключ.
    /// Материал без нужного свойства считаем белым: так возврат его не испортит.
    /// </remarks>
    private void CacheOriginalColors()
    {
        originalColors = new Color[renderers.Length][];

        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer target = renderers[rendererIndex];

            if (target == null)
                continue;

            Material[] materials = target.sharedMaterials;
            Color[] colors = new Color[materials.Length];

            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];

                colors[materialIndex] = material != null && material.HasProperty(propertyId)
                    ? material.GetColor(propertyId)
                    : Color.white;
            }

            originalColors[rendererIndex] = colors;
        }
    }

    private void SetColor(Renderer target, int materialIndex, Color value)
    {
        target.GetPropertyBlock(block, materialIndex);
        block.SetColor(propertyId, value);
        target.SetPropertyBlock(block, materialIndex);
    }
}
