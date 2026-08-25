# PRUnitySDK

Набор переиспользуемых систем и базовых классов для разработки игр на Unity.
SDK предоставляет общий жизненный цикл компонентов, инициализацию через bootstrap-сцену,
события, паузу, игровое время, состояния, сущности и вспомогательные runtime-инструменты.

> [!WARNING]
> Проект находится в активной разработке. Публичные API, структура каталогов и процесс
> инициализации могут изменяться. Перед обновлением SDK фиксируйте используемый commit.

## Возможности

| Система | Назначение |
| --- | --- |
| `PRUnitySDK` | Центральная точка доступа к настройкам, менеджерам, трекерам и сервисам SDK |
| `Bootstrap` | Единая точка запуска и последовательная инициализация SDK |
| `PRMonoBehaviour` | Базовый Unity-компонент с PR lifecycle, поддержкой паузы и физическими callback'ами |
| `EventBus` | Глобальная типизированная шина событий с защитой от повторной подписки |
| `PauseManager` | Раздельное управление общей, логической, музыкальной и другими видами паузы |
| `PRTime` / `PRTimeScale` | Игровое и реальное время с учётом логической паузы и пользовательских time scale |
| `PRCoroutineBase` | Обёртки над Unity-корутинами с единым запуском и остановкой |
| `HookSystem` | Последовательная обработка изменяемых и отменяемых событий |
| [`FlagsSystem`](Core/FlagsSystem/README.md) | Совместное управление состояниями объекта из нескольких источников |
| Entity / Items / Wallet / Reward | Базовые модели сущностей, предметов, ресурсов и наград |
| State / Progression / Damage | Переиспользуемые игровые модули |
| Quality / Localization / Logging | Качество предметов, переводы и структурированное логирование |

## Требования

- Unity 2022.3 LTS или новее;
- [Newtonsoft Json for Unity](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@latest);
- [DOTween](https://dotween.demigiant.com/) для tween-расширений и модуля `DOTweenEffects`.

Некоторые интеграции имеют дополнительные зависимости:

- `YG2.Integration` требует YG2 Plugin и соответствующие модули YG2;
- `Core.Zenject.Integration~` требует Zenject и по умолчанию исключён из компиляции Unity.

## Установка

Репозиторий пока не оформлен как UPM-пакет: в корне отсутствует `package.json`.
Размещайте содержимое репозитория внутри `Assets/PRUnitySDK`.

Через Git submodule:

```bash
git submodule add https://github.com/prethink/PRUnitySDK.git Assets/PRUnitySDK
git submodule update --init --recursive
```

Либо скачайте репозиторий и скопируйте его содержимое в:

```text
Assets/PRUnitySDK
```

После импорта убедитесь, что Unity завершил компиляцию без ошибок и установлены
перечисленные выше зависимости.

## Быстрый старт

### 1. Подготовьте bootstrap-сцену

1. Создайте отдельную сцену и добавьте её первой в `File → Build Settings`.
2. Поместите в неё GameObject с компонентом `Bootstrap`.
3. Добавьте основную игровую сцену следующей, с build index `1`.

В текущей реализации после завершения инициализации `Bootstrap` вызывает переход на
сцену с индексом `1`. В Unity Editor класс `PlayFromBootstrap` автоматически назначает
сцену с индексом `0` стартовой при входе в Play Mode.

> [!IMPORTANT]
> Если проект использует другую схему загрузки сцен, измените обработчик
> `Bootstrap.OnInitialized()` или переопределите bootstrap-процесс через
> `OverrideBootstrapAttribute`.

### 2. Используйте PR lifecycle

```csharp
using UnityEngine;

public class RotatingObject : PRMonoBehaviour
{
    [SerializeField] private float speed = 90f;

    protected override void PRUpdate()
    {
        transform.Rotate(Vector3.up, speed * PRTime.Instance.GameDeltaTime);
    }
}
```

`PRUpdate`, `PRLateUpdate` и `PRFixedUpdate` не выполняются во время логической паузы.
Для временных расчётов используйте `PRTime`, если система должна следовать правилам
паузы SDK.

### 3. Подпишитесь на глобальное событие

Событие описывается интерфейсом:

```csharp
public interface ICoinsChanged : IGlobalSubscriber
{
    void OnCoinsChanged(int value);
}
```

Подписчик реализует интерфейс и регистрируется в `EventBus`:

```csharp
public class CoinsView : MonoBehaviour, ICoinsChanged
{
    private void OnEnable() => EventBus.Subscribe(this);
    private void OnDisable() => EventBus.Unsubscribe(this);

    public void OnCoinsChanged(int value)
    {
        Debug.Log($"Coins: {value}");
    }
}
```

Вызов события:

```csharp
EventBus.RaiseEvent<ICoinsChanged>(listener => listener.OnCoinsChanged(10));
```

Компоненты, наследуемые от `PRMonoBehaviour`, автоматически регистрируются в
`EventBus` при стандартной инициализации и снимаются с регистрации при уничтожении.

### 4. Управляйте логической паузой

```csharp
PRUnitySDK.PauseManager.SetLogicPaused(true, this);
PRUnitySDK.PauseManager.SetLogicPaused(false, this);
```

Состояние доступно через:

```csharp
bool isPaused = PRUnitySDK.PauseManager.IsLogicPaused;
```

### 5. Запустите PR-корутину

```csharp
var delay = new WaitGameSecondsCoroutine(
    callback: () => Debug.Log("Completed"),
    duration: 2f,
    instance: this);

delay.Execute();
```

Если `MonoBehaviour` не передан, корутина запускается на глобальном
`PRMonoBehaviourHost`. Для повторного запуска одного объекта корутины используйте
`StopAndExecute()`, а для остановки — `Stop()`.

## Инициализация SDK

Основная последовательность запуска:

1. `Bootstrap.Awake()` вызывает `PRUnitySDK.InitializeSDK()`.
2. Инициализируются правила, конвертеры, singleton-сервисы и фабрики.
3. Через method hooks подключаются дополнительные модули.
4. Инициализируются контейнеры менеджеров и окон.
5. Устанавливается `PRUnitySDK.IsInitialized`.
6. Публикуется `ISDKEvents.OnInitialized()` и устанавливается `ReadySignal`.

Повторный параллельный запуск блокируется флагом `PRUnitySDK.IsStartInitialize`.
Готовность можно проверить через `PRUnitySDK.IsInitialized` или
`PRUnitySDK.ReadySignal`.

## Структура репозитория

```text
PRUnitySDK/
├── Core/                       # Ядро, сервисы, базовые модели и инструменты
├── Modules/                    # Опциональные игровые модули
├── Examples/                   # Примеры использования
├── Resources/                  # Runtime-ресурсы SDK
├── Utils/                      # Дополнительные утилиты
├── YG2.Integration/            # Интеграция с YG2
└── Core.Zenject.Integration~/  # Отключённая по умолчанию интеграция с Zenject
```

## Дополнительная документация

### Ядро и расширение SDK

- [SDK](Core/SDK/README.md) — facade, инициализация и service resolver
- [ResourcePaths](Core/ResourcePaths/README.md) — канонические пути к runtime-ресурсам и правила расширения
- [Окна Database и Settings](Core/Editor/README.md) — секции, поиск, заполнение каталогов и валидация definitions
- [Attributes](Core/@Attributes/README.md) — method hooks, переопределение сервисов и расширение Inspector
- [Actions](Core/@Actions/README.md) — переиспользуемые действия с единым контрактом проверки и выполнения
- [EventBus](Core/@Events/EventBus/README.md) — типизированная шина уведомлений о произошедшем
- [HookSystem](Core/HookSystem/README.md) — перехват действий с возможностью изменить или запретить их
- [FlagsSystem](Core/FlagsSystem/README.md) — согласование независимых решений компонентов без прямых зависимостей

### Жизненный цикл и время

- [PRMonoBehaviour](Core/PRMonoBehaviour/README.md) — базовый компонент с lifecycle-хуками, учитывающими логическую паузу
- [Coroutines](Core/Coroutines/README.md) — обёртки над корутинами с запуском, перезапуском и остановкой
- [Yields](Core/Yields/README.md) — `CustomYieldInstruction`, останавливающие корутины на логической паузе
- [PauseSystem](Core/PauseSystem/README.md) — раздельные причины паузы и мониторы аниматоров и физических тел
- [PRTime](Core/PRTime/README.md) — источник времени: реальное, игровое и учёт паузы
- [PRTimeScale](Core/PRTimeScale/README.md) — слои скорости времени, модификаторы с владельцами и драйверы для анимации и физики

### Менеджеры

- [Обзор менеджеров](Core/@Managers/README.md) — доступ, жизненный цикл, порядок и расширение контейнера
- [GameManager](Core/@Managers/GameManager/README.md) — загрузка и сохранение `ProjectData`/`GameSettings`, autosave и готовность данных
- [ProjectPropertiesManager](Core/@Managers/ProjectPropertiesManager/README.md) — свойства `long`, `float`, `DateTime`, `string` и `bool`
- [ResourceManager](Core/Items/Resources/README.md) — игровые ресурсы, баланс, списание и события изменений
- [OpenedItemsManager](Core/@Managers/OpenedItemsManager/README.md) — открытые предметы и количество в `ProjectData`
- [PRManagerContainer](Core/@Managers/PRManagerContainer/README.md) — hook-порядок создания runtime-менеджеров
- [SoundManager](Core/@Managers/SoundManager/README.md) — музыка, UI-звуки, позиционные эффекты и наборы `AudioSet`
- [CursorManager](Core/@Managers/CursorManager/README.md) — запросы Show/Hide курсора с приоритетом последнего обращения

### Модели, сервисы и утилиты

- [Фабрики MonoBehaviour](Core/Factories/README.md) — обычные prefab, singleton-компоненты, MonoWindow и Notifier
- [Trackers](Core/Trackers/README.md) — игроки, сущности, камеры и UI-реестры
- [MonoWindow](Core/%23UI/MonoWindow/README.md) — модальные runtime-окна, фабрики и параметры открытия
- [Reward](Core/Reward/README.md) — модели наград, экземплярный сервис выдачи и проектные обработчики
- [Enumeration](Core/Models/Enumeration/README.md) — расширяемый строковый идентификатор вместо `enum`
- [Services](Core/Services/README.md) — `NameService` и сервис имени текущего игрока
- [GameDataStorage](Core/GameDataStorage/README.md) — storage-контракты и универсальный `ProjectDataMap`
- [Utils](Core/Utils/README.md) — вспомогательные классы: время, отложенные вызовы, имена
- [Proxies](Core/Proxies/README.md) — переадресация Unity-callback'ов с дочерних объектов родительским компонентам
- [Property modifiers](Core/PropertyContainer/README.md) — динамические характеристики, персональные модификаторы и `GameRules`.


### Модули и интеграции

- [Modules](Modules/README.md) — опциональные игровые модули: `StateManager`, `XPManager`
- [DOTweenEffects](Modules/DOTweenEffects/README.md) — связь DOTween с логической паузой и `PRTimeScale`
- [YG2 Integration](YG2.Integration/README.md) — облачные сохранения, реклама и платформенные возможности Яндекс Игр
- [Zenject Integration](Core.Zenject.Integration~/README.md) — отключена по умолчанию; папку включают удалением `~` из имени

## Текущие ограничения

- SDK распространяется как Unity Assets, а не как UPM-пакет.
- Автоматический installer пока не создаёт настройки, слои, теги и prefab'ы.
- Bootstrap по умолчанию предполагает сцены с индексами `0` и `1`.
- Часть каталогов и API всё ещё находится в процессе переноса и рефакторинга.
- Не все модули имеют отдельную документацию и тестовое покрытие.

## Репозиторий

[github.com/prethink/PRUnitySDK](https://github.com/prethink/PRUnitySDK)
