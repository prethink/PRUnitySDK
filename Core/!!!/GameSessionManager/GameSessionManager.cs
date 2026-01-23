using System;
using System.Collections.Generic;

public class GameSessionManager 
{
    public PlayerTracker PlayerTracker { get; private set; }
    public EntityTracker EntityTracker { get; private set; }

    public RoundSession Rounds { get; private set; }

    public bool IsActiveSession { get; private set; }

    public bool IsRoundAction => Rounds.IsRoundActive;

    public event Action OnSessionStart;
    public event Action OnSessionEnd;
    public event Action OnPreparingData;

    public void StartPreparingData()
    {
        OnPreparingData?.Invoke();
    }

    public void StartSession()
    {
        IsActiveSession = true;
        OnSessionStart?.Invoke();
    }

    public void EndSession()
    {
        WriteResults();
        EntityTracker.Clear();
        PlayerTracker.Clear();
        IsActiveSession = false;
        OnSessionEnd?.Invoke();
    }

    public void EndSessionWithDestroy()
    {
        WriteResults();
        DestroyAllEntites(EntityLifeTime.Session);
        IsActiveSession = false;
        OnSessionEnd?.Invoke();
    }

    public void DestroyAllEntites(EntityLifeTime lifeTime)
    {
        if(lifeTime == EntityLifeTime.Session)
        {
            EntityTracker.ClearSession();
            PlayerTracker.ClearSession();
        }

        if (lifeTime == EntityLifeTime.Round)
        {
            EntityTracker.ClearRound();
            PlayerTracker.ClearRound();
        }

    }

    public RoundData GetCurrentRoundData(PlayerBase player)
    {
        return Rounds.GetOrCreateCurrentRoundData(player);
    }

    public void WriteResults()
    {

    }

    public void Reset()
    {
        EndSession();
        SetDefaultValues();
        EntityTracker.Clear();
        PlayerTracker.Clear();
        //poolSystem.ClearData();
        StartPreparingData();
        StartSession();
    }

    private void SetDefaultValues()
    {
        RestoreDefaultGameSpeed();
    }

    public void RestoreDefaultGameSpeed()
    {
        //GameSpeed.OnNext(gameSettings.BaseGameSettings.BaseGameSpeed);
    }

    public void ChangeGameSpeed(float gameSpeed)
    {
        //if(GameSpeed.Value == gameSettings.BaseGameSettings.BaseGameSpeed)
        //    GameSpeed.OnNext(gameSpeed);
    }

    //public GameSessionSettings Settings => PRUnitySDK.Settings.GameSession;
    //public GlobalGameSettings gameSettings => PRUnitySDK.Settings.Glo;
    //private ObjectPoolSystem poolSystem;

    //public GameSessionManager(ObjectPoolSystem poolSystem)
    //{
    //    this.PlayerTracker = new PlayerTracker(this);
    //    this.EntityTracker = new EntityTracker(this);
    //    this.Rounds = new RoundSession(this);
    //    this.Settings = sessionSettings;
    //    this.poolSystem = poolSystem;
    //    this.gameSettings = gameSettings;

    //    SetDefaultValues();
    //}
}
