using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Публичный контракт самостоятельного модуля покупок.
/// </summary>
public interface IPurchaseService
{
    /// <summary>
    /// Вызывается после успешной покупки продукта.
    /// </summary>
    event Action<string> PurchaseSucceeded;

    /// <summary>
    /// Заполняет цену и иконку продукта, если он доступен.
    /// </summary>
    bool TryUpdateProduct(
        string productId,
        ScriptableObject purchaseData,
        Image icon,
        TextMeshProUGUI priceText);

    /// <summary>
    /// Отдаёт ссылки на картинки продукта из каталога площадки.
    /// </summary>
    /// <remarks>
    /// Для того, кто грузит картинки сам и хочет знать, когда они на месте — например,
    /// карточке, которая не даёт нажать на оплату, пока не показала товар целиком.
    /// Ссылка, которой у продукта нет, возвращается пустой.
    /// </remarks>
    /// <param name="productId">Идентификатор продукта площадки.</param>
    /// <param name="imageUrl">Картинка самого продукта.</param>
    /// <param name="currencyImageUrl">Значок валюты, в которой названа цена.</param>
    /// <returns><see langword="false"/>, если продукта нет в каталоге.</returns>
    bool TryGetProductImages(string productId, out string imageUrl, out string currencyImageUrl);

    /// <summary>
    /// Отдаёт цену продукта, как её назвала площадка: число и код валюты.
    /// </summary>
    /// <remarks>
    /// Для подписи, которая не кнопка окна: у таблички над объектом нет ни текста, ни значка,
    /// которые заполнил бы <see cref="TryUpdateProduct"/>. Код валюты отдельно: рядом
    /// со значком валюты он лишний, а без значка цена без него читается как рубли в любой стране.
    /// </remarks>
    /// <param name="productId">Идентификатор продукта площадки.</param>
    /// <param name="price">Цена числом, например «99».</param>
    /// <param name="currencyCode">Код валюты, например «YAN»; пусто, если площадка его не назвала.</param>
    /// <returns><see langword="false"/>, если продукта нет в каталоге или цена ещё не пришла.</returns>
    bool TryGetProductPrice(string productId, out string price, out string currencyCode);

    /// <summary>
    /// Запускает покупку продукта.
    /// </summary>
    bool TryPurchase(string productId, ScriptableObject purchaseData);
}
