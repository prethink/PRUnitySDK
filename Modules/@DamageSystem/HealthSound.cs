using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Проигрывает звук в месте попадания, когда связанная сущность получила урон или умерла.
/// </summary>
public sealed class HealthSound : MonoBehaviour
{
    private enum PlaybackOrder
    {
        Sequential,
        Random
    }

    [SerializeField] private HealthComponent healthComponent;
    [SerializeField] private List<AudioClip> damageClips = new();

    [Tooltip("Играет при смерти вместо звука урона. Если список пуст, смертельный удар звучит как обычный.")]
    [SerializeField] private List<AudioClip> deathClips = new();

    [SerializeField] private PlaybackOrder playbackOrder;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField, Range(0.1f, 3f)] private float minPitch = 0.9f;
    [SerializeField, Range(0.1f, 3f)] private float maxPitch = 1.1f;

    private DamageOutcome lastPlayedOutcome;
    private int nextDamageClipIndex;
    private int nextDeathClipIndex;

    private void OnEnable()
    {
        if (healthComponent == null)
            healthComponent = GetComponentInParent<HealthComponent>();

        if (healthComponent == null)
        {
            Debug.LogWarning("HealthSound не нашёл HealthComponent.", this);
            return;
        }

        // Смерть обрабатывается до скрытия объекта; для остальных
        // попаданий достаточно итогового события обработки.
        healthComponent.OnHealthChange += OnHealthChanged;
        healthComponent.OnDamageProcessed += OnDamageProcessed;
    }

    private void OnDisable()
    {
        if (healthComponent != null)
        {
            healthComponent.OnHealthChange -= OnHealthChanged;
            healthComponent.OnDamageProcessed -= OnDamageProcessed;
        }

        lastPlayedOutcome = null;
    }

    private void OnHealthChanged(HealthChangedEventArgsBase change)
    {
        if (change?.DamageOutcome?.Result == DamageResult.Killed)
            PlaySound(change.DamageOutcome);
    }

    private void OnDamageProcessed(DamageOutcome outcome)
    {
        PlaySound(outcome);
    }

    private void PlaySound(DamageOutcome outcome)
    {
        if (outcome == null || !outcome.WasApplied || ReferenceEquals(lastPlayedOutcome, outcome))
            return;

        SoundManager sound = PRUnitySDK.Managers?.Sound;
        if (sound == null)
            return;

        AudioClip clip = SelectClip(outcome);
        if (clip == null)
            return;

        lastPlayedOutcome = outcome;
        Vector3 position = outcome.HitPoint ?? outcome.VictimPosition ?? transform.position;
        Vector2 pitchRange = new(Mathf.Min(minPitch, maxPitch), Mathf.Max(minPitch, maxPitch));
        sound.PlaySoundEffectAtPoint(clip, position, pitchRange, volume);
    }

    /// <summary>
    /// Выбирает звук смерти или урона.
    /// </summary>
    /// <remarks>
    /// Смерть через <see cref="HealthComponent.Kill(IEntity, IWeapon)"/> приходит с нулевым
    /// уроном, поэтому звук урона на ней не играет, а звук смерти играет.
    /// </remarks>
    private AudioClip SelectClip(DamageOutcome outcome)
    {
        if (outcome.Result == DamageResult.Killed)
        {
            AudioClip deathClip = SelectClip(deathClips, ref nextDeathClipIndex);
            if (deathClip != null)
                return deathClip;
        }

        return outcome.AppliedDamage > 0f
            ? SelectClip(damageClips, ref nextDamageClipIndex)
            : null;
    }

    private AudioClip SelectClip(List<AudioClip> clips, ref int nextClipIndex)
    {
        if (clips == null || clips.Count == 0)
            return null;

        if (playbackOrder == PlaybackOrder.Sequential)
        {
            for (int offset = 0; offset < clips.Count; offset++)
            {
                int index = (nextClipIndex + offset) % clips.Count;
                AudioClip clip = clips[index];
                if (clip == null)
                    continue;

                nextClipIndex = (index + 1) % clips.Count;
                return clip;
            }

            return null;
        }

        int availableCount = 0;
        foreach (AudioClip clip in clips)
        {
            if (clip != null)
                availableCount++;
        }

        if (availableCount == 0)
            return null;

        int selected = UnityEngine.Random.Range(0, availableCount);
        foreach (AudioClip clip in clips)
        {
            if (clip == null)
                continue;

            if (selected-- == 0)
                return clip;
        }

        return null;
    }
}
