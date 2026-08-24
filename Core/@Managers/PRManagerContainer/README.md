# PRManagerContainer

`PRManagerContainer` — partial-контейнер runtime-менеджеров PRUnitySDK. Единственный экземпляр создаётся статически в `PRUnitySDK.Managers`, а `Initialize()` создаёт `PRContainer` с именем `Managers` и выполняет hook-инициализаторы.

## Базовые менеджеры

| Приоритет | Поле | Инициализация |
| ---: | --- | --- |
| 10 | `Game` | `GameManager.Instance`, затем `InitializeGameManager()` |
| 20 | `ProjectProperties` | `ProjectPropertiesManager.Instance` |
| 20 | `Resource` | `ResourceManager.Instance` |
| 20 | `AudioMixer` | prefab `Resources/PRUnitySDK/Prefabs/AudioMixer` |
| 30 | `Sound` | prefab `Resources/PRUnitySDK/Prefabs/SoundManager`, затем регистрация в `AudioMixer` |
| 35 | `ObjectPool` | `ObjectPoolManagerFactory` |
| 40 | `OpenedItems` | `OpenedItemsManager.Instance` |
| 50 | `Flags` | `FlagsManager.Instance` |

Одинаковый приоритет не должен использоваться как гарантия взаимного порядка. Если новый менеджер зависит от другого, задайте больший приоритет явно.

## InitializeMonoManager

`InitializeMonoManager<T>(Func<T>)` оборачивает создание в `PRUnitySDK.InitializeManager(...)` и после создания делает компонент дочерним объектом `ManagerContainer`. Фабрика должна вернуть уже созданный `MonoBehaviour`.

```csharp
InitializeMonoManager(() =>
{
    Example = new ExampleManagerFactory().Create();
    return Example;
});
```

Обычные C# singleton-менеджеры регистрируются напрямую:

```csharp
PRUnitySDK.InitializeManager(() =>
{
    Example = ExampleManager.Instance;
    return Example;
});
```

## Расширение модулем

Не редактируйте центральный список для модульного менеджера. Создайте рядом с модулем partial-файл по образцу `Modules/@ProgressionModule/PRManagerContainer.XPManager.cs`:

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

Зависите от общего контракта, когда менеджер имеет сменные реализации, не создавайте второй singleton и не вызывайте initializer вручную: стадия `PostOperation` запускается контейнером.

## Готовность

Поле контейнера назначается во время соответствующего hook. Отдельный менеджер может иметь дополнительную асинхронную готовность: главный пример — `GameManager.ReadySignal`. Поэтому «поле не null» и «данные менеджера готовы» не всегда одно и то же.
