/// <summary>
/// Объект, у которого есть свои слои времени.
/// </summary>
/// <remarks>
/// По слоям <see cref="PRTimeScale"/> решает, какие модификаторы объект задевают. Слоёв может быть несколько:
/// враг — <see cref="TimeLayer.NPC"/> и <see cref="TimeLayer.Enemy"/>.
/// </remarks>
public interface ITimeScaleLayer
{
    /// <summary>
    /// Слои времени объекта.
    /// </summary>
    public TimeLayer GetTimeLayers();
}
