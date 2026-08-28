using UnityEngine;

[CreateAssetMenu(fileName = "Yan definition", menuName = "PRUnitySDK/Create/Definition/Resources/Yan currency")]
public class YandexCurrencyDefinition : ResourceItemDefinitionBase
{
    public static Enumeration Yan = new Enumeration(nameof(Yan));

    public override string Id => Yan.Value;
}
