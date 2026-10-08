# Статистика

Числа об игроке за всё время: сколько раз заходил, сколько играл, сколько какого ресурса
получил. Они только растут и ни на что не тратятся, поэтому лежат отдельно от остатков
ресурсов и от произвольных свойств проекта.

```csharp
StatisticsManager statistics = PRUnitySDK.Managers.Statistics;

long launches = statistics.LaunchCount;
long minutes = statistics.PlaytimeMinutes;
long coinsEarned = PRUnitySDK.Managers.Resource.GetResourceTotal(ResourceEnumerations.Coin);
```

Читать и менять статистику можно после `GameManager.ReadySignal`: до загрузки сохранения
данных нет.

## Что считается само

| Число | Кто считает |
| --- | --- |
| `LaunchCount` | `StatisticsManager` один раз за сессию, когда сохранение загружено |
| `PlaytimeMinutes` | фоновая задача `PlaytimeTrackerTask`, раз в минуту игрового времени; на паузе стоит |
| `Resources` | `ResourceManager` при любом росте значения ресурса; траты счётчик не уменьшают |

Счётчик ресурса, которого в сохранении ещё нет, начинается с остатка на момент первого
начисления. `ResourceManager.GetResourceTotal` до этого момента отдаёт текущий остаток.

## Счётчики модулей

Своё число модуль кладёт в `Counters` под своим именем:

```csharp
statistics.AddCounter("TotalLevel", gainedLevels);
long total = statistics.GetCounter("TotalLevel");
```

`TryGetCounter` отличает отсутствующий счётчик от нуля. Это нужно, чтобы завести его
от текущего значения у игрока, который начал играть раньше.

Если одного числа мало, `GameStatistics` объявлен `partial`: модуль добавляет своё свойство
рядом с собой и копирует его хуком стадии `GameStatistics.CloningStage`.

```csharp
public partial class GameStatistics
{
    public Dictionary<string, long> Kills { get; set; } = new();

    [MethodHook(CloningStage)]
    private void CloneKills(GameStatistics clone)
    {
        clone.Kills = new Dictionary<string, long>(Kills);
    }
}
```

## Хранение

Данные лежат в `ProjectData.Statistics` и уходят на диск с обычным сохранением. Статистика сама
запись на диск не запускает.

Окно `PRUnitySDK/Windows/Debug Window` показывает статистику в значениях сохранения, в категориях
`Statistics`, `Statistics.Resources` и `Statistics.Counters`.

## Зависимости

| От чего зависит | Зачем |
| --- | --- |
| `GameManager` | данные сохранения и сигнал готовности |
| `ProjectPropertiesManager` | перенос запусков и времени из старых свойств |
| [BackgroundTasks](../BackgroundTasks/README.md) | задача учёта времени |
