using System;
using UnityEngine;

public abstract class EntityInfoBase : ScriptableObject, IEntityInfo
{
    [field: SerializeField] public string Name { get; protected set; }

    [field:SerializeField] public Sprite Icon { get; protected set; }
}
