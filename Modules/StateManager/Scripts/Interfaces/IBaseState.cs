using UnityEngine;

/// <summary>
/// Базовый интерфейс для состояний FSM (Finite State Machine).
/// </summary>
public interface IBaseState<T> : IBaseState
    where T : StateManagerBase<T>
{
    #region Поля и свойства


    /// <summary>
    /// Определяет, является ли состояние стартовым при инициализации FSM.
    /// </summary>
    public T StateManager { get; }

    #endregion

    #region Методы

    /// <summary>
    /// Привязывает состояние к менеджеру состояний.
    /// Используется для управления переходами и доступом к FSM.
    /// </summary>
    /// <param name="stateManager">Менеджер состояний.</param>
    public void LinkToStateManager(T stateManager);

    #endregion
}


/// <summary>
/// Базовый интерфейс для состояний FSM (Finite State Machine).
/// </summary>
public interface IBaseState
{
    #region Поля и свойства

    /// <summary>
    /// Уникальный ключ состояния, используется для идентификации и переключения.
    /// </summary>
    public Enumeration StateKey { get; }

    /// <summary>
    /// Определяет, является ли состояние стартовым при инициализации FSM.
    /// </summary>
    public bool IsStartState { get; }


    #endregion

    #region Методы

    /// <summary>
    /// Вызывается при входе в состояние.
    /// Используется для инициализации логики состояния.
    /// </summary>
    public abstract void EnterState();

    /// <summary>
    /// Вызывается при выходе из состояния.
    /// Используется для очистки данных и остановки логики.
    /// </summary>
    public abstract void ExitState();

    /// <summary>
    /// Обновление состояния, вызывается каждый кадр.
    /// </summary>
    public abstract void UpdateState();

    /// <summary>
    /// Отдельный тик, чтобы не создавать большую нагрузку. 
    /// Вызывается с определённым интервалом, который задаётся в менеджере состояний.
    /// </summary>
    public abstract void Tick();

    /// <summary>
    /// Определяет ключ следующего состояния.
    /// Возвращает null или текущий ключ, если переход не требуется.
    /// </summary>
    /// <returns>Ключ следующего состояния.</returns>
    public abstract Enumeration GetNextState();

    /// <summary>
    /// Вызывается при входе другого коллайдера в триггер.
    /// </summary>
    /// <param name="other">Коллайдер, вошедший в триггер.</param>
    public abstract void OnTriggerEnter(Collider other);

    /// <summary>
    /// Вызывается, пока другой коллайдер находится в триггере.
    /// </summary>
    /// <param name="other">Коллайдер, находящийся в триггере.</param>
    public abstract void OnTriggerStay(Collider other);

    /// <summary>
    /// Вызывается при выходе другого коллайдера из триггера.
    /// </summary>
    /// <param name="other">Коллайдер, вышедший из триггера.</param>
    public abstract void OnTriggerExit(Collider other);

    /// <summary>
    /// Триггер анимационного события без параметров.
    /// </summary>
    public virtual void AnimationTrigger() { }

    /// <summary>
    /// Триггер анимационного события с параметром float.
    /// </summary>
    /// <param name="data">Числовое значение события.</param>
    public virtual void AnimationTriggerFloat(float data) { }

    /// <summary>
    /// Триггер анимационного события с параметром int.
    /// </summary>
    /// <param name="data">Целочисленное значение события.</param>
    public virtual void AnimationTriggerInt(int data) { }

    /// <summary>
    /// Триггер анимационного события с параметром string.
    /// </summary>
    /// <param name="data">Строковое значение события.</param>
    public virtual void AnimationTriggerString(string data) { }

    /// <summary>
    /// Триггер анимационного события с параметром GameObject.
    /// </summary>
    /// <param name="data">Связанный объект сцены.</param>
    public virtual void AnimationTriggerGameObject(GameObject data) { }

    #endregion
}