using System;
using UnityEngine;

public class RedTeam : IPlayerTeam
{
    public Guid Guid => Guid.Parse(TeamGuids.RedTeamGuid);

    public string Name => "Red Team";

    public string Description => "";

    public Color Color => Color.red;
}
