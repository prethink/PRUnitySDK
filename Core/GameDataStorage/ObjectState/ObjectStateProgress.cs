/// <summary>
/// Сколько объектов показано и сколько их всего.
/// </summary>
/// <remarks>
/// Что считать прогрессом, зависит от смысла объектов, поэтому обе стороны отдаются
/// как есть. У подбираемых предметов собранное — это <see cref="Hidden"/>: кристалл
/// исчезает, когда его взяли. У построек тайкуна наоборот, прогресс — это
/// <see cref="Opened"/>.
/// </remarks>
public readonly struct ObjectStateProgress
{
    /// <summary>
    /// Показанные объекты.
    /// </summary>
    public readonly int Opened;

    /// <summary>
    /// Спрятанные объекты.
    /// </summary>
    public readonly int Hidden;

    /// <summary>
    /// Всего объектов.
    /// </summary>
    public readonly int Total;

    public ObjectStateProgress(int opened, int hidden)
    {
        Opened = opened;
        Hidden = hidden;
        Total = opened + hidden;
    }

    /// <summary>
    /// Доля показанных, от 0 до 1.
    /// </summary>
    public float OpenedRatio => Total > 0 ? (float)Opened / Total : 0f;

    /// <summary>
    /// Доля спрятанных, от 0 до 1.
    /// </summary>
    public float HiddenRatio => Total > 0 ? (float)Hidden / Total : 0f;

    /// <inheritdoc />
    public override string ToString()
    {
        return $"показано {Opened} из {Total}";
    }
}
