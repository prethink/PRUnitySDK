#if PRSDK_TESTS
/// <summary>
/// Контроллер камеры без поведения — для проверок стека камер.
/// </summary>
/// <remarks>
/// Лежит вне <c>Editor</c>: компонент из Editor-сборки на объект не вешается.
/// </remarks>
public class CameraControllerTestDouble : CameraControllerBase
{
    /// <inheritdoc />
    public override bool CanPushStack => true;

    /// <inheritdoc />
    protected override void HandleCamera()
    {
    }
}
#endif
