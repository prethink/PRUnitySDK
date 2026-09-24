using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Визуальная копия выключенной сущности без игровых компонентов и коллайдеров.
/// </summary>
internal sealed class EntityWireVisual : IDisposable
{
    private static readonly int SurfaceGridId = Shader.PropertyToID("_SurfaceGrid");

    private static Material lineMaterial;
    private static Material surfaceMaterial;

    private readonly GameObject root;
    private readonly List<Mesh> ownedMeshes = new();

    private EntityWireVisual(GameObject source)
    {
        root = new GameObject($"{source.name} Wire");
        root.transform.SetParent(source.transform.parent, false);
        CopyTransform(source.transform, root.transform);
        CopyRenderers(source.transform, root.transform);
    }

    /// <summary>
    /// Создаёт каркас на месте видимых MeshRenderer и SkinnedMeshRenderer.
    /// </summary>
    public static EntityWireVisual Create(GameObject source)
    {
        if (!EnsureMaterials())
            return null;

        return new EntityWireVisual(source);
    }

    private static bool EnsureMaterials()
    {
        if (lineMaterial != null && surfaceMaterial != null)
            return true;

        Shader shader = Resources.Load<Shader>("PRUnitySDK/EntityWire");
        if (shader == null)
        {
            Debug.LogError("Не найден шейдер PRUnitySDK/EntityWire для режима HideWire.");
            return false;
        }

        lineMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        surfaceMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        surfaceMaterial.SetFloat(SurfaceGridId, 1f);
        return true;
    }

    private void CopyRenderers(Transform source, Transform target)
    {
        if (source.gameObject.activeSelf)
        {
            MeshRenderer meshRenderer = source.GetComponent<MeshRenderer>();
            MeshFilter meshFilter = source.GetComponent<MeshFilter>();
            if (meshRenderer != null && meshRenderer.enabled && meshFilter != null && meshFilter.sharedMesh != null)
                AddMesh(target.gameObject, meshFilter.sharedMesh);

            SkinnedMeshRenderer skinnedRenderer = source.GetComponent<SkinnedMeshRenderer>();
            if (skinnedRenderer != null && skinnedRenderer.enabled && skinnedRenderer.sharedMesh != null)
            {
                Mesh bakedMesh = new Mesh();
                skinnedRenderer.BakeMesh(bakedMesh);
                ownedMeshes.Add(bakedMesh);
                AddMesh(target.gameObject, bakedMesh);
            }
        }

        foreach (Transform child in source)
        {
            if (!child.gameObject.activeSelf)
                continue;

            GameObject copy = new GameObject(child.name);
            copy.transform.SetParent(target, false);
            CopyTransform(child, copy.transform);
            CopyRenderers(child, copy.transform);
        }
    }

    private static void CopyTransform(Transform source, Transform target)
    {
        target.gameObject.layer = source.gameObject.layer;
        target.localPosition = source.localPosition;
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;
    }

    private void AddMesh(GameObject target, Mesh sourceMesh)
    {
        Mesh wireMesh = sourceMesh.isReadable ? BuildLineMesh(sourceMesh) : null;
        if (wireMesh != null)
            ownedMeshes.Add(wireMesh);

        target.AddComponent<MeshFilter>().sharedMesh = wireMesh != null ? wireMesh : sourceMesh;
        MeshRenderer renderer = target.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = wireMesh != null ? lineMaterial : surfaceMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    private static Mesh BuildLineMesh(Mesh source)
    {
        var indices = new List<int>();
        var edges = new HashSet<ulong>();

        for (int subMesh = 0; subMesh < source.subMeshCount; subMesh++)
        {
            if (source.GetTopology(subMesh) != MeshTopology.Triangles)
                continue;

            int[] triangles = source.GetIndices(subMesh);
            for (int i = 0; i + 2 < triangles.Length; i += 3)
            {
                AddEdge(triangles[i], triangles[i + 1], edges, indices);
                AddEdge(triangles[i + 1], triangles[i + 2], edges, indices);
                AddEdge(triangles[i + 2], triangles[i], edges, indices);
            }
        }

        if (indices.Count == 0)
            return null;

        var result = new Mesh
        {
            name = $"{source.name} Wire",
            indexFormat = source.vertexCount > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16,
            vertices = source.vertices
        };
        result.SetIndices(indices, MeshTopology.Lines, 0, false);
        result.bounds = source.bounds;
        return result;
    }

    private static void AddEdge(int a, int b, HashSet<ulong> edges, List<int> indices)
    {
        uint first = (uint)Mathf.Min(a, b);
        uint second = (uint)Mathf.Max(a, b);
        ulong key = ((ulong)first << 32) | second;
        if (!edges.Add(key))
            return;

        indices.Add(a);
        indices.Add(b);
    }

    public void Dispose()
    {
        if (root != null)
        {
            root.SetActive(false);
            UnityEngine.Object.Destroy(root);
        }

        foreach (Mesh mesh in ownedMeshes)
            UnityEngine.Object.Destroy(mesh);

        ownedMeshes.Clear();
    }
}
