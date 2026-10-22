using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YG;
using YG.Utils.LB;

public class YandexRatingSystem : IRatingSystem, ISDKEvents, IDisposable
{

    #region IRatingSystem

    public long Rank { get; private set; }

    public HashSet<string> TableName => new HashSet<string>();

    public void RequestRating()
    {
        foreach (var name in TableName)
            YG2.GetLeaderboard(name, ratingSettings.QuantityTop, ratingSettings.QuantityAround, ratingSettings.GetPhotoSettings());
    }

    public IEnumerator UpdateRatingCorutina()
    {
        yield return new WaitForSeconds(ratingSettings.WaitingInitializedSecond);
        do
        {
            RequestRating();
            yield return new WaitForSeconds(ratingSettings.RepeatUpdateSecond);
        }
        while (ratingSettings.RepeatUpdate);
    }

    public void RegisterTableName(string tableName)
    {
        if (TableName.Add(tableName))
            YG2.GetLeaderboard(tableName, ratingSettings.QuantityTop, ratingSettings.QuantityAround, ratingSettings.GetPhotoSettings());
    }

    #endregion

    #region Zinject

    public YandexRatingSystem()
    {
        EventBus.Subscribe(this);
        //this.gameManager.AutoSaveEvent += AutoSaveEvent;
        YG2.onGetLeaderboard += GetLeaderBoard;

        RegisterTableName(ratingSettings.TableName);
    }

    ~YandexRatingSystem()
    {
        EventBus.Unsubscribe(this);
        YG2.onGetLeaderboard -= GetLeaderBoard;
    }

    private void UpdateDataCurrentUser(LBData data)
    {
        PRLog.WriteDebug(this, $"Start update current Rank");
        if (data?.currentPlayer?.rank > 0)
        {
            PRLog.WriteDebug(this, $"Update rating current user, rank = {data.currentPlayer.rank}", new PRLogSettings() { LevelDebug = 2 });

            PRUnitySDK.Managers.ProjectPropertiesManager.SetLong(ConstantsNames.CURRENT_RANK, data.currentPlayer.rank);
            Rank = data.currentPlayer.rank;
            //EventBus.RaiseEvent<INotifyEventUI>(inv => inv.ChangeStateUI(ConstantsNames.CURRENT_RANK, data.currentPlayer.rank.ToString()));
            //if (data.currentPlayer.rank > 0 && data?.currentPlayer?.rank < 4)
                //EventBus.RaiseEvent<IAchievementNotify>(x => x.CompleteTriggerAchievement(AchievementTriggerDropDown.TOP_3));
        }
        PRLog.WriteDebug(this, $"End update current Rank");
    }

    private void GetLeaderBoard(LBData data)
    {
        try
        {
            UpdateDataCurrentUser(data);
        }
        catch (Exception e)
        {
            PRLog.WriteError(this, e.ToString());
        }
    }

    private void AutoSaveEvent()
    {
        //if(gameManager.GetProjectData().AchievementProperties.AchievementData.TryGetValue(AchievementTriggerDropDown.UPDATE_KILL, out var value))
        //    YandexGame.NewLeaderboardScores(ConstantsNames.RATING_TABLE_NAME, value);
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        EventBus.Unsubscribe(this);
        YG2.onGetLeaderboard -= GetLeaderBoard;
    }

    public void OnInitialized()
    {
        PRUnitySDK.Managers.GameManager.StartCoroutine(UpdateRatingCorutina());
    }

    #endregion
}
