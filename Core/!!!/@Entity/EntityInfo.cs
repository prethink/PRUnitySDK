using System;
using UnityEngine;

//[CreateAssetMenu(fileName = "Entity Info", menuName = "PRUnitySDK/Entities/Entity Info")]
public abstract class EntityInfoBase : ScriptableObject, IEntityInfo
{
    public abstract Guid TypeGuid { get; }
    [field: SerializeField] public string Name { get; protected set; }

    [field:SerializeField] public Sprite Icon { get; protected set; }
}
