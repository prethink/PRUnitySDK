# Менеджеры PRUnitySDK

Папка содержит runtime-менеджеры ядра и контейнер, который публикует их через `PRUnitySDK.Managers`. Контейнер создаётся на этапе инициализации SDK, формирует объект `Managers` и запускает методы с `[MethodHook(MethodHookStage.PostOperation, priority)]` в порядке приоритета.

## Состав

| Менеджер | Доступ | Назначение |
| --- | --- | --- |
| [GameManager](GameManager/README.md) | `PRUnitySDK.Managers.Game` | загрузка `ProjectData` и `GameSettings`, сохранение, autosave, сигнал готовности |
| [ProjectPropertiesManager](ProjectPropertiesManager/README.md) | `PRUnitySDK.Managers.ProjectProperties` | типизированные произвольные свойства проекта |
| [ResourceManager](../Items/Resources/README.md) | `PRUnitySDK.Managers.Resource` | числовые игровые ресурсы, изменение баланса и безопасное списание |
| [SoundManager](SoundManager/README.md) | `PRUnitySDK.Managers.Sound` | музыка, UI-звуки, 2D/3D-эффекты и звуковые категории |
| `AudioMixerManager` | `PRUnitySDK.Managers.AudioMixer` | пользовательский и системный mute; находится в модуле `SoundManager` |
| [OpenedItemsManager](OpenedItemsManager/README.md) | `PRUnitySDK.Managers.OpenedItems` | что открыто и сколько его есть: `ProjectData.OpenedItems`, с делением по видам |
| [CursorManager](CursorManager/README.md) | `CursorManager.Instance` | конкурирующие запросы состояния системного курсора |
| [PRManagerContainer](PRManagerContainer/README.md) | `PRUnitySDK.Managers` | создание, порядок и расширение набора менеджеров |
| [BoosterManager](../../../PRUnitySDKPrivate/Modules/Boosters/README.md) | `PRUnitySDK.Managers.Booster` | private-модуль временных множителей ресурсов |
| [VipManager](../../../PRUnitySDKPrivate/Modules/VIP/README.md) | `PRUnitySDK.Managers.Vip` | private-модуль временного VIP-статуса |

Контейнер также публикует менеджеры из соседних подсистем: `ObjectPool` и `Flags`. Модули могут добавлять новые поля и инициализаторы через partial-файлы `PRManagerContainer`.

## Жизненный цикл и доступ

Не используйте поля `PRUnitySDK.Managers` до завершения соответствующего hook инициализации. Для кода, которому нужны загруженные данные игрока, ориентиром служит `PRUnitySDK.Managers.Game.ReadySignal`, а не только наличие ссылки на `GameManager`.

```csharp
PRUnitySDK.Managers.Game.ReadySignal.SubscribeOnReady(() =>
{
    long coins = PRUnitySDK.Managers.ProjectProperties.GetLong("Coins");
});
```

`CursorManager` — обычный C# singleton и намеренно не зарегистрирован полем контейнера. Остальные менеджеры могут быть как обычными singleton-объектами, так и `MonoBehaviour`, созданными фабриками из `Resources`.

## Добавление менеджера

Расширяйте `PRManagerContainer` отдельным partial-файлом рядом с модулем. Выберите свободный приоритет, создайте экземпляр через принятую factory/singleton-схему и верните его из `PRUnitySDK.InitializeManager(...)`. Для `MonoBehaviour` используйте `InitializeMonoManager`, чтобы объект стал дочерним для runtime-контейнера `Managers`.

```csharp
public partial class PRManagerContainer
{
    public ExampleManager Example { get; private set; }

    [MethodHook(MethodHookStage.PostOperation, 120)]
    private void InitializeExampleManager()
    {
        InitializeMonoManager(() =>
        {
            Example = new ExampleManagerFactory().Create();
            return Example;
        });
    }
}
```

Не добавляйте второй singleton, если жизненным циклом объекта уже владеет контейнер, и не вызывайте hook-инициализатор вручную.
