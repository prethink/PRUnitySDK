using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public partial class PrefabContainer : ScriptableObjectSingleton<PrefabContainer>
{
    [SerializeField] private List<GameObject> core = new();
    [SerializeField] private List<GameObject> modules = new();
    [SerializeField] private List<MonoWindowBase> monoWindows = new();

    public IEnumerable<MonoWindowBase> MonoWindowBases => monoWindows.ToList();
    public IEnumerable<GameObject> Core => core.ToList();
    public IEnumerable<GameObject> Modules => modules.ToList();


    private readonly List<GameObject> prefabObjects = new();

    public IEnumerable<GameObject> GetAll()
    {
        prefabObjects.Clear();

        prefabObjects.AddRange(core);
        prefabObjects.AddRange(modules);
        prefabObjects.AddRange(monoWindows);

        this.RunMethodHooks(MethodHookStage.CreateCollections);

        return prefabObjects;
    }

    public T Get<T>() where T : MonoBehaviour
    {
        var allPrefabs = GetAll();
        foreach (var prefab in allPrefabs)
        {
            var component = prefab.GetComponent<T>();
            if (component != null)
                return component;
        }
        return null;
    }

    public bool TryGet<T>(out T component) 
        where T : MonoBehaviour
    {
        component = Get<T>();
        return component != null;
    }
}
