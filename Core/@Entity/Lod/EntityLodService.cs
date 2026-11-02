using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Считает уровень детализации всех <see cref="EntityLod"/> по камере игрока.
/// </summary>
/// <remarks>
/// Видимость и расстояние даёт <see cref="CullingGroup"/>: Unity проверяет сферы сущностей в той же
/// отрисовке, что и меши, — без своих проверок пирамиды и перебора камер. Камера — <see cref="Camera.main"/>
/// (камера игрока помечена MainCamera); нет камеры или детализация выключена в настройках — все живут полностью.
/// <para>
/// Уровень: ближе <see cref="EntityLodSettings.NearDistance"/> — <see cref="EntityLodLevel.Full"/>, дальше
/// в кадре — <see cref="EntityLodLevel.Reduced"/>, дальше вне кадра — <see cref="EntityLodLevel.Hidden"/>.
/// </para>
/// </remarks>
public class EntityLodService : PRMonoBehaviourSingletonBase<EntityLodService>
{
    private readonly List<EntityLod> members = new();

    private readonly float[] distanceBands = new float[1];

    private BoundingSphere[] spheres = new BoundingSphere[64];

    private CullingGroup group;

    private Camera viewer;

    /// <summary>
    /// Сдвиг за кадр, после которого видимость по прошлой отрисовке не в счёт: телепорт, респаун, прыжок камеры.
    /// Обычный бег — доли метра за кадр.
    /// </summary>
    private const float JumpDistance = 4f;

    private Vector3 lastCameraPosition;

    private bool hasLastCamera;

    /// <summary>
    /// Сколько сущностей под детализацией.
    /// </summary>
    public int Count => members.Count;

    internal void Register(EntityLod lod)
    {
        if (lod == null || lod.ServiceIndex >= 0)
            return;

        lod.ServiceIndex = members.Count;
        lod.SettleFrame = Time.frameCount + 1;
        members.Add(lod);

        if (spheres.Length < members.Count)
        {
            System.Array.Resize(ref spheres, spheres.Length * 2);
            group?.SetBoundingSpheres(spheres);
        }

        spheres[lod.ServiceIndex] = lod.WorldSphere;
        group?.SetBoundingSphereCount(members.Count);
    }

    internal void Unregister(EntityLod lod)
    {
        int index = lod != null ? lod.ServiceIndex : -1;

        if (index < 0 || index >= members.Count || members[index] != lod)
            return;

        // На место ушедшего — последний: список без дыр, как и массив сфер у CullingGroup.
        int last = members.Count - 1;

        if (index != last)
        {
            EntityLod moved = members[last];
            members[index] = moved;
            spheres[index] = spheres[last];
            moved.ServiceIndex = index;
            moved.SettleFrame = Time.frameCount + 1;
            moved.HasLastCenter = false;
        }

        members.RemoveAt(last);
        lod.ServiceIndex = -1;
        group?.SetBoundingSphereCount(members.Count);
    }

    protected override void PRUpdate()
    {
        base.PRUpdate();

        EntityLodSettings settings = PRUnitySDK.Settings != null ? PRUnitySDK.Settings.EntityLod : null;
        Camera camera = settings == null || settings.Enabled ? Camera.main : null;

        if (camera == null)
        {
            SetAllFull();
            return;
        }

        SetupGroup(camera, settings != null ? settings.NearDistance : 15f);

        int frame = Time.frameCount;

        // Камера прыгнула (респаун, смена вида): прошлая отрисовка видела другое — кадр всем полной детализации.
        Vector3 cameraPosition = camera.transform.position;
        if (hasLastCamera && (cameraPosition - lastCameraPosition).sqrMagnitude > JumpDistance * JumpDistance)
        {
            foreach (EntityLod member in members)
                member.SettleFrame = frame + 1;
        }

        lastCameraPosition = cameraPosition;
        hasLastCamera = true;

        for (int index = members.Count - 1; index >= 0; index--)
        {
            // Слушатель мог выключить сущность, и список сдвинулся.
            if (index >= members.Count)
                continue;

            EntityLod lod = members[index];
            BoundingSphere sphere = lod.WorldSphere;
            spheres[index] = sphere;

            // CullingGroup отвечает по прошлой отрисовке: сущность, прыгнувшая за кадр, могла уйти в кадр из-за спины.
            if (lod.HasLastCenter && (sphere.position - lod.LastCenter).sqrMagnitude > JumpDistance * JumpDistance)
                lod.SettleFrame = frame + 1;
            lod.LastCenter = sphere.position;
            lod.HasLastCenter = true;

            // Пока данных о новом месте нет — полная детализация: лишняя тень на кадр незаметна, замершая поза — заметна.
            if (frame < lod.SettleFrame)
            {
                lod.SetLevel(EntityLodLevel.Full);
                continue;
            }

            EntityLodLevel level = group.GetDistance(index) == 0
                ? EntityLodLevel.Full
                : group.IsVisible(index) ? EntityLodLevel.Reduced : EntityLodLevel.Hidden;

            lod.SetLevel(level);
        }
    }

    protected override void UnRegisterEventsOnDestroy()
    {
        group?.Dispose();
        group = null;
        base.UnRegisterEventsOnDestroy();
    }

    private void SetupGroup(Camera camera, float nearDistance)
    {
        if (group == null)
        {
            group = new CullingGroup();
            group.SetBoundingSpheres(spheres);
            group.SetBoundingSphereCount(members.Count);
        }

        if (viewer != camera)
        {
            viewer = camera;
            group.targetCamera = camera;
            group.SetDistanceReferencePoint(camera.transform);

            // Сменилась камера — прежние ответы о видимости не про неё.
            int settle = Time.frameCount + 1;
            foreach (EntityLod lod in members)
                lod.SettleFrame = settle;
        }

        if (!Mathf.Approximately(distanceBands[0], nearDistance))
        {
            distanceBands[0] = nearDistance;
            group.SetBoundingDistances(distanceBands);
        }
    }

    private void SetAllFull()
    {
        for (int index = members.Count - 1; index >= 0; index--)
        {
            if (index < members.Count)
                members[index].SetLevel(EntityLodLevel.Full);
        }
    }
}
