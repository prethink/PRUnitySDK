using System;
using UnityEngine;

/// <summary>
/// Время объекта по его слоям: шаг кадра и непрерывные часы «до момента».
/// </summary>
/// <remarks>
/// Для всего, что живёт своим темпом: держат часы полем и спрашивают <see cref="DeltaTime"/> вместо
/// <see cref="PRTime.GameDeltaTime"/> мира. Тогда выборочное замедление («заморозить NPC», «всё, кроме игрока»)
/// доходит и до таймеров, а не только до анимации и физики.
/// <para>
/// Часы <see cref="Now"/> непрерывны и при смене слоёв: у каждой маски в <see cref="PRTime"/> свои часы, и они
/// расходятся, поэтому при смене маски часы объекта продолжают с того же значения, а не прыгают на чужие. Иначе
/// перезарядка, начатая до смены, укоротилась бы или растянулась на разницу часов.
/// </para>
/// <para>
/// Слои компонента — у его сущности: сначала по ссылке <see cref="EntityLinkBase"/> на объекте или в родителях (тело
/// игрока связано с сущностью ссылкой), затем сущность в родителях — как у <see cref="EntityTimeScaleDriver"/>.
/// Не нашлось — мир, и поиск повторяется позже: ссылку и сущность могут добавить после первого запроса.
/// </para>
/// <para>
/// Контракт перепривязки: сущность у ссылки читается при каждом обращении, поэтому перепривязывать объект — через
/// существующий <see cref="EntityLinkBase"/> (как в пуле). Найденный компонент ссылки или сущности часы не
/// перепроверяют: заменили сам компонент — создайте новые часы.
/// </para>
/// </remarks>
public sealed class TimeLayerClock
{
    /// <summary>
    /// Через сколько кадров повторить поиск сущности, если её не нашлось.
    /// </summary>
    private const int RetryFrames = 30;

    private readonly Component component;
    private readonly Func<TimeLayer> layersSource;
    private readonly Func<TimeLayer, float> timeOf;

    private EntityLinkBase link;
    private EntityBase entity;
    private bool found;
    private int retryFrame = int.MinValue;

    private bool started;
    private TimeLayer lastLayers;
    private float offset;

    /// <summary>
    /// Часы компонента: слои — у его сущности.
    /// </summary>
    public TimeLayerClock(Component component)
    {
        this.component = component;
        timeOf = DefaultTimeOf;
    }

    /// <summary>
    /// Часы со своим источником слоёв — у мозга, мотора, оружия, которые знают сущность сами.
    /// </summary>
    /// <param name="layers">Слои объекта; спрашиваются при каждом обращении.</param>
    /// <param name="timeOf">Часы маски; по умолчанию <see cref="PRTime.GetTime"/>. Подменяются в тестах.</param>
    public TimeLayerClock(Func<TimeLayer> layers, Func<TimeLayer, float> timeOf = null)
    {
        layersSource = layers;
        this.timeOf = timeOf ?? DefaultTimeOf;
    }

    /// <summary>
    /// Слои времени объекта.
    /// </summary>
    public TimeLayer Layers => layersSource != null ? layersSource() : ComponentLayers();

    /// <summary>
    /// Шаг кадра в темпе слоёв объекта.
    /// </summary>
    public float DeltaTime => PRTime.Instance.GetDeltaTime(Layers);

    /// <summary>
    /// Часы объекта для ожиданий «до момента», непрерывные и при смене слоёв. Сравнивать только с моментами,
    /// взятыми отсюда же.
    /// </summary>
    public float Now
    {
        get
        {
            TimeLayer layers = Layers;

            if (!started)
            {
                started = true;
                lastLayers = layers;
            }
            else if (layers != lastLayers)
            {
                // Маска сменилась: часы продолжают с того значения, что показывали на старой маске.
                offset += timeOf(lastLayers) - timeOf(layers);
                lastLayers = layers;
            }

            return timeOf(layers) + offset;
        }
    }

    private static float DefaultTimeOf(TimeLayer layers) => PRTime.Instance.GetTime(layers);

    private TimeLayer ComponentLayers()
    {
        Resolve();

        EntityBase owner = link != null && link.Entity != null ? link.Entity : entity;
        return owner != null ? owner.GetTimeLayers() : TimeLayer.World;
    }

    private void Resolve()
    {
        if (found || component == null || Time.frameCount < retryFrame)
            return;

        link = component.GetComponentInParent<EntityLinkBase>(true);
        entity = component.GetComponentInParent<EntityBase>(true);
        found = link != null || entity != null;

        if (!found)
            retryFrame = Time.frameCount + RetryFrames;
    }
}
