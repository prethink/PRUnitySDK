using System.Linq;

public sealed class ItemCategory
{
    public string Value { get; }

    private readonly string[] parts;

    public ItemCategory(string value)
    {
        Value = value;
        parts = value.Split('.');
    }

    public bool Is(string tag)
    {
        return parts.Any(p => p == tag);
    }

    public bool IsPath(string path)
    {
        return Value.StartsWith(path);
    }

    public bool Contains(string part)
    {
        return parts.Contains(part);
    }

    public override string ToString() => Value;

    //public static bool HasTag(string tag, ItemTag itemTag)
    //{

    //}
}
