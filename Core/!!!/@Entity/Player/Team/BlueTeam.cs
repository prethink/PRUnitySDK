using System;
using UnityEngine;

public class BlueTeam : IPlayerTeam
{
    public Guid Guid => Guid.Parse(TeamGuids.BlueTeamGuid);

    public string Name => "Blue Team";

    public string Description => "";

    public Color Color => Color.blue;
}
