/// <summary>
/// Масштаб времени изменился: наложили или сняли модификатор.
/// </summary>
public interface IOnPRTimeScaleChange : IGlobalSubscriber
{
    /// <summary>
    /// Масштаб изменился у этих слоёв.
    /// </summary>
    /// <param name="layers">
    /// Слои, на которые наложен модификатор (<see cref="TimeScaleModifier.Affects"/>). Подписчику со своими слоями
    /// достаточно <see cref="PRTimeScale.Touches"/>; тело с драйвером зависит ещё и от мира
    /// (<see cref="TimeLayer.World"/>) — его темп заложен в шаг физики.
    /// </param>
    void OnPRTimeScaleChange(TimeLayer layers);
}

public static class PRTimeScaleEvents
{
    public static void RaiseTimeScaleChange(TimeLayer layers)
        => EventBus.RaiseEvent<IOnPRTimeScaleChange>(x => x.OnPRTimeScaleChange(layers));
}
