using UnityEngine;

/// <summary>
/// Своя сторона сущности вместо той, что выводится из её вида или типа игрока.
/// </summary>
/// <remarks>
/// Вешается на объект сущности. Нужен отдельным экземплярам: особый ящик, который бьют
/// только игроки, питомец, который на стороне игроков.
/// </remarks>
[DisallowMultipleComponent]
public class EntitySideOverride : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Сторона этой сущности.")]
    private EnumerationReference<EntitySideEnumerations> side = new();

    /// <summary>
    /// Сторона сущности.
    /// </summary>
    public Enumeration Side => side?.ToEnumeration();

    /// <summary>
    /// Меняет сторону на ходу — например, перешедший на сторону игрока монстр.
    /// </summary>
    public void SetSide(Enumeration value)
    {
        side ??= new EnumerationReference<EntitySideEnumerations>();
        side.Set(value);
    }
}
