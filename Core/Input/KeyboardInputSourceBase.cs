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
    protected override bool CanInput()
    {
        return PRUnitySDK.DeviceInfo.IsDesktop() && base.CanInput();
    }
}
