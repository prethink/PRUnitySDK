# SoundManager

`SoundManager` — менеджер звука PRUnitySDK. Он воспроизводит музыку, UI-звуки, обычные и позиционные эффекты, поддерживает категоризированные наборы `AudioSet` и управляет долгоживущими зацикленными эффектами.

Менеджер создаётся SDK автоматически из `Resources/PRUnitySDK/Prefabs/SoundManager.prefab` и доступен после инициализации через:

```csharp
SoundManager sound = PRUnitySDK.Managers.Sound;
```

## Возможности

- отдельные источники для музыки и UI;
- расширяемые пулы `AudioSource` для одновременных 2D- и 3D-эффектов;
- случайный pitch для одноразовых эффектов;
- регистрация нескольких вариантов звука под одной категорией;
- зацикленные эффекты с явным временем жизни;
- последовательное воспроизведение фоновой музыки;
- синхронизация громкости с `GameSettings` и состоянием `AudioMixerManager`;
- пауза и продолжение музыки вместе с логической паузой SDK.

## Быстрый старт

Все примеры предполагают, что SDK уже инициализирован.

### Обычный 2D-эффект

```csharp
[SerializeField] private AudioClip hitSound;

private void PlayHit()
{
    PRUnitySDK.Managers.Sound.PlaySoundEffectOneShot(hitSound);
}
```

Можно передать множитель громкости и диапазон случайного pitch:

```csharp
PRUnitySDK.Managers.Sound.PlaySoundEffectOneShot(
    hitSound,
    volume: 0.8f,
    randomPitch: new Vector2(0.9f, 1.1f));
```

Каждый одновременно звучащий эффект получает свободный источник из пула, поэтому изменение pitch нового звука не влияет на уже запущенные эффекты.

### Позиционный 3D-эффект

```csharp
[SerializeField] private AudioClip footstepSound;

private void PlayFootstep()
{
    PRUnitySDK.Managers.Sound.PlaySoundEffectAtPoint(
        footstepSound,
        transform.position,
        randomPitch: new Vector2(0.95f, 1.05f),
        volume: 0.75f);
}
```

Позиционные источники используют `spatialBlend = 1`, логарифмическое затухание и отдельный пул. Стартовые значения пула, `minDistance` и `maxDistance` задаются в prefab менеджера.

`PlayClipAtPoint` — сокращённый вариант без случайного pitch:

```csharp
PRUnitySDK.Managers.Sound.PlayClipAtPoint(explosionSound, explosionPosition, 1f);
```

### UI-звук

```csharp
PRUnitySDK.Managers.Sound.PlaySoundUIOneShot(buttonClickSound);

// С явным множителем громкости:
PRUnitySDK.Managers.Sound.PlaySoundUIOneShot(buttonClickSound, 0.5f);
```

### Зацикленный эффект

Долгоживущие звуки, например двигатель или электрический гул, получают уникальный `Guid` и играют до явного удаления:

```csharp
using System;

private Guid engineSoundId;

private void StartEngine(AudioClip engineLoop)
{
    engineSoundId = Guid.NewGuid();
    PRUnitySDK.Managers.Sound.PlayEffectWithLifetime(engineSoundId, engineLoop);
}

private void StopEngine()
{
    PRUnitySDK.Managers.Sound.RemoveEffect(engineSoundId);
}
```

Повторный вызов `PlayEffectWithLifetime` с уже зарегистрированным `Guid` игнорируется.

## AudioSet и категории

`AudioSet` объединяет категорию, несколько вариантов клипа и настройки воспроизведения:

| Поле | Назначение |
| --- | --- |
| `Key` | имя категории, например `Hit` или `Footstep` |
| `AudioClips` | варианты, один из которых выбирается случайно |
| `SoundType` | источник воспроизведения: `Effect`, `Music` или `UI` |
| `Volume` | громкость источника |
| `Pitch` | фиксированный pitch |
| `PanStereo` | положение в стереопанораме |
| `RandomPitch` | включает случайный pitch, определяемый реализацией `AudioSet` |

Набор сначала регистрируется, затем воспроизводится по категории:

```csharp
[SerializeField] private List<AudioClip> hitVariants;

private void RegisterSounds()
{
    var hitSet = new AudioSet("Hit", hitVariants, SoundType.Effect);
    PRUnitySDK.Managers.Sound.RegisterSoundList(hitSet);
}

private void PlayRandomHit()
{
    PRUnitySDK.Managers.Sound.PlaySound("hit");
}
```

Категории сравниваются без учёта регистра. В `AudioSet` должен быть хотя бы один ненулевой клип.

### Разделение категорий по владельцу

Одинаковые имена категорий можно изолировать с помощью типа, компонента или произвольной строки:

```csharp
PRUnitySDK.Managers.Sound.RegisterSoundList(typeof(PlayerCombat), playerHitSet);
PRUnitySDK.Managers.Sound.PlaySound(typeof(PlayerCombat), "Hit");

PRUnitySDK.Managers.Sound.RegisterSoundList(this, footstepSet);
PRUnitySDK.Managers.Sound.PlaySound(this, "Footstep", transform.position);

PRUnitySDK.Managers.Sound.RegisterSoundList("Environment", windSet);
PRUnitySDK.Managers.Sound.PlaySound("Environment", "Wind");
```

Передача позиции в `PlaySound` всегда включает позиционное воспроизведение, независимо от `SoundType` набора.

Если набор не найден, менеджер пишет предупреждение в `PRLog`. Повторная регистрация уже существующей пары «владелец + категория» не заменяет прежний набор.

## Фоновая музыка

При запуске менеджер копирует треки из `PRUnitySDK.Database.Sounds.BackgroundMusic` и начинает воспроизводить их по порядку. После последнего трека плейлист начинается заново.

Чтобы настроить музыку:

1. Откройте asset базы SDK, содержащий `SoundDatabase`.
2. Заполните список `Background Music`.
3. Убедитесь, что в prefab `SoundManager` назначен `musicSource`.

`PlayBackgroundMusic()` можно вызвать повторно, чтобы запустить текущий трек заново. Во время `PRUnitySDK.PauseManager.IsLogicPaused` музыка ставится на паузу и затем продолжается с прежней позиции.

## Громкость и mute

Менеджер примерно каждые `0.2` секунды читает текущие `GameSettings`:

- `MasterVolume` ограничивает итоговую громкость каналов;
- `MusicVolume`, `EffectVolume` и `UIVolume` управляют соответствующими источниками;
- `OffMusic` отключает музыку;
- `OffSound` отключает весь звук.

Проверить общее состояние можно через:

```csharp
bool muted = PRUnitySDK.Managers.Sound.IsMute();
```

Для пользовательского или системного mute предпочтительно использовать `AudioMixerManager`: он хранит причину отключения и синхронизируется с музыкальной паузой.

```csharp
PRUnitySDK.Managers.AudioMixer.MuteByUser(this);
PRUnitySDK.Managers.AudioMixer.UnMuteByUser(this);

PRUnitySDK.Managers.AudioMixer.MuteBySystem(this);
PRUnitySDK.Managers.AudioMixer.UnMuteBySystem(this);
```

Методы `SoundManager.Mute()` и `UnMute()` напрямую меняют громкость источников и обычно нужны только внутренней интеграции.

## Публичный API

| Метод | Назначение |
| --- | --- |
| `PlaySoundEffectOneShot` | воспроизвести одноразовый 2D-эффект |
| `PlaySoundEffectAtPoint` | воспроизвести одноразовый 3D-эффект со случайным pitch |
| `PlayClipAtPoint` | воспроизвести одноразовый 3D-эффект |
| `PlaySoundUIOneShot` | воспроизвести UI-звук |
| `PlayEffectWithLifetime` | запустить зацикленный эффект по `Guid` |
| `RemoveEffect` | остановить и удалить зацикленный эффект |
| `RegisterSoundList` | зарегистрировать `AudioSet` |
| `PlaySound` | воспроизвести зарегистрированную категорию |
| `PlayBackgroundMusic` | запустить текущий трек фонового плейлиста |
| `IsMute` | проверить общее состояние mute |
| `Mute` / `UnMute` | напрямую изменить громкость источников менеджера |

## Ограничения

- `SoundManagerData.RegisterSound()` пока не регистрирует наборы: внутри метода оставлен `TODO`. Регистрируйте `AudioSet` напрямую через `SoundManager`.
- Пулы растут при нехватке свободных источников и не уменьшаются автоматически.
- Для вызовов через `PRUnitySDK.Managers.Sound` дождитесь завершения инициализации SDK.
