# PRUnitySDK — рабочий контекст для агентов

Этот файл действует для публичного SDK в `Assets/PRUnitySDK`. Код здесь должен оставаться пригодным для повторного использования в других Unity-проектах и не должен зависеть от `PRUnitySDKPrivate`.

## Правила работы

- Это Unity-проект. Не запускайте `dotnet build`; проверяйте компиляцию по свежему `%LOCALAPPDATA%/Unity/Editor/Editor.log`.
- Не утверждайте, что Unity пересобрала код, если время изменения лога не обновилось.
- Сохраняйте существующие `.meta`, GUID, prefab и ScriptableObject-ссылки.
- Не изменяйте сторонний plugin ради локального поведения окна SDK, если проблему можно изолировать в собственном Editor.
- Перед изменением сериализованного типа оцените миграцию. Добавление нового поля часто безопаснее замены generic-типа старой коллекции.
- `FormerlySerializedAs` добавляйте только когда обратная совместимость действительно требуется.
- Новый русский текст сохраняйте в UTF-8. Не копируйте mojibake из старых файлов.
- Каждый top-level класс и интерфейс создавайте в отдельном `.cs`-файле. Имя файла должно совпадать с именем основного типа.
- Для наследников `MonoBehaviour` и `ScriptableObject` отдельный файл обязателен: Unity связывает MonoScript, компоненты, `CreateAssetMenu` и сериализованные assets с конкретным типом и именем файла.
- Не размещайте несколько `MonoBehaviour`, `ScriptableObject`, публичных интерфейсов или других самостоятельных типов в одном файле ради удобства. Вложенными могут оставаться только небольшие private-типы, являющиеся деталью реализации владельца.
- Публичные свойства и неоднозначная логика должны иметь многострочные XML-комментарии:

```csharp
/// <summary>
/// Описание.
/// </summary>
```

- Сначала ищите существующие interfaces, extensions, factories, services и enumeration providers.
- Изменение публичного API сопровождайте обновлением README соответствующего модуля.
- Перед изменением runtime-менеджера прочитайте `Core/@Managers/README.md`, README конкретного менеджера и все partial-файлы `PRManagerContainer`, которые могут задавать его порядок или зависимости.

## Границы публичного SDK

- Общие контракты и переиспользуемая реализация находятся здесь.
- Проектные сущности, Yandex-адаптеры, конкретные brainrots, pets и game-specific UI находятся в `PRUnitySDKPrivate`.
- Публичный SDK не должен ссылаться на private-типы. Private-слой может расширять публичные partial-классы.
- `NaughtyAttributes` принадлежит private SDK. В публичном SDK используйте стандартные Unity-атрибуты и Editor API.
- Старый Zenject-код может использоваться как источник поведения, но текущий SDK не должен вновь зависеть от `[Inject]`.

## Как добавлять новый модуль

Новый модуль обычно размещается в `Modules/<ModuleName>`. Интеграция выполняется небольшими partial-файлами рядом с модулем, а не редактированием одного центрального списка.

Определите тип интеграции:

- самостоятельный сервис — partial `PRUnitySDK` и `MethodHookStage.SDK`;
- runtime-менеджер — partial `PRManagerContainer` и обычно `MethodHookStage.PostOperation`;
- MonoWindow — partial `PRWindowsContainer`, factory, prefab и ключ окна;
- каталог definitions — partial `PRSDKDatabase`;
- конфигурация — partial `PRSDKSettings`;
- сохраняемые данные — partial ProjectData с существующими стадиями `Cloning` и `Initializing`.

### Сервис через partial PRUnitySDK

Сначала создайте интерфейс. Потребители должны зависеть от общего контракта, а не от платформенной реализации.

```csharp
public partial class PRUnitySDK
{
    private const int ExampleSystemPriority = 80;

    /// <summary>
    /// Сервис Example.
    /// </summary>
    public static IExampleSystem ExampleSystem;

    [MethodHook(MethodHookStage.SDK, ExampleSystemPriority)]
    private static void InitializeExampleSystem()
    {
        InitializeModuleSDK(nameof(IExampleSystem), () =>
        {
            typeof(PRUnitySDK).TryOverrideStaticProperty(typeof(IExampleSystem));

            InitializeDefault(
                nameof(ExampleSystem),
                () => ExampleSystem,
                () => ExampleSystem = new ExampleSystem());

            return ExampleSystem;
        });
    }
}
```

`InitializeModuleSDK` защищает от повторной инициализации и регистрирует результат в resolver. Дополнительный специализированный интерфейс можно зарегистрировать отдельно через `RegisterService<T>()`.

Меньший priority выполняется раньше. Проверьте hooks той же стадии и ставьте зависимый модуль после его зависимостей. Не вызывайте initializer вручную из `InitializeSDK()` — стадия `SDK` запускается автоматически.

### Менеджер через partial PRManagerContainer

`PRUnitySDK.Managers` — единственный контейнер runtime-менеджеров. Менеджер ядра находится в `Core/@Managers/<ManagerName>`, а менеджер отдельного модуля и его partial-интеграция `PRManagerContainer.<ManagerName>.cs` — рядом с модулем. Центральный `Core/@Managers/PRManagerContainer/PRManagerContainer.cs` изменяйте только для действительно базового менеджера ядра.

Перед добавлением или изменением менеджера определите модель владения:

- обычный C# singleton — контейнер возвращает существующий `Instance` из `PRUnitySDK.InitializeManager(...)`;
- `MonoBehaviour` — контейнер создаёт его через принятую factory base и `InitializeMonoManager`, который регистрирует тип и помещает объект под runtime-контейнер `Managers`;
- сменная реализация — публичное поле/свойство и потребители зависят от интерфейса, а созданный экземпляр дополнительно регистрируется через `PRUnitySDK.RegisterService(...)`;
- самостоятельный SDK-сервис без GameObject и manager-семантики регистрируется через partial `PRUnitySDK`, а не маскируется под менеджер.

Модульный `MonoBehaviour`-менеджер:

```csharp
public partial class PRManagerContainer
{
    public IExampleManager ExampleManager { get; private set; }

    [MethodHook(MethodHookStage.PostOperation, 120)]
    private void InitializeExampleManager()
    {
        InitializeMonoManager(() =>
        {
            var instance = new ExampleManagerFactory().Create();
            ExampleManager = instance;
            PRUnitySDK.RegisterService(ExampleManager);
            return instance;
        });
    }
}
```

Обычный singleton-менеджер:

```csharp
public partial class PRManagerContainer
{
    public ExampleManager Example { get; private set; }

    [MethodHook(MethodHookStage.PostOperation, 120)]
    private void InitializeExampleManager()
    {
        PRUnitySDK.InitializeManager(() =>
        {
            Example = ExampleManager.Instance;
            Example.Initialize();
            return Example;
        });
    }
}
```

Правила инициализации:

- runtime-менеджеры инициализируются на `MethodHookStage.PostOperation`, потому что `ManagerContainer` создаётся между `PreOperation` и `PostOperation`;
- меньший priority выполняется раньше; проверьте hooks во всех partial-файлах, а не только в `Core/@Managers`;
- одинаковый priority не является контрактом взаимного порядка; зависимому менеджеру задайте явно больший priority;
- initializer должен быть идемпотентным через `PRUnitySDK.InitializeManager(...)` или собственную доказанную защиту и не должен вызываться вручную;
- наличие ссылки в `PRUnitySDK.Managers` не всегда означает готовность данных. Если менеджер предоставляет `ReadySignal`, потребители ждут его;
- не читайте `ProjectData`, `GameSettings` и зависящие от storage данные до `GameManager.ReadySignal`;
- подписки `EventBus`, корутины и другие долгоживущие ресурсы должны иметь симметричное освобождение в подходящем lifecycle-методе.

Правила фабрик и prefab:

- используйте существующую factory base (`SingletonMonoBehaviourFactoryBase<T>` или ближайший аналог) и canonical `PRUnitySDK.ResourcePaths`;
- prefab менеджера храните в принятом `Resources/PRUnitySDK/Prefabs/...` пути и проверяйте обязательные компоненты и сериализованные ссылки;
- не создавайте второй singleton и не обходите factory прямым `new GameObject`, если жизненным циклом уже владеет контейнер;
- после перемещения менеджера убедитесь, что старый `.cs` больше не компилируется параллельно: дубликат типа в старом и новом пути даёт `CS0101`/`CS0111`.

Документация менеджеров:

- главный индекс: `Core/@Managers/README.md`;
- локальная документация: `Core/@Managers/<ManagerName>/README.md` либо README модуля для module-owned менеджера;
- при добавлении, удалении, переименовании, изменении доступа, порядка, readiness или публичного API обновляйте главный индекс и локальный README вместе;
- документируйте фактическое поведение кода и отдельно отмечайте временные ограничения/TODO; не описывайте зарезервированный параметр или закомментированную интеграцию как работающую.

### MonoWindow через partial PRWindowsContainer

Нужны:

1. Класс окна на основе подходящего `MonoWindowBase`.
2. Args, если окно принимает данные.
3. Factory и prefab в `Resources/PRUnitySDK/Prefabs/Windows/...`.
4. Уникальный ключ в partial `MonoWindowKeyEnumerationProvider`.
5. Partial-регистрация:

```csharp
public partial class PRWindowsContainer
{
    public ExampleWindow ExampleWindow { get; private set; }

    [MethodHook(MethodHookStage.PostOperation, 120)]
    private void InitializeExampleWindow()
    {
        ExampleWindow = new ExampleWindowFactory().CreateMonoWindow();
    }
}
```

Не добавляйте модульное окно напрямую в core `PRWindowsContainer.cs`.

## PRSDKSettings

`PRSDKSettings` — partial `ScriptableObjectSingleton`. Доступ: `PRUnitySDK.Settings`. Asset: `Assets/PRUnitySDK/Resources/PRUnitySDK/PRSDKSettings.asset`.

```csharp
using System;
using UnityEngine;

public partial class PRSDKSettings
{
    /// <summary>
    /// Настройки Example.
    /// </summary>
    [field: SerializeField]
    public ExampleSettings Example { get; private set; } = new();
}

[Serializable]
public sealed class ExampleSettings
{
    [field: SerializeField, Min(0)]
    public int Limit { get; private set; } = 10;
}
```

Использование: `PRUnitySDK.Settings.Example.Limit`.

- Вложенный settings-класс должен быть `[Serializable]`.
- Задавайте безопасные defaults, `Tooltip`, `Min`/`Range`.
- Настройки являются конфигурацией, не сохранением прогресса.
- После добавления поля откройте asset и проверьте сериализацию.
- Не создавайте отдельный singleton для каждого небольшого модуля.

## PRSDKDatabase

`PRSDKDatabase` — partial `ScriptableObjectSingleton`. Доступ: `PRUnitySDK.Database`. Asset: `Assets/PRUnitySDK/Resources/PRUnitySDK/PRSDKDatabase.asset`.

Для каталога definitions используйте `Database<T>`:

```csharp
using System;
using UnityEngine;

public partial class PRSDKDatabase
{
    /// <summary>
    /// Definitions Example.
    /// </summary>
    [field: SerializeField]
    public ExampleDatabase Examples { get; protected set; } = new();
}

[Serializable]
public sealed class ExampleDatabase : Database<ExampleDefinition>
{
    public static ExampleDatabase Instance => PRUnitySDK.Database.Examples;
}
```

Использование: `PRUnitySDK.Database.Examples.Data`.

Если нужны разные коллекции, создайте отдельный `[Serializable]` database-класс с приватными `[SerializeField]` списками и read-only API по образцу `RewardDatabase`.

- Database хранит definitions/config assets, но не изменяемый прогресс игрока.
- Возвращайте `IReadOnlyList`, `IReadOnlyCollection` или `IEnumerable`, а не изменяемый список.
- Ищите definition по стабильному Id, а не по локализованному имени.
- Валидируйте `null`, повторяющиеся Id/ключи и повреждённые ссылки в Editor.
- Общие правила находятся в `Database<T>.Validate()`. Конкретная база дополняет их через override, сначала возвращая результаты `base.Validate()`.
- Видимость действий окна Database задаётся `[DatabaseEditorOptions]` на классе базы; без атрибута используются включённые defaults.
- `DatabaseEditorPresentation.Auto` показывает наследников `ItemDefinitionBase` сеткой по `IIconProvider`/`INameProvider`; справа редактируется выбранный asset. `Default` оставляет обычный список, `Grid` включает каталог явно.
- После изменения проверяйте asset и сериализацию после перезапуска Unity.

Partial-поля Settings и Database не требуют ручного добавления в центральные файлы. Исходник должен входить в runtime assembly и не находиться в папке `Editor`.

## EventBus и данные игрока

- Для межмодульных уведомлений предпочитайте EventBus прямым зависимостям.
- Событие игрока должно содержать стабильный player id или конкретного исполнителя для split-screen.
- Не связывайте core с конкретным UI, если достаточно события или интерфейса.
- Изменяемое состояние храните в ProjectData/Properties, а не в settings/database definitions.

## Характеристики и PropertyContainer

- Итоговая характеристика: `EntityStatsBase` → modifiers → `GameRules`.
- Используйте `EntityStatsUtils.GetStat`/`GetStatInt`, не обходите цепочку прямым чтением.
- Система должна поддерживать float-значения (скорость) и целочисленные значения (прыжки).
- Учитывайте `IStatModifiersProvider` и `IStatModifierProvider`.
- Property modifiers и game rules должны накладываться предсказуемо и документироваться в `Core/PropertyContainer/README.md`.

## Rewards, веса и кейсы

- `RewardItem` оборачивает `ItemDefinitionBase`; `RewardResource` хранит ресурс и количество; `RewardAction` выполняет action.
- `RewardContainerBase` — общая основа контейнеров.
- `RewardGrantService` — экземплярный SDK-сервис `PRUnitySDK.RewardGrantService`. Он выдаёт общие типы через `IRewardGrantHandler` и публикует событие только после успеха.
- Проектные `RewardItem` обрабатываются private-обработчиками с приоритетом выше fallback `RewardItemGrantHandler`.
- `WeightUtils` должен корректно обрабатывать нулевые веса и большие суммы без переполнения.
- Конкретные кейсы, визуальная roll-сессия и Editor-генератор являются частью `PRUnitySDKPrivate`; публичный reward core не должен от них зависеть.

## Проверка

1. Проверьте свежие `error CS...` в Unity Editor.log.
2. Проверьте GUID и сериализованные ссылки.
3. Для публичного API обновите README.
4. Для событий/сервисов проверьте повторную инициализацию и освобождение subscriptions.
5. Для Editor-кода убедитесь, что он находится в `Editor` и не попадает в runtime/build.
6. Для менеджера проверьте уникальность типа, hook stage/priority, готовность зависимостей, factory/Resources-путь, родителя `ManagerContainer` и ссылки в `Core/@Managers/README.md`.
