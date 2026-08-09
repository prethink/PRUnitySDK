using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Хранит игроков сессии, назначает переиспользуемые Player ID и локальные слоты.
/// </summary>
public class PlayerTracker : EntityTrackerBase<IPlayer>
{
    /// <summary>
    /// Зарезервированный Player ID первого локального игрока.
    /// </summary>
    public const long LocalPlayerOneId = 100_000;

    /// <summary>
    /// Зарезервированный Player ID второго локального игрока.
    /// </summary>
    public const long LocalPlayerTwoId = 200_000;

    /// <summary>
    /// Максимальное количество локальных игроков для текущего типа устройства.
    /// </summary>
    public int MaxLocalPlayer => PRUnitySDK.DeviceInfo.IsDesktop() ? 2 : 1;

    #region Поля и свойства

    /// <summary>
    /// Возвращает снимок зарегистрированных живых игроков.
    /// </summary>
    public List<IPlayer> Players => elements.Where(x => !x.IsNull()).ToList();

    /// <summary>
    /// Количество зарегистрированных живых игроков.
    /// </summary>
    public int PlayersCount => elements.Count(x => !x.IsNull());

    /// <summary>
    /// Количество игроков, управляемых человеком.
    /// </summary>
    public int HumanCount => elements.Count(x => !x.IsNull() && x.PlayerType == PlayerType.Human);

    /// <summary>
    /// Количество игроков, управляемых искусственным интеллектом.
    /// </summary>
    public int AICount => elements.Count(x => !x.IsNull() && x.PlayerType == PlayerType.AI);

    /// <summary>
    /// Количество занятых локальных слотов.
    /// </summary>
    public int LocalPlayerCount => playerLocals.Count;

    /// <summary>
    /// Наибольший Player ID, выделенный обычному игроку.
    /// </summary>
    private long playerIds;

    /// <summary>
    /// Следующий индекс последовательного Player ID.
    /// </summary>
    private int nextPlayerId = 0;

    /// <summary>
    /// Освобождённые Player ID, доступные для повторного использования.
    /// </summary>
    private readonly SortedSet<long> freePlayerIds = new();

    /// <summary>
    /// Локальные игроки, сопоставленные с индексами локальных слотов.
    /// </summary>
    private readonly Dictionary<int, PlayerLocal> playerLocals = new();

    #endregion

    #region События

    /// <summary>
    /// Вызывается при смерти игрока и передаёт убийцу и погибшего игрока.
    /// </summary>
    public event Action<IEntity, PlayerBase> OnPlayerDead;

    #endregion

    #region Методы



    /// <summary>
    /// Уничтожает игроков и сбрасывает локальные слоты и генератор Player ID.
    /// </summary>
    public override void Clear()
    {
        foreach (var player in elements.ToList())
        {
            if (player == null || player.IsNull())
            {
                elements.Remove(player);
                continue;
            }

            player.DestroyEntity(new EntityDestroyOptions { FullDestroy = true });
            Unregister(player);
        }

        playerLocals.Clear();
        ResetIds();
    }

    public void Destroy()
    {
        Clear();
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

    /// <summary>
    /// Регистрирует игрока, назначает идентификаторы и вызывает вход в игру.
    /// Локальный игрок не регистрируется, если все локальные слоты заняты.
    /// </summary>
    public override bool Register(IPlayer player)
    {
        if (player == null || player.IsNull() || elements.Contains(player))
            return false;

        if (player is PlayerLocal && playerLocals.Count >= MaxLocalPlayer)
        {
            PRLog.WriteWarning(this, $"Cannot add local player: all {MaxLocalPlayer} local slots are occupied.");
            return false;
        }

        var playerId = GetPlayerId();

        player.GenerateId(EntityIdGenerator.Instance.RegisterId);
        player.GeneratePlayerId(() => playerId);
        elements.Add(player);
        RegisterLocalPlayer(player);
        player.JoinGame();

        PRLog.WriteDebug(this, $"Игрок {player.Info?.GetName() ?? "<unnamed>"} - EntityID:{player.Id}, PlayerID:{playerId} зарегистрирован.");

        return true;
    }

    protected void RegisterLocalPlayer(IPlayer player)
    {
        if (player is not PlayerLocal localPlayer)
            return;

        if (playerLocals.Count >= MaxLocalPlayer)
            throw new InvalidOperationException($"Cannot add new local player. Max local players is {MaxLocalPlayer}");

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
            playerLocals.Remove(slotToRemove.Value);
            PlayerEvents.RaiseOnLocalPlayerLeftGame(localPlayer, LocalPlayerCount);
        }
    }

    public PlayerLocal GetLocalPlayer(int id)
    {
        playerLocals.TryGetValue(id, out var localPlayer);
        return localPlayer;
    }

    public List<PlayerLocal> GetLocalPlayers()
    {
        return playerLocals.OrderBy(x => x.Key).Select(x => x.Value).Where(x => x != null).ToList();
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

    /// <summary>
    /// Удаляет игрока и освобождает его Player ID и локальный слот.
    /// </summary>
    public override bool Unregister(IPlayer player)
    {
        if (player == null || !elements.Remove(player))
            return false;

        ReleasePlayerId(player.PlayerId);
        UnregisterLocalPlayer(player);
        PRLog.WriteDebug(this,
            $"Игрок {player.Info?.GetName() ?? "<unnamed>"} - ID:{player.Id} удален из сессии.");

        return true;
    }

    public void InvokeOnPlayerDead(IEntity killer, PlayerBase player)
    {
        OnPlayerDead?.Invoke(killer, player);
    }

    #endregion
}
