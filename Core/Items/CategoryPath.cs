using System;
using System.Linq;

public sealed class CategoryPath
{
    public string Value { get; }

    private readonly string[] parts;

    public CategoryPath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("ItemCategory value cannot be null or empty", nameof(value));

        Value = value;
        parts = value.Split('.');
    }

    public CategoryPath(params string[] values)
    {
        if (values == null || values.Length == 0)
            throw new ArgumentException("ItemCategory values cannot be null or empty", nameof(values));

        Value = string.Join(".", values);
        parts = values;
    }

    public CategoryPath(CategoryPath parent, params string[] parts)
    {
        if (parent == null)
            throw new ArgumentNullException(nameof(parent));

        if (parts == null || parts.Length == 0)
            throw new ArgumentException(nameof(parts));

        this.parts = parent.parts.Concat(parts).ToArray();
        Value = string.Join(".", this.parts);
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
