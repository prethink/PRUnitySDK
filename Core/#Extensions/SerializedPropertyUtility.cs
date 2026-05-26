public static class SerializedPropertyUtility
{
    public static string GetBackingField(this string propertyName)
    {
        return $"<{propertyName}>k__BackingField";
    }
}