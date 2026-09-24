using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Проигрывает звук в месте попадания, когда связанная сущность получила урон.
/// </summary>
public sealed class DamageSound : MonoBehaviour
{
    private enum PlaybackOrder
    {
        Sequential,
        Random
    }

    [SerializeField] private HealthComponent healthComponent;
    [SerializeField] private List<AudioClip> damageClips = new();
    [SerializeField] private PlaybackOrder playbackOrder;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField, Range(0.1f, 3f)] private float minPitch = 0.9f;
    [SerializeField, Range(0.1f, 3f)] private float maxPitch = 1.1f;

    private DamageOutcome lastPlayedOutcome;
    private int nextClipIndex;

    private void OnEnable()
    {
        if (healthComponent == null)
            healthComponent = GetComponentInParent<HealthComponent>();

        if (healthComponent == null)
        {
            Debug.LogWarning("DamageSound не нашёл HealthComponent.", this);
            return;
        }

        // Смертельный урон обрабатывается до скрытия объекта; для остальных
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
            PlayDamageSound(change.DamageOutcome);
    }

    private void OnDamageProcessed(DamageOutcome outcome)
    {
        PlayDamageSound(outcome);
    }

    private void PlayDamageSound(DamageOutcome outcome)
    {
        if (outcome == null || !outcome.WasApplied ||
            outcome.AppliedDamage <= 0f || ReferenceEquals(lastPlayedOutcome, outcome))
            return;

        SoundManager sound = PRUnitySDK.Managers?.Sound;
        if (sound == null)
            return;

        AudioClip clip = SelectClip();
        if (clip == null)
            return;

        lastPlayedOutcome = outcome;
        Vector3 position = outcome.HitPoint ?? outcome.VictimPosition ?? transform.position;
        Vector2 pitchRange = new(Mathf.Min(minPitch, maxPitch), Mathf.Max(minPitch, maxPitch));
        sound.PlaySoundEffectAtPoint(clip, position, pitchRange, volume);
    }

    private AudioClip SelectClip()
    {
        if (damageClips == null || damageClips.Count == 0)
            return null;

        if (playbackOrder == PlaybackOrder.Sequential)
        {
            for (int offset = 0; offset < damageClips.Count; offset++)
            {
                int index = (nextClipIndex + offset) % damageClips.Count;
                AudioClip clip = damageClips[index];
                if (clip == null)
                    continue;

                nextClipIndex = (index + 1) % damageClips.Count;
                return clip;
            }

            return null;
        }

        int availableCount = 0;
        foreach (AudioClip clip in damageClips)
        {
            if (clip != null)
                availableCount++;
        }

        if (availableCount == 0)
            return null;

        int selected = UnityEngine.Random.Range(0, availableCount);
        foreach (AudioClip clip in damageClips)
        {
            if (clip == null)
                continue;

            if (selected-- == 0)
                return clip;
        }

        return null;
    }
}
