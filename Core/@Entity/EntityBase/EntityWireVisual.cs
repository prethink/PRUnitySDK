using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Из каких рёбер строится каркас.
/// </summary>
internal enum EntityWireStyle
{
    /// <summary>
    /// Все рёбра всех треугольников, с диагоналями внутри граней.
    /// </summary>
    Triangles,

    /// <summary>
    /// Только рёбра граней: ребро между треугольниками одной плоскости не рисуется.
    /// </summary>
    Polygons
}

/// <summary>
/// Визуальная копия выключенной сущности без игровых компонентов и коллайдеров.
/// </summary>
internal sealed class EntityWireVisual : IDisposable
{
    private static readonly int SurfaceGridId = Shader.PropertyToID("_SurfaceGrid");

    /// <summary>
    /// Наибольший угол между нормалями соседних треугольников, при котором они считаются
    /// одной гранью, в градусах.
    /// </summary>
    /// <remarks>
    /// Не ноль: вершины после импорта и сжатия лежат в плоскости лишь приблизительно,
    /// и диагональ ровного квадрата иначе то появлялась бы, то нет.
    /// </remarks>
    private const float CoplanarAngle = 1f;

    /// <summary>
    /// Во сколько раз увеличиваются координаты перед округлением, когда совпадающие
    /// вершины сводятся в одну.
    /// </summary>
    private const float WeldPrecision = 10000f;

    private static readonly int WireColorId = Shader.PropertyToID("_WireColor");
    private static readonly int FillColorId = Shader.PropertyToID("_FillColor");

    /// <summary>
    /// Материалы по видам каркаса: у каждого свой цвет линий.
    /// </summary>
    /// <remarks>
    /// Общие на все каркасы вида, а не свои у каждого: спрятанных блоков на уровне десятки,
    /// и материал на каждый множил бы вызовы отрисовки.
    /// </remarks>
    private static readonly Dictionary<EntityWireStyle, (Material Line, Material Surface)> materials = new();

    private static Shader wireShader;

    private readonly GameObject root;
    private readonly List<Mesh> ownedMeshes = new();
    private readonly EntityWireStyle style;
    private readonly Material lineMaterial;
    private readonly Material surfaceMaterial;

    private EntityWireVisual(GameObject source, EntityWireStyle style, Material lineMaterial, Material surfaceMaterial)
    {
        this.style = style;
        this.lineMaterial = lineMaterial;
        this.surfaceMaterial = surfaceMaterial;
        root = new GameObject($"{source.name} Wire");
        root.transform.SetParent(source.transform.parent, false);
        CopyTransform(source.transform, root.transform);
        CopyRenderers(source.transform, root.transform);
    }

    /// <summary>
    /// Создаёт каркас на месте видимых MeshRenderer и SkinnedMeshRenderer.
    /// </summary>
    /// <param name="source">Сущность, на месте которой встаёт каркас.</param>
    /// <param name="style">Из каких рёбер строить каркас.</param>
    public static EntityWireVisual Create(GameObject source, EntityWireStyle style)
    {
        if (!TryGetMaterials(style, out Material line, out Material surface))
            return null;

        return new EntityWireVisual(source, style, line, surface);
    }

    /// <remarks>
    /// Цвета берутся из настроек при каждом создании: поправленный в игре цвет ложится
    /// на следующий спрятанный блок, а через общий материал — и на уже спрятанные.
    /// </remarks>
    private static bool TryGetMaterials(EntityWireStyle style, out Material line, out Material surface)
    {
        line = null;
        surface = null;

        if (wireShader == null)
        {
            wireShader = Resources.Load<Shader>("PRUnitySDK/EntityWire");
            if (wireShader == null)
            {
                Debug.LogError("Не найден шейдер PRUnitySDK/EntityWire для режимов HideWire и HideWirePolygons.");
                return false;
            }
        }

        if (!materials.TryGetValue(style, out var pair) || pair.Line == null || pair.Surface == null)
        {
            pair = (new Material(wireShader) { hideFlags = HideFlags.HideAndDontSave },
                new Material(wireShader) { hideFlags = HideFlags.HideAndDontSave });
            pair.Surface.SetFloat(SurfaceGridId, 1f);
            materials[style] = pair;
        }

        ApplyColors(style, pair.Line, pair.Surface);

        line = pair.Line;
        surface = pair.Surface;
        return true;
    }

    private static void ApplyColors(EntityWireStyle style, Material line, Material surface)
    {
        EntitySettings settings = PRUnitySDK.Settings.Entity;
        Color wireColor = style == EntityWireStyle.Polygons
            ? settings.PolygonWireColor
            : settings.TriangleWireColor;

        line.SetColor(WireColorId, wireColor);
        surface.SetColor(WireColorId, wireColor);
        surface.SetColor(FillColorId, settings.WireFillColor);
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
        Mesh wireMesh = sourceMesh.isReadable ? BuildLineMesh(sourceMesh, style) : null;
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

    private static Mesh BuildLineMesh(Mesh source, EntityWireStyle style)
    {
        Vector3[] vertices = source.vertices;
        List<int> indices = style == EntityWireStyle.Polygons
            ? CollectPolygonEdges(source, vertices)
            : CollectTriangleEdges(source);

        if (indices.Count == 0)
            return null;

        var result = new Mesh
        {
            name = $"{source.name} Wire",
            indexFormat = source.vertexCount > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16,
            vertices = vertices
        };
        result.SetIndices(indices, MeshTopology.Lines, 0, false);
        result.bounds = source.bounds;
        return result;
    }

    private static List<int> CollectTriangleEdges(Mesh source)
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

        return indices;
    }

    private static void AddEdge(int a, int b, HashSet<ulong> edges, List<int> indices)
    {
        if (!edges.Add(EdgeKey(a, b)))
            return;

        indices.Add(a);
        indices.Add(b);
    }

    /// <summary>
    /// Ребро по обе стороны: какие треугольники к нему примыкают.
    /// </summary>
    private sealed class PolygonEdge
    {
        public int A;
        public int B;
        public Vector3 FirstNormal;
        public int TriangleCount;
        public bool IsCrease;
    }

    /// <summary>
    /// Собирает рёбра граней: те, что стоят на изломе поверхности или на её краю.
    /// </summary>
    /// <remarks>
    /// Модель приходит уже разбитой на треугольники, исходные многоугольники после импорта
    /// не сохраняются. Поэтому грань восстанавливается по плоскости: ребро, по обе стороны
    /// которого треугольники лежат в одной плоскости, — внутреннее, и его не рисуем.
    /// Ровная сетка из нескольких квадратов тоже сольётся в одну грань.
    /// <para>
    /// Соседство ищется по положению вершин, а не по номерам: на острых рёбрах и швах
    /// развёртки вершины раздваиваются, и треугольники одной грани по номерам соседями
    /// бы не оказались — диагональ осталась бы на месте.
    /// </para>
    /// </remarks>
    private static List<int> CollectPolygonEdges(Mesh source, Vector3[] vertices)
    {
        int[] welded = WeldVertices(vertices);
        var edges = new Dictionary<ulong, PolygonEdge>();
        var order = new List<PolygonEdge>();
        float creaseCos = Mathf.Cos(CoplanarAngle * Mathf.Deg2Rad);

        for (int subMesh = 0; subMesh < source.subMeshCount; subMesh++)
        {
            if (source.GetTopology(subMesh) != MeshTopology.Triangles)
                continue;

            int[] triangles = source.GetIndices(subMesh);
            for (int i = 0; i + 2 < triangles.Length; i += 3)
            {
                int a = triangles[i];
                int b = triangles[i + 1];
                int c = triangles[i + 2];

                Vector3 normal = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);

                // У вырожденного треугольника плоскости нет, и соседей он бы только путал.
                if (normal.sqrMagnitude < 1e-12f)
                    continue;

                normal.Normalize();

                AddPolygonEdge(a, b, normal, welded, edges, order, creaseCos);
                AddPolygonEdge(b, c, normal, welded, edges, order, creaseCos);
                AddPolygonEdge(c, a, normal, welded, edges, order, creaseCos);
            }
        }

        var indices = new List<int>();

        foreach (PolygonEdge edge in order)
        {
            // Одним треугольником ребро держится на краю поверхности, тремя и больше —
            // на стыке нескольких: и то и другое граница грани.
            if (edge.TriangleCount == 2 && !edge.IsCrease)
                continue;

            indices.Add(edge.A);
            indices.Add(edge.B);
        }

        return indices;
    }

    private static void AddPolygonEdge(int a, int b, Vector3 normal, int[] welded,
        Dictionary<ulong, PolygonEdge> edges, List<PolygonEdge> order, float creaseCos)
    {
        // Ребро, стянутое сваркой в точку, линии не даёт.
        if (welded[a] == welded[b])
            return;

        ulong key = EdgeKey(welded[a], welded[b]);

        if (!edges.TryGetValue(key, out PolygonEdge edge))
        {
            edge = new PolygonEdge { A = a, B = b, FirstNormal = normal };
            edges.Add(key, edge);
            order.Add(edge);
        }
        else if (Vector3.Dot(edge.FirstNormal, normal) < creaseCos)
        {
            edge.IsCrease = true;
        }

        edge.TriangleCount++;
    }

    /// <summary>
    /// Сводит вершины с одинаковым положением к одному номеру.
    /// </summary>
    private static int[] WeldVertices(Vector3[] vertices)
    {
        var result = new int[vertices.Length];
        var byPosition = new Dictionary<Vector3Int, int>(vertices.Length);

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 v = vertices[i] * WeldPrecision;
            var key = new Vector3Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));

            if (!byPosition.TryGetValue(key, out int index))
            {
                index = i;
                byPosition.Add(key, index);
            }

            result[i] = index;
        }

        return result;
    }

    private static ulong EdgeKey(int a, int b)
    {
        uint first = (uint)Mathf.Min(a, b);
        uint second = (uint)Mathf.Max(a, b);
        return ((ulong)first << 32) | second;
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
