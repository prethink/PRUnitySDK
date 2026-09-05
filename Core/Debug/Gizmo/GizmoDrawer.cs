using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GizmoDrawerHost : PRMonoBehaviourSingletonBase<GizmoDrawer>
{
    // TODO: класс нигде не используется. GizmoDrawer берут через GetComponent
    // (см. PRPhysics), а Instance у базы отдаёт GizmoDrawer, а не сам хост.
}

public class GizmoDrawer : MonoBehaviour
{
    public bool ClearOnDraw = true;

    private readonly List<GizmoArgsBase> argsCollection = new List<GizmoArgsBase>();

    // static readonly - маппинг не зависит от состояния конкретного экземпляра,
    // не нужно пересоздавать его на каждый GizmoDrawer.
    private static readonly Dictionary<Enumeration, Action<GizmoArgsBase>> drawActions = new Dictionary<Enumeration, Action<GizmoArgsBase>>()
    {
        { GizmoEnumerations.Line,       (args) => DrawLine(args as GizmoLineArgs) },
        { GizmoEnumerations.Ray,        (args) => DrawRay(args as GizmoRayArgs) },
        { GizmoEnumerations.Mesh,       (args) => DrawMesh(args as GizmoMeshArgs) },
        { GizmoEnumerations.Sphere,     (args) => DrawSphere(args as GizmoSphereArgs) },
        { GizmoEnumerations.WireSphere, (args) => DrawWireSphere(args as GizmoWireSphereArgs) },
        { GizmoEnumerations.Cube,       (args) => DrawCube(args as GizmoCubeArgs) },
        { GizmoEnumerations.WireCube,   (args) => DrawWireCube(args as GizmoWireCubeArgs) },
    };

    public void AddGizmoArgs(GizmoArgsBase args)
    {
        argsCollection.Add(args);
    }

    private void Start()
    {
        StartCoroutine(ClearAtEndOfFrame());
    }

    private void OnDrawGizmos()
    {
        foreach (var args in argsCollection)
        {
            if (args == null)
                continue;

            // OnDrawGizmos вызывается на каждый repaint Scene view, поэтому исключение
            // отсюда засыпает консоль и обрывает отрисовку гизмо других объектов.
            if (!drawActions.TryGetValue(args.GizmoType, out var draw))
            {
                PRLog.WriteWarning(this, $"Нет обработчика отрисовки для GizmoType '{args.GizmoType}'.");
                continue;
            }

            Gizmos.color = args.Color;
            draw.Invoke(args);
        }
    }

    private IEnumerator ClearAtEndOfFrame()
    {
        while (true)
        {
            yield return new WaitForEndOfFrame();

            if (ClearOnDraw)
                argsCollection.Clear();
        }
    }

    private static void DrawLine(GizmoLineArgs args)
    {
        if (args == null)
            return;

        Gizmos.DrawLine(args.From, args.To);
        args.SetShowing();
    }

    private static void DrawRay(GizmoRayArgs args)
    {
        if (args == null)
            return;

        // Второй параметр Gizmos.DrawRay — вектор направления, а не конечная точка,
        // поэтому из абсолютного To вычитается From.
        if (args.Ray.direction == Vector3.zero)
            Gizmos.DrawRay(args.From, args.To - args.From);
        else
            Gizmos.DrawRay(args.Ray);

        args.SetShowing();
    }

    private static void DrawMesh(GizmoMeshArgs args)
    {
        if (args == null || args.Mesh == null)
            return;

        if (args.Wireframe)
            Gizmos.DrawWireMesh(args.Mesh, args.Position, args.Rotation, args.Scale);
        else
            Gizmos.DrawMesh(args.Mesh, args.Position, args.Rotation, args.Scale);

        args.SetShowing();
    }

    private static void DrawSphere(GizmoSphereArgs args)
    {
        if (args == null)
            return;

        Gizmos.DrawSphere(args.Center, args.Radius);
        args.SetShowing();
    }

    private static void DrawWireSphere(GizmoSphereArgs args)
    {
        if (args == null)
            return;

        Gizmos.DrawWireSphere(args.Center, args.Radius);
        args.SetShowing();
    }

    private static void DrawCube(GizmoCubeArgs args)
    {
        if (args == null)
            return;

        Gizmos.DrawCube(args.Center, args.Size);
        args.SetShowing();
    }

    private static void DrawWireCube(GizmoCubeArgs args)
    {
        if (args == null)
            return;

        Gizmos.DrawWireCube(args.Center, args.Size);
        args.SetShowing();
    }
}