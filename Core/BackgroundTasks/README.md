# BackgroundTasks

Фоновые задачи, выполняемые по расписанию и живущие дольше сцены. Наследнику достаточно
указать интервал и описать тело задачи — расписанием, обработкой ошибок и диагностикой
занимается `BackgroundTaskTracker`.

Все задачи обслуживаются **одним** тиком `PRMonoBehaviourHost`, поэтому их количество
не влияет на число корутин.

## Для чего это

Работа, у которой нет владельца на сцене:

- периодическое автосохранение;
- начисление офлайн-дохода;
- отложенная аналитика;
- опрос источника, который сам о себе не сообщает: наступление нового дня по серверному
  времени, состояние сети, баланс, изменённый на стороне сервера.

Для логики, привязанной к объекту сцены, фоновая задача не нужна — там достаточно
`PRUpdate()` или `DelayAction`.

## Состав

| Тип | Назначение |
| --- | --- |
| `IBackgroundTask` | Контракт задачи для трекера |
| `BackgroundTaskRuntime` | Общая механика: расписание, состояние, счётчики, ошибки |
| `BackgroundTask` | Базовая задача, не привязанная к сцене |
| `BackgroundTaskBehaviour` | Базовая задача-компонент на объекте сцены |
| `IWatcherTask<T>` | Контракт задачи-наблюдателя |
| `WatcherState<T>` | Наблюдаемое значение: сравнение и уведомление об изменении |
| `WatcherTask<T>` | Наблюдатель, не привязанный к сцене |
| `WatcherTaskBehaviour<T>` | Наблюдатель-компонент на объекте сцены |
| `AutoBackgroundTaskAttribute` | Помечает задачу для автоматической регистрации |
| `BackgroundTaskKeyEnumerationProvider` | Ключи задач; расширяется `partial`-частью |
| `BackgroundTaskStatus` | Состояние задачи в цикле выполнения |
| `BackgroundTaskTracker` | Реестр задач и единый цикл их выполнения |
| `BackgroundTaskService` | Singleton-доступ к трекеру |

`BackgroundTaskService.Instance` возвращает **`BackgroundTaskTracker`**; тот же объект
доступен как `PRUnitySDK.Trackers.BackgroundTasks`.

## Два вида задач

Задача бывает обычным классом или компонентом сцены. Компонент не может наследовать
`BackgroundTask`, потому что уже наследует `PRMonoBehaviour`, а второго базового класса
в C# нет. Поэтому механика вынесена в отдельный объект (мост), а общее для обоих —
в интерфейс:

```text
IBackgroundTask ──────────────► BackgroundTaskRuntime
      ▲                          расписание, статус, счётчики,
      │                          история ошибок, лимиты
      ├── BackgroundTask           (обычный класс)
      └── BackgroundTaskBehaviour  (PRMonoBehaviour)

IWatcherTask<T> ──────────────► WatcherState<T>
      ▲                          последнее значение, сравнение,
      │                          события изменения
      ├── WatcherTask<T>           (обычный класс)
      └── WatcherTaskBehaviour<T>  (PRMonoBehaviour)
```

Интерфейс отвечает на вопрос «что это за задача» — ключ, интервал, лимиты, тело;
`Runtime` — «как она выполняется». Обе реализации ничего не дублируют, а трекер держит
их в одном реестре и не различает. Наблюдение устроено тем же приёмом: `WatcherState<T>`
хранит значение и решает, что считать изменением, а владелец только отвечает,
как это значение прочитать.

| Что нужно | Что использовать |
| --- | --- |
| Задача не связана с объектами сцены | `BackgroundTask` |
| Нужны ссылки на компоненты или настройка в инспекторе | `BackgroundTaskBehaviour` |
| Опрос значения без привязки к сцене | `WatcherTask<T>` |
| Опрос значения у объекта сцены | `WatcherTaskBehaviour<T>` |

### Задача-компонент

```csharp
public class SpawnerHealthTask : BackgroundTaskBehaviour
{
    [SerializeField] private Spawner spawner;

    public override Enumeration Key => BackgroundTaskKeyEnumerationProvider.SpawnerHealth;

    public override bool CanExecute() => spawner != null && spawner.IsActive;

    protected override void OnExecute()
    {
        spawner.RefillIfNeeded();
    }
}
```

Расписание задаётся в инспекторе — интервал, задержка, лимит запусков, игровое время,
`StartPaused` и порог ошибок сериализуются полями. Виртуальные свойства при этом
переопределяемы, если значение удобнее вычислять кодом.

Компонент **регистрируется при включении и снимается при выключении**: выключенный
объект задачу не выполняет, и отдельно останавливать её не нужно. Атрибут
`[AutoBackgroundTask]` для компонентов не применяется — их создаёт Unity вместе
с объектом; если пометить компонент атрибутом, SDK предупредит и проигнорирует его.

Уничтоженный компонент трекер пропускает: проверка идёт через `IsNull()`, который
учитывает «фальшивый null» Unity-объектов.

## Ключи

Ключ задачи — `Enumeration`, как у окон, статов и слоёв времени. Ключи объявляются
`partial`-частью `BackgroundTaskKeyEnumerationProvider` рядом со своим модулем — общий
файл SDK править не нужно:

```csharp
// Modules/Economy/BackgroundTaskKeyEnumerationProvider.Economy.cs
public partial class BackgroundTaskKeyEnumerationProvider
{
    public static readonly Enumeration AutoSave = new(nameof(AutoSave));
    public static readonly Enumeration OfflineIncome = new(nameof(OfflineIncome));
}
```

Это даёт автодополнение вместо строковых литералов и защищает от опечатки, которая
иначе тихо превратилась бы в «задача не найдена».

## Периодическая задача

```csharp
public class AutoSaveTask : BackgroundTask
{
    public override Enumeration Key => BackgroundTaskKeyEnumerationProvider.AutoSave;

    public override float RepeatSeconds => 120f;

    public override bool CanExecute() => GameManager.Instance.ReadySignal.IsReady;

    protected override void OnExecute()
    {
        GameManager.Instance.SaveProjectData();
    }
}
```

Регистрация:

```csharp
PRUnitySDK.Trackers.BackgroundTasks.Register(new AutoSaveTask());
```

## Автоматическая регистрация

Чтобы не регистрировать задачу вручную, пометьте её атрибутом — SDK найдёт и создаст
её сам при инициализации:

```csharp
[AutoBackgroundTask]
public class AutoSaveTask : BackgroundTask { /* ... */ }
```

`RegisterAutoTasks()` вызывается из `PRUnitySDK.InitializeSDK` после менеджеров, но до
того, как SDK объявляет себя готовым, — первый запуск гарантированно придётся на
полностью инициализированный проект.

Требования к классу: наследник `BackgroundTask`, не абстрактный, с публичным
конструктором без параметров. Если что-то не так — тип абстрактный, конструктор с
параметрами, атрибут висит не на задаче — SDK напишет предупреждение с именем типа и
пропустит его, а не упадёт. Исключение в конструкторе одной задачи не мешает
зарегистрироваться остальным.

| Параметр атрибута | Назначение |
| --- | --- |
| `Order` | Порядок регистрации: меньшее значение раньше |
| `Enabled` | `false` — не регистрировать вовсе, не убирая атрибут |

```csharp
[AutoBackgroundTask(order: 10, Enabled = false)]
```

Задачи, которым нужны параметры конструктора или ссылка на объект сцены, регистрируйте
вручную — атрибут для них не подходит.

## Зарегистрирована, но не запущена

Отдельно от `Enabled` есть свойство `StartPaused`: задача **попадает в реестр**, но сразу
переходит в `Paused` и ждёт явной команды.

```csharp
[AutoBackgroundTask]
public class OfflineIncomeTask : BackgroundTask
{
    public override Enumeration Key => BackgroundTaskKeyEnumerationProvider.OfflineIncome;
    public override float RepeatSeconds => 30f;
    public override bool StartPaused => true;      // стартует не с SDK, а с игрой

    protected override void OnExecute() { /* ... */ }
}
```

```csharp
var tracker = PRUnitySDK.Trackers.BackgroundTasks;

// Когда тип задачи известен - доступны её собственные методы.
tracker.TryGet(BackgroundTaskKeyEnumerationProvider.OfflineIncome, out OfflineIncomeTask task);
task.Resume();   // например, при входе в матч
task.Pause();    // при выходе

// Когда тип неизвестен - управление идёт через мост.
tracker.TryGet(BackgroundTaskKeyEnumerationProvider.OfflineIncome, out IBackgroundTask any);
any.Runtime.Resume();
```

Разница с `Enabled = false` существенная: выключенной атрибутом задачи в реестре нет
вообще, а `StartPaused` оставляет её видимой — и в коде, и в отладочном окне, — поэтому
про неё не забудешь.

`Resume()` планирует следующий запуск через обычный интервал от момента возобновления,
а не выполняет его немедленно: иначе задача, простоявшая на паузе дольше своего интервала,
срабатывала бы сразу же. Если запуск нужен прямо сейчас — `Execute()` или `ForceExecute()`
у трекера.

## Задача-наблюдатель

Если нужно следить за значением, наследуйтесь от `WatcherTask<T>`: сравнение и
уведомление берёт на себя базовый класс, наследник отвечает только на вопрос
«как прочитать значение».

```csharp
public class ServerDayTask : WatcherTask<int>
{
    public override Enumeration Key => BackgroundTaskKeyEnumerationProvider.ServerDay;

    public override float RepeatSeconds => 60f;

    public override int Read() => PRUnitySDK.ServerTime.GetNow().DayOfYear;
}
```

```csharp
var task = new ServerDayTask();
task.Changed += day => PRLog.WriteDebug(this, $"Наступил день {day}");
PRUnitySDK.Trackers.BackgroundTasks.Register(task);
```

Событие `Changed` поднимается **только при изменении**. Есть и `ChangedWithPrevious`
с парой «было — стало». Текущее значение всегда доступно через `CurrentValue` и
`HasValue`, поэтому подписчику, пришедшему позже, ничего не теряется.

Если сравнение по умолчанию не подходит (например, `float` с допуском), переопределите
`AreEqual`.

### Наблюдатель на объекте сцены

Когда опрашивать нужно что-то со сцены, наследуйтесь от `WatcherTaskBehaviour<T>` —
контракт тот же, но это компонент:

```csharp
public class ArenaPlayerCountTask : WatcherTaskBehaviour<int>
{
    [SerializeField] private Arena arena;

    public override Enumeration Key => BackgroundTaskKeyEnumerationProvider.ArenaPlayers;

    public override bool CanExecute() => arena != null;

    public override int Read() => arena.PlayersInside;
}
```

```csharp
arenaTask.Changed += count => hud.SetPlayers(count);
```

Расписание настраивается в инспекторе, регистрация происходит при включении объекта.
Подписываться на `Changed` можно и до `Awake`: состояние наблюдателя создаётся лениво
при первом обращении.

## Разовая отложенная задача

Комбинация задержки и лимита запусков даёт задачу, которая выполнится один раз:

```csharp
public class WelcomeGiftTask : BackgroundTask
{
    public override Enumeration Key => BackgroundTaskKeyEnumerationProvider.WelcomeGift;

    public override float RepeatSeconds => 0f;
    public override float InitialDelaySeconds => 30f;
    public override int MaxRepeatCount => 1;

    protected override void OnExecute() { /* выдать подарок */ }
}
```

После исчерпания лимита задача переходит в `Completed` и больше не выполняется, оставаясь
в реестре для диагностики. Вернуть её в работу можно через `ResetRepeatCount()`.

## Настройка

| Свойство | По умолчанию | Назначение |
| --- | --- | --- |
| `Key` | — | Уникальный `Enumeration`-ключ, обязателен |
| `Name` | значение `Key` | Имя для логов |
| `RepeatSeconds` | — | Интервал между запусками; `0` — каждый тик хоста |
| `InitialDelaySeconds` | `0` | Задержка перед первым запуском |
| `MaxRepeatCount` | `-1` | Лимит запусков; меньше 1 — без ограничения |
| `UseGameTime` | `false` | Считать по игровому времени, то есть вставать на паузе |
| `StartPaused` | `false` | Зарегистрировать, но не запускать до `Resume()` |
| `MaxConsecutiveErrors` | `5` | Сколько ошибок подряд до отключения; меньше 1 — без защиты |
| `RaiseOnFirstRead` | `true` | Поднимать `Changed` при первом чтении (только `WatcherTask<T>`) |

`CanExecute()` вызывается **перед каждым** запуском. Возврат `false` — не ошибка: запуск
пропускается, счётчик `SkippedCount` растёт, лимит запусков не тратится, проверка
повторяется в следующее окно. Исключение внутри самой проверки трактуется как `false`
и логируется.

## Состояния

| Статус | Когда |
| --- | --- |
| `Pending` | Создана, но не зарегистрирована |
| `Scheduled` | Ждёт первого запуска |
| `Executing` | Выполняется |
| `WaitingNextRun` | Ждёт следующего запуска |
| `Skipped` | Последний запуск пропущен по `CanExecute()` |
| `Paused` | Приостановлена вручную через `Pause()` |
| `Faulted` | Отключена из-за череды ошибок |
| `Completed` | Исчерпала `MaxRepeatCount` |

Смену состояния можно слушать через событие `StatusChanged`.

## Диагностика

Состояние и счётчики живут в `Runtime`: `ExecutedCount`, `SkippedCount`, `ErrorCount`,
`ConsecutiveErrors`, `LastRunRealTime`, `LastRunDurationMs`, `LastError` и список `Errors`
из десяти последних исключений. Там же события `Executed`, `Failed` и `StatusChanged`.

```csharp
task.Runtime.Failed += exception => PRLog.WriteWarning(this, exception.Message);
```

У трекера:

```csharp
var tasks = PRUnitySDK.Trackers.BackgroundTasks;

tasks.TryGet(BackgroundTaskKeyEnumerationProvider.ServerDay, out ServerDayTask day);  // поиск с приведением типа
tasks.ForceExecute(BackgroundTaskKeyEnumerationProvider.ServerDay);                  // запуск вне расписания
tasks.GetFaulted();                                     // что отвалилось
tasks.GetByStatus(BackgroundTaskStatus.Skipped);        // что простаивает
```

`ForceExecute` не выполняет `CanExecute()` — это ручной запуск для отладки.

### Окно PRUnitySDKDebug

Вкладка `Tasks` показывает все зарегистрированные задачи: состояние, интервал, обратный
отсчёт, счётчики выполнений, пропусков и ошибок, длительность последнего запуска и — для
`WatcherTask<T>` — текущее прочитанное значение. Оттуда же задачу можно выполнить вручную,
поставить на паузу или вернуть в работу после отказа. См.
[Editor](../Editor/README.md).

## Как это работает внутри

1. Трекер при первой регистрации встаёт в тиковый цикл `PRMonoBehaviourHost`
   как `IPRTickable`.
2. На каждом проходе он смотрит, у кого истёк интервал, и выполняет только их.
3. Запуск оборачивается в `try/catch`, замеряется, считается; после ошибки счётчик
   `ConsecutiveErrors` растёт, а успешный запуск его обнуляет.
4. Следующее время считается **от момента фактического выполнения**, а не от расписания,
   поэтому пропущенные интервалы не копятся и не вызывают серию догоняющих запусков.

Задачи не выполняются, пока `PRUnitySDK.IsInitialized` не станет `true`: они почти всегда
обращаются к менеджерам, которых до инициализации нет. Проверка идёт по состоянию SDK,
а не по разовому событию, поэтому момент создания трекера ни на что не влияет.

## Ограничения

- **Точность расписания ограничена тиком хоста.** Интервал хоста задаётся в настройках
  проекта (`PRMonobehaviourHost.Tick`); задача с `RepeatSeconds` меньше этого значения
  будет выполняться не чаще хоста.
- **Выполнение синхронное.** Долгая работа внутри `OnExecute()` блокирует кадр. Для сетевых
  запросов запускайте из задачи корутину, а результат кладите в поле, которое читает `Read()`.
- **Состояние не сохраняется.** Счётчики живут только в памяти: после перезапуска игры
  отложенная задача начнёт отсчёт заново. Если нужна устойчивость к перезапуску, храните
  отметку времени в `ProjectData` и сверяйтесь с ней в `CanExecute()`.
- **`MaxConsecutiveErrors` считает ошибки подряд**, а не суммарно: один успешный запуск
  обнуляет счётчик. Это сделано намеренно, чтобы временный сбой не выводил задачу из строя
  навсегда, но означает, что «падает через раз» не приведёт к отключению — такое видно
  только по `ErrorCount`.
- **Порядок обхода** соответствует порядку регистрации; для автоматических задач им
  управляет `Order` в атрибуте.
- **Сканируется одна сборка** — та, где объявлен `BackgroundTask` (`Assembly-CSharp`).
  Задачи из других сборок автоматически найдены не будут, их нужно регистрировать вручную.
- **Атрибут не наследуется.** Производный класс не регистрируется автоматически, пока не
  пометит себя сам, — иначе базовая задача и её наследник попали бы в реестр оба.
- **Задачи-компоненты не создаются автоматически.** Их создаёт Unity вместе с объектом,
  и регистрируются они при включении; `[AutoBackgroundTask]` на компоненте игнорируется
  с предупреждением.
- **`Runtime` открыт наружу.** Это цена моста: состояние доступно всем, кто держит ссылку
  на задачу, поэтому счётчики намеренно доступны только для чтения, а менять статус может
  лишь трекер.

## Происхождение модели

Контракт близок к механизму фоновых задач из [PRTelegramBot](https://github.com/prethink/PRTelegramBot):
оттуда взяты `CanExecute()` как штатный пропуск, задержка первого запуска, лимит числа
запусков, явная машина состояний и хранение истории ошибок. Имена типов здесь идут без
префикса `PR` — он в этом SDK закреплён за инфраструктурой Unity-слоя (`PRMonoBehaviour`,
`PRTime`, `PRTimeScale`), а фоновые задачи к ней не относятся.

Отличия продиктованы средой: в Unity нет `async/await` в главном цикле и нет DI, поэтому
вместо `Task` и `CancellationToken` используется синхронный запуск на общем хосте, а вместо
метаданных в атрибуте — свойства базового класса. Атрибутную регистрацию по образцу
`PRBackgroundTaskAttribute` можно добавить позже, если задач станет много.

## Примеры

Два рабочих примера лежат в [Examples/BackgroundTasks](../../Examples/BackgroundTasks/README.md):
учёт времени в игре (`BackgroundTask`) и отслеживание смены суток (`WatcherTask<int>`).
Обе задачи выключены параметром `Enabled = false`, поэтому поведение проекта не меняют.

## Смотрите также

- [PRMonoBehaviour](../PRMonoBehaviour/README.md) — `PRMonoBehaviourHost` и тиковый цикл
- [Trackers](../Trackers/README.md) — общий контракт реестров
- [Coroutines](../Coroutines/README.md) — корутины с учётом паузы
- [PRTime](../PRTime/README.md) — источники времени
