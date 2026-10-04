using System;
using System.Collections.Generic;

/// <summary>
/// Статистика игрока за всё время: числа, которые только растут и ни на что не тратятся.
/// </summary>
/// <remarks>
/// Класс <c>partial</c>: модуль, которому мало счётчика в <see cref="Counters"/>, добавляет
/// свой раздел рядом с собой и копирует его хуком стадии <see cref="CloningStage"/>.
/// </remarks>
public partial class GameStatistics : ICloneable
{
    /// <summary>
    /// Стадия хуков копирования: аргументом приходит копия, в которую модуль дописывает свой раздел.
    /// </summary>
    public const string CloningStage = "GameStatistics.Cloning";

    /// <summary>
    /// Сколько раз игрок заходил в игру.
    /// </summary>
    public long LaunchCount { get; set; }

    /// <summary>
    /// Время в игре в минутах. На логической паузе не идёт.
    /// </summary>
    public long PlaytimeMinutes { get; set; }

    /// <summary>
    /// Сколько каждого ресурса получено за всё время. Ключи те же, что в <c>ProjectData.Resources</c>.
    /// </summary>
    public Dictionary<string, long> Resources { get; set; } = new();

    /// <summary>
    /// Счётчики модулей по именам: набранные уровни, пройденные этапы и подобное.
    /// </summary>
    public Dictionary<string, long> Counters { get; set; } = new();

    /// <inheritdoc />
    public object Clone()
    {
        var clone = new GameStatistics
        {
            LaunchCount = LaunchCount,
            PlaytimeMinutes = PlaytimeMinutes,
            Resources = new Dictionary<string, long>(Resources ?? new()),
            Counters = new Dictionary<string, long>(Counters ?? new())
        };

        this.RunMethodHooks(CloningStage, clone);

        return clone;
    }
}
