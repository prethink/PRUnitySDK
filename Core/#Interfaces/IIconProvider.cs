using UnityEngine;

/// <summary>
/// ќпредел€ет интерфейс дл€ предметов, которые имеют иконку.
/// »спользуетс€ дл€ отображени€ предмета в UI, инвентаре, магазинах и т.п.
/// </summary>
public interface IIconProvider
{
    /// <summary>
    /// »конка, визуально представл€юща€ предмет.
    /// </summary>
    Sprite Icon { get; }
}
