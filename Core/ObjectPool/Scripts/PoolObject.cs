using System;
using UnityEngine;

public class PoolObject : IDisposable
{
    public PoolObjectType PoolObjectType { get; set; }
    public string Type { get; set; }
    public string Category { get; set; }
    public GameObject InstanceGameObject { get; set; }
    public TimeSpan Lifetime { get; set; }

    public readonly Guid Guid = Guid.NewGuid();

    private bool disposed;

    public void UpdateData()
    {
        UpdateData(this);
    }

    public static void UpdateData(PoolObject instance)
    {
        if(instance.InstanceGameObject.TryGetComponent<ParticleSystem>(out var particleSystem))
        {
            instance.PoolObjectType = PoolObjectType.Particles;
            if (!particleSystem.main.loop)
                instance.Lifetime = TimeSpan.FromSeconds(particleSystem.main.duration + particleSystem.main.startLifetime.constantMax);
        }  
    }

    public PoolObject(string category, string action, GameObject instance)
    {
        this.Type = category;
        this.Category = action;
        this.InstanceGameObject = instance;
        UpdateData();
    }

    public void Dispose()
    {
        if(disposed) 
            return;

        if (InstanceGameObject != null)
            MonoBehaviour.Destroy(InstanceGameObject);

        disposed = true;
    }
}
