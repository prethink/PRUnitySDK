using UnityEngine;

/// <summary>
/// Базовый класс для управления камерой в игре.
/// Отвечает за активацию/деактивацию камеры и обработку её логики.
/// </summary>
public abstract class CameraControllerBase : PRMonoBehaviour, IGameplayEvent
{
    [field: SerializeField] public Camera CurrentCamera { get; protected set; }

    private bool _isCurrent;
    private CameraTracker tracker => CameraTracker.Instance;

    public bool IsCurrent
    {
        get => _isCurrent;
        protected set => _isCurrent = value;
    }

    public abstract bool CanPushStack { get; }

    protected override void PRUpdate()
    {
        if (CurrentCamera == null)
            return;

        if (!IsCurrent)
            return;

        HandleCamera();
    }

    /// <summary>
    /// Обновление логики активной камеры.
    /// </summary>
    protected abstract void HandleCamera();

    /// <summary>
    /// Устанавливает эту камеру как основную.
    /// </summary>
    public virtual void SetMain(bool pushInStack = true)
    {
        if (gameObject == null)
        {
            Debug.LogError("SetMain вызван на уничтоженном объекте");
            return;
        }

        if (tracker == null)
        {
            Debug.LogError("CameraTracker не инициализирован", gameObject);
            return;
        }

        SetCameraHandler();

        var newCamera = CameraEvents.InvokeChangeCamera(gameObject);
        if (newCamera == null)
        {
            Debug.LogWarning("CameraEvents.InvokeChangeCamera вернул null", gameObject);
            return;
        }

        CurrentCamera = newCamera;

        if (pushInStack && CanPushStack)
        {
            tracker.Push(this);
        }
    }

    /// <summary>
    /// Инициализирует камеру через трекер.
    /// </summary>
    protected virtual void SetCameraHandler()
    {
        if (tracker == null)
            return;

        CurrentCamera = tracker.MainCamera;
        if (CurrentCamera == null)
        {
            Debug.LogError("MainCamera в CameraTracker равна null", gameObject);
            return;
        }

        tracker.SetCurrent(this, CurrentCamera);
        tracker.HidePlayerCameras();
    }

    /// <summary>
    /// Устанавливает, является ли эта камера текущей.
    /// </summary>
    public virtual void SetCurrent(bool value)
    {
        if (_isCurrent == value)
            return;

        _isCurrent = value;

        if (CurrentCamera == null)
            return;

        OnCameraStateChanged(value);
    }

    /// <summary>
    /// Вызывается когда состояние камеры изменяется.
    /// Переопределите для кастомной логики.
    /// </summary>
    protected virtual void OnCameraStateChanged(bool isActive)
    {
        // Может быть переопределено в наследниках
    }

    /// <summary>
    /// Очищает камеру и помечает как неактивную.
    /// </summary>
    public virtual void ClearCamera()
    {
        IsCurrent = false;
        CurrentCamera = null;
    }

    /// <summary>
    /// Восстанавливает предыдущую камеру из стека.
    /// </summary>
    /// <summary>
    /// Восстанавливает предыдущую камеру из стека.
    /// </summary>
    public virtual void Restore()
    {
        var tracker = CameraTracker.Instance;
        if (tracker == null)
            return;

        // ВАЖНО: Удаляем себя из стека ВЕЗДЕ, не только с вершины!
        tracker.RemoveFromStack(this);

        // Восстанавливаем следующую живую камеру
        tracker.RestorePreviousCamera();
    }

    /// <summary>
    /// Обрабатывает события смены камеры.
    /// </summary>
    public virtual void Track(GameplayEventArgsBase args)
    {
        if (args == null)
            return;

        if (args is CameraChangerEvent cameraArgs)
        {
            // Если это событие от другого объекта, деактивируем нашу камеру
            if (cameraArgs.Executer != null && cameraArgs.Executer != gameObject)
            {
                ClearCamera();
            }
        }
    }

    /// <summary>
    /// Очистка при уничтожении.
    /// </summary>
    protected virtual void OnDestroy()
    {
        ClearCamera();

        // Если мы были активной камерой, пытаемся восстановить предыдущую
        if (IsCurrent)
        {
            Restore();
        }
    }
}