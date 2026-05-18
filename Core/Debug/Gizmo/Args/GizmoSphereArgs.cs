using UnityEngine;

public class GizmoSphereArgs : GizmoArgsBase
{
    public Vector3 Center;
    public float Radius;

    public GizmoSphereArgs(Vector3 center, float radius) : base(Color.white)
    {
        Center = center;
        Radius = radius;
    }

    public GizmoSphereArgs(Vector3 center, float radius, Color color) : base(color)
    {
        Center = center;
        Radius = radius;
    }

    public override Enumeration GizmoType => GizmoEnumerationProvider.Sphere;
}
