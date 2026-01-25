public class ItemsCategoryRoot
{
    public const string Root = "Item";
}


public partial class ResourceItemCategory
{
    public const string Category = "Resource";

    public const string Root = ItemsCategoryRoot.Root;

    public static string Coin => $"{Root}.{Category}.{nameof(Coin)}";
    public string Crystal => $"{Root}.{Category}.{nameof(Crystal)}";

    public static string GetCategory(string category)
    {
        return $"{Root}.{Category}.{category}";
    }
}

public partial class ObjectItemCategory
{
    public const string Category = "Object";

    public const string Root = nameof(Root);

    public const string Skin = nameof(Skin);
    public const string Mask = nameof(Mask);
    public const string Hair = nameof(Hair);
    public const string Hat = nameof(Hat);
    public const string Glasses = nameof(Glasses);
    public const string Hand = nameof(Hand);
    public const string Leg = nameof(Leg);

    public const string SpineEffect = nameof(SpineEffect);
    public const string HandsEffect = nameof(HandsEffect);

    public const string Pet = nameof(Pet);
    public const string Wings = nameof(Wings);
}

public partial class WeaponItemCategory
{
    public static string Root => ObjectItemCategory.Category;
    public static string Category => "Weapon";
    public static string FullCategory => $"{Root}.{Category}";
}
