using System;
using UnityEngine;

public abstract class EntityInfoBase : ScriptableObject, IEntityInfo
{
    public abstract Guid TypeGuid { get; }
    [field: SerializeField] public string Name { get; protected set; }

    [field:SerializeField] public Sprite Icon { get; protected set; }
}
