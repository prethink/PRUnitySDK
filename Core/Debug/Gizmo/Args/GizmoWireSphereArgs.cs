using UnityEngine;

public class GizmoWireSphereArgs : GizmoSphereArgs
{
    public GizmoWireSphereArgs(Vector3 center, float radius) : base(center, radius)
    {
    }

    public GizmoWireSphereArgs(Vector3 center, float radius, Color color) : base(center, radius, color)
    {
    }

    public override Enumeration GizmoType => GizmoEnumerationProvider.WireSphere;
}
