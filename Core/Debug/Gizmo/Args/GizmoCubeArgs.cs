using UnityEngine;

public class GizmoCubeArgs : GizmoArgsBase
{
    public Vector3 Center;
    public Vector3 Size;

    public GizmoCubeArgs(Vector3 center, Vector3 size) : base(Color.white)
    {
        Center = center;
        Size = size;
    }

    public GizmoCubeArgs(Vector3 center, Vector3 size, Color color) : base(color)
    {
        Center = center;
        Size = size;
    }

    public override Enumeration GizmoType => GizmoEnumerationProvider.Cube;
}
