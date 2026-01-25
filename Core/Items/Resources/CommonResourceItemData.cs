using UnityEngine;

[CreateAssetMenu(fileName = "Common resource data", menuName = "PRUnitySDK/Items/Data/Resources/Common")]
public class CommonResourceItemData : ResourceItemData
{
    [SerializeField] private string nameItem;
    [SerializeField] private string id;

    public override string Id => id;

    public override string Name => nameItem;

    public override CategoryPath Category => new CategoryPath(ResourceItemCategory.GetCategory(Name));
}
