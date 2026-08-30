using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Хранит игроков сессии и назначает переиспользуемые Player ID.
/// </summary>
/// <remarks>
/// Локальные игроки — разделённый экран, слоты, клавиатурные раскладки — живут
/// в расширении приватного слоя: там же лежит и сам <c>PlayerLocal</c>. Публичная часть
/// обращается к ним только через partial-хуки, поэтому без приватного слоя трекер
/// продолжает собираться.
/// </remarks>
public partial class PlayerTracker : EntityTrackerBase<IPlayer>
{
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

        ClearLocalPlayers();
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

        bool allowed = true;
        CheckRegister(player, ref allowed);

        if (!allowed)
            return false;

        var playerId = GetPlayerId();

        player.GenerateId(EntityIdGenerator.Instance.RegisterId);
        player.GeneratePlayerId(() => playerId);
        elements.Add(player);
        RegisterLocalPlayer(player);
        player.JoinGame();

        PRLog.WriteDebug(this, $"Игрок {player.Description?.GetName() ?? "<unnamed>"} - EntityID:{player.Id}, PlayerID:{playerId} зарегистрирован.");

        return true;
    }

    /// <summary>
    /// Проверяет, можно ли регистрировать игрока.
    /// </summary>
    /// <remarks>
    /// Приватный слой отказывает локальному игроку, когда все слоты разделённого экрана
    /// заняты. Без него ограничения нет — обычным игрокам оно и не нужно.
    /// </remarks>
    partial void CheckRegister(IPlayer player, ref bool allowed);

    /// <summary>
    /// Заводит локальный слот для игрока.
    /// </summary>
    partial void RegisterLocalPlayer(IPlayer player);

    /// <summary>
    /// Освобождает локальный слот игрока.
    /// </summary>
    partial void UnregisterLocalPlayer(IPlayer player);

    /// <summary>
    /// Сбрасывает таблицу локальных игроков.
    /// </summary>
    partial void ClearLocalPlayers();

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
            $"Игрок {player.Description?.GetName() ?? "<unnamed>"} - ID:{player.Id} удален из сессии.");

        return true;
    }

    public void InvokeOnPlayerDead(IEntity killer, PlayerBase player)
    {
        OnPlayerDead?.Invoke(killer, player);
    }

    #endregion
}
