using UnityEngine;

/// <summary>
/// Время компонента по слоям его сущности: шаг кадра и часы «до момента».
/// </summary>
/// <remarks>
/// Для компонентов поведения, которые не знают свою сущность напрямую: держат часы полем и спрашивают
/// <see cref="DeltaTime"/> вместо <see cref="PRTime.GameDeltaTime"/> мира. Тогда выборочное замедление («заморозить
/// NPC», «всё, кроме игрока») доходит и до их таймеров, а не только до анимации и физики.
/// <para>
/// Сущность ищется один раз: на объекте или в родителях, а если её нет — по ссылке <see cref="EntityLinkBase"/>
/// (тело игрока лежит отдельно от сущности и связано с ней ссылкой). Слои спрашиваются при каждом обращении: ссылку
/// переназначают в рантайме, а слои сущности могут смениться. Ничего не нашлось — мир.
/// </para>
/// </remarks>
public sealed class TimeLayerClock
{
    private readonly Component component;
    private EntityBase entity;
    private EntityLinkBase link;
    private bool resolved;

    /// <param name="component">Компонент, чьё время нужно.</param>
    public TimeLayerClock(Component component)
    {
        this.component = component;
    }

    /// <summary>
    /// Слои времени компонента — у его сущности.
    /// </summary>
    public TimeLayer Layers
    {
        get
        {
            Resolve();

            EntityBase owner = entity != null ? entity : link != null ? link.Entity : null;
            return owner != null ? owner.GetTimeLayers() : TimeLayer.World;
        }
    }

    /// <summary>
    /// Шаг кадра в темпе слоёв компонента.
    /// </summary>
    public float DeltaTime => PRTime.Instance.GetDeltaTime(Layers);

    /// <summary>
    /// Часы слоёв компонента — для ожиданий «до момента». Сравнивать только с моментами, взятыми отсюда же.
    /// </summary>
    public float Now => PRTime.Instance.GetTime(Layers);

    private void Resolve()
    {
        if (resolved || component == null)
            return;

        resolved = true;
        entity = component.GetComponentInParent<EntityBase>(true);
        if (entity == null)
            link = component.GetComponentInParent<EntityLinkBase>(true);
    }
}
