using System;
using System.Collections.Generic;
using System.Linq;

public class PlayerTracker : EntityTrackerBase<IPlayer>
{
    #region Поля и свойства

    public List<IPlayer> Players => elements.ToList();

    public int PlayersCount => elements.Count;

    public int HumanCount => elements.Count(x => x.PlayerType == PlayerType.Human);
    public int AICount => elements.Count(x => x.PlayerType == PlayerType.AI);

    private long playerIds;
    private int nextPlayerId = 0;

    private readonly SortedSet<long> freePlayerIds = new();

    #endregion

    #region События

    public event Action<IEntity, PlayerBase> OnPlayerDead;

    #endregion

    #region Методы

    public override void Clear()
    {
        foreach (var entity in elements)
            entity.DestroyEntity(new EntityDestroyOptions { FullDestroy = true });

        elements.Clear();
        ResetIds();
    }

    public void Destroy()
    {
        foreach (var entity in elements)
            entity.DestroyEntity(new EntityDestroyOptions { FullDestroy = true });

        elements.Clear();
        ResetIds();
    }

    private void ResetIds()
    {
        playerIds = 0;
        nextPlayerId = 0;
        freePlayerIds.Clear();
    }

    /// <summary>
    /// ID сущности (общий глобальный ID).
    /// </summary>
    public long GetPlayerEntityId()
    {
        return playerIds++;
    }

    /// <summary>
    /// CS-style Player ID (reuse freed slots).
    /// </summary>
    public long GetPlayerId()
    {
        long id;

        if (freePlayerIds.Count > 0)
        {
            id = freePlayerIds.Min;
            freePlayerIds.Remove(id);
        }
        else
        {
            id = nextPlayerId++;
        }

        return id;
    }

    private void ReleasePlayerId(long id)
    {
        freePlayerIds.Add(id);
    }

    public override bool Register(IPlayer player)
    {
        var playerId = GetPlayerId();

        player.GenerateId(EntityIdGenerator.Instance.RegisterId);
        player.GeneratePlayerId(() => playerId);

        player.JoinGame();
        elements.Add(player);

        PRLog.WriteDebug(this,
            $"Игрок {player.Info.GetName()} - EntityID:{player.Id}, PlayerID:{playerId} зарегистрирован.");

        return true;
    }

    public override bool Unregister(IPlayer player)
    {
        elements.Remove(player);

        ReleasePlayerId(player.PlayerId); 

        PRLog.WriteDebug(this,
            $"Игрок {player.Info.GetName()} - ID:{player.Id} удален из сессии.");

        return true;
    }

    public void InvokeOnPlayerDead(IEntity killer, PlayerBase player)
    {
        OnPlayerDead?.Invoke(killer, player);
    }

    #endregion
}