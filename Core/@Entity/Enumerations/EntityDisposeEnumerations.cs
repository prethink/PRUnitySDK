public class EntityDisposeEnumerations : EnumerationProviderBase
{
    public static readonly Enumeration Destroy      = new Enumeration(nameof(Destroy));
    public static readonly Enumeration HideInPool   = new Enumeration(nameof(HideInPool));
    public static readonly Enumeration Hide         = new Enumeration(nameof(Hide));
    public static readonly Enumeration HideWire     = new Enumeration(nameof(HideWire));
    /// <summary>
    /// Как <see cref="HideWire"/>, но каркас по граням, а не по треугольникам: рёбер
    /// между треугольниками одной плоскости нет, и от куба остаются его двенадцать рёбер.
    /// </summary>
    public static readonly Enumeration HideWirePolygons = new Enumeration(nameof(HideWirePolygons));
    /// <inheritdoc />
    public override Enumeration Default => FirstOption;

    public override bool IncludeInherited => true;
}
