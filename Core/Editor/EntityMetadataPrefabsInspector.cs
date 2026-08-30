using UnityEngine;

/// <summary>
/// Блок «Префабы» под описанием или определением.
/// </summary>
/// <remarks>
/// Ассет сам по себе мало что говорит: чтобы понять, кого он называет, приходилось искать
/// по проекту, кто на него ссылается. Блок показывает это сеткой с превью прямо в окне
/// каталога, а сама сетка живёт в <see cref="EntityPrefabsGrid"/> - её же использует
/// вкладка определений.
/// </remarks>
public class EntityMetadataPrefabsInspector : IDatabaseAssetInspector
{
    private readonly EntityPrefabsGrid grid = new();

    /// <inheritdoc />
    public int Order => 50;

    /// <inheritdoc />
    public bool CanDraw(Object asset)
    {
        return asset is EntityMetadataBase or ItemDefinitionBase;
    }

    /// <inheritdoc />
    public void Draw(Object asset)
    {
        grid.Draw(asset);
    }
}
