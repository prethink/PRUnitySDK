using System.Collections.Generic;

public class EndRoundPlayerArgs : EndRoundArgsBase
{
    /// <summary> Победивший игрок (если игра одиночная или PvP). </summary>
    public IPlayer WinnerPlayer { get; set; }

    /// <summary> Все игроки, участвовавшие в раунде. </summary>
    public IReadOnlyList<IPlayer> Participants { get; set; }

    public override CategoryPath GetEventId()
    {
        return new CategoryPath(base.GetEventId(), "EndPlayer");
    }
}

public class EndRoundTeamArgs : EndRoundArgsBase
{
    /// <summary> Победившая команда (если режим командный). </summary>
    public IPlayerTeam WinnerTeam { get;  set; }

    /// <summary> Все игроки, участвовавшие в раунде. </summary>
    public IReadOnlyList<PlayerBase> Participants { get; set; }

    /// <summary> Все команды (если применимо). </summary>
    public IReadOnlyList<IPlayerTeam> Teams { get;  set; }

    public override CategoryPath GetEventId()
    {
        return new CategoryPath(base.GetEventId(), "EndTeam");
    }
}

public abstract class EndRoundArgsBase : RoundArgsBase { }

public class StartRoundArgsBase : RoundArgsBase 
{
    public override CategoryPath GetEventId()
    {
        return new CategoryPath(base.GetEventId(), "Start");
    }
}


public class StartRoundFactory
{
    public static StartRoundArgsBase Create()
    {
        return new StartRoundArgsBase();
    }
}

public abstract class RoundArgsBase : GameSessionEventArgsBase
{
    /// <summary>
    /// Номер раунда.
    /// </summary>
    public int RoundNumber { get; set; }

    /// <summary>
    /// Тип раунда.
    /// </summary>
    public RoundType RoundType { get; set; }

    /// <summary>
    /// Дополнительные данные, специфичные для режима. 
    /// </summary>
    public Dictionary<string, object> CustomData { get; protected set; } = new();

    public T GetCustom<T>(string key, T defaultValue = default)
    {
        if (CustomData.TryGetValue(key, out var value) && value is T typed)
            return typed;

        return defaultValue;
    }

    public void SetCustom<T>(string key, T value)
    {
        CustomData[key] = value!;
    }

    public override CategoryPath GetEventId()
    {
        return new CategoryPath(base.GetEventId(), "Round");
    }
}


