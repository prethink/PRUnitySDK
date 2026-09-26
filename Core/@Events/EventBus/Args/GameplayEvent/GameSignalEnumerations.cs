/// <summary>
/// Игровые сигналы: «случилось такое-то событие», без своих аргументов.
/// </summary>
/// <remarks>
/// Набор пустой: сигналы объявляет проект partial-классом рядом с тем, что их поднимает,
/// например «впервые открыт лаки-блок». Поднимаются через
/// <see cref="GameplayEvents.RaiseSignal"/>, слушаются как <see cref="GameSignalEventArgs"/>
/// в <see cref="IGameplayEvent"/>.
/// </remarks>
public partial class GameSignalEnumerations : EnumerationProviderBase
{
    /// <inheritdoc />
    public override bool IncludeInherited => true;

    /// <inheritdoc />
    public override Enumeration Default => FirstOption;
}
