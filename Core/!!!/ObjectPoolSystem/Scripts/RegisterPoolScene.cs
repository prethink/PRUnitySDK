using System.Collections.Generic;
using UnityEngine;

public class RegisterPoolScene : MonoBehaviour
{
    #region Поля и свойства

    [SerializeField] private List<RegisterPoolObject> registredObjets = new();

    #endregion

    #region Поля и свойства

    public void RegisterItems()
    {
        foreach (var obj in registredObjets)
            poolSystem.RegisterPoolObject(obj.Name, obj.Prefabs, obj.Count);
    }

    #endregion

    #region Zinject

    private ObjectPoolSystem poolSystem;

    #endregion
}
