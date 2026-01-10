public class GlobalGameSettingsSession 
{
    public GlobalGameSettings Data { get; private set; }

    public float GameSpeed { get; private set; }

    public void Reset()
    {
        GameSpeed = Data.BaseGameSettings.BaseGameSpeed;
    }

    public GlobalGameSettingsSession(GlobalGameSettings baseData)
    {
        this.Data = baseData;
        Reset();
    }
}
