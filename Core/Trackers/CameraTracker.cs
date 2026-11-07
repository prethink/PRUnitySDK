using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Управляет стеком камер и отслеживает игровые камеры.
/// Потокобезопасен для базовых операций.
/// </summary>
/// <remarks>
/// Реестр камер игроков — проектная часть: конкретный тип камеры игрока живёт
/// в приватном слое. Публичная часть переключает их только через partial-хук
/// <see cref="SetPlayerCamerasActive"/>, поэтому без приватного слоя трекер
/// продолжает собираться и управлять стеком контроллеров.
/// </remarks>
public partial class CameraTracker : SingletonProviderBase<CameraTracker>
{
    /// <summary>
    /// LIFO-стек временно активных контроллеров камер.
    /// </summary>
    private readonly Stack<CameraControllerBase> _cameraStack = new();

    /// <summary>
    /// Объект синхронизации доступа к коллекциям и текущему состоянию камер.
    /// </summary>
    private readonly object _lock = new object();

    /// <summary>
    /// Контроллер, которому в данный момент передано управление камерой.
    /// </summary>
    private CameraControllerBase _currentController;

    /// <summary>
    /// Указывает, используется ли сейчас основная камера вместо камеры игрока.
    /// </summary>
    private bool _isMainCameraActive = true;

    /// <summary>
    /// Основная камера сцены, используемая вне игровых камер игроков.
    /// </summary>
    public Camera MainCamera { get; protected set; }

    /// <summary>
    /// Камера, выбранная трекером в качестве текущей.
    /// </summary>
    public Camera Current { get; protected set; }
    /// <summary>
    /// Количество контроллеров в стеке камер.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
                return _cameraStack.Count;
        }
    }
    /// <summary>
    /// Указывает, активна ли основная камера сцены.
    /// </summary>
    public bool IsMainCameraActive => _isMainCameraActive;

    /// <summary>
    /// Кладёт камеру на вершину стека; уже лежащая ниже переносится наверх.
    /// </summary>
    /// <remarks>
    /// Камера в стеке одна: иначе после «A → B → снова A» закрытие A оставило бы её
    /// копию под B, и восстановление вернуло бы уже закрытую камеру.
    /// </remarks>
    public void Push(CameraControllerBase cameraController)
    {
        if (cameraController == null)
        {
            PRLog.WriteWarning(this, "Попытка добавить null камеру в стек");
            return;
        }

        lock (_lock)
        {
            if (_cameraStack.Count > 0 && _cameraStack.Peek() == cameraController)
                return;

            RemoveAllFromStack(cameraController);
            _cameraStack.Push(cameraController);
        }
    }

    public bool IsLastStack()
    {
        lock (_lock)
        {
            return _cameraStack.Count == 1;
        }
    }

    public bool IsEmptyStack()
    {
        lock (_lock)
        {
            return _cameraStack.Count == 0;
        }
    }

    /// <summary>
    /// Извлекает первый живой контроллер с вершины стека.
    /// </summary>
    public CameraControllerBase Pop()
    {
        lock (_lock)
        {
            while (_cameraStack.Count > 0)
            {
                var controller = _cameraStack.Pop();
                if (controller != null)
                    return controller;
            }

            return null;
        }
    }

    /// <summary>
    /// Возвращает первый живой контроллер, не удаляя его из стека.
    /// </summary>
    public CameraControllerBase Peek()
    {
        return PeekAlive();
    }

    /// <summary>
    /// Очищает мертвые ссылки из стека (уничтоженные контроллеры).
    /// </summary>
    private void CleanDeadReferences()
    {
        lock (_lock)
        {
            // Преобразуем в список для безопасного удаления
            var deadControllers = _cameraStack.Where(c => c == null || c.gameObject == null).ToList();

            if (deadControllers.Count == 0)
                return;

            PRLog.WriteWarning(this, $"Найдено {deadControllers.Count} мертвых ссылок в стеке камер, очищаем...");

            // Создаем новый стек без мертвых ссылок
            var tempStack = new Stack<CameraControllerBase>();
            while (_cameraStack.Count > 0)
            {
                var controller = _cameraStack.Pop();
                if (controller != null && controller.gameObject != null)
                {
                    tempStack.Push(controller);
                }
            }

            // Восстанавливаем стек в правильном порядке
            while (tempStack.Count > 0)
            {
                _cameraStack.Push(tempStack.Pop());
            }
        }
    }

    /// <summary>
    /// Получает первый живой контроллер с вершины стека.
    /// </summary>
    private CameraControllerBase PeekAlive()
    {
        lock (_lock)
        {
            while (_cameraStack.Count > 0)
            {
                var controller = _cameraStack.Peek();

                // Если контроллер жив, возвращаем
                if (controller != null && controller.gameObject != null)
                    return controller;

                // Если мертв, удаляем и продолжаем
                PRLog.WriteWarning(this, "Удаляем мертвую ссылку из стека камер");
                _cameraStack.Pop();
            }

            return null;
        }
    }

    /// <summary>
    /// Деактивирует все игровые камеры (переходит на основную камеру).
    /// </summary>
    public void HidePlayerCameras()
    {
        lock (_lock)
        {
            this.RunMethodHooks(PlayerCamerasActiveStage, false);
            _isMainCameraActive = true;
            ApplyMainCameraActive();
        }
    }

    /// <summary>
    /// Активирует все игровые камеры.
    /// </summary>
    public void ShowPlayerCameras()
    {
        lock (_lock)
        {
            this.RunMethodHooks(PlayerCamerasActiveStage, true);
            _isMainCameraActive = false;
            ApplyMainCameraActive();
        }
    }

    /// <summary>
    /// Включает камеру сцены, только пока камеры игроков спрятаны.
    /// </summary>
    /// <remarks>
    /// Раньше флаг менялся, а сама камера оставалась включённой: глубина у неё ниже, и она
    /// каждый кадр перерисовывала всю сцену под камерами игроков впустую — в PickBox это
    /// была половина draw call и треугольников. Выключается компонент, а не объект: на нём
    /// могут жить другие компоненты сцены.
    /// </remarks>
    private void ApplyMainCameraActive()
    {
        if (MainCamera != null)
            MainCamera.enabled = _isMainCameraActive;
    }

    /// <summary>
    /// Стадия хука «переключить камеры игроков».
    /// </summary>
    /// <remarks>
    /// Реестров камер может быть несколько: каждый объявляет свой метод
    /// <c>[MethodHook(PlayerCamerasActiveStage)] void Имя(bool isActive)</c>,
    /// порядок задаёт <c>Order</c>. Хуки выполняются уже под <c>_lock</c> трекера,
    /// поэтому брать его повторно не нужно. Без единого хука переключать нечего,
    /// и трекер работает только со стеком контроллеров.
    /// </remarks>
    public const string PlayerCamerasActiveStage = "PlayerCamerasActive";

    internal void SetMainCamera(Camera camera)
    {
        if (camera == null)
        {
            PRLog.WriteError(this, "Попытка установить null как MainCamera");
            return;
        }
        lock (_lock)
        {
            MainCamera = camera;

            // Камера уровня регистрируется и после того, как камеры игроков уже показаны
            // (смена сцены при живом игроке), — она сразу получает текущее состояние.
            ApplyMainCameraActive();
        }
    }

    /// <summary>
    /// Устанавливает текущую камеру и обновляет состояние всех контроллеров.
    /// </summary>
    internal void SetCurrent(CameraControllerBase cameraControllerBase, Camera camera)
    {
        if (cameraControllerBase == null || camera == null)
        {
            PRLog.WriteError(this, "SetCurrent получил null параметры");
            return;
        }

        lock (_lock)
        {
            // Отмечаем предыдущий контроллер как неактивный
            if (_currentController != null && _currentController != cameraControllerBase)
            {
                _currentController.SetCurrent(false);
            }

            _currentController = cameraControllerBase;
            _currentController.SetCurrent(true);
            Current = camera;

            // Обновляем состояние только для контроллеров в стеке
            foreach (var item in _cameraStack)
            {
                if (item != null)  // Проверяем на null
                {
                    item.SetCurrent(item == cameraControllerBase);
                }
            }
        }
    }

    /// <summary>
    /// Удаляет контроллер из стека независимо от его позиции.
    /// </summary>
    public void RemoveFromStack(CameraControllerBase controller)
    {
        if (controller == null)
            return;

        lock (_lock)
        {
            RemoveAllFromStack(controller);
            PRLog.WriteDebug(this, $"Удален контроллер из стека. Осталось: {_cameraStack.Count}");
        }
    }

    /// <summary>
    /// Убирает из стека все вхождения контроллера, сохраняя порядок остальных.
    /// </summary>
    /// <remarks>
    /// Все, а не первое: стек мог достаться с повтором от прежнего кода, и закрытая камера не должна
    /// всплыть из-под других при восстановлении. Вызывать под <c>_lock</c>.
    /// </remarks>
    private void RemoveAllFromStack(CameraControllerBase controller)
    {
        if (!_cameraStack.Contains(controller))
            return;

        // ToList идёт от вершины к низу; обратно кладём с низа, чтобы вершина осталась вершиной.
        var remaining = _cameraStack.Where(item => !ReferenceEquals(item, controller)).ToList();
        _cameraStack.Clear();

        for (int i = remaining.Count - 1; i >= 0; i--)
            _cameraStack.Push(remaining[i]);
    }

    /// <summary>
    /// Восстанавливает предыдущую камеру, пропуская мертвые ссылки.
    /// </summary>
    public void RestorePreviousCamera()
    {
        lock (_lock)
        {
            // Сначала очищаем мертвые ссылки
            CleanDeadReferences();

            if (_cameraStack.Count == 0)
            {
                PRLog.WriteDebug(this, "Стек камер пуст, показываем игровые камеры");
                ClearCurrent();
                ShowPlayerCameras();
                return;
            }

            // Получаем первого живого контроллера
            var previous = PeekAlive();

            if (previous == null)
            {
                PRLog.WriteWarning(this, "Не найдено живых контроллеров камер в стеке");
                ClearCurrent();
                ShowPlayerCameras();
                return;
            }

            PRLog.WriteDebug(this, $"Восстанавливаем камеру: {previous.gameObject.name}");

            // Активируем найденную камеру
            previous.SetMain(pushInStack: false);
        }
    }

    /// <summary>
    /// Восстанавливает камеру, когда текущий контроллер уходит вместе со своей сценой.
    /// </summary>
    /// <remarks>
    /// Контроллеры из выгружаемой сцены уничтожаются вслед за этим — переключаться на них нельзя. А пережить
    /// выгрузку могут: <c>PlayerCamerasStack</c> живёт в <c>DontDestroyOnLoad</c>. Без восстановления игрок,
    /// ушедший со сцены посреди крупного плана, остался бы без своих камер. Поэтому берётся первый живой
    /// контроллер не из этой сцены; нет такого — как при пустом стеке: текущий сбрасывается, камеры игрока
    /// показываются (живые — включатся, уходящие со сценой — пропускаются).
    /// </remarks>
    /// <param name="unloading">Сцена, которая выгружается.</param>
    public void RestoreSurvivingCamera(UnityEngine.SceneManagement.Scene unloading)
    {
        lock (_lock)
        {
            CleanDeadReferences();

            CameraControllerBase survivor = _cameraStack.FirstOrDefault(controller =>
                controller != null && controller.gameObject.scene != unloading);

            if (survivor == null)
            {
                PRLog.WriteDebug(this, $"Сцена {unloading.name} выгружается, переживших её камер в стеке нет: показываем игровые камеры");
                ClearCurrent();
                ShowPlayerCameras();
                return;
            }

            PRLog.WriteDebug(this, $"Сцена {unloading.name} выгружается, восстанавливаем камеру: {survivor.gameObject.name}");
            survivor.SetMain(pushInStack: false);
        }
    }

    /// <summary>
    /// Забывает контроллер как текущий, не переключаясь ни на какую камеру.
    /// </summary>
    /// <remarks>
    /// Для уничтожения без восстановления (выход из игры, выгрузка сцены): трекер живёт дольше сцены
    /// и иначе держал бы камеру выгруженной сцены в <see cref="Current"/>. Чужой текущий контроллер не трогается.
    /// </remarks>
    public void ClearCurrentIf(CameraControllerBase controller)
    {
        lock (_lock)
        {
            if (!ReferenceEquals(_currentController, controller))
                return;

            ClearCurrent();
        }
    }

    /// <summary>
    /// Забывает текущий контроллер, когда в стеке не осталось ни одного.
    /// </summary>
    /// <remarks>
    /// Без этого <see cref="Current"/> указывал бы на камеру закрытого контроллера, а сам он
    /// считал бы себя текущим и продолжал вести камеру. Контроллера нет — нет и текущей камеры:
    /// читающие <see cref="Current"/> уже обрабатывают <c>null</c>. Вызывать под <c>_lock</c>.
    /// </remarks>
    private void ClearCurrent()
    {
        if (_currentController != null)
            _currentController.SetCurrent(false);

        _currentController = null;
        Current = null;
    }
}
