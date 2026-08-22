using System;

public class PRSaveData : ICloneable
{
    public string SaveId;

    public DateTime SaveDate;

    public GameSettings GameSettings;

    public ProjectData ProjectData;

    public PRSaveData()
    {
        SaveId = Guid.NewGuid().ToString();
        SaveDate = PRUnitySDK.ServerTime.GetNow();
        GameSettings = new GameSettings();
        ProjectData = new ProjectData();
    }

    public object Clone()
    {
        var data = new PRSaveData();
        data.SaveId = SaveId;
        data.SaveDate = SaveDate;
        data.GameSettings = (GameSettings)GameSettings.Clone();
        data.ProjectData = (ProjectData)ProjectData.Clone();
        return data;
    }
}
