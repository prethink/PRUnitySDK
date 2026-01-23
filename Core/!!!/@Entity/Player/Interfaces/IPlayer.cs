/// <summary>
/// Сущность игрока.
/// </summary>
public partial interface IPlayer : IEntity
{
    /// <summary>
    /// Количество очков.
    /// </summary>
    public long Points { get; }

    /// <summary>
    /// Количество смертей.
    /// </summary>
    public int Deaths { get; }

    /// <summary>
    /// Количество убийств.
    /// </summary>
    public int Kills { get;}

    /// <summary>
    /// Тип игрока. Кто управляет, человек или AI.
    /// </summary>
    public PlayerType PlayerType { get;}

    /// <summary>
    /// Команда игрока.
    /// </summary>
    public IPlayerTeam PlayerTeam { get;}

    /// <summary>
    /// Характеристики игрока.
    /// </summary>
    public IPlayerStats PlayerStats { get;}


    /// <summary>
    /// Номер игрока. Используется когда несколько живых игроков на 1 экране. Для ботов значение -1.
    /// </summary>
    public int HumanId { get;}

    ///// <summary>
    ///// Респавн игрока.
    ///// </summary>
    ///// <param name="spawnPosition">Точка спавна.</param>
    //public void ReSpawn(Vector3 spawnPosition);

    /// <summary>
    /// Установить ник.
    /// </summary>
    /// <param name="playerName">Новый игровой ник.</param>
    public void SetNick(string playerName);

    /// <summary>
    /// Установить команду игроку.
    /// </summary>
    /// <param name="team">Команда игрока.</param>
    public void SetTeam(IPlayerTeam team);

    /// <summary>
    /// Добавить предмет игроку.
    /// </summary>
    public bool AddPlayerItem(IPlayerItem item);

    /// <summary>
    /// Игрок присоединился к игре.
    /// </summary>
    public void JoinGame();

    ///// <summary>
    ///// Инициализация игрока.
    ///// </summary>
    //public void PlayerInitialize();
}