using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Базовая сущность игрока.
/// </summary>
public abstract class PlayerBase : EntityBase, IPlayer, IReadySignalProvider
{
    #region Поля и свойства

    protected IPlayerTeam playerTeam = new DefaultTeam();

    [Header("Игрок")]
    [SerializeField] protected string playerName;

    [Header("Debug")]
    [SerializeField] protected int debugPlayerId;

    public UnityEvent<string> OnNickChange;

    public UnityEvent<PlayerBase> OnPlayerInit;

    public bool IsAttackState { get; protected set; }

    #endregion

    #region IPlayer

    public abstract PlayerType PlayerType { get; }

    public IPlayerTeam PlayerTeam => playerTeam;

    public long Points { get; protected set; }

    public int Deaths { get; protected set; }

    public int Kills { get; protected set; }

    public IPlayerStats PlayerStats { get; protected set; }

    public virtual int HumanId { get; private set; }

    public PlayerBase Attacker { get; protected set; }

    public long PlayerId { get; protected set; }

    public override void DestroyEntity()
    {
        DestroyEntity(new EntityDestroyOptions());
    }

    public void SetAttackState(bool isAttackState)
    {
        this.IsAttackState = isAttackState;
    }

    public void SetAttacker(PlayerBase playerBase)
    {
        this.Attacker = playerBase;
    }

    public void ClearAttacker()
    {
        this.Attacker = null;
    }

    public override void DestroyEntity(EntityDestroyOptions options)
    {
        OnPlayerDestroy?.Invoke(this);
        if (options.FullDestroy)
            RemovePlayer();

        Destroy(RootEntityObject);
    }

    public event Action<PlayerBase> OnPlayerDestroy;

    protected override void InitializeEntityInfo()
    {
        Info = new EntityInfoContainer(PRUnitySDK.Database.EntityInfo.Data.Single(x => x.Name == "Player"));
    }

    #endregion

    #region MonoBehaviour

    protected override void RegisterEntity()
    {
        PRUnitySDK.Trackers.Players.Register(this);
        OnPlayerInit?.Invoke(this);
    }

    protected override void UnregisterEntity()
    {
        PRUnitySDK.Trackers.Players.Unregister(this);
    }

    #endregion

    #region Базовый класс

    //public override bool Kill()
    //{
    //    var killer = GameEventEntityFactory.CreateEventGame();
    //    var isKilled = base.Kill(killer);
    //    if (isKilled)
    //        GameSessionBehaviour.Manager.PlayerTracker.InvokeOnPlayerDead(killer, this);

    //    return isKilled;
    //}

    //public override bool Suicide()
    //{
    //    var isSuicide = base.Suicide();
    //    if (isSuicide)
    //        GameSessionBehaviour.Manager.PlayerTracker.InvokeOnPlayerDead(GameEventEntityFactory.CreateEventSuicide(), this);

    //    return isSuicide;
    //}

    //public override bool Kill(IEntity attacker)
    //{
    //    var isKilled = base.Kill(attacker);
    //    if (isKilled)
    //        GameSessionBehaviour.Manager.PlayerTracker.InvokeOnPlayerDead(attacker, this);

    //    return isKilled;
    //}

    //protected override void OnEntityDeadInvoke(IEntity attacker)
    //{
    //    Deaths++;
    //    OnDeathsChange?.Invoke(Deaths);
    //    if (attacker is PlayerBase player)
    //        player.AddKill();

    //    base.OnEntityDeadInvoke(attacker);
    //}

    //public virtual void AddKill()
    //{
    //    Kills++;
    //    OnKillsChange?.Invoke(Kills);
    //}

    public virtual void AddPoints(long points)
    {
        var oldValue = Points;
        var valueContainer = new ValueLongContainer() { Value = points };
        OnPointsChanging?.Invoke(valueContainer);
        Points += valueContainer.Value;
        OnPointAdd?.Invoke(valueContainer.Value);
        OnPointsChanged?.Invoke(Points);
    }

    public virtual void RemovePoints(long points)
    {
        var oldValue = Points;
        var valueContainer = new ValueLongContainer() { Value = points };
        Points = Math.Max(0, Points - valueContainer.Value);
        OnPointRemove?.Invoke(valueContainer.Value);
        OnPointsChanged?.Invoke(Points);
    }

    public void JoinGame()
    {
        //if (PlayerType == PlayerType.Human)
        //    HumanId = PRUnitySDK.Trackers.Players.GetNextId();

        //debugPlayerId = HumanId;

        PlayerEvents.RaiseOnJoinGame(this);
    }

    public void RemovePlayer()
    {
        //if (PlayerType == PlayerType.Human)
        //    PRUnitySDK.Trackers.Players.RemoveId(HumanId);

        PlayerEvents.RaiseOnLeftGame(this);
    }

    #endregion

    #region События

    public event Action<long> OnPointsChanged;
    public event Action<long> OnPointAdd;
    public event Action<long> OnPointRemove;
    public event Action<ValueLongContainer> OnPointsChanging;

    public event Action<long> OnDeathsChange;

    public event Action<long> OnKillsChange;


    #endregion

    #region Методы

    public virtual void Teleport(Vector3 position)
    {
        transform.position = position;
    }

    public virtual void SetNick(string playerName)
    {
        this.playerName = playerName;
        OnNickChange?.Invoke(playerName);
    }

    public void SetTeam(IPlayerTeam team)
    {
        this.playerTeam = team;
    }

    public virtual bool AddPlayerItem(IPlayerItem item)
    {
        return item.Apply(this);
    }

    public void ResetState()
    {
        Kills = 0;
        Deaths = 0;
        Points = 0;
        //Killer = null;
    }

    public static PlayerTypeFlags ConvertToFlag(PlayerType type)
    {
        return type switch
        {
            PlayerType.Human => PlayerTypeFlags.Human,
            PlayerType.AI => PlayerTypeFlags.AI,
            PlayerType.NPC => PlayerTypeFlags.NPC,
            _ => PlayerTypeFlags.None
        };
    }

    public void GeneratePlayerId(Func<long> register)
    {
        PlayerId = register();
    }

    public virtual PlayerInputState GetInput()
    {
        return InputTranslator.Instance.GetPlayer(PlayerId);
    }

    protected readonly ReadySignal readySignal = new ReadySignal();
    public IReadySignal ReadySignal => readySignal;


    #endregion
}
