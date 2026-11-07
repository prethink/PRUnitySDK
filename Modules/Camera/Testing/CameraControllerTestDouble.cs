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

    /// <summary>
    /// Подменяет ответ «можно ли восстановить камеру при уничтожении»: выход из игры и выгрузку
    /// сцены в EditMode не воспроизвести. <c>null</c> — как у настоящего контроллера.
    /// </summary>
    public bool? RestoreOnDestroyOverride;

    /// <inheritdoc />
    protected override bool CanRestoreOnDestroy => RestoreOnDestroyOverride ?? base.CanRestoreOnDestroy;

    /// <summary>
    /// То, что Unity делает при уничтожении: в редакторе без Awake её OnDestroy не зовётся.
    /// </summary>
    public void DestroyForTest()
    {
        UnRegisterEventsOnDestroy();
    }
}
#endif
