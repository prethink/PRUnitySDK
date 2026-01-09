using YG;

public class YandexDeviceInfo : DeviceInfoBase
{
    public override bool IsDesktop()
    {
        return YG2.envir.isDesktop;
    }

    public override bool IsIOS()
    {
        return YG2.envir.browser.Contains("Safari", System.StringComparison.OrdinalIgnoreCase);
    }

    public override bool IsMobile()
    {
        return YG2.envir.isMobile;
    }

    public override bool IsTablet()
    {
        return YG2.envir.isTablet;
    }

    public override bool IsTV()
    {
        return YG2.envir.isTV;
    }
}
