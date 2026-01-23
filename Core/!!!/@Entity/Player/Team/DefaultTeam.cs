using System;
using UnityEngine;

public class DefaultTeam : IPlayerTeam
{
    public Guid Guid => Guid.Parse(TeamGuids.DefaultTeamGuid);

    public string Name => "Default Team";

    public string Description => "";

    public Color Color => Color.gray;
}
