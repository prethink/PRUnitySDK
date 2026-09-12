using UnityEngine;

/// <summary>
/// Задаёт сущности свою подпись для игрока поверх описания.
/// </summary>
/// <remarks>
/// Источник берётся первым заданным, как у <c>LocalizationObserver</c>: ключ в базе
/// локализации либо перевод, записанный прямо на объекте. Так экземпляр на сцене получает
/// своё название, не заводя отдельный ассет описания.
/// <para>
/// Перевод берётся в момент запроса, поэтому смену языка подпись переживает: описание
/// каждый раз спрашивает актуальную строку.
/// </para>
/// </remarks>
public class EntityLocalizationOverride : MonoBehaviour, IEntityDescriptionOverride
{
    [Tooltip("Ключ в базе локализации. Если задан, перевод берётся по нему.")]
    [SerializeField] private string globalKey;

    [Tooltip("Перевод, заданный прямо на объекте.")]
    [SerializeField] private LocalizationControl localization = new();

    /// <summary>
    /// Задаёт ключ в базе локализации.
    /// </summary>
    /// <param name="key">Ключ; пустой означает «брать перевод с объекта».</param>
    public void SetGlobalKey(string key)
    {
        globalKey = key;
    }

    /// <inheritdoc />
    public void Apply(EntityDescription description)
    {
        description?.SetLocalizationOverride(GetTranslate);
    }

    private string GetTranslate()
    {
        if (!string.IsNullOrWhiteSpace(globalKey))
            return L.Tr(globalKey);

        return PRLocalization.GetTranslate(localization);
    }
}
