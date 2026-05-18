using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GizmoDrawerHost : PRMonoBehaviourSingletonBase<GizmoDrawer>
{
    
}

public class GizmoDrawer : MonoBehaviour
{
    public bool ClearOnDraw = true;

    private List<GizmoArgsBase> argsCollection = new List<GizmoArgsBase>();

    private Dictionary<Enumeration, Action<GizmoArgsBase>> drawActions = new Dictionary<Enumeration, Action<GizmoArgsBase>>()
    {
        { GizmoEnumerationProvider.Line, (args) => { DrawLine(args as GizmoLineArgs); } },
        { GizmoEnumerationProvider.Ray, (args) => { DrawRay(args as GizmoRayArgs); } },
        { GizmoEnumerationProvider.Sphere, (args) => { DrawSphere(args as GizmoSphereArgs); } },
        { GizmoEnumerationProvider.WireSphere, (args) => { DrawWireSphere(args as GizmoWireSphereArgs); } },
        { GizmoEnumerationProvider.Cube, (args) => { DrawCube(args as GizmoCubeArgs); } },
        { GizmoEnumerationProvider.WireCube, (args) => { DrawWireCube(args as GizmoWireCubeArgs); } },
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
            Gizmos.color = args.Color;
            drawActions[args.GizmoType].Invoke(args);
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
        Gizmos.DrawLine(args.From, args.To);
        args.SetShowing();
    }

    private static void DrawRay(GizmoRayArgs args)
    {
        if(args.Ray.direction == Vector3.zero)
            Gizmos.DrawRay(args.From, args.To);
        else
            Gizmos.DrawRay(args.Ray);

        args.SetShowing();
    }

    private static void DrawSphere(GizmoSphereArgs args)
    {
        Gizmos.DrawSphere(args.Center, args.Radius);
        args.SetShowing();
    }

    private static void DrawWireSphere(GizmoSphereArgs args)
    {
        Gizmos.DrawWireSphere(args.Center, args.Radius);
        args.SetShowing();
    }

    private static void DrawCube(GizmoCubeArgs args)
    {
        Gizmos.DrawCube(args.Center, args.Size);
        args.SetShowing();
    }

    private static void DrawWireCube(GizmoCubeArgs args)
    {
        Gizmos.DrawWireCube(args.Center, args.Size);
        args.SetShowing();
    }
}
