using System.Collections.Generic;
using UnityEngine;


public class PRPhysics : PRMonoBehaviourSingletonBase<PRPhysicsHost> { }

[RequireComponent(typeof(GizmoDrawer))]
public class PRPhysicsHost : PRMonoBehaviour
{
    private GizmoDrawer gizmoDrawer;

    public static bool DebugEnabled => PRUnitySDK.Settings.Project.PhysicsDebug;

    protected override void Awake()
    {
        base.Awake();

        gizmoDrawer = GetComponent<GizmoDrawer>();
    }

    private void SendToGizmos(GizmoArgsBase args)
    {
        gizmoDrawer?.AddGizmoArgs(args);
    }

    // =========================
    // === RAYCAST ===
    // =========================

    public bool RaycastHell(int rayCount, Vector3 origin, Vector3 direction, float radius, int layerMask, out List<RaycastHit> hits)
    {
        bool isHit = false;
        hits = new List<RaycastHit>();
        for (int i = 0; i < rayCount; i++)
        {
            float angle = i * Mathf.PI * 2f / rayCount;

            float x = Mathf.Cos(angle);
            float z = Mathf.Sin(angle);

            bool rayHit = false;

            var offset = new Vector3(x, 0, z) * radius;
            var rayOrigin = origin + offset;
            if (Raycast(rayOrigin, direction, out var hit))
            {
                rayHit = true;
                isHit = true;
                hits.Add(hit);
                Draw(rayOrigin, direction, hit.distance, true, Color.gray);
            }
            else
            {
                Draw(rayOrigin, direction, 1f, false, Color.red);
            }

        }

        return isHit;
    }

    public RaycastHit FindBestByMoveDirection(Vector3 origin, Vector3 moveDirection, IEnumerable<RaycastHit> hits)
    {
        RaycastHit bestHit = default;
        float bestDot = -Mathf.Infinity;

        // убираем вертикаль
        moveDirection.y = 0f;
        moveDirection.Normalize();

        foreach (var hit in hits)
        {
            Vector3 toHit = hit.point - origin;
            toHit.y = 0f;

            if (toHit.sqrMagnitude < 0.0001f)
                continue;

            toHit.Normalize();

            float dot = Vector3.Dot(moveDirection, toHit);

            if (dot > bestDot)
            {
                bestDot = dot;
                bestHit = hit;
            }
        }

        return bestHit;
    }

    public bool Raycast(Vector3 origin, Vector3 direction)
    {
        bool result = Physics.Raycast(origin, direction);
        Draw(origin, direction, 100f, result);
        return result;
    }

    public bool Raycast(Vector3 origin, Vector3 direction, float distance)
    {
        bool result = Physics.Raycast(origin, direction, distance);
        Draw(origin, direction, distance, result);
        return result;
    }

    public bool Raycast(Vector3 origin, Vector3 direction, float distance, int layerMask)
    {
        bool result = Physics.Raycast(origin, direction, distance, layerMask);
        Draw(origin, direction, distance, result);
        return result;
    }

    public bool Raycast(Vector3 origin, Vector3 direction, float distance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
    {
        bool result = Physics.Raycast(origin, direction, distance, layerMask, queryTriggerInteraction);
        Draw(origin, direction, distance, result);
        return result;
    }

    public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hit)
    {
        bool result = Physics.Raycast(origin, direction, out hit);
        Draw(origin, direction, 100f, result);
        return result;
    }

    public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hit, float distance)
    {
        bool result = Physics.Raycast(origin, direction, out hit, distance);
        Draw(origin, direction, distance, result);
        return result;
    }

    public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hit, float distance, int layerMask)
    {
        bool result = Physics.Raycast(origin, direction, out hit, distance, layerMask);
        Draw(origin, direction, distance, result);
        return result;
    }

    public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hit, float distance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
    {
        bool result = Physics.Raycast(origin, direction, out hit, distance, layerMask, queryTriggerInteraction);
        Draw(origin, direction, distance, result);
        return result;
    }

    // =========================
    // === SPHERE CAST ===
    // =========================

    public bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hit, float distance, PRPhysicsOptions? options = null)
    {
        bool result = Physics.SphereCast(origin, radius, direction, out hit, distance);
        DrawSphereCast(origin, radius, direction, distance, result, options);

        return result;
    }

    public bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hit, float distance, int layerMask, PRPhysicsOptions? options = null)
    {
        bool result = Physics.SphereCast(origin, radius, direction, out hit, distance, layerMask);
        DrawSphereCast(result ? hit.point : origin, radius, direction, distance, result, options);

        return result;
    }

    public bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hit, float distance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PRPhysicsOptions? options = null)
    {
        bool result = Physics.SphereCast(origin, radius, direction, out hit, distance, layerMask, queryTriggerInteraction);
        DrawSphereCast(origin, radius, direction, distance, result, options);
        return result;
    }

    private void DrawSphereCast(Vector3 origin, float radius, Vector3 direction, float distance, bool result, PRPhysicsOptions? options = null)
    {
        if (options != null && options.DebugEnabled)
        {
            gizmoDrawer.AddGizmoArgs(new GizmoWireSphereArgs(origin + direction * distance, radius, result ? options.PositiveColor : options.NegativeColor));
        }
        else if (options == null && DebugEnabled)
        {
            gizmoDrawer.AddGizmoArgs(new GizmoWireSphereArgs(origin + direction * distance, radius, result ? Color.green : Color.red));
        }
    }

    // =========================
    // === CHECK SPHERE ===
    // =========================

    public bool CheckSphere(Vector3 position, float radius, int layerMask)
    {
        bool result = Physics.CheckSphere(position, radius, layerMask);

        if (DebugEnabled)
            DrawWireSphere(position, radius, result ? Color.green : Color.red);

        return result;
    }

    // =========================
    // === DRAW HELPERS ===
    // =========================

    private void Draw(Vector3 origin, Vector3 direction, float distance, bool hit, Color? color = null)
    {
        if (!DebugEnabled) return;

        Debug.DrawRay(
            origin,
            direction.normalized * distance,
            color ?? (hit ? Color.green : Color.red)
        );
    }

    private void DrawWireSphere(Vector3 position, float radius, Color color)
    {
        Debug.DrawLine(position + Vector3.up * radius, position - Vector3.up * radius, color);
        Debug.DrawLine(position + Vector3.right * radius, position - Vector3.right * radius, color);
        Debug.DrawLine(position + Vector3.forward * radius, position - Vector3.forward * radius, color);
    }

    private void OnDrawGizmos()
    {
        if (!DebugEnabled)
            return;

       
    }
}

public class PRPhysicsOptions
{
    public bool DebugEnabled;
    public Color PositiveColor = Color.green;
    public Color NegativeColor = Color.red;
    public float? radius;
}