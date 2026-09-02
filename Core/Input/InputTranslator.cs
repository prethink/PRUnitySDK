using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Хранит состояния ввода игроков и маршрутизирует их события по Input GUID.
/// </summary>
/// <remarks>
/// Транспорт, а не биндинг: translator не читает устройства и не знает, какие ключи
/// существуют в игре. Источники ввода — клавиатура, джойстик, бот — пишут в состояние
/// владельца через <c>SetKey</c>, <c>SetAxis</c> и <c>SetVector</c>, а потребители
/// читают его по своему <c>InputGuid</c>. Набор ключей объявляет игра собственным
/// наследником <c>EnumerationProviderBase</c>.
/// </remarks>
public class InputTranslator : PRMonoBehaviourSingletonBase<InputTranslator>
{
    /// <summary>
    /// Состояния ввода, зарегистрированные по идентификаторам игроков.
    /// </summary>
    private readonly Dictionary<Guid, PlayerInputState> inputs = new();

    /// <summary>
    /// Кэшируемый snapshot состояний для безопасной синхронизации кадра.
    /// </summary>
    private PlayerInputState[] frameSnapshot = Array.Empty<PlayerInputState>();

    /// <summary>
    /// Указывает, что snapshot состояний необходимо перестроить.
    /// </summary>
    private bool frameSnapshotDirty = true;

    /// <summary>
    /// Вызывается при отпускании кнопки одним из игроков.
    /// </summary>
    public event Action<Guid, Enumeration> OnReleasedKey;

    /// <summary>
    /// Вызывается при нажатии кнопки одним из игроков.
    /// </summary>
    public event Action<Guid, Enumeration> OnPressedKey;

    /// <summary>
    /// Вызывается при изменении векторного ввода одного из игроков.
    /// </summary>
    public event Action<Guid, Enumeration, Vector2> OnChangeVector;

    /// <summary>
    /// Вызывается при изменении осевого ввода одного из игроков.
    /// </summary>
    public event Action<Guid, Enumeration, float> OnChangeAxis;

    /// <summary>
    /// Пытается получить уже существующий экземпляр, не создавая новый GameObject.
    /// </summary>
    public static bool TryGetExisting(out InputTranslator translator)
    {
        translator = instance;
        return translator != null;
    }

    /// <summary>
    /// Возвращает существующее состояние игрока или создаёт и регистрирует новое.
    /// </summary>
    public PlayerInputState GetPlayer(Guid inputGuid)
    {
        if (inputs.TryGetValue(inputGuid, out PlayerInputState state))
            return state;

        state = new PlayerInputState(inputGuid);
        SubscribeToState(state);
        inputs.Add(inputGuid, state);
        frameSnapshotDirty = true;
        return state;
    }

    /// <summary>
    /// Пытается получить состояние игрока без его автоматического создания.
    /// </summary>
    public bool TryGetPlayer(Guid inputGuid, out PlayerInputState state)
    {
        return inputs.TryGetValue(inputGuid, out state);
    }

    /// <summary>
    /// Удаляет состояние игрока и отсоединяет внутренние обработчики событий.
    /// </summary>
    public bool RemovePlayer(Guid inputGuid)
    {
        if (!inputs.TryGetValue(inputGuid, out PlayerInputState state))
            return false;

        UnsubscribeFromState(state);
        inputs.Remove(inputGuid);
        frameSnapshotDirty = true;
        return true;
    }

    public bool IsHeld(Guid inputGuid, Enumeration key) => GetPlayer(inputGuid).IsHeld(key);
    public bool IsPressed(Guid inputGuid, Enumeration key) => GetPlayer(inputGuid).IsPressed(key);
    public bool IsReleased(Guid inputGuid, Enumeration key) => GetPlayer(inputGuid).IsReleased(key);
    public float GetAxis(Guid inputGuid, Enumeration key) => GetPlayer(inputGuid).GetAxis(key);
    public Vector2 GetVector(Guid inputGuid, Enumeration key) => GetPlayer(inputGuid).GetVector(key);

    public void SetKey(Guid inputGuid, Enumeration key, bool isDown) => GetPlayer(inputGuid).SetKey(key, isDown);
    public void SetAxis(Guid inputGuid, Enumeration key, float value) => GetPlayer(inputGuid).SetAxis(key, value);
    public void SetVector(Guid inputGuid, Enumeration key, Vector2 value) => GetPlayer(inputGuid).SetVector(key, value);

    /// <summary>
    /// Переводит временные состояния кнопок всех игроков в snapshot текущего кадра.
    /// </summary>
    protected void LateUpdate()
    {
        foreach (PlayerInputState state in GetFrameSnapshot())
            state.FrameSync();
    }

    /// <summary>
    /// Освобождает состояния ввода перед уничтожением translator'а.
    /// </summary>
    protected override void UnRegisterEventsOnDestroy()
    {
        foreach (PlayerInputState state in inputs.Values)
            UnsubscribeFromState(state);

        inputs.Clear();
        frameSnapshot = Array.Empty<PlayerInputState>();
        frameSnapshotDirty = false;
        base.UnRegisterEventsOnDestroy();
    }

    /// <summary>
    /// Возвращает snapshot, который не изменяется обработчиками во время FrameSync.
    /// </summary>
    private PlayerInputState[] GetFrameSnapshot()
    {
        if (!frameSnapshotDirty)
            return frameSnapshot;

        frameSnapshot = new PlayerInputState[inputs.Count];
        inputs.Values.CopyTo(frameSnapshot, 0);
        frameSnapshotDirty = false;
        return frameSnapshot;
    }

    private void SubscribeToState(PlayerInputState state)
    {
        state.OnReleasedKey += HandleReleasedKey;
        state.OnPressedKey += HandlePressedKey;
        state.OnChangeVector += HandleChangeVector;
        state.OnChangeAxis += HandleChangeAxis;
    }

    private void UnsubscribeFromState(PlayerInputState state)
    {
        state.OnReleasedKey -= HandleReleasedKey;
        state.OnPressedKey -= HandlePressedKey;
        state.OnChangeVector -= HandleChangeVector;
        state.OnChangeAxis -= HandleChangeAxis;
    }

    private void HandleReleasedKey(Guid inputGuid, Enumeration key) => OnReleasedKey?.Invoke(inputGuid, key);
    private void HandlePressedKey(Guid inputGuid, Enumeration key) => OnPressedKey?.Invoke(inputGuid, key);
    private void HandleChangeVector(Guid inputGuid, Enumeration key, Vector2 value) => OnChangeVector?.Invoke(inputGuid, key, value);
    private void HandleChangeAxis(Guid inputGuid, Enumeration key, float value) => OnChangeAxis?.Invoke(inputGuid, key, value);
}
