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

    // public int AliveCount => entities.Count(x => x.IsAlive());

    // public int DeadCount => entities.Count(x => !x.IsAlive);

    private HashSet<int> CurrentPlayersIds = new HashSet<int>();

    #endregion

    #region События

    public event Action<IEntity, PlayerBase> OnPlayerDead;

    #endregion

    #region Методы

    public override void Clear()
    {
        foreach (var entity in elements)
            entity.DestroyEntity(new EntityDestroyOptions() { FullDestroy = true });

        generatedNextId = 0;
        elements.Clear();
    }

    public void Destroy()
    {
        foreach (var entity in elements)
            entity.DestroyEntity(new EntityDestroyOptions() { FullDestroy = true });

        generatedNextId = 0;
        elements.Clear();
    }

    public override bool Register(IPlayer player)
    {
        player.GenerateId(GenerateId);
        player.JoinGame();
        elements.Add(player);
        //OnPlayerConnected?.Invoke(player);

        PRLog.WriteDebug(this, $"Игрок {player.Info.Name} - ID:{player.Id} зарегистрирован в игровой сессии.");
        return true;
    }

    public override bool Unregister(IPlayer player)
    {
        elements.Remove(player);
        //OnPlayerDisconnected?.Invoke(player);
        PRLog.WriteDebug(this, $"Игрок {player.Info.Name} - ID:{player.Id} удален из игровой сессии.");
        return true;
    }

    public void InvokeOnPlayerDead(IEntity killer, PlayerBase player)
    {
        OnPlayerDead?.Invoke(killer, player);
    }

    public int GetNextId()
    {
        if (CurrentPlayersIds.Count == 0)
        {
            CurrentPlayersIds.Add(0);
            return 0;
        }

        // Создаем отсортированный список из текущих ID
        var sortedIds = CurrentPlayersIds.OrderBy(id => id).ToList();

        // Ищем первый пропущенный ID
        for (int i = 0; i < sortedIds.Count; i++)
        {
            if (sortedIds[i] != i)
            {
                // Если ID не совпадает с индексом, это пропущенный ID
                CurrentPlayersIds.Add(i);  // Добавляем пропущенный ID в HashSet
                return i;
            }
        }

        // Если все ID идут подряд, то следующий будет максимальный ID + 1
        int nextId = sortedIds.Last() + 1;
        CurrentPlayersIds.Add(nextId);  // Добавляем новый ID в HashSet
        return nextId;
    }

    public void RemoveId(int id)
    {
        CurrentPlayersIds.Remove(id);
    }

    #endregion
}