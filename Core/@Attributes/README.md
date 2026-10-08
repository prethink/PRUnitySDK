# Attributes

Атрибуты PRUnitySDK связывают независимые части `partial`-классов, подключают модули к
инициализации SDK, позволяют интеграциям заменять стандартные сервисы и расширяют Unity
Inspector. Большинство runtime-атрибутов обрабатывается через `ReflectionExtension`.

## Группы атрибутов

| Группа | Атрибуты | Назначение |
| --- | --- | --- |
| Method hooks | `MethodHookAttribute`, `InvokePartialAttribute` | Вызов методов расширения в заданной стадии и порядке |
| Override | `OverridePropertyAttribute`, `OverrideBootstrapAttribute` | Замена стандартной реализации сервиса или bootstrap-процесса |
| Runtime control | `DisableMethodsAttribute` | Отключение отдельных callback'ов `PRMonoBehaviour` |
| Автоматическая регистрация | `AutoBackgroundTaskAttribute` | Создание и регистрация [фоновой задачи](../BackgroundTasks/README.md) при старте SDK |
| Inspector | `SpritePreviewAttribute`, `PrefabPreviewAttribute` | Preview сериализованных Unity-объектов |
| Inspector | `ReferenceSelectorAttribute` | Выбор реализации для поля `[SerializeReference]` |
| Inspector | `InlineAssetAttribute` | Поля ассета прямо в компоненте, который на него ссылается |
| Virtual metadata | `VirtualAttributeAttribute` | Описание виртуально добавляемого атрибута |

## MethodHookAttribute

`MethodHook` помечает метод, который должен быть вызван на определённой стадии. Методы
одной стадии сортируются по `Order`: меньшее значение выполняется раньше.

```csharp
public partial class PRUnitySDK
{
    private const int InventoryInitializationOrder = 100;

    [MethodHook(MethodHookStage.SDK, InventoryInitializationOrder)]
    private static void InitializeInventory()
    {
        RegisterService<IInventoryService>(new InventoryService());
    }
}
```

Во время основной инициализации SDK выполняется:

```csharp
typeof(PRUnitySDK).RunStaticMethodHooks(MethodHookStage.SDK);
```

Методы hook вызываются reflection в порядке `Order`:

```text
RunStaticMethodHooks(SDK)
├── Order 0
├── Order 10
├── Order 30
└── Order 100
```

Метод может быть `private`, `protected` или `public`. Для static runner он должен быть
статическим, для instance runner — экземплярным.

### Аргументы hook-методов

По умолчанию hook вызывается без аргументов. Instance runner умеет передавать аргументы,
если стадии нужен контекст:

```csharp
public object Clone()
{
    var clone = new ProjectData();
    // ...
    this.RunMethodHooks(MethodHookStage.Cloning, clone);
    return clone;
}
```

```csharp
[MethodHook(MethodHookStage.Cloning)]
public void CloneInventory(ProjectData clone)
{
    clone.InventoryData = (InventoryData)InventoryData.Clone();
}
```

Правила сопоставления на одной стадии:

- hook без параметров вызывается всегда, независимо от переданных аргументов;
- hook, у которого число параметров совпадает с числом аргументов, получает их;
- при несовпадении hook пропускается с предупреждением в лог, а не роняет всю стадию.

Аргументы поддерживает только instance runner (`RunMethodHooks`); `RunStaticMethodHooks`
вызывает статические hook'и без аргументов.

### Изменяемые аргументы

Hook может вернуть решение через параметр `ref`: значение пишется обратно в переданный
массив аргументов. Так стадия получает результат, даже если hook'ов несколько.

```csharp
var hookArgs = new object[] { other, 0L, false };
this.RunMethodHooks(ExecutorStage, hookArgs);

if (hookArgs[2] is bool found && found)
    Show((long)hookArgs[1]);
```

```csharp
[MethodHook(ExecutorStage)]
protected void ResolveExecutor(Collider other, ref long executor, ref bool hasExecutor)
{
    hasExecutor = other.TryGetLocalPlayer(out PlayerLocal player);

    if (hasExecutor)
        executor = player.PlayerId;
}
```

Массив нужно создать заранее и читать значения из него же: `params` создаст свой массив,
и записи в нём вызывающий код не увидит. Hook'и одной стадии выполняются по возрастанию
`Order` и видят результат предыдущего, поэтому итог определяет последний записавший.
Если решений может быть несколько, договоритесь о правиле явно: например, «выставляю
`true`, только когда разбираю своё событие, чужое не трогаю».

### Видимость hook-методов

Hook ищется через `GetMethods(BindingFlags.Instance | NonPublic | Public)` по
**фактическому типу объекта**. Reflection не возвращает `private` методы базового класса,
поэтому hook на базовом классе, экземпляры которого — наследники, должен быть
`protected` или выше:

```csharp
public abstract partial class CameraControllerBase
{
    // private здесь не вызовется: экземпляр — наследник, а не сам базовый класс
    [MethodHook(SomeStage)]
    protected void OnSomeStage() { }
}
```

`private` безопасен только там, где тип объекта совпадает с типом, объявившим hook:
`PRWindowsContainer`, `PRManagerContainer`, `ProjectData`, `PlayerTracker`, `CameraTracker`.
Ошибка не видна: стадия просто отработает вхолостую.

### Несколько реализаций одной точки расширения

Если точку расширения могут занять несколько независимых частей, используйте `MethodHook`
(вызываются все, по возрастанию `Order`) или `InvokePartial`, когда нужно собрать
результаты. У `partial void` может быть ровно одна реализация на весь partial-класс:
вторая даёт ошибку компиляции `CS0757`.

### Кеширование

`ReflectionExtension` кеширует результат сканирования типа по паре (тип, стадия), поэтому
атрибуты читаются один раз, а не на каждом вызове. Стадия `Pause` вызывается очень часто:
её получает каждый `PRMonoBehaviour` при каждом переключении паузы.

### Стандартные стадии

`MethodHookStage` включает несколько групп:

- Unity lifecycle: `PreAwake`, `PostAwake`, `PreStart`, `PostStart`, `PreOnEnable`,
  `PostOnEnable`, `PreOnDisable`, `PostOnDisable`;
- данные: `PreSave`, `Saving`, `PostSave`, `PreClone`, `Cloning`, `PostClone`;
- инициализация: `PreInitialize`, `Initializing`, `PostInitialize`, `ReadyProject`;
- SDK: `SDK`, `RegisterFactories`, `Converter`, `DefaultSettings`;
- операции: `PreOperation`, `PostOperation`, `CreateCollections`, `Custom`;
- прочие стадии: `Construct`, `Awake`, `Start`, `Pause`.

Наличие значения в enum не означает автоматический вызов. Стадия выполняется только там,
где код явно вызывает `RunMethodHooks` или `RunStaticMethodHooks`.

Можно использовать пользовательское строковое имя:

```csharp
[MethodHook("BeforeInventoryLoad", order: 10)]
private void PrepareInventory() { }

this.RunMethodHooks("BeforeInventoryLoad");
```

## Связь MethodHook с SDK

`PRUnitySDK.InitializeSDK()` использует method hooks для подключения модулей без ручного
списка зависимостей в центральном классе:

```text
PRUnitySDK.InitializeSDK()
├── GameRules.Initialize()
├── Converter hooks
├── singleton initialization
├── RegisterFactories hooks
├── SDK hooks
│   ├── service resolver
│   ├── device info
│   ├── storage
│   ├── metrics
│   ├── server time
│   └── optional modules
├── Managers.Initialize()
└── Windows.Initialize()
```

Новый SDK-модуль обычно оформляется как `partial class PRUnitySDK` и добавляет static
метод с `[MethodHook(MethodHookStage.SDK, order)]`.

Приоритет лучше хранить в именованной константе. Одинаковый `Order` не задаёт надёжного
взаимного порядка методов: если порядок важен, используйте разные значения.

## OverridePropertyAttribute

Позволяет интеграции заменить стандартную реализацию сервиса до применения fallback:

```csharp
public partial class PRUnitySDK
{
    [OverrideProperty(typeof(IServerTime), order: -100)]
    private static void UsePlatformServerTime()
    {
        ServerTime = new PlatformServerTime();
    }
}
```

Основной модуль вызывает:

```csharp
typeof(PRUnitySDK).TryOverrideStaticProperty(typeof(IServerTime));
```

После этого `InitializeDefault` создаёт стандартную реализацию только в том случае, если
поле всё ещё равно `null`.

Вызывается только первый override по `Order`: меньшее значение имеет более высокий
приоритет.

`OverridePropertyAttribute` не изменяет C# property автоматически: метод с атрибутом сам
должен присвоить поле или зарегистрировать нужную реализацию.

## OverrideBootstrapAttribute

Используется интеграциями, которым нужно отложить стандартную инициализацию SDK. Например,
YG2 сначала отключает обычный запуск, затем вызывает `InitializeSDK()` после получения
данных платформы:

```csharp
public partial class Bootstrap
{
    [OverrideBootstrap]
    private void OverrideInitialize()
    {
        isOverriden = true;
    }
}
```

Bootstrap выбирает один найденный метод. Override обязан самостоятельно обеспечить
последующий запуск SDK, иначе `PRUnitySDK.IsInitialized` останется `false`.

> [!WARNING]
> Текущая реализация `ReflectionExtension.GetMethods<T>()` проверяет наличие любого
> `Attribute`, а не конкретного `T`. Поэтому поиск `OverrideBootstrapAttribute` может
> выбрать неподходящий атрибутированный метод. До исправления не размещайте на Bootstrap
> лишние атрибутированные методы либо исправьте фильтр на `GetCustomAttribute<T>()`.

## InvokePartialAttribute

Позволяет собрать результаты нескольких instance-методов одного объекта:

```csharp
public partial class LootSource
{
    [InvokePartial(order: 10)]
    private IEnumerable<Item> CollectCommonLoot(int level)
    {
        return commonItems;
    }

    [InvokePartial(order: 20)]
    private Item CollectBonusLoot(int level)
    {
        return bonusItem;
    }
}

IEnumerable<Item> loot = source.CollectPartialResult<Item>(level);
```

Подходящими считаются методы:

- с атрибутом `InvokePartial`;
- с точным совпадением типов параметров;
- возвращающие `T`, `T[]` или `IEnumerable<T>`.

Результаты объединяются по возрастанию `Order`.

Ограничения текущей реализации:

- `null` нельзя передать как параметр: для определения типа вызывается `GetType()`;
- совместимые базовые типы не учитываются — требуется точное совпадение;
- метод, вернувший `null`, может привести к ошибке в диагностической ветке;
- поиск выполняется reflection при каждом вызове и не кэшируется.

## DisableMethodsAttribute

Отключает поддерживаемые callback'и для всего класса и его наследников:

```csharp
[DisableMethods("OnTriggerStay", "OnCollisionStay")]
public class SensorWithoutStay : PRMonoBehaviour
{
}
```

Проверка выполняется через `this.IsMethodDisabled(...)` внутри вызывающего кода.
В текущем `PRMonoBehaviour` она применяется к десяти именам:

```text
OnTriggerEnter      OnCollisionEnter     OnTriggerEnter2D
OnTriggerStay       OnCollisionStay      OnTriggerStay2D
OnTriggerExit       OnCollisionExit      OnTriggerExit2D
OnPauseStateChanged
```

Атрибут не отключает произвольный метод автоматически. Имя должно совпадать с тем,
которое конкретный вызывающий код передаёт в `IsMethodDisabled`. Указывается имя
Unity-метода, а не PR-хука: `"OnTriggerStay"`, не `"PROnTriggerStay"`.

Физические callback'и объявлены в `PRMonoBehaviour` как `private`, поэтому `nameof`
в наследнике для них недоступен (CS0122). Остаётся строковый литерал, и опечатка ничего
не отключит. Для `OnPauseStateChanged` (он `public virtual`) `nameof` работает.

### Наследование заменяет список

`Inherited = true` означает не объединение, а перекрытие: атрибут производного класса
полностью вытесняет список базового.

```csharp
[DisableMethods("OnTriggerStay", "OnCollisionStay")]
public class Base : PRMonoBehaviour { }

public class ChildA : Base { }                 // OnTriggerStay, OnCollisionStay

[DisableMethods("OnTriggerEnter")]
public class ChildB : Base { }                 // только OnTriggerEnter — Stay снова разрешены

[DisableMethods()]
public class ChildC : Base { }                 // блокировок нет вовсе
```

Пустой атрибут — штатный способ вернуть наследнику callback'и, отключённые базовым
классом. Обратная сторона: добавив атрибут в производный класс, легко незаметно включить
обратно всё остальное.

Атрибут читается с фактического типа экземпляра и кешируется до перезагрузки домена.
Поэтому включить или снять блокировку для отдельного объекта в рантайме нельзя — только
для типа целиком.

## InlineAssetAttribute

Атрибут раскрывает поля ссылочного ассета прямо в инспекторе компонента, без поиска
файла в Project:

```csharp
[field: SerializeField, InlineAsset] public PlayerStats Stats { get; protected set; }
```

Треугольник у подписи поля сворачивает и разворачивает поля ассета в рамке. Правки идут
в сам ассет и попадают в Undo.

Поля читаются у **присвоенного экземпляра**, а не у типа поля:

- поле, объявленное базовым типом, показывает поля наследника, который в нём лежит;
- атрибут на поле базового класса работает во всех наследниках компонента — например,
  `Stats` объявлен в `PlayerControllerCoreBase`, а виден в инспекторе у `PlayerControllerCore`.

Аргументы: `expanded` — раскрыть сразу (по умолчанию свёрнуто), `showAssetPath` — строка
с путём к ассету под полями (по умолчанию есть). Путь напоминает, что данные общие:
правка меняет их всем, кто ссылается на тот же ассет.

При множественном выделении блок полей не рисуется: ассеты у объектов разные.
Вложенность ограничена тремя уровнями: ассет со своим `[InlineAsset]` внутри раскрывается,
взаимные ссылки не зацикливаются.

## ReferenceSelectorAttribute

Unity умеет сериализовать в поле `[SerializeReference]` экземпляр произвольного класса,
но выбрать тип в инспекторе не даёт — поле остаётся пустым. Атрибут добавляет выпадающий
список подходящих реализаций и рисует их поля тут же:

```csharp
[SerializeReference, ReferenceSelector] private IAction action;
[SerializeReference, ReferenceSelector] private List<IAction> actions;
```

Настройка хранится внутри объекта-владельца, отдельный ассет не нужен. Основное
применение — [встроенные действия](../@Actions/README.md#встроенное-действие).

Реализация попадает в список, если она:

- не абстрактная, не интерфейс и не открытый generic;
- помечена `[Serializable]`;
- имеет публичный конструктор без параметров;
- **не** наследует `UnityEngine.Object`.

Последнее — ограничение самого Unity: ScriptableObject и MonoBehaviour сериализуются
ссылкой на ассет или компонент и в `[SerializeReference]` не попадают. Для них нужно
обычное поле, как двумя списками в `ActionRunner`.

Список реализаций кэшируется до перезагрузки домена. Параметр `showFullName: true`
выводит типы с namespace — полезно, когда одинаковые имена встречаются в разных модулях.

Ограничения, о которых стоит помнить:

- переименование класса или смена namespace теряют сохранённую ссылку; штатное лечение —
  атрибут `[MovedFrom]` из `UnityEngine.Scripting.APIUpdating`;
- поле, у которого тип был удалён из проекта, выглядит так же, как незаполненное;
- значения полей не переносятся при смене типа — выбор реализации создаёт новый экземпляр.

## Inspector-атрибуты

### SpritePreviewAttribute

Добавляет preview для сериализованного Sprite:

```csharp
[SerializeField, SpritePreview(140f)]
private Sprite icon;
```

### PrefabPreviewAttribute

Добавляет preview для ссылки на prefab:

```csharp
[SerializeField, PrefabPreview(140f)]
private GameObject prefab;
```

Параметр конструктора задаёт высоту preview в пикселях. Отрисовка выполняется
соответствующими `PropertyDrawer` в папке `Core/Editor` и доступна только в Unity Editor.

## VirtualAttributeAttribute

Хранит имя property, тип атрибута и параметры для виртуального добавления metadata:

```csharp
[VirtualAttribute("Icon", typeof(SpritePreviewAttribute), 120f)]
public class ItemDefinition
{
}
```

`VirtualAttributeProcessor<T>` предоставляет общий механизм обработки атрибутов, но сам
VirtualAttributeAttribute в нём не считывается. Атрибут экспериментальный: без конкретного
editor processor он не меняет Inspector.

## Производительность и безопасность

- Поиск hook и override-методов пока не кэшируется.
- Ошибка внутри hook вызывается через reflection и может остановить текущую стадию.
- Сигнатуры hook-методов не валидируются заранее.
- Reflection ищет методы текущего runtime-типа; поведение private-методов в иерархии
  наследования следует проверять отдельно.
- Для критического порядка используйте уникальные значения `Order` и небольшие шаги между
  ними, чтобы интеграции могли вставить собственный этап.

## Связанная документация

- [SDK](../SDK/README.md)
- [PRMonoBehaviour](../PRMonoBehaviour/README.md)
- [PauseSystem](../PauseSystem/README.md)
- [YG2 Integration](../../YG2.Integration/README.md)
