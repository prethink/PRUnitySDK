using UnityEngine;

/// <summary>
/// Задаёт сущности своё служебное имя поверх описания.
/// </summary>
/// <remarks>
/// Имя — идентификатор для кода и логов, игроку его не показывают: на экран идёт подпись
/// из <see cref="EntityLocalizationOverride"/> и описания. Пустое значение не переопределяет
/// ничего — иначе забытое поле стёрло бы имя сущности.
/// </remarks>
public class EntityNameOverride : MonoBehaviour, IEntityDescriptionOverride
{
    [Tooltip("Служебное имя этого экземпляра. Пустое — описание отдаёт своё.")]
    [SerializeField] private string entityName;

    /// <summary>
    /// Имя, которое отдаёт описание.
    /// </summary>
    public string EntityName
    {
        get => entityName;
        set => entityName = value;
    }

    /// <inheritdoc />
    public void Apply(EntityDescription description)
    {
        if (description == null)
            return;

        description.SetNameOverride(() => string.IsNullOrWhiteSpace(entityName)
            ? description.GetMetadata()?.Name
            : entityName);
    }
}
