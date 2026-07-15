using System;

/// <summary>
/// Флаги типов игроков для логики разрешений, фильтрации и т.д.
/// </summary>
[Flags]
public enum PlayerTypeFlags
{
    None = 0,
    Human = 1 << 0,
    AI = 1 << 1,
    NPC = 1 << 2,
}