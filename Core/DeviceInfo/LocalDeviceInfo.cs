using UnityEngine;

public sealed class LocalDeviceInfo : DeviceInfoBase
{
    public override bool IsDesktop()
    {
        return SystemInfo.deviceType == DeviceType.Desktop;
    }

    public override bool IsMobile()
    {
        return SystemInfo.deviceType == DeviceType.Handheld && !IsTablet();
    }

    public override bool IsTablet()
    {
        if (SystemInfo.deviceType != DeviceType.Handheld)
            return false;

        float dpi = Screen.dpi;
        float widthInches = Screen.width / dpi;
        float heightInches = Screen.height / dpi;
        float diagonal = Mathf.Sqrt(widthInches * widthInches + heightInches * heightInches);

        return diagonal >= 7f;
    }

    public override bool IsTV()
    {
        return Application.platform == RuntimePlatform.tvOS;
    }

    public override bool IsIOS()
    {
        return Application.platform == RuntimePlatform.IPhonePlayer;
    }
}