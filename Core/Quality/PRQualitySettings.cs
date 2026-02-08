using System;
using UnityEngine;

[Serializable]
public class PRQualitySettings
{
    [field: SerializeField]
    public bool UseDefaultColor { get; protected set; } = true;

    [field: SerializeField, Header("Common")] 
    public ulong CommonWeight { get; protected set; } = 100;

    [field: SerializeField] 
    public Color CommonColor { get; protected set; }

    [field: SerializeField, Header("UnCommon")] 
    public ulong UncommonWeight { get; protected set; } = 20;

    [field: SerializeField] 
    public Color UncommonColor { get; protected set; }

    [field: SerializeField, Header("Rare")] 
    public ulong RareWeight { get; protected set; } = 15;

    [field: SerializeField] 
    public Color RareColor { get; protected set; }

    [field: SerializeField, Header("Mythic")] 
    public ulong MythicWeight { get; protected set; } = 2;

    [field: SerializeField] 
    public Color MythicColor { get; protected set; }

    [field: SerializeField, Header("Epic")] 
    public ulong EpicWeight { get; protected set; } = 5;

    [field: SerializeField] 
    public Color EpicColor { get; protected set; }

    [field: SerializeField, Header("Legendary")] 
    public ulong LegendaryWeight { get; protected set; } = 3;

    [field: SerializeField] 
    public Color LegendaryColor { get; protected set; }

    [field: SerializeField, Header("Ancient")] 
    public ulong AncientWeight { get; protected set; } = 1;

    [field: SerializeField] 
    public Color AncientColor { get; protected set; }

    [field: SerializeField, Header("Godlike")] 
    public ulong GodlikeWeight { get; protected set; } = 1;

    [field: SerializeField] 
    public Color GodlikeColor { get; protected set; }
}

