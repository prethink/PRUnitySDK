using UnityEngine;

public class GizmoEnumerations : EnumerationProviderBase
{
    /// <inheritdoc />
    public override Enumeration Default => FirstOption;

    public override bool IncludeInherited => true;

    public static readonly Enumeration Line = new Enumeration(nameof(Line));
    public static readonly Enumeration Ray = new Enumeration(nameof(Ray));
    public static readonly Enumeration Mesh = new Enumeration(nameof(Mesh));
    public static readonly Enumeration Sphere = new Enumeration(nameof(Sphere));
    public static readonly Enumeration Cube = new Enumeration(nameof(Cube));
    public static readonly Enumeration WireSphere = new Enumeration(nameof(WireSphere));
    public static readonly Enumeration WireCube = new Enumeration(nameof(WireCube));
}