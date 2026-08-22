public static class PRGameStorageService 
{

    public static GameSettingsStorage GameSettings = new GameSettingsStorage();

    public static ResourceStorage Resources = new ResourceStorage();
}


/// <summary>
/// База для типизированных хранилищ значений, сгруппированных по категории.
/// Значения лежат в ProjectData.ProjectProperties и обслуживаются
/// <see cref="ProjectPropertiesManager"/> - раньше здесь был отдельный механизм в
/// IGameDataStorage (свой словарь object'ов в PRSaveData), который дублировал
/// ProjectPropertiesManager и был реализован только в Yandex-хранилище, а в
/// PlayerPrefs кидал NotImplementedException.
/// </summary>
public abstract class GameStorageBase
{
    public abstract Enumeration Category { get; }

    protected T GetValue<T>(EnumerationType<T> enumeration, T defaultValue)
    {
        return ProjectPropertiesManager.Instance.GetValue(BuildKey(enumeration), defaultValue);
    }

    protected void SetValue<T>(EnumerationType<T> enumeration, T value, bool IsRequiredSave = true)
    {
        ProjectPropertiesManager.Instance.SetValue(BuildKey(enumeration), value, IsRequiredSave);
    }

    /// <summary>
    /// Категория входит в имя свойства, чтобы одинаковые ключи из разных хранилищ
    /// не пересекались в общих словарях ProjectProperties.
    /// </summary>
    private string BuildKey<T>(EnumerationType<T> enumeration)
    {
        if (enumeration == null)
            throw new System.ArgumentNullException(nameof(enumeration));

        return $"{Category.Value}.{enumeration.Value}";
    }
}

public class GameSettingsStorage : GameStorageBase
{
    public override Enumeration Category => Enumeration.GetOrCreate("GameSettings");

    public float GetSensitivity()
    {
        return 1;//return GetValue<float>(GameSettingsEnumerationProvider.Sensitivity, PRUnitySDK.Settings.Default.Control.Sensitivity);
    }
}

public class ResourceStorage : GameStorageBase
{
    public override Enumeration Category => Enumeration.GetOrCreate("Resources");

    public float GetValue(EnumerationType<float> enumeration, float defaultValue) 
    {
        return base.GetValue<float>(enumeration, defaultValue);
    }

    public void SetValue(EnumerationType<float> enumeration, float value, bool IsRequiredSave = true) 
    {
        base.SetValue<float>(enumeration, value, IsRequiredSave);
    }

    public void AddValue(EnumerationType<float> enumeration, float value, bool IsRequiredSave = true)
    {
        var originValue = this.GetValue(enumeration, 0);
        SetValue(enumeration, originValue + value, IsRequiredSave);
    }
}

