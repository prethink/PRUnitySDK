/// <summary>
/// Источник ввода с клавиатуры и мыши.
/// </summary>
/// <remarks>
/// От <see cref="PlayerInputSourceBase"/> отличается одним: не работает там, где
/// клавиатуры нет. Проверка идёт до поиска владельца, поэтому на мобильной сборке
/// источник не делает вообще ничего.
/// </remarks>
public abstract class KeyboardInputSourceBase : PlayerInputSourceBase
{
    /// <inheritdoc />
    /// <remarks>
    /// Сведения об устройстве проверяются на <c>null</c>: источник живёт на сцене
    /// и продолжает обновляться в кадрах, когда SDK ещё не поднялся или уже свернулся —
    /// при выходе из Play Mode это давало <c>NullReferenceException</c> каждый кадр.
    /// </remarks>
    protected override bool CanInput()
    {
        DeviceInfoBase deviceInfo = PRUnitySDK.DeviceInfo;

        return deviceInfo != null && deviceInfo.IsDesktop() && base.CanInput();
    }
}
