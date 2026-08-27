# Wallet

`Wallet` — фасад над `ResourceManager` для операций с валютой: узнать баланс, начислить,
списать, проверить платёжеспособность. Собственного хранилища у него нет — все значения
лежат в ресурсах и сохраняются вместе с ними.

```text
WalletBase / WalletResources → WalletService → ResourceManager → ProjectData
```

Смысл слоя — дать коду покупок и наград словарь предметной области (`Buy`, `CanBuy`,
`GetBalance`) вместо работы с ресурсами напрямую.

## Состав

| Тип | Когда использовать |
| --- | --- |
| `WalletService` | Базовый API; валюта задаётся как `Enumeration` |
| `WalletResources` | То же, но валюта задаётся через `ResourceItemDefinition` |
| `WalletBase` | Заготовка кошелька, привязанного к одной валюте |

## WalletService

Singleton, работает с валютой как с `Enumeration`:

```csharp
long coins = WalletService.Instance.GetBalance(ResourceEnumerationProvider.Coin);

WalletService.Instance.Add(ResourceEnumerationProvider.Coin, 100);

if (WalletService.Instance.CanBuy(ResourceEnumerationProvider.Coin, 250))
    WalletService.Instance.Buy(ResourceEnumerationProvider.Coin, 250);
```

`Buy` атомарен: он проверяет баланс и списывает за один вызов, возвращая `false`, если
средств не хватило. Отдельный `CanBuy` нужен только для UI — чтобы заранее показать
кнопку неактивной. Проверять `CanBuy` перед `Buy` не обязательно.

`Add` принимает флаг `save` (по умолчанию `true`) — при массовом начислении имеет смысл
передать `false` и сохранить один раз в конце. `Buy` сохраняет всегда.

## WalletResources

То же самое, но точка входа — `ResourceItemDefinition`, то есть ассет ресурса. Удобно,
когда цена задана ссылкой на предмет в инспекторе, а не перечислением в коде:

```csharp
private readonly WalletResources wallet = new();

if (wallet.CanBuy(priceResource, price))
    wallet.Buy(priceResource, price);
```

Создаётся обычным `new()` — синглтоном не является. Если у ресурса не настроен тип валюты,
методы не бросают исключение: чтение вернёт `0`, начисление и списание запишут warning
и ничего не сделают. Проверить настройку заранее можно через `IsConfigured(resource)`.

## WalletBase

Заготовка под кошелёк одной валюты — чтобы не передавать `Enumeration` в каждый вызов:

```csharp
public class CoinWallet : WalletBase
{
    public override Enumeration Currency => ResourceEnumerationProvider.Coin;
}

// далее
var wallet = new CoinWallet();
wallet.Add(50);
wallet.Buy(10);
```

Наследников в проекте пока нет — все вызовы идут через `WalletService` и `WalletResources`.

## Что использовать

- цена приходит ассетом из инспектора → `WalletResources`;
- валюта известна в коде → `WalletService`;
- один тип валюты используется в классе постоянно → наследник `WalletBase`;
- нужны не деньги, а произвольный числовой ресурс (прогресс, ключи, энергия) → работайте
  с [`ResourceManager`](../Items/Resources/README.md) напрямую, `Wallet` ничего не добавит.

## Ограничения

- **`Add` не проверяет знак.** `Add(currency, -100)` уменьшит баланс, минуя проверку
  достаточности средств, и может увести его в минус. Списывать нужно только через `Buy`:
  там отрицательная сумма отсекается, а нехватка средств возвращает `false`. Если
  отрицательные начисления в проекте не нужны, стоит закрыть это проверкой в `WalletService.Add`.
- **Нет транзакций.** Списать одну валюту и начислить другую атомарно нельзя: если второй
  шаг не выполнится, первый уже применён.
- **Нет собственных событий.** Об изменении баланса сообщает `ResourceManager` своими
  событиями ресурсов; подписываться нужно на них.
- **`WalletBase` без наследников** — путь не проверен на реальном коде.
- **Валюта — это обычный ресурс.** Никакой защиты от того, что «валютой» окажется
  служебный счётчик, нет; корректность связки задаёт `ResourceEnumerationProvider`.

## Смотрите также

- [Items/Resources](../Items/Resources/README.md) — `ResourceManager`, хранилище и события
- [Reward](../Reward/README.md) — выдача наград, использует `WalletResources`
- [Purchase](../Purchase/README.md) — покупки
