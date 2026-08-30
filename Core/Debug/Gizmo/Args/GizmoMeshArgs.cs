using UnityEngine;

public class GizmoMeshArgs : GizmoArgsBase
{
    public override Enumeration GizmoType => GizmoEnumerations.Mesh;

    public Mesh Mesh { get; }
    public Vector3 Position { get; }
    public Quaternion Rotation { get; }
    public Vector3 Scale { get; }

    /// <summary>Рисует меш каркасом (Gizmos.DrawWireMesh), а не сплошным
    /// (Gizmos.DrawMesh) - обычно нужнее для отладки (видно сквозь объект).
    /// Если нужен сплошной вариант - см. Wireframe в GizmoDrawer.DrawMesh.</summary>
    public bool Wireframe { get; }

    public GizmoMeshArgs(Color color, Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, bool wireframe = true)
        : base(color)
    {
        Mesh = mesh;
        Position = position;
        Rotation = rotation;
        Scale = scale;
        Wireframe = wireframe;
    }

    public GizmoMeshArgs(Color color, Mesh mesh, Vector3 position, Quaternion rotation, bool wireframe = true)
        : this(color, mesh, position, rotation, Vector3.one, wireframe)
    {
    }
}