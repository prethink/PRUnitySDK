using System.Collections.Generic;

/// <summary>
/// Система, которая выдаёт предметы своим способом.
/// </summary>
/// <remarks>
/// Достижения, подарки, кейсы — каждая система сама говорит, что раздаёт, и ни одна
/// не знает об остальных. Тот, кому нужен ответ «можно ли получить эту вещь иначе»,
/// спрашивает <see cref="ReservedItemsManager"/>, а не обходит системы по списку.
/// </remarks>
public interface IReservedItemsProvider
{
    /// <summary>
    /// Кто выдаёт предметы: подпись для игрока и для отладки.
    /// </summary>
    string ReservationSource { get; }

    /// <summary>
    /// Идентификаторы предметов, которые эта система может выдать.
    /// </summary>
    IEnumerable<string> GetReservedItemIds();
}
