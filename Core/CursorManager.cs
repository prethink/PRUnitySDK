using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Менеджер управления курсором. Работает через именованные запросы состояния
/// (Show/Hide с указанием source - обычно `this` вызывающего кода, как и в
/// FlagResolver): пока активен хотя бы один запрос "показать", курсор виден,
/// независимо от того, в каком порядке снимаются остальные запросы. Release(source)
/// возвращает курсор к состоянию самого позднего из ОСТАВШИХСЯ активных запросов,
/// а не к жёстко зафиксированному "предыдущему" - это устойчиво к ситуации, когда
/// открыто два окна и закрывается не последнее из них.
/// </summary>
public class CursorManager : SingletonProviderBase<CursorManager>
{
    public readonly static EnumerationType<bool> CursorStatePropertyName = new EnumerationType<bool>(nameof(CursorStatePropertyName));

    private bool isLoadingState;

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

    /// <summary>Активные запросы в порядке добавления. Список, а не Stack -
    /// Release ищет и удаляет запись конкретного source, а не обязательно
    /// последнюю добавленную, что и даёт устойчивость к произвольному порядку.</summary>
    private readonly List<(object Source, CursorState State)> activeRequests = new();

    /// <summary>
    /// Состояние по умолчанию, применяемое, когда нет ни одного активного запроса
    /// Show/Hide. Не задано (null), пока не был вызван LoadCursorState хотя бы
    /// один раз - до этого момента поле пустое, а не какое-то заранее зашитое
    /// значение "по умолчанию по умолчанию".
    /// </summary>
    private CursorState? defaultState;

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
    /// Снимает запрос конкретного source и применяет состояние самого позднего
    /// из оставшихся активных запросов - либо defaultState (или EmergencyFallback,
    /// если LoadCursorState ещё ни разу не вызывался), если запросов больше нет.
    /// Это и есть "вернуть как было до этого", но корректно работающее и при
    /// нескольких одновременных запросах, снятых в любом порядке.
    /// </summary>
    public void Release(object source)
    {
        int index = activeRequests.FindIndex(r => Equals(r.Source, source));

        if (index >= 0)
            activeRequests.RemoveAt(index);

        if (activeRequests.Count > 0)
        {
            Apply(activeRequests[activeRequests.Count - 1].State);
            return;
        }

        Apply(defaultState ?? EmergencyFallback);
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
    /// активных, затем сразу применяет переданное состояние к системному
    /// курсору - то есть последний вызвавший Show/Hide всегда выигрывает.
    /// </summary>
    private void SetRequest(object source, CursorState state)
    {
        int index = activeRequests.FindIndex(r => Equals(r.Source, source));

        if (index >= 0)
            activeRequests[index] = (source, state);
        else
            activeRequests.Add((source, state));

        Apply(state);
    }

    /// <summary>
    /// Применяет CursorState к реальным системным свойствам UnityEngine.Cursor.
    /// Единственное место в классе, которое напрямую трогает Cursor.lockState/
    /// Cursor.visible - все остальные методы должны идти через этот вызов.
    /// </summary>
    private void Apply(CursorState state)
    {
        Cursor.lockState = state.LockMode;
        Cursor.visible = state.Visible;
    }
}