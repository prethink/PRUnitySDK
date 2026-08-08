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
| `FlagsSystem` | Совместное управление состояниями объекта из нескольких источников |
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

- [Core](Core/README.md)
- [SDK](Core/SDK/README.md)
- [Attributes](Core/@Attributes/README.md)
- [Actions](Core/@Actions/README.md)
- [HookSystem](Core/HookSystem/README.md)

### Жизненный цикл и время

- [PRMonoBehaviour](Core/PRMonoBehaviour/README.md)
- [Coroutines](Core/Coroutines/README.md)
- [Yields](Core/Yields/README.md)
- [PauseSystem](Core/PauseSystem/README.md)
- [PRTime](Core/PRTime/README.md)
- [PRTimeScale](Core/PRTimeScale/README.md)

### Модели, сервисы и утилиты

- [Enumeration](Core/Models/Enumeration/README.md)
- [Utils](Core/Utils/README.md)
- [Proxies](Core/Proxies/README.md)
- [SoundManager](Core/SoundManager/README.md)

### Модули и интеграции

- [Modules](Modules/README.md)
- [DOTweenEffects](Modules/DOTweenEffects/README.md)
- [YG2 Integration](YG2.Integration/README.md)
- [Zenject Integration](Core.Zenject.Integration~/README.md)

## Текущие ограничения

- SDK распространяется как Unity Assets, а не как UPM-пакет.
- Автоматический installer пока не создаёт настройки, слои, теги и prefab'ы.
- Bootstrap по умолчанию предполагает сцены с индексами `0` и `1`.
- Часть каталогов и API всё ещё находится в процессе переноса и рефакторинга.
- Не все модули имеют отдельную документацию и тестовое покрытие.

## Репозиторий

[github.com/prethink/PRUnitySDK](https://github.com/prethink/PRUnitySDK)
