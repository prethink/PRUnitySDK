using System;
using UnityEngine;

/// <summary>
/// Пределы позиционных звуков: сколько их звучит разом и как часто запускается одно и то же.
/// </summary>
/// <remarks>
/// Ударов и шагов бывают десятки в секунду. Без предела они сливаются в шум, а каждый голос стоит
/// процессора — на телефоне особенно. Звук сверх предела пропускается, а не обрывает играющий:
/// оборванный удар слышен как щелчок, пропущенный не слышен вовсе.
/// <para>
/// Лежат в настройках проекта, а не в префабе менеджера звука: у каждой игры своя плотность звуков,
/// и подбирать её приходится на устройстве, не трогая ни префаб, ни код.
/// </para>
/// <para>
/// Каждое число задано дважды: для компьютера и для телефона с планшетом
/// (<c>PRUnitySDK.DeviceInfo.IsTouchDevice()</c>). На обычные (2D) эффекты и звуки интерфейса
/// пределы не действуют. Позиционные эффекты с <c>ignoreLimits: true</c>, включая шаги персонажей,
/// также исключены из этих пределов.
/// </para>
/// </remarks>
[Serializable]
[SettingsDescription("Пределы позиционных звуков: сколько ударов и шагов звучит разом, как часто повторяется один клип, запас для важных звуков. Отсюда начинают, когда звуки на телефоне пропадают или сливаются в шум.")]
public sealed class SoundLimitSettings
{
    [Header("Сколько позиционных эффектов звучит разом")]
    [SerializeField, Min(0)]
    [Tooltip("Новый звук сверх предела не играется. 0 - без предела.")]
    private int voiceLimit = 32;

    [SerializeField, Min(0)]
    [Tooltip("Тот же предел на телефоне и планшете: там голосов меньше, и они дороже. 0 - без предела.")]
    private int touchVoiceLimit = 12;

    [Header("Один и тот же клип")]
    [SerializeField, Min(0f)]
    [Tooltip("Один клип не запускается позиционно чаще этого, секунды: два одинаковых звука в один кадр " +
             "дают только громкость и щелчок. 0 - без ограничения.")]
    private float sameClipInterval = 0.015f;

    [SerializeField, Min(0f)]
    [Tooltip("Тот же промежуток на телефоне и планшете. 0 - без ограничения.")]
    private float touchSameClipInterval = 0.045f;

    [Header("Звуки с одним ключом")]
    [SerializeField, Min(0)]
    [Tooltip("Сколько позиционных эффектов с одним ключом звучит разом. Ключ передаёт тот, кто играет звук: " +
             "так ограничивается один шумный источник - скажем, удары по блокам одного вида, - а остальные " +
             "звуки не страдают. 0 - без предела.")]
    private int keyVoiceLimit = 8;

    [SerializeField, Min(0)]
    [Tooltip("Тот же предел на телефоне и планшете. 0 - без предела.")]
    private int touchKeyVoiceLimit = 4;

    [SerializeField, Min(0f)]
    [Tooltip("Звук с тем же ключом не запускается чаще этого, секунды. 0 - без ограничения.")]
    private float keyInterval = 0.02f;

    [SerializeField, Min(0f)]
    [Tooltip("Тот же промежуток на телефоне и планшете. 0 - без ограничения.")]
    private float touchKeyInterval = 0.05f;

    [Header("Запас для важных звуков")]
    [SerializeField, Min(0)]
    [Tooltip("Сколько голосов сверх общего предела могут занять важные звуки: смерть цели, награда. " +
             "Без запаса такой звук пропадал бы, когда все голоса заняты ударами.")]
    private int importantVoiceReserve = 8;

    [SerializeField, Min(0)]
    [Tooltip("Тот же запас на телефоне и планшете.")]
    private int touchImportantVoiceReserve = 4;

    /// <summary>
    /// Сколько позиционных эффектов звучит разом; ноль — без предела.
    /// </summary>
    public int GetVoiceLimit(bool touch) => Mathf.Max(0, touch ? touchVoiceLimit : voiceLimit);

    /// <summary>
    /// Как часто можно запускать один и тот же клип, секунды; ноль — без ограничения.
    /// </summary>
    public float GetSameClipInterval(bool touch) => Mathf.Max(0f, touch ? touchSameClipInterval : sameClipInterval);

    /// <summary>
    /// Сколько эффектов с одним ключом звучит разом; ноль — без предела.
    /// </summary>
    public int GetKeyVoiceLimit(bool touch) => Mathf.Max(0, touch ? touchKeyVoiceLimit : keyVoiceLimit);

    /// <summary>
    /// Как часто можно запускать звук с одним ключом, секунды; ноль — без ограничения.
    /// </summary>
    public float GetKeyInterval(bool touch) => Mathf.Max(0f, touch ? touchKeyInterval : keyInterval);

    /// <summary>
    /// Сколько голосов сверх общего предела доступно важным звукам.
    /// </summary>
    public int GetImportantVoiceReserve(bool touch) => Mathf.Max(0, touch ? touchImportantVoiceReserve : importantVoiceReserve);
}
