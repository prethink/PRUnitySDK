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
| Inspector | `SpritePreviewAttribute`, `PrefabPreviewAttribute` | Preview сериализованных Unity-объектов |
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

По умолчанию hook вызывается без аргументов. Instance runner дополнительно умеет передавать
аргументы — это нужно, когда стадии требуется контекст, а не только факт её наступления:

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

- hook без параметров вызывается всегда, независимо от переданных аргументов — старые hook'и
  продолжают работать после того, как вызывающий код начал передавать контекст;
- hook, у которого число параметров совпадает с числом аргументов, получает их;
- при несовпадении hook пропускается с предупреждением в лог, а не роняет всю стадию.

Аргументы поддерживает только instance runner (`RunMethodHooks`); `RunStaticMethodHooks`
вызывает статические hook'и без аргументов.

### Кеширование

`ReflectionExtension` кеширует результат сканирования типа по паре (тип, стадия), поэтому
атрибуты читаются один раз, а не на каждом вызове. Это важно для стадий, которые
выполняются часто: `Cloning` и `Initializing` у `ProjectData` срабатывают на каждом
сохранении.

### Стандартные стадии

`MethodHookStage` включает несколько групп:

- Unity lifecycle: `PreAwake`, `PostAwake`, `PreStart`, `PostStart`, `PreOnEnable`,
  `PostOnEnable`, `PreOnDisable`, `PostOnDisable`;
- данные: `PreSave`, `Saving`, `PostSave`, `PreClone`, `Cloning`, `PostClone`;
- инициализация: `PreInitialize`, `Initializing`, `PostInitialize`, `ReadyProject`;
- SDK: `SDK`, `RegisterFactories`, `Converter`, `DefaultSettings`;
- операции: `PreOperation`, `PostOperation`, `CreateCollections`, `Custom`;
- интеграции: `InstallBindings`, `ZenjectConstruct`;
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

Рекомендуется хранить приоритет в именованной константе. Одинаковый `Order` не задаёт
надёжного взаимного порядка методов — если порядок важен, используйте разные значения.

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

Алгоритм сортирует подходящие override-методы по `Order`, но вызывает только первый.
Следовательно, меньшее значение имеет более высокий приоритет.

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

Проверка выполняется через `IsMethodDisabled(nameof(...))`. В текущем
`PRMonoBehaviour` она применяется к:

- 3D trigger callback'ам;
- 3D collision callback'ам;
- 2D trigger callback'ам;
- `OnPauseStateChanged`.

Атрибут не отключает произвольный метод автоматически. Имя должно совпадать с тем,
которое конкретный вызывающий код передаёт в `IsMethodDisabled`.

Атрибут наследуется, а список методов хранится на типе. Проверка использует reflection
при каждом вызове, что особенно важно учитывать для частых `Stay` callback'ов.

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

Параметр конструктора задаёт высоту preview в пикселях. Отрисовка реализована
соответствующими `PropertyDrawer` в папке `Core/Editor` и доступна только в Unity Editor.

## VirtualAttributeAttribute

Хранит имя property, тип атрибута и параметры для виртуального добавления metadata:

```csharp
[VirtualAttribute("Icon", typeof(SpritePreviewAttribute), 120f)]
public class ItemDefinition
{
}
```

Сейчас `VirtualAttributeProcessor<T>` предоставляет общий механизм обработки атрибутов,
но сам `VirtualAttributeAttribute` напрямую в нём не считывается. Атрибут следует считать
экспериментальным: без конкретного editor processor он не меняет Inspector.

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
- [Zenject Integration](../../Core.Zenject.Integration~/README.md)

