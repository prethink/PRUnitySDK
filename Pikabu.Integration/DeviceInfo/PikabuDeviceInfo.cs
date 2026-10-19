#if !PRSDK_DISABLE_PIKABU
using System;
using UnityEngine;

/// <summary>
/// Устройство игрока в браузере на Пикабу.
/// </summary>
/// <remarks>
/// Площадка устройство не сообщает. Телефон определяет браузер (<see cref="Application.isMobilePlatform"/>),
/// планшет от телефона не отличается, телевизора нет. iOS — по системе, которую сообщает браузер.
/// </remarks>
public class PikabuDeviceInfo : DeviceInfoBase
{
    public override bool IsDesktop()
    {
        return !Application.isMobilePlatform;
    }

    public override bool IsMobile()
    {
        return Application.isMobilePlatform;
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
