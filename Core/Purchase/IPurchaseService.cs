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
    /// Запускает покупку продукта.
    /// </summary>
    bool TryPurchase(string productId, ScriptableObject purchaseData);
}
