using System;

public class PRSaveData : ICloneable
{
    public string SaveId;

    /// <summary>
    /// Дата создания сохранения по времени <see cref="PRUnitySDK.ServerTime"/>.
    /// </summary>
    public DateTime SaveDate;

    /// <summary>
    /// Дата последней записи сохранения по времени <see cref="PRUnitySDK.ServerTime"/>.
    /// </summary>
    public DateTime UpdateDate;

    public GameSettings GameSettings;

    public ProjectData ProjectData;

    public PRSaveData()
    {
        SaveId = Guid.NewGuid().ToString();
        SaveDate = PRUnitySDK.ServerTime.GetNow();
        UpdateDate = SaveDate;
        GameSettings = new GameSettings();
        ProjectData = new ProjectData();
    }

    public object Clone()
    {
        var data = new PRSaveData();
        data.SaveId = SaveId;
        data.SaveDate = SaveDate;
        data.UpdateDate = UpdateDate;
        data.GameSettings = (GameSettings)GameSettings.Clone();
        data.ProjectData = (ProjectData)ProjectData.Clone();
        return data;
    }
}
