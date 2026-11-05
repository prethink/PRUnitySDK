/// <summary>
/// Компонент, который упрощает себя по уровню детализации своей сущности.
/// </summary>
/// <remarks>
/// Компонент-наследник <see cref="PRMonoBehaviour"/> при включении сам подписывается на детализацию своей сущности
/// и сразу получает текущий уровень, при выключении отписывается (<see cref="EntityLod.Subscribe"/>); включившихся
/// раньше детализации она подбирает сама. Дальше его зовут при каждой смене уровня, а при выключении детализации —
/// с <see cref="EntityLodLevel.Full"/>. Искать <see cref="EntityLod"/> и проверять уровень каждый кадр не нужно.
/// </remarks>
public interface IEntityLodListener
{
    /// <summary>
    /// Уровень детализации сменился.
    /// </summary>
    /// <param name="lod">Детализация сущности: <see cref="EntityLod.Level"/>, <see cref="EntityLod.IsSimplified"/>.</param>
    void OnEntityLodChanged(EntityLod lod);
}
