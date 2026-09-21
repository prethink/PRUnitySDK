using UnityEngine;

/// <summary>
/// Колышет текстуру на материале: вода, лава, туман.
/// </summary>
/// <remarks>
/// Приём тот же, что у <see cref="TextureOffsetScroller"/>: смещение и масштаб пишутся
/// через <c>MaterialPropertyBlock</c>, поэтому личной копии материала у объекта
/// не появляется.
/// <para>
/// Отличается движение. Лента едет ровно, а поверхность воды дышит: тайлинг медленно
/// расходится и сходится, текстуру сносит течением и ведёт вбок. Все три движения
/// складываются в один вектор <c>_ST</c>.
/// </para>
/// </remarks>
[RequireComponent(typeof(Renderer))]
public class TextureWaterMotion : PRMonoBehaviour
{
    [Tooltip("Скорость сноса текстуры по осям, за игровую секунду.")]
    [SerializeField] private Vector2 speed = new Vector2(0.01f, 0.03f);

    [Tooltip("На сколько тайлинг расходится в обе стороны: 0.05 - это пять процентов.")]
    [SerializeField, Range(0f, 0.5f)] private float pulseAmplitude = 0.05f;

    [Tooltip("За сколько игровых секунд проходит полный вдох и выдох. Ноль - без дыхания.")]
    [SerializeField, Min(0f)] private float pulsePeriod = 6f;

    [Tooltip("Насколько текстуру ведёт вбок, в долях текстуры.")]
    [SerializeField, Range(0f, 0.5f)] private float swayAmplitude = 0.01f;

    [Tooltip("За сколько игровых секунд проходит полное покачивание. Ноль - без него.")]
    [SerializeField, Min(0f)] private float swayPeriod = 4f;

    [Tooltip("Имя текстуры в шейдере. URP — _BaseMap, встроенный конвейер — _MainTex.")]
    [SerializeField] private string texturePropertyName = "_BaseMap";

    private new Renderer renderer;
    private MaterialPropertyBlock block;
    private Vector2 drift;
    private Vector2 baseScale = Vector2.one;
    private float pulseTime;
    private float swayTime;
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
                $"В материале объекта [{name}] нет текстуры [{texturePropertyName}]: колыхать нечего.");

            return;
        }

        // Тайлинг материала - опорный: дыхание считается от него, иначе первый же кадр
        // растянул бы текстуру до одного тайла.
        baseScale = material.GetTextureScale(texturePropertyName);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Живёт в <c>PRUpdate</c>, поэтому на логической паузе вода замирает вместе с игрой.
    /// </remarks>
    protected override void PRUpdate()
    {
        if (!hasProperty)
            return;

        float delta = PRTime.Instance.GameDeltaTime;

        // Каждое движение считает своё время и сворачивает его в собственный период.
        // Общий растущий счётчик через час игры потерял бы точность, и вода задёргалась бы.
        pulseTime = pulsePeriod > 0f ? Mathf.Repeat(pulseTime + delta, pulsePeriod) : 0f;
        swayTime = swayPeriod > 0f ? Mathf.Repeat(swayTime + delta, swayPeriod) : 0f;

        drift += speed * delta;
        drift.x = Mathf.Repeat(drift.x, 1f);
        drift.y = Mathf.Repeat(drift.y, 1f);

        Vector2 scale = GetScale();
        Vector2 offset = drift + GetSway() + GetPulsePivot(scale);

        renderer.GetPropertyBlock(block);
        block.SetVector(propertyId, new Vector4(scale.x, scale.y, offset.x, offset.y));
        renderer.SetPropertyBlock(block);
    }

    /// <summary>
    /// Тайлинг текущего кадра.
    /// </summary>
    /// <remarks>
    /// Оси идут со сдвигом в четверть периода. В такт это читается как наезд камеры,
    /// а вразнобой - как волна.
    /// </remarks>
    private Vector2 GetScale()
    {
        if (pulseAmplitude <= 0f || pulsePeriod <= 0f)
            return baseScale;

        float phase = Mathf.PI * 2f * pulseTime / pulsePeriod;

        return new Vector2(
            baseScale.x * (1f + pulseAmplitude * Mathf.Sin(phase)),
            baseScale.y * (1f + pulseAmplitude * Mathf.Sin(phase + Mathf.PI * 0.5f)));
    }

    /// <summary>
    /// Покачивание поверх сноса.
    /// </summary>
    /// <remarks>
    /// По осям разные доли периода, поэтому текстура ходит по петле, а не туда-сюда
    /// по прямой: прямая возвращает картинку в то же место и выдаёт повтор.
    /// </remarks>
    private Vector2 GetSway()
    {
        if (swayAmplitude <= 0f || swayPeriod <= 0f)
            return Vector2.zero;

        float phase = Mathf.PI * 2f * swayTime / swayPeriod;

        return new Vector2(
            swayAmplitude * Mathf.Sin(phase),
            swayAmplitude * Mathf.Cos(phase * 0.5f));
    }

    /// <summary>
    /// Поправка, удерживающая дыхание в середине текстуры.
    /// </summary>
    /// <remarks>
    /// Шейдер считает координату как <c>uv * scale + offset</c>, то есть тайлинг растёт
    /// от угла развёртки. Без поправки вода на каждом вдохе уезжала бы в сторону,
    /// вместо того чтобы расходиться на месте.
    /// </remarks>
    /// <param name="scale">Тайлинг текущего кадра.</param>
    private static Vector2 GetPulsePivot(Vector2 scale)
    {
        return new Vector2(0.5f * (1f - scale.x), 0.5f * (1f - scale.y));
    }
}
