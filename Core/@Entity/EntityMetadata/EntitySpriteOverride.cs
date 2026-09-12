using UnityEngine;

/// <summary>
/// Задаёт сущности свою иконку поверх описания.
/// </summary>
/// <remarks>
/// Обычный <see cref="MonoBehaviour"/>, а не <see cref="PRMonoBehaviour"/>: компонент только
/// хранит значение и ни на что не подписывается, а регистрация в трекерах и сохранениях
/// у каждой сущности стоила бы лишних записей ни за чем.
/// </remarks>
public class EntitySpriteOverride : MonoBehaviour, IEntityDescriptionOverride
{
    [Tooltip("Иконка этого экземпляра. Пустая — описание отдаёт свою.")]
    [SerializeField] private Sprite sprite;

    /// <summary>
    /// Иконка, которую отдаёт описание.
    /// </summary>
    public Sprite Sprite
    {
        get => sprite;
        set => sprite = value;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Значение читается в момент запроса, поэтому иконку можно менять и по ходу игры.
    /// </remarks>
    public void Apply(EntityDescription description)
    {
        description?.SetSpriteOverride(() => sprite);
    }
}
