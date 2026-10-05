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
    /// Запускает покупку продукта.
    /// </summary>
    bool TryPurchase(string productId, ScriptableObject purchaseData);
}
