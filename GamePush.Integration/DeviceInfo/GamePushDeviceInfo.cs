#if !PRSDK_DISABLE_GAMEPUSH
using System;
using GamePush;
using UnityEngine;

/// <summary>
/// Устройство игрока по данным GamePush.
/// </summary>
/// <remarks>
/// GamePush различает только мобильное устройство и компьютер: планшет у него мобильный,
/// телевизора нет вовсе. iOS определяется по системе, которую сообщает браузер.
/// </remarks>
public class GamePushDeviceInfo : DeviceInfoBase
{
    public override bool IsDesktop()
    {
        return GP_Device.IsDesktop();
    }

    public override bool IsMobile()
    {
        return GP_Device.IsMobile();
    }

    public override bool IsTablet()
    {
        return false;
    }

    public override bool IsTV()
    {
        return false;
    }

    public override bool IsIOS()
    {
        string system = SystemInfo.operatingSystem ?? string.Empty;

        return system.IndexOf("iPhone", StringComparison.OrdinalIgnoreCase) >= 0
               || system.IndexOf("iPad", StringComparison.OrdinalIgnoreCase) >= 0
               || system.IndexOf("iOS", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
#endif
