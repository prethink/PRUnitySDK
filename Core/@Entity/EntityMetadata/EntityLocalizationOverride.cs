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
    /// <remarks>
    /// Переопределяется источник перевода, а не готовая строка: подпись отдают
    /// в <c>SetLocalization</c>, и провайдер нужен целиком — по нему
    /// <c>LocalizationObserver</c> перечитывает текст при смене языка.
    /// </remarks>
    public void Apply(EntityDescription description)
    {
        description?.SetLocalizationProviderOverride(GetProvider);
    }

    private ILocalizationProvider GetProvider()
    {
        if (string.IsNullOrWhiteSpace(globalKey))
            return localization;

        // Словарь из базы, а не одна строка: провайдер должен уметь ответить на любом языке.
        // Собираем при каждом обращении — в редакторе базу правят на ходу.
        return new LocalizationProvider(globalKey, L.GetDictionary(globalKey));
    }
}
