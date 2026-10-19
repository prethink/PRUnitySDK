#if !PRSDK_DISABLE_VK
using UnityEngine;

/// <summary>
/// Устройство игрока по платформе запуска VK (<c>vk_platform</c>).
/// </summary>
/// <remarks>
/// <c>desktop_web</c> — компьютер, <c>mobile_ipad</c> — планшет, остальные <c>mobile_*</c> — телефон; в Одноклассниках
/// те же имена с окончанием <c>_ok</c>. Без площадки устройство определяет браузер.
/// </remarks>
public class VKDeviceInfo : DeviceInfoBase
{
    private static string Platform => VKBridge.Launch?.platform ?? string.Empty;

    private static bool HasPlatform => !string.IsNullOrEmpty(Platform);

    public override bool IsDesktop()
    {
        return HasPlatform ? Platform.StartsWith("desktop") : !Application.isMobilePlatform;
    }

    public override bool IsMobile()
    {
        return HasPlatform ? Platform.StartsWith("mobile") && !IsTablet() : Application.isMobilePlatform;
    }

    public override bool IsTablet()
    {
        return Platform.StartsWith("mobile_ipad");
    }

    public override bool IsTV()
    {
        return false;
    }

    public override bool IsIOS()
    {
        return Platform.StartsWith("mobile_iphone") || Platform.StartsWith("mobile_ipad")
               || (SystemInfo.operatingSystem ?? string.Empty).Contains("iPhone");
    }
}
#endif
