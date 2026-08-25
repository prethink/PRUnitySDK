using System;

/// <summary>
/// Optional diagnostics exposed by a game data storage without extending the
/// required <see cref="IGameDataStorage"/> contract.
/// </summary>
public interface IGameDataStorageSaveInfo
{
    /// <summary>
    /// Date when the current save was created.
    /// </summary>
    DateTime? CreationDate { get; }

    /// <summary>
    /// Date when the current save was most recently written.
    /// </summary>
    DateTime? LastUpdateDate { get; }
}
