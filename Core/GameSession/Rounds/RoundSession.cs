using System.Collections.Generic;

public class RoundSession
{
    public GameSession GameSession { get; private set; }
    public int CurrentRound { get; protected set; }
    private readonly Dictionary<int, Dictionary<long, RoundData>> roundsData = new();

    public RoundType TypeRound { get; protected set; }

    public bool IsRoundActive { get; protected set; }

    public void Reset()
    {
        CurrentRound = 0;
        roundsData.Clear();
    }

    public void StartNextRound(StartRoundArgsBase args)
    {
        CurrentRound++;
        IsRoundActive = true;
        args.RoundNumber = CurrentRound;

        GameSessionEvents.RaiseStartRound(args);
    }

    public void StartNextRound()
    {
        var defaultArgs = new StartRoundArgsBase()
        {
            RoundType = TypeRound
        };
        StartNextRound(defaultArgs);
    }

    public void EndRound(EndRoundArgsBase args)
    {
        IsRoundActive = false;
        args.RoundNumber = CurrentRound;

        GameSessionEvents.RaiseEndRound(args);
    }

    public RoundData GetOrCreateCurrentRoundData(IPlayer player)
    {
        return GetOrCreateRoundData(CurrentRound, player);
    }

    public RoundData GetOrCreateRoundData(int roundId, IPlayer player)
    {
        if (!roundsData.TryGetValue(roundId, out var playerData))
        {
            playerData = new Dictionary<long, RoundData>();
            roundsData[roundId] = playerData;
        }

        if (!playerData.TryGetValue(player.Id, out var data))
        {
            data = new RoundData();
            playerData[player.Id] = data;
        }

        return data;
    }

    public RoundSession(GameSession gameSession)
    {
        this.GameSession = gameSession;
    }
}


