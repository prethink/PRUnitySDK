using System;
using System.Collections.Generic;
using System.Linq;

public class PlayerTracker : EntityTrackerBase<IPlayer>
{
    public const long LocalPlayerOneId = 100_000;
    public const long LocalPlayerTwoId = 200_000;

    public int MaxLocalPlayer => PRUnitySDK.DeviceInfo.IsDesktop() ? 2 : 1;

    #region Поля и свойства

    public List<IPlayer> Players => elements.ToList();

    public int PlayersCount => elements.Count;

    public int HumanCount => elements.Count(x => x.PlayerType == PlayerType.Human);
    public int AICount => elements.Count(x => x.PlayerType == PlayerType.AI);
    public int LocalPlayerCount => playerLocals.Count;

    private long playerIds;
    private int nextPlayerId = 0;

    private readonly SortedSet<long> freePlayerIds = new();

    private readonly Dictionary<int, PlayerLocal> playerLocals = new();

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
        playerLocals.Clear();
        ResetIds();
    }

    public void Destroy()
    {
        foreach (var entity in elements)
            entity.DestroyEntity(new EntityDestroyOptions { FullDestroy = true });

        elements.Clear();
        playerLocals.Clear();
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

    public int GetPlayerLocalIndex(IPlayer player)
    {
        if (player is not PlayerLocal playerLocal)
            throw new ArgumentException($"player is not PlayerLocal");

        foreach (var kvp in playerLocals)
        {
            if (kvp.Value == playerLocal)
                return kvp.Key;
        }

        throw new ArgumentException($"player is not registered as a local player");
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
        RegisterLocalPlayer(player);
        player.JoinGame();
        elements.Add(player);

        PRLog.WriteDebug(this, $"Игрок {player.Info.GetName()} - EntityID:{player.Id}, PlayerID:{playerId} зарегистрирован.");

        return true;
    }

    protected void RegisterLocalPlayer(IPlayer player)
    {
        if (player is not PlayerLocal localPlayer)
            return;

        if (playerLocals.Count >= MaxLocalPlayer)
            throw new Exception($"Cannot add new local player. Max local players is {MaxLocalPlayer}");

        int slotId = FindFreeSlotId();
        localPlayer.SetLocalId(slotId == 0 ? LocalPlayerOneId : LocalPlayerTwoId);
        playerLocals[slotId] = localPlayer;
        PlayerEvents.RaiseOnLocalPlayerJoinGame(localPlayer, LocalPlayerCount);
    }

    protected void UnregisterLocalPlayer(IPlayer player)
    {
        if (player is not PlayerLocal localPlayer)
            return;

        int? slotToRemove = null;

        foreach (var kvp in playerLocals)
        {
            if (kvp.Value == localPlayer)
            {
                slotToRemove = kvp.Key;
                break;
            }
        }

        if (slotToRemove.HasValue)
        {
            PlayerEvents.RaiseOnLocalPlayerLeftGame(localPlayer, LocalPlayerCount);
            playerLocals.Remove(slotToRemove.Value);
        }
    }

    public PlayerLocal GetLocalPlayer(int id)
    {
        playerLocals.TryGetValue(id, out var localPlayer);
        return localPlayer;
    }

    private int FindFreeSlotId()
    {
        for (int i = 0; i < MaxLocalPlayer; i++)
        {
            if (!playerLocals.ContainsKey(i))
                return i;
        }

        // не должно случиться благодаря проверке Count >= MaxLocalPlayer выше,
        // но пусть будет явная ошибка, а не тихий баг, если логика где-то разъедется
        throw new InvalidOperationException("No free local player slot despite count check passing.");
    }

    public override bool Unregister(IPlayer player)
    {
        elements.Remove(player);

        ReleasePlayerId(player.PlayerId);
        UnregisterLocalPlayer(player);
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