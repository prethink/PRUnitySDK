public static class PRGameStorageService 
{
    public static bool IsReady => PRUnitySDK.IsInitialized;

    public static GameSettingsStorage GameSettings = new GameSettingsStorage();

    public static ResourceStorage Resources = new ResourceStorage();
}


public abstract class GameStorageBase
{
    public abstract Enumeration Category { get; }

    protected T GetValue<T>(Enumeration<T> enumeration, T defaultValue)
    {
        return PRUnitySDK.GameDataStorage.GetValue<T>(Category, enumeration, defaultValue);
    }

    protected void SetValue<T>(Enumeration<T> enumeration, T value, bool IsRequiredSave = true)
    {
        PRUnitySDK.GameDataStorage.SetValue<T>(Category, enumeration, value, IsRequiredSave);
    }
}

public class GameSettingsStorage : GameStorageBase
{
    public override Enumeration Category => new Enumeration("GameSettings");

    public float GetSensitivity()
    {
        return GetValue<float>(GameSettingsEnumerationProvider.Sensitivity, PRUnitySDK.Settings.Default.Control.Sensitivity);
    }
}

public class ResourceStorage : GameStorageBase
{
    public override Enumeration Category => new Enumeration("Resources");

    public float GetValue(Enumeration<float> enumeration, float defaultValue) 
    {
        return base.GetValue<float>(enumeration, defaultValue);
    }

    public void SetValue(Enumeration<float> enumeration, float value, bool IsRequiredSave = true) 
    {
        base.SetValue<float>(enumeration, value, IsRequiredSave);
    }

    public void AddValue(Enumeration<float> enumeration, float value, bool IsRequiredSave = true)
    {
        var originValue = this.GetValue(enumeration, 0);
        SetValue(enumeration, originValue + value, IsRequiredSave);
    }
}

