
using UnityEngine;

public abstract class GizmoArgsBase
{
    public abstract Enumeration GizmoType { get; }
    public Color Color { get; }

    public bool IsShowing { get; protected set; }

    protected GizmoArgsBase(Color color)
    {
        Color = color;
    }

    public void SetShowing()
    {
        IsShowing = true;
    }
}
