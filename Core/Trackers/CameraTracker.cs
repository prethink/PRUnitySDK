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
    /// Добавляет камеру в стек, если она не уже на вершине.
    /// </summary>
    public void Push(CameraControllerBase cameraController)
    {
        if (cameraController == null)
        {
            Debug.LogWarning("Попытка добавить null камеру в стек");
            return;
        }

        lock (_lock)
        {
            if (_cameraStack.Count > 0 && _cameraStack.Peek() == cameraController)
                return;

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

            Debug.LogWarning($"Найдено {deadControllers.Count} мертвых ссылок в стеке камер, очищаем...");

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
                Debug.LogWarning("Удаляем мертвую ссылку из стека камер");
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
        }
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
            Debug.LogError("Попытка установить null как MainCamera");
            return;
        }
        lock (_lock)
            MainCamera = camera;
    }

    /// <summary>
    /// Устанавливает текущую камеру и обновляет состояние всех контроллеров.
    /// </summary>
    internal void SetCurrent(CameraControllerBase cameraControllerBase, Camera camera)
    {
        if (cameraControllerBase == null || camera == null)
        {
            Debug.LogError("SetCurrent получил null параметры");
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
            // Преобразуем в список, удаляем контроллер, восстанавливаем стек
            var tempList = _cameraStack.ToList();
            tempList.Remove(controller);

            _cameraStack.Clear();

            // Восстанавливаем в обратном порядке (стек работает LIFO)
            for (int i = tempList.Count - 1; i >= 0; i--)
            {
                _cameraStack.Push(tempList[i]);
            }

            Debug.Log($"Удален контроллер из стека. Осталось: {_cameraStack.Count}");
        }
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
                Debug.Log("Стек камер пуст, показываем игровые камеры");
                ShowPlayerCameras();
                return;
            }

            // Получаем первого живого контроллера
            var previous = PeekAlive();

            if (previous == null)
            {
                Debug.LogWarning("Не найдено живых контроллеров камер в стеке");
                ShowPlayerCameras();
                return;
            }

            Debug.Log($"Восстанавливаем камеру: {previous.gameObject.name}");

            // Активируем найденную камеру
            previous.SetMain(pushInStack: false);
        }
    }
}
