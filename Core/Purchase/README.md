# Purchase

`IPurchaseService` — публичный контракт покупок за реальные деньги. Он не принадлежит Advertising
и не знает площадку: модули просят купить продукт или показать его цену, не связываясь с конкретным SDK.

Контракт отвечает за:

- признак, бывают ли покупки на площадке вообще (`IsSupported`);
- цену, значок валюты и картинки продукта из каталога площадки (`TryUpdateProduct`, `TryGetProductPrice`,
  `TryGetProductImages`);
- запуск покупки (`TryPurchase`) и событие успешной оплаты (`PurchaseSucceeded`).

Что выдать после оплаты, решают подписчики события, а не служба.

`IPurchaseService.TryGetProductPrice` отдаёт цену продукта, как её назвала площадка: число и отдельно код
валюты. Нужен для подписи без текста цены и значка валюты: их заполняет `TryUpdateProduct`.

Реализации живут в проектном слое и в интеграциях площадок: службу создаёт
`PRManagerContainer.InitializePurchaseService` (`PRUnitySDK.Managers.PurchaseService`), без интеграции остаётся
`UnavailablePurchaseService`. Потребители ищут её через `PRUnitySDK.TryResolve(out IPurchaseService service)`.
Подробности — в [PRUnitySDKPrivate/Modules/Purchase](../../../PRUnitySDKPrivate/Modules/Purchase/README.md).
Advertising использует контракт только для опциональной покупки Remove Ads.
