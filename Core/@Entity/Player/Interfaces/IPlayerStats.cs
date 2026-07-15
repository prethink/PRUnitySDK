public interface IPlayerStats 
{
    public void ResetSpeed();

    public float GetBaseSpeed();

    public float GetSpeed();
}

public interface IPlayerStatsDecorator
{
    public void RemoveDecorator();
}
