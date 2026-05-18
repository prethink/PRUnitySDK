using UnityEngine;

public class GizmoWireCubeArgs : GizmoCubeArgs
{
    public GizmoWireCubeArgs(Vector3 center, Vector3 size) : base(center, size)
    {
    }

    public GizmoWireCubeArgs(Vector3 center, Vector3 size, Color color) : base(center, size, color)
    {
    }

    public override Enumeration GizmoType => GizmoEnumerationProvider.WireCube;
}
