/// <summary>
/// События SDK.
/// </summary>
public interface ISDKEvents : IGlobalSubscriber
{
    /// <summary>
    /// События завершения инициализации SDK.
    /// </summary>
    void OnInitialized();
}
