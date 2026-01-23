using System;

/// <summary>
/// Информация о сущности.
/// </summary>
public interface IEntityInfo : INameProvider, IIconProvider
{
    /// <summary>
    /// Guid типа сущности.
    /// </summary>
    Guid TypeGuid { get; }
}
