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
| [`BackgroundTasks`](Core/BackgroundTasks/README.md) | Работа по расписанию вне сцены и наблюдение за значениями, которые сами о себе не сообщают |
| `HookSystem` | Последовательная обработка изменяемых и отменяемых событий |
| [`FlagsSystem`](Core/FlagsSystem/README.md) | Совместное управление состояниями объекта из нескольких источников |
| [`Entity`](Core/@Entity/README.md) / Items / [`Wallet`](Core/Wallet/README.md) / [`Reward`](Core/Reward/README.md) | Базовые модели сущностей, предметов, ресурсов и наград |
| [`GameRules`](Core/GameRules/README.md) | Глобальные ограничения характеристик поверх персональных модификаторов |
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

Физические callback'и объявлены в базовом классе, поэтому Unity вызывает их у любого
наследника. Ненужные отключаются на уровне типа:

```csharp
[DisableMethods("OnTriggerStay", "OnCollisionStay")]
public class Pickup : PRMonoBehaviour { }
```

Атрибут действует только на физические callback'и и `OnPauseStateChanged`, имена
задаются строками, а у наследника список заменяется целиком, а не дополняется —
подробности в [PRMonoBehaviour](Core/PRMonoBehaviour/README.md#disablemethodsattribute--блокировка-callbackов).
Там же чек-лист «метод не вызывается — что проверить».

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

## Какой механизм расширения выбрать

В SDK шесть способов вклиниться в чужое поведение. Они не взаимозаменяемы: каждый решает
свою задачу, и выбор определяется одним вопросом — **что вам нужно сделать с чужим кодом**.

| Нужно | Механизм | Где описан |
| --- | --- | --- |
| Выполнить свой код в определённый момент | `MethodHookAttribute` | [Attributes](Core/@Attributes/README.md) |
| Узнать, что что-то уже произошло | `EventBus` | [EventBus](Core/@Events/EventBus/README.md) |
| Изменить или запретить действие до того, как оно случилось | `HookSystem` | [HookSystem](Core/HookSystem/README.md) |
| Согласовать решение, на которое влияют несколько компонентов | `FlagsSystem` | [FlagsSystem](Core/FlagsSystem/README.md) |
| Собрать данные из всех модулей в один список | `InvokePartialAttribute` | [Attributes](Core/@Attributes/README.md) |
| Подменить стандартную реализацию сервиса | `OverridePropertyAttribute` | [Attributes](Core/@Attributes/README.md) |
| Выполнять работу по расписанию вне сцены | `BackgroundTask` | [BackgroundTasks](Core/BackgroundTasks/README.md) |
| Узнать об изменении значения, которое само о себе не сообщает | `WatcherTask<T>` | [BackgroundTasks](Core/BackgroundTasks/README.md) |

### Как отличить друг от друга

**`MethodHook` — «выполни это на такой-то стадии».** Инициализация модуля, регистрация
фабрик, клонирование данных. Порядок задаётся числом, результат не собирается.

```csharp
[MethodHook(MethodHookStage.SDK, order: 20)]
private static void InitializeInventory() { }
```

**`EventBus` — «сообщи, что уже случилось».** Отправитель не ждёт ответа и не знает
подписчиков. Обновить UI, проиграть звук, записать метрику.

```csharp
public class CoinsView : MonoBehaviour, IResourceValueChangedEvent { }
```

**`HookSystem` — «дай вмешаться до того, как случится».** В отличие от `EventBus`,
обработчик получает изменяемый контекст: может уменьшить урон, заменить награду или
отменить действие. Обработчики идут цепочкой в порядке `Order`.

```csharp
public class DamageResistanceComponent : PRMonoBehaviour, IHookListener<DamageHookEvent> { }
```

**`FlagsSystem` — «можно ли сейчас?».** Когда на один вопрос («можно ли прыгать»)
влияют несколько независимых источников — оглушение, катсцена, туториал. Каждый
добавляет своё влияние, система сводит их к ответу.

```csharp
flagResolver.Add(PlayerFlags.CanJump, this, false);
```

**`InvokePartial` — «соберите со всех».** Каждый модуль возвращает свой кусок, вызывающий
получает объединённый результат.

```csharp
IEnumerable<Modifier> modifiers = this.CollectPartialResult<Modifier>(context);
```

**`OverrideProperty` — «замени реализацию».** Интеграция подставляет свой сервис вместо
стандартного до применения fallback: серверное время платформы вместо локального,
облачное хранилище вместо PlayerPrefs.

```csharp
[OverrideProperty(typeof(IServerTime), order: -100)]
private static void UsePlatformServerTime() => ServerTime = new PlatformServerTime();
```

**`BackgroundTask` — «делай это раз в N секунд».** Работа, у которой нет владельца на
сцене и которая должна пережить её смену: автосохранение, офлайн-доход, отложенная
аналитика. Задача сама переживает паузу так, как ей нужно, и умеет пропускать запуск,
пока условия не готовы.

```csharp
[AutoBackgroundTask]
public class PlaytimeTrackerTask : BackgroundTask
{
    public override Enumeration Key => BackgroundTaskKeyEnumerationProvider.PlaytimeTracker;
    public override float RepeatSeconds => 60f;

    protected override void OnExecute() { }
}
```

**`WatcherTask<T>` — «сообщи, когда значение изменится».** Отличие от `EventBus` в том,
что источник ничего не отправляет: смену суток, состояние сети или изменённый на сервере
баланс можно узнать только опросом. Наблюдатель опрашивает по расписанию и поднимает
событие только в момент изменения.

### Частые ошибки выбора

- **`EventBus` там, где нужен `HookSystem`.** Если обработчику надо повлиять на результат,
  событие не подойдёт: оно уведомляет уже после того, как решение принято.
- **`HookSystem` там, где хватило бы `EventBus`.** Перехват дороже и делает поток
  неочевидным: обработчик может незаметно отменить действие.
- **Свой флаг вместо `FlagsSystem`.** Булево поле «нельзя прыгать» на компоненте ломается,
  когда источников запрета становится двое: кто снял — тот и разрешил, хотя второй ещё против.
- **`MethodHook` для реакции на игровое событие.** Хуки стадий вызываются там, где код явно
  их запускает; для игровых событий есть `EventBus`.
- **Своя корутина вместо `BackgroundTask`.** Для периодической работы без владельца на сцене
  корутина требует объекта-хозяина, теряется при смене сцены и не даёт ни счётчиков,
  ни защиты от череды ошибок.
- **Опрос в `Update` вместо `WatcherTask<T>`.** Проверять раз в кадр то, что меняется раз
  в минуту, — лишняя работа; наблюдатель делает это по расписанию и сообщает только
  об изменении.

### Расширение без вклинивания

Отдельно стоят механизмы, которые не перехватывают чужое поведение, а **дополняют данные**.
Им не нужен ни один из шести способов выше:

| Что добавить | Как |
| --- | --- |
| Поле в сохранение | `partial class ProjectData` + `[MethodHook(Cloning)]` |
| Новое окно | `partial class MonoWindowKeyEnumerationProvider` + `partial class PRWindowsContainer` |
| Фоновая задача | наследник `BackgroundTask` + `[AutoBackgroundTask]` + ключ `partial`-частью `BackgroundTaskKeyEnumerationProvider` |
| Путь к ресурсам модуля | `partial class ResourcePaths` |
| Новый ключ, тип, слой | наследник `EnumerationProviderBase` или `partial`-часть существующего |

Правило простое: **общие файлы SDK править не нужно** — почти всё расширяется `partial`-частью
рядом с модулем.

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
- [Окна Database и Settings](Core/Editor/README.md) — секции, поиск, заполнение каталогов, валидация definitions
  и [наборы состава базы](Core/Editor/DATABASE-PRESETS.md) для разных игр
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
- [BackgroundTasks](Core/BackgroundTasks/README.md) — фоновые задачи по расписанию и наблюдение за значениями

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

- [Entity](Core/@Entity/README.md) — сущности игрового мира: идентификаторы, описание, реестр, время жизни и пул
- [Фабрики MonoBehaviour](Core/Factories/README.md) — обычные prefab, singleton-компоненты, MonoWindow и Notifier
- [Trackers](Core/Trackers/README.md) — игроки, сущности, камеры и UI-реестры
- [MonoWindow](Core/%23UI/MonoWindow/README.md) — модальные runtime-окна, фабрики и параметры открытия
- [Reward](Core/Reward/README.md) — модели наград, экземплярный сервис выдачи и проектные обработчики
- [Wallet](Core/Wallet/README.md) — баланс, начисление и списание валюты поверх `ResourceManager`
- [Enumeration](Core/Models/Enumeration/README.md) — расширяемый строковый идентификатор вместо `enum`
- [Services](Core/Services/README.md) — `NameService` и сервис имени текущего игрока
- [GameDataStorage](Core/GameDataStorage/README.md) — storage-контракты и универсальный `ProjectDataMap`
- [Utils](Core/Utils/README.md) — вспомогательные классы: время, отложенные вызовы, имена
- [Proxies](Core/Proxies/README.md) — переадресация Unity-callback'ов с дочерних объектов родительским компонентам
- [Property modifiers](Core/PropertyContainer/README.md) — динамические характеристики, персональные модификаторы и `GameRules`
- [GameRules](Core/GameRules/README.md) — глобальные границы характеристик, применяемые последними


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
