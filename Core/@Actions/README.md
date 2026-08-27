# Actions

Система `Actions` описывает переиспользуемые действия с единым контрактом проверки и выполнения. Действие можно хранить как `ScriptableObject`, размещать компонентом на `GameObject` или предоставлять через `IActionProvider`.

## Основные типы

| Тип | Назначение |
| --- | --- |
| `IAction` | Контракт с методами `CanExecute()` и `Execute()` |
| `IActionProvider` | Предоставляет действие через свойство `Action` |
| `ActionExecuter` | Общая механика проверки и выполнения для разных базовых Unity-типов |
| `ActionBase` | Основа ScriptableObject-действий |
| `ActionMonoBehaviourBase` | Основа действий-компонентов |
| `InlineActionBase` | Основа действий, настраиваемых прямо в инспекторе владельца |
| `IconActionBase` | ScriptableObject-действие с иконкой |
| `InlineActionContainer` | Ассет с одним встроенным действием |
| `InlineActionPipeline` | Ассет из нескольких встроенных действий по порядку |
| `ActionSequence` | Последовательное выполнение набора действий |
| `ActionRunner` | Компонент, выполняющий список действий по порядку |
| `OpenUrlInlineAction` | Встроенный вариант открытия URL |
| `AddResourceInlineAction` | Встроенное начисление ресурса |
| `OpenURLAction` | Открывает проверенный HTTP/HTTPS URL |
| `LangAction` | Переключает язык через LanguageManager SDK |
| `AddBoolValueAction` | Устанавливает bool-свойство в `ProjectPropertiesManager` |
| `AddLongValueAction` | Прибавляет значение к long-свойству в `ProjectPropertiesManager` |
| `AddFloatValueAction` | Прибавляет значение к float-свойству в `ProjectPropertiesManager` |
| `AddStringValueAction` | Устанавливает string-свойство в `ProjectPropertiesManager` |
| `AddDateTimeValueAction` | Устанавливает DateTime-свойство из ISO-8601 строки |

`ActionExecuter` намеренно используется через композицию: `ScriptableObject` и `MonoBehaviour` не могут наследоваться от одного общего класса действий, но должны выполнять одинаковую проверку.

## Выполнение

`Execute()` передаёт в executor виртуальный метод `CanExecute()` владельца. Поэтому переопределённые проверки всегда применяются и при прямом вызове `Execute()`.

```text
Execute()
└── CanExecute()
    ├── false → действие не вызывается, результат false
    └── true  → Action(), результат true
```

Базовая проверка требует завершённой инициализации `PRUnitySDK`. Наследник расширяет её через `base.CanExecute()`:

```csharp
public override bool CanExecute()
{
    return base.CanExecute() && amount > 0;
}
```

`true` означает, что внутреннее действие было вызвано. Исключения из действия не преобразуются в `false`: они передаются вызывающему коду.

## ScriptableObject-действие

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Actions/Load level")]
public sealed class LoadLevelAction : ActionBase
{
    [SerializeField] private int sceneIndex;

    public override bool CanExecute()
    {
        return base.CanExecute() && sceneIndex >= 0;
    }

    protected override void Action()
    {
        SceneChanger.Instance.SceneChange(sceneIndex);
    }
}
```

После компиляции создайте asset через `Create → Game → Actions → Load level`.

ScriptableObject-действия подходят для конфигурации, которая переиспользуется между сценами. Не храните изменяемое состояние конкретного вызова в общем asset: один экземпляр может одновременно использоваться несколькими объектами.

## Действие-компонент

```csharp
public sealed class DisableObjectAction : ActionMonoBehaviourBase
{
    public override bool CanExecute()
    {
        return base.CanExecute() && gameObject.activeSelf;
    }

    protected override void Action()
    {
        gameObject.SetActive(false);
    }
}
```

`ActionMonoBehaviourBase` подходит для действий, зависящих от состояния конкретного `GameObject`.

## Встроенное действие

Когда действие уникально для одного объекта, заводить под него ассет незачем:
`InlineActionBase` настраивается прямо в инспекторе владельца.

```csharp
[Serializable]
public class GiveCoinsAction : InlineActionBase
{
    [SerializeField] private long amount = 100;

    public override bool CanExecute() => base.CanExecute() && amount > 0;

    protected override void Action()
    {
        WalletService.Instance.Add(ResourceEnumerationProvider.Coin, amount);
    }
}
```

Поле владельца объявляется с `[SerializeReference]` и атрибутом
[`ReferenceSelector`](../@Attributes/README.md#referenceselectorattribute), который
добавляет выпадающий список реализаций:

```csharp
[SerializeReference, ReferenceSelector] private IAction action;
[SerializeReference, ReferenceSelector] private List<IAction> actions;
```

В инспекторе выбирается тип, и его поля рисуются тут же. Отдельный файл не создаётся —
данные лежат внутри объекта-владельца.

Правило простое: настройка общая для многих мест — ассет; настройка своя у каждого
объекта — встроенное действие. Если нужно и то и другое сразу, есть третий вариант.

### Ассеты из встроенных действий

Третий вариант снимает недостатки обоих: ассет **переиспользуется и ссылается отовсюду**,
а его содержимое настраивается встроенными действиями — без класса под каждую комбинацию.
Есть в двух видах.

**`InlineActionContainer`** — одно действие. Простой и самый частый случай: нужно
переиспользуемое «дать 100 монет», а писать под него класс незачем.

```text
PRUnitySDK/Actions/Inline action
└── Стартовый бонус
    └── AddResourceInlineAction  (Coin, 100)
```

**`InlineActionPipeline`** — несколько действий по порядку.

```text
PRUnitySDK/Actions/Inline action pipeline
└── Награда за вход
    ├── AddResourceInlineAction  (Coin, 100)
    ├── OpenUrlInlineAction      (...)
    └── ...
```

Список из одного элемента только загромождает инспектор кнопками массива, поэтому для
единственного действия берите первый вариант, а конвейер — когда действий правда несколько.

Оба наследуют `IconActionBase`, поэтому у них есть иконка и они подходят везде, где она
нужна. Ссылаться на них можно как на обычное действие: полем `ActionBase`, из
`ActionRunner`, из `ActionContainer`.

> Не путайте с `ActionContainer` из [@Entity](../@Entity/README.md#контейнеры): там
> «контейнер» — это подбираемая сущность на сцене, здесь — обёртка над действием.

Проверки у них разные, и это намеренно:

- `InlineActionContainer.CanExecute()` требует, чтобы вложенное действие было выбрано
  **и готово** — обёртка над одним действием обязана честно отвечать за него;
- `InlineActionPipeline.CanExecute()` проверяет только, что конвейер не пуст: частично
  применимый набор — обычная ситуация, часть наград может быть уже выдана. Узнать,
  сработает ли хоть что-то, можно через `CanExecuteAny()`, а число сработавших после
  выполнения — через `LastExecutedCount`.

### Четыре способа хранения — что выбрать

| | `ActionBase` | `InlineActionBase` | `InlineActionContainer` | `InlineActionPipeline` |
| --- | --- | --- | --- | --- |
| Где живёт | свой ассет | внутри владельца | свой ассет | свой ассет |
| Нужен новый класс | на каждое действие | на каждый вид действия | нет | нет |
| Переиспользование | да | нет | да | да |
| Сколько действий | одно | одно | одно | несколько |
| Когда брать | нужна своя логика | уникальная настройка объекта | одно действие, много ссылок | цепочка, много ссылок |

### ActionRunner

Готовый компонент для кнопок и триггеров: держит список встроенных действий, список
ассетных и выполняет их по порядку.

```csharp
GetComponent<ActionRunner>().Execute();   // вернёт число успешных
```

Флаг `stopOnFailure` прерывает цепочку на первом отказавшем действии — например, чтобы
не выдавать награду, если списание не прошло. Два списка нужны потому, что
`[SerializeReference]` не хранит наследников `UnityEngine.Object`: ассетные действия
кладутся отдельным полем.

## Использование через provider

```csharp
public sealed class ActionProvider : MonoBehaviour, IActionProvider
{
    [SerializeField] private ActionBase action;

    public IAction Action => action;
}
```

Потребитель работает только с интерфейсом:

```csharp
if (provider.Action?.Execute() == true)
{
    OnActionExecuted();
}
```

Нет необходимости отдельно вызывать `CanExecute()` перед `Execute()`: `Execute()` повторно выполняет полную виртуальную проверку. Отдельный вызов полезен для отображения доступности кнопки, подсказки или интерактивного объекта.

## OpenURLAction

`OpenURLAction` выполняется только для абсолютных URL со схемой `http` или `https`. Пустые строки, относительные адреса и другие схемы отклоняются через `CanExecute()`.

## Действия ProjectProperties

Набор покрывает все типы `ProjectPropertiesManager`:

- `AddBoolValueAction` устанавливает bool (историческое имя сохранено для совместимости);
- `AddLongValueAction` и `AddFloatValueAction` прибавляют `count`;
- `AddStringValueAction` устанавливает строку, включая пустую;
- `AddDateTimeValueAction` устанавливает дату из ISO-8601 строки, например `2026-12-31T23:59:59Z`.

Все действия требуют непустой `propertyName`; DateTime-действие дополнительно проверяет формат даты в `CanExecute()`.

`AddLongValueAction` и `AddFloatValueAction` выполняют изменение с `save: false`. Это позволяет составному reward/purchase-процессу выполнить одну общую запись после нескольких изменений. Остальные действия сохраняют данные сразу через стандартные `Set*`-методы.

## Рекомендации

- Условия доступности размещайте в `CanExecute()`.
- Эффект размещайте в защищённом `Action()`.
- В переопределённом `CanExecute()` обычно вызывайте `base.CanExecute()`.
- Не вызывайте `Action()` напрямую: это обходит проверки.
- Учитывайте результат `Execute()` в потребителях.
- Проверяйте сериализованные ссылки на действие на `null`.
- Для асинхронных операций и подробного результата потребуется отдельный async/result-контракт; текущий `IAction` является синхронным.
