using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GizmoDrawerHost : PRMonoBehaviourSingletonBase<GizmoDrawer>
{
    // ВНИМАНИЕ: не менял эту часть - без исходника PRMonoBehaviourSingletonBase<T>
    // не могу подтвердить, что параметризация другим классом (GizmoDrawer, а не
    // самим GizmoDrawerHost) действительно работает так, как задумано. Обычно
    // синглтон-база параметризуется самим наследником (class Foo : Base<Foo>).
    // Если AddGizmoArgs у вас вызывается как GizmoDrawerHost.Instance.AddGizmoArgs(...)
    // и это компилируется и работает - оставляйте как есть. Если нет - вероятно,
    // нужен один из вариантов:
    //   public class GizmoDrawer : PRMonoBehaviourSingletonBase<GizmoDrawer> { ... }
    // (и тогда GizmoDrawerHost не нужен вовсе), либо GizmoDrawerHost должен сам
    // быть MonoBehaviour, хранящим статическую ссылку на GizmoDrawer.
}

public class GizmoDrawer : MonoBehaviour
{
    public bool ClearOnDraw = true;

    private readonly List<GizmoArgsBase> argsCollection = new List<GizmoArgsBase>();

    // static readonly - маппинг не зависит от состояния конкретного экземпляра,
    // не нужно пересоздавать его на каждый GizmoDrawer.
    private static readonly Dictionary<Enumeration, Action<GizmoArgsBase>> drawActions = new Dictionary<Enumeration, Action<GizmoArgsBase>>()
    {
        { GizmoEnumerationProvider.Line,       (args) => DrawLine(args as GizmoLineArgs) },
        { GizmoEnumerationProvider.Ray,        (args) => DrawRay(args as GizmoRayArgs) },
        { GizmoEnumerationProvider.Mesh,       (args) => DrawMesh(args as GizmoMeshArgs) },
        { GizmoEnumerationProvider.Sphere,     (args) => DrawSphere(args as GizmoSphereArgs) },
        { GizmoEnumerationProvider.WireSphere, (args) => DrawWireSphere(args as GizmoWireSphereArgs) },
        { GizmoEnumerationProvider.Cube,       (args) => DrawCube(args as GizmoCubeArgs) },
        { GizmoEnumerationProvider.WireCube,   (args) => DrawWireCube(args as GizmoWireCubeArgs) },
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

            // Защита от KeyNotFoundException - OnDrawGizmos вызывается Unity очень
            // часто (каждый repaint Scene view, не только раз в кадр в Play Mode),
            // необработанное исключение здесь может засыпать консоль и в некоторых
            // случаях обрывает отрисовку гизмо для ДРУГИХ объектов в этом же кадре.
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

        // ВАЖНО: Gizmos.DrawRay(Vector3 from, Vector3 direction) - второй параметр
        // это ВЕКТОР НАПРАВЛЕНИЯ (длина = длина луча), а НЕ абсолютная конечная точка,
        // в отличие от DrawLine(from, to). Раньше здесь передавался args.To напрямую
        // как если бы это была точка назначения - луч рисовался в направлении от
        // мировых координат (0,0,0) к To, а не от From к To, с неверной длиной.
        //
        // Если GizmoRayArgs.To у вас хранит АБСОЛЮТНУЮ точку - фикс ниже правильный.
        // Если To уже хранит готовый вектор направления - верните `Gizmos.DrawRay(args.From, args.To)`
        // как было, тогда бага не было и это ложное срабатывание с моей стороны.
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