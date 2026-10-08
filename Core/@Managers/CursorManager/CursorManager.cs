using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Менеджер управления курсором. Работает через именованные запросы состояния
/// (Show/Hide с указанием source - обычно `this` вызывающего кода, как и в
/// FlagResolver): пока активен хотя бы один запрос "показать", курсор виден,
/// независимо от того, в каком порядке снимаются остальные запросы. Без запросов
/// "показать" действует самый поздний из оставшихся запросов, а без запросов вовсе -
/// состояние по умолчанию. Это устойчиво к ситуации, когда открыто два окна и
/// закрывается не последнее из них.
/// <para>
/// Менеджер - единственный, кто пишет <c>Cursor.visible</c> и <c>Cursor.lockState</c>:
/// прямая запись жила бы только до ближайшего пересчёта и спорила бы с запросами.
/// </para>
/// </summary>
public class CursorManager : SingletonProviderBase<CursorManager>
{
    public readonly static EnumerationType<bool> CursorStatePropertyName = new EnumerationType<bool>(nameof(CursorStatePropertyName));

    private bool isLoadingState;

    /// <summary>
    /// Сбрасывает singleton-экземпляр при каждом запуске Play Mode, включая
    /// случай, когда в Editor отключён Domain Reload (Project Settings → Editor
    /// → Enter Play Mode Settings). Без этого сброса isLoadingState, activeRequests
    /// и defaultState пережили бы предыдущую Play-сессию как есть, и повторный
    /// LoadCursorState во второй сессии не сработал бы, потому что isLoadingState
    /// остался бы true с прошлого раза.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnLoad()
    {
        Override(null);
    }

    /// <summary>
    /// Снимок состояния курсора - режим блокировки и видимость. Публичный,
    /// потому что передаётся как параметр fallback-значения в LoadCursorState
    /// и как возвращаемое значение состояния запроса.
    /// </summary>
    public readonly struct CursorState
    {
        public CursorLockMode LockMode { get; }
        public bool Visible { get; }

        public CursorState(CursorLockMode lockMode, bool visible)
        {
            LockMode = lockMode;
            Visible = visible;
        }
    }

    /// <summary>Запасной вариант на случай, если Release вызвали раньше, чем хоть
    /// раз отработал LoadCursorState - defaultState тогда ещё null, а применить
    /// нужно хоть какое-то валидное состояние.</summary>
    private static readonly CursorState EmergencyFallback = new CursorState(CursorLockMode.Locked, false);

    /// <summary>Активные запросы в порядке последнего обращения (не порядке
    /// первого добавления) - Release ищет и удаляет запись конкретного source,
    /// а не обязательно последнюю добавленную, что и даёт устойчивость к
    /// произвольному порядку снятия запросов. SetRequest переносит обновлённую
    /// запись в конец списка, поэтому "последний элемент" всегда означает
    /// "источник, который трогали позже всех остальных", а не просто "источник,
    /// который добавили раньше остальных".</summary>
    private readonly List<(object Source, CursorState State)> activeRequests = new();

    /// <summary>
    /// Состояние по умолчанию, применяемое, когда нет ни одного активного запроса
    /// Show/Hide. Не задано (null), пока не был вызван LoadCursorState хотя бы
    /// один раз - до этого момента поле пустое, а не какое-то заранее зашитое
    /// значение "по умолчанию по умолчанию".
    /// </summary>
    private CursorState? defaultState;

    /// <summary>
    /// Курсор показан по чьему-то запросу: указатель сейчас принадлежит интерфейсу.
    /// </summary>
    /// <remarks>
    /// По нему ввод игрока решает, отдавать ли мышь и свайп камере и атаке. Так окну,
    /// колесу эмоций или рекламе достаточно попросить курсор — запрещать взгляд и удар
    /// каждому игроку по отдельности не нужно. На тач-устройствах курсор не рисуется,
    /// но смысл тот же: палец нужен интерфейсу.
    /// </remarks>
    public bool IsCursorShown { get; private set; }

    /// <summary>
    /// Запрашивает видимый, разблокированный курсор от имени source (например,
    /// конкретное открытое окно UI). Повторный вызов с тем же source обновляет
    /// его существующую запись вместо создания дубликата в списке активных.
    /// </summary>
    public void Show(object source)
    {
        SetRequest(source, new CursorState(CursorLockMode.None, true));
    }

    /// <summary>
    /// Запрашивает скрытый и заблокированный курсор от имени source. Как и Show,
    /// не создаёт дубликат записи при повторном вызове с тем же source, а просто
    /// обновляет уже существующий запрос этого источника новым состоянием.
    /// </summary>
    public void Hide(object source)
    {
        SetRequest(source, new CursorState(CursorLockMode.Locked, false));
    }

    /// <summary>
    /// Снимает запрос конкретного source и пересчитывает курсор по оставшимся
    /// запросам (см. <see cref="ApplyActive"/>). Это и есть "вернуть как было до этого",
    /// но корректно работающее и при нескольких одновременных запросах, снятых в любом
    /// порядке. Источник без запроса ничего не меняет - снимать "на всякий случай" можно.
    /// </summary>
    public void Release(object source)
    {
        int index = activeRequests.FindIndex(r => Equals(r.Source, source));

        if (index < 0)
            return;

        activeRequests.RemoveAt(index);
        ApplyActive();
    }

    /// <summary>
    /// Применяет заново то состояние курсора, которое следует из активных запросов.
    /// </summary>
    /// <remarks>
    /// Нужен после того, как курсор тронул кто-то мимо менеджера. Так делает плагин площадки:
    /// на время рекламы или оплаты он ставит игру на паузу, а выходя из неё, возвращает
    /// курсор «как было до паузы» — и затирает то, что за время паузы попросило окно.
    /// Запросы при этом не меняются: менеджер лишь повторяет свой последний ответ.
    /// </remarks>
    public void Reapply()
    {
        ApplyActive();
    }

    /// <summary>
    /// Проверяет, есть ли у указанного source сейчас активный запрос (Show или
    /// Hide, неважно какой именно) в списке. Полезно перед повторным Show/Hide,
    /// если вызывающий код хочет узнать, уже ли он что-то запросил ранее.
    /// </summary>
    public bool HasRequest(object source)
    {
        return activeRequests.Exists(r => Equals(r.Source, source));
    }

    /// <summary>
    /// Возвращает снимок активных запросов от старого к последнему обновлённому.
    /// </summary>
    public IReadOnlyList<(object Source, CursorState State)> GetActiveRequests()
    {
        return activeRequests.ToArray();
    }

    /// <summary>
    /// Загружает состояние по умолчанию: если defaultState уже был установлен
    /// раньше (кем-то вызывался LoadCursorState до этого) - возвращает именно
    /// его, игнорируя переданный аргумент. Если ещё не установлен - сохраняет
    /// переданный defaultState как новое значение поля и возвращает его же.
    /// </summary>
    public void LoadCursorState(object source, CursorState defaultState)
    {
        if (isLoadingState)
            return;

        var gameManager = GameManager.Instance;
        gameManager.ReadySignal.SubscribeOnReady(() =>
        {
            if(ProjectPropertiesManager.Instance.TryGetValue(CursorStatePropertyName, out var value))
            {
                SetRequest(source, new CursorState(CursorLockMode.Locked, value));
            }
            else
            {
                SetRequest(source, defaultState);
            }
        });

        isLoadingState = true;
    }

    /// <summary>
    /// Меняет спрайт курсора немедленно, в обход системы запросов Show/Hide.
    /// Не запоминается как часть CursorState и не восстанавливается через
    /// Release - если нужен спрайт, привязанный к конкретному запросу, сообщите,
    /// добавим поле Texture в CursorState и протянем через Show/Hide/Release.
    /// </summary>
    public void SetCursorSprite(Sprite cursorSprite)
    {
        var texture = cursorSprite != null ? cursorSprite.texture : null;
        Cursor.SetCursor(texture, Vector2.zero, CursorMode.Auto);
    }

    /// <summary>
    /// Добавляет или обновляет запись запроса конкретного source в списке
    /// активных и пересчитывает курсор (см. <see cref="ApplyActive"/>).
    /// Если у source уже была запись, старая позиция удаляется и запись
    /// добавляется заново в конец списка - иначе повторный Show/Hide уже
    /// известного source не двигал бы его позицию, и "последний в списке"
    /// в Release означал бы "первый добавленный", а не "последний тронутый".
    /// </summary>
    private void SetRequest(object source, CursorState state)
    {
        int index = activeRequests.FindIndex(r => Equals(r.Source, source));

        if (index >= 0)
            activeRequests.RemoveAt(index);

        activeRequests.Add((source, state));

        ApplyActive();
    }

    /// <summary>
    /// Применяет итоговое состояние: самый поздний запрос "показать", если такой есть,
    /// иначе самый поздний из запросов, иначе defaultState (или EmergencyFallback,
    /// если LoadCursorState ещё ни разу не вызывался).
    /// </summary>
    /// <remarks>
    /// "Показать" побеждает, потому что его просит тот, кому нужен указатель: окно,
    /// реклама, колесо эмоций. Постоянный запрос "спрятать" (например, у локального
    /// игрока) не должен отбирать курсор у открытого окна только потому, что его
    /// тронули позже.
    /// </remarks>
    private void ApplyActive()
    {
        for (int i = activeRequests.Count - 1; i >= 0; i--)
        {
            if (activeRequests[i].State.Visible)
            {
                Apply(activeRequests[i].State);
                IsCursorShown = true;
                return;
            }
        }

        CursorState state = activeRequests.Count > 0
            ? activeRequests[activeRequests.Count - 1].State
            : defaultState ?? EmergencyFallback;

        Apply(state);
        IsCursorShown = state.Visible;
    }

    /// <summary>
    /// Применяет CursorState к реальным системным свойствам UnityEngine.Cursor.
    /// Единственное место в классе, которое напрямую трогает Cursor.lockState/
    /// Cursor.visible - все остальные методы должны идти через этот вызов.
    /// На тач-устройствах курсор никогда не блокируется: при Locked
    /// StandaloneInputModule перестаёт обрабатывать перетаскивание и для касаний,
    /// и экранный джойстик не работает.
    /// </summary>
    private void Apply(CursorState state)
    {
        Cursor.lockState = PRUnitySDK.DeviceInfo.IsTouchDevice() ? CursorLockMode.None : state.LockMode;
        Cursor.visible = state.Visible;
    }
}