using TMPro;
using UnityEngine;

/// <summary>
/// Держит текст переведённым по ключу из базы локализации.
/// </summary>
/// <remarks>
/// Тот же живой перевод, что и у <see cref="LocalizationObserver"/>, но источник один —
/// ключ. Подходит подписям, которые правит дизайнер: строка лежит в базе, а на объекте
/// остаётся только её имя.
/// <para>
/// Своего словаря и провайдера здесь нет намеренно. Наблюдатель с провайдером носит
/// перевод на префабе, и такие строки в общий список не попадают: отдать их на перевод
/// или добавить язык можно только обходом префабов. Там, где ключа достаточно,
/// выбирают этот компонент — тогда перевод виден в окне локализации целиком.
/// </para>
/// <para>
/// Аргументы подставляются через <c>string.Format</c>, поэтому в переводе пишут
/// <c>{0}</c>, а не склеивают строку в коде.
/// </para>
/// </remarks>
[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizationKeyObserver : LocalizationObserverBase
{
    /// <summary>
    /// Ключ в базе локализации.
    /// </summary>
    [SerializeField] protected string globalKey;

    /// <summary>
    /// Ключ, по которому берётся перевод.
    /// </summary>
    public string GlobalKey => globalKey;

    /// <summary>
    /// Задаёт ключ в базе локализации.
    /// </summary>
    public void SetGlobalKey(string key)
    {
        this.globalKey = key;
        Refresh();
    }

    /// <summary>
    /// Задаёт ключ вместе с аргументами.
    /// </summary>
    public void SetGlobalKey(string key, string[] args)
    {
        this.globalKey = key;
        SetArgs(args);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Язык здесь не спрашивают: <see cref="L.Tr"/> сам берёт текущий из менеджера,
    /// а база хранит все языки в одной записи.
    /// </remarks>
    protected override bool TryGetTranslate(string langKey, out string result)
    {
        result = string.Empty;

        if (string.IsNullOrEmpty(globalKey))
            return false;

        result = Format(L.Tr(globalKey), globalKey);
        return true;
    }
}
