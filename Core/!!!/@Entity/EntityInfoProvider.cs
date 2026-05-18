using System;
using UnityEngine;

public class EntityInfoProvider : PRMonoBehaviour, IEntityInfo
{
    public EntityInfoBase EntityInfo { get; private set; }

    public Guid TypeGuid => EntityInfo.TypeGuid;

    public string Name => EntityInfo.Name;

    public Sprite Icon => EntityInfo.Icon;
}
