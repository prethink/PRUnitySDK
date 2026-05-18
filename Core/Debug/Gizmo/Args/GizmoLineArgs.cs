
using UnityEngine;

public class GizmoLineArgs : GizmoArgsBase
{
    public Vector3 From { get; }
    public Vector3 To { get; }

    public GizmoLineArgs(Vector3 from, Vector3 to) : base(Color.white)
    {
        From = from;
        To = to;
    }

    public GizmoLineArgs(Vector3 from, Vector3 to, Color color) : base(color)
    {
        From = from;
        To = to;
    }

    public override Enumeration GizmoType => GizmoEnumerationProvider.Line;
}
