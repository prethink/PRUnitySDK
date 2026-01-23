using System;
using UnityEngine;

public class EntityInfoImplementer : IEntityInfo
{
    public Guid TypeGuid { get; private set; }

    public string Name { get; private set; }

    public Sprite Icon { get; private set; }

    public EntityInfoImplementer(Guid type, string name, Sprite icon)
    {
        TypeGuid = type;
        Name = name;
        Icon = icon;
    }
}
