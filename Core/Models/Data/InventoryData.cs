using System;
using System.Collections.Generic;

public class InventoryData : ICloneable
{
    /// <summary>
    /// Ресурсы.
    /// </summary>
    public Dictionary<string, int> Resources = new();

    /// <summary>
    /// Патроны.
    /// </summary>
    public Dictionary<string, int> Ammo = new();

    #region ICloneable

    public object Clone()
    {
        return new InventoryData()
        {
            Resources = new Dictionary<string, int>(Resources),
            Ammo = new Dictionary<string, int>(Ammo),
        };
    }

    #endregion

    #region Конструкторы

    public InventoryData()
    {
        Resources = new Dictionary<string, int>();
        Ammo = new Dictionary<string, int>();
    }

    #endregion
}
