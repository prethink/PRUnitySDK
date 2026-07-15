using System;
using UnityEngine;

public interface IPlayerTeam 
{
    public Guid Guid { get; }

    public string Name { get; }

    public string Description { get; }

    public Color Color { get; }
}
