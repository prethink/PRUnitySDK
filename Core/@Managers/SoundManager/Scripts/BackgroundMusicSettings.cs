using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Фоновая музыка игры: какие треки играют, в каком порядке и повторяются ли.
/// </summary>
/// <remarks>
/// Играет её <see cref="SoundManager"/> на своём музыкальном источнике: он же ставит музыку
/// на паузу вместе с игрой и держит громкость по настройкам игрока. Здесь только то, что
/// решает дизайнер, — состав и поведение плейлиста.
/// <para>
/// Трек может быть один, а может быть несколько; плейлист либо крутится без конца, либо играет
/// один раз и замолкает (вступление, титры, сцена с одной композицией).
/// </para>
/// </remarks>
[Serializable]
[SettingsDescription("Фоновая музыка: треки, порядок и повтор. Играет менеджер звука — он же ставит музыку на паузу вместе с игрой и слушается громкости из настроек игрока.")]
public sealed class BackgroundMusicSettings
{
    [SerializeField]
    [Tooltip("Играть фоновую музыку. Выключено — менеджер звука музыку сам не запускает.")]
    private bool enabled = true;

    [SerializeField]
    [Tooltip("Треки по порядку. Один трек — играет он один. Пусто — берётся прежний список из базы (Sounds → Background Music).")]
    private List<AudioClip> tracks = new();

    [SerializeField]
    [Tooltip("Повторять без конца: после последнего трека снова первый. Выключено — плейлист играет один раз и замолкает.")]
    private bool loop = true;

    [SerializeField]
    [Tooltip("Случайный порядок. Один и тот же трек дважды подряд не ставится.")]
    private bool shuffle;

    [SerializeField, Range(0f, 1f)]
    [Tooltip("Громкость музыки относительно той, что выбрал игрок: 0.5 — вдвое тише его ползунка.")]
    private float volume = 1f;

    /// <summary>
    /// Играть ли фоновую музыку.
    /// </summary>
    public bool Enabled => enabled;

    /// <summary>
    /// Треки по порядку.
    /// </summary>
    public IReadOnlyList<AudioClip> Tracks => tracks;

    /// <summary>
    /// Повторять ли плейлист без конца.
    /// </summary>
    public bool Loop => loop;

    /// <summary>
    /// Ставить ли треки в случайном порядке.
    /// </summary>
    public bool Shuffle => shuffle;

    /// <summary>
    /// Громкость музыки относительно выбранной игроком, от нуля до единицы.
    /// </summary>
    public float Volume => Mathf.Clamp01(volume);
}
