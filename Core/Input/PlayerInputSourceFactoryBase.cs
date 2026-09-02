/// <summary>
/// Фабрика источника ввода: создаёт его префаб из общей папки ввода.
/// </summary>
/// <typeparam name="T">Тип источника ввода.</typeparam>
public abstract class PlayerInputSourceFactoryBase<T> : SingletonMonoBehaviourFactoryBase<T>
    where T : PlayerInputSourceBase
{
    /// <summary>
    /// Имя префаба в папке <see cref="ResourcePaths.InputsPath"/>.
    /// </summary>
    public abstract string Name { get; }

    public override string ResourcePath => $"{PRUnitySDK.ResourcePaths.InputsPath}/{Name}";
}
