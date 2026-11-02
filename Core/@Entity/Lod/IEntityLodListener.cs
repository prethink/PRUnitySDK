/// <summary>
/// Компонент, который упрощает себя по уровню детализации своей сущности.
/// </summary>
/// <remarks>
/// <see cref="EntityLod"/> находит такие компоненты на своём объекте и под ним и зовёт их при каждой
/// смене уровня, а при выключении — с <see cref="EntityLodLevel.Full"/>. Искать <see cref="EntityLod"/>
/// и проверять уровень каждый кадр не нужно.
/// </remarks>
public interface IEntityLodListener
{
    /// <summary>
    /// Уровень детализации сменился.
    /// </summary>
    /// <param name="lod">Детализация сущности: <see cref="EntityLod.Level"/>, <see cref="EntityLod.IsSimplified"/>.</param>
    void OnEntityLodChanged(EntityLod lod);
}
