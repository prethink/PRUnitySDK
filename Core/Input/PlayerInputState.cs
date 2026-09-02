using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Состояние ввода одного владельца — игрока или бота.
/// </summary>
/// <remarks>
/// Обычный C#-класс без <see cref="MonoBehaviour"/>: он не читает устройства, а только
/// хранит уже разобранный ввод. Что именно означают ключи, решает игра — состояние
/// работает с любым <see cref="Enumeration"/>, поэтому одинаково подходит клавиатуре,
/// джойстику и боту.
/// </remarks>
public class PlayerInputState
{
    /// <summary>
    /// Идентификатор владельца ввода.
    /// </summary>
    public readonly Guid InputGuid;

    /// <summary>Удерживаемые ключи.</summary>
    private readonly Dictionary<Enumeration, bool> held = new();

    /// <summary>Буфер нажатий текущего кадра, в который идёт запись.</summary>
    private Dictionary<Enumeration, bool> pressedFrame = new();

    /// <summary>Буфер отпусканий текущего кадра, в который идёт запись.</summary>
    private Dictionary<Enumeration, bool> releasedFrame = new();

    /// <summary>Снимок нажатий, безопасный для чтения в течение кадра.</summary>
    private Dictionary<Enumeration, bool> pressed = new();

    /// <summary>Снимок отпусканий, безопасный для чтения в течение кадра.</summary>
    private Dictionary<Enumeration, bool> released = new();

    /// <summary>Осевые значения.</summary>
    private readonly Dictionary<Enumeration, float> axis = new();

    /// <summary>Векторные значения.</summary>
    private readonly Dictionary<Enumeration, Vector2> vectors = new();

    /// <summary>
    /// Ключ отпущен.
    /// </summary>
    public event Action<Guid, Enumeration> OnReleasedKey;

    /// <summary>
    /// Ключ нажат.
    /// </summary>
    public event Action<Guid, Enumeration> OnPressedKey;

    /// <summary>
    /// Изменился векторный ввод.
    /// </summary>
    public event Action<Guid, Enumeration, Vector2> OnChangeVector;

    /// <summary>
    /// Изменился осевой ввод.
    /// </summary>
    public event Action<Guid, Enumeration, float> OnChangeAxis;

    public PlayerInputState(Guid inputGuid)
    {
        InputGuid = inputGuid;
    }

    /// <summary>
    /// Ключ удерживается прямо сейчас.
    /// </summary>
    public bool IsHeld(Enumeration key) =>
        held.TryGetValue(key, out var v) && v;

    /// <summary>
    /// Ключ был нажат в этом кадре.
    /// </summary>
    public bool IsPressed(Enumeration key) =>
        pressed.TryGetValue(key, out var v) && v;

    /// <summary>
    /// Ключ был отпущен в этом кадре.
    /// </summary>
    public bool IsReleased(Enumeration key) =>
        released.TryGetValue(key, out var v) && v;

    /// <summary>
    /// Возвращает осевое значение или <c>0</c>, если ключ не задан.
    /// </summary>
    public float GetAxis(Enumeration key) =>
        axis.TryGetValue(key, out var v) ? v : 0f;

    /// <summary>
    /// Возвращает векторное значение или <see cref="Vector2.zero"/>, если ключ не задан.
    /// </summary>
    public Vector2 GetVector(Enumeration key) =>
        vectors.TryGetValue(key, out var v) ? v : Vector2.zero;

    /// <summary>
    /// Выставляет удержание ключа, не порождая нажатие или отпускание.
    /// </summary>
    public void SetHeld(Enumeration key, bool value)
    {
        held[key] = value;
    }

    /// <summary>
    /// Отмечает нажатие ключа в текущем кадре.
    /// </summary>
    public void SetPressed(Enumeration key)
    {
        pressedFrame[key] = true;
        held[key] = true;
    }

    /// <summary>
    /// Отмечает отпускание ключа в текущем кадре.
    /// </summary>
    public void SetReleased(Enumeration key)
    {
        releasedFrame[key] = true;
        held[key] = false;
    }

    /// <summary>
    /// Обновляет ключ по текущему состоянию устройства.
    /// </summary>
    /// <remarks>
    /// Нажатие и отпускание выставляются только на переходе, поэтому источник ввода
    /// может звать метод каждый кадр, не следя за предыдущим состоянием сам.
    /// </remarks>
    /// <param name="key">Ключ ввода.</param>
    /// <param name="isDown">Состояние устройства в этом кадре.</param>
    public void SetKey(Enumeration key, bool isDown)
    {
        bool wasDown = held.TryGetValue(key, out var v) && v;

        if (isDown && !wasDown)
            SetPressed(key);
        else if (!isDown && wasDown)
            SetReleased(key);
        else if (isDown)
            SetHeld(key, true);
    }

    /// <summary>
    /// Записывает осевое значение и сообщает об изменении.
    /// </summary>
    public void SetAxis(Enumeration key, float value)
    {
        axis[key] = value;
        OnChangeAxis?.Invoke(InputGuid, key, value);
    }

    /// <summary>
    /// Записывает векторное значение и сообщает об изменении.
    /// </summary>
    public void SetVector(Enumeration key, Vector2 value)
    {
        vectors[key] = value;
        OnChangeVector?.Invoke(InputGuid, key, value);
    }

    /// <summary>
    /// Переводит накопленные за кадр нажатия и отпускания в снимок для чтения.
    /// </summary>
    /// <remarks>
    /// Буферы меняются местами, а не пересоздаются, поэтому кадровая синхронизация
    /// не мусорит. Пока идёт запись следующего кадра, читатели видят стабильный снимок.
    /// </remarks>
    public void FrameSync()
    {
        (pressed, pressedFrame) = (pressedFrame, pressed);
        (released, releasedFrame) = (releasedFrame, released);

        pressedFrame.Clear();
        releasedFrame.Clear();

        foreach (var key in pressed.Keys)
            OnPressedKey?.Invoke(InputGuid, key);

        foreach (var key in released.Keys)
            OnReleasedKey?.Invoke(InputGuid, key);
    }
}
