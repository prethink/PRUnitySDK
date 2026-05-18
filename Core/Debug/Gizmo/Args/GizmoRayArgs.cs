using UnityEngine;

public class GizmoRayArgs : GizmoArgsBase
{
    public Ray Ray { get; }
    public Vector3 From { get; }
    public Vector3 To { get; }

    public override Enumeration GizmoType => GizmoEnumerationProvider.Ray;

    public GizmoRayArgs(Vector3 from, Vector3 to) : base(Color.white)
    {
        From = from;
        To = to;
    }

    public GizmoRayArgs(Vector3 from, Vector3 to, Color color) : base(color)
    {
        From = from;
        To = to;
    }

    public GizmoRayArgs(Ray ray) : base(Color.white)
    {
        Ray = ray;
    }

    public GizmoRayArgs(Ray ray, Color color) : base(color)
    {
        Ray = ray;
    }
}
