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

Позиционные источники используют `spatialBlend = 1`, линейное затухание и отдельный пул. Стартовые значения пула, `minDistance` и `maxDistance` задаются в prefab менеджера.

У позиционных эффектов есть предел: ударов и шагов бывают десятки в секунду, и без него пул рос бы под каждый пик.

Все пределы лежат в настройках проекта — `PRUnitySDK.Settings.SoundLimits` (`SoundLimitSettings`), а не в префабе менеджера: подбирать их приходится на устройстве, и лезть ради этого в префаб или код не нужно. Каждое число задано дважды — для компьютера и для телефона с планшетом.

| Поле | Компьютер | Телефон | Что задаёт |
| --- | --- | --- | --- |
| `Voice Limit` | 32 | 12 | сколько позиционных эффектов звучит разом; 0 — без предела |
| `Same Clip Interval` | 0.015 | 0.045 | один и тот же клип не запускается чаще этого, секунды; 0 — без ограничения |

Звук сверх предела пропускается, а не обрывает играющий: оборванный удар слышен как щелчок, пропущенный не слышен вовсе.

### Эффекты без ограничений

Для звуков, которые должны проходить независимо от занятых голосов и частоты повторов,
передают `ignoreLimits: true`:

```csharp
sound.PlaySoundEffectAtPoint(footstepSound, position, randomPitch, ignoreLimits: true);
```

Так воспроизводятся шаги персонажей из `PlayerControllerMovement`. Они обходят общий предел,
предел по ключу и оба интервала повторов на компьютере и телефоне. Их источники переиспользуются
в отдельном пуле и не занимают лимит обычных эффектов. Позиция, затухание, громкость и mute
работают как у остальных позиционных звуков.

### Важные звуки

Звук, который нельзя терять — смерть цели, награда, — играют с `important: true`:

```csharp
sound.PlaySoundEffectAtPoint(clip, position, pitch, volume, limitKey: 0, important: true);
```

Важному звуку доступен запас голосов сверх общего предела, поэтому занятые ударами голоса его не глушат. Обычным звукам запас недоступен.

| Поле | Компьютер | Телефон | Что задаёт |
| --- | --- | --- | --- |
| `Important Voice Reserve` | 8 | 4 | сколько голосов сверх общего предела могут занять важные звуки |

Это запас, а не гарантия: важный звук всё равно пропускается, если запас исчерпан или такой же клип только что прозвучал (`Same Clip Interval`) — второй одинаковый звук в тот же миг ничего не добавляет. Предел по ключу важность не снимает: звуку, который должен пройти мимо него, ключ не передают.
 Телефон и планшет определяются по `PRUnitySDK.DeviceInfo.IsTouchDevice()`. На обычные (2D) эффекты и звуки интерфейса предел не действует.

### Ключ источника шума

Общий предел — страховка. Прицельно шумный источник ограничивается ключом: последний параметр `PlaySoundEffectAtPoint` — `limitKey`.

```csharp
// Свой предел у объекта:
sound.PlaySoundEffectAtPoint(clip, position, pitch, volume, GetInstanceID());

// Общий предел у группы объектов:
int key = SoundManager.GetLimitKey("Block.Copper");
sound.PlaySoundEffectAtPoint(clip, position, pitch, volume, key);
```

Звуков с одним ключом звучит не больше предела, и новый с тем же ключом не запускается чаще промежутка; звуки с другими ключами и без ключа это не задевает. Ноль — «без ключа».

| Поле | Компьютер | Телефон | Что задаёт |
| --- | --- | --- | --- |
| `Key Voice Limit` | 8 | 4 | сколько звуков с одним ключом звучит разом; 0 — без предела |
| `Key Interval` | 0.02 | 0.05 | звук с тем же ключом не запускается чаще этого, секунды |

`HealthSound` передаёт ключ сам: поле `Limit Key` — общий ключ (одинаковая строка у нескольких объектов даёт общий предел), пусто — ключом служит сам объект. Звук смерти идёт без ключа и важным звуком: он случается раз, и терять его нельзя. Одного «без ключа» для этого мало — общий предел голосов действует и на такие звуки.

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
| `RandomPitch` | включает случайный pitch из жёстко заданного диапазона `[-3, 3]` |

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

Музыка настраивается в настройках проекта, раздел `Background Music` (`PRUnitySDK.Settings.BackgroundMusic`):

| Поле | По умолчанию | Что делает |
| --- | --- | --- |
| `Enabled` | вкл. | Играть фоновую музыку. Выключено — менеджер сам её не запускает |
| `Tracks` | пусто | Треки по порядку: один или несколько. Пусто — берётся прежний список из базы (`Sounds → Background Music`) |
| `Loop` | вкл. | Повторять без конца. Выключено — плейлист играет один раз и замолкает |
| `Shuffle` | выкл. | Случайный порядок; один и тот же трек дважды подряд не ставится |
| `Volume` | 1 | Громкость относительно выбранной игроком: 0,5 — вдвое тише его ползунка |

При запуске менеджер берёт эти настройки и начинает играть. Один трек с повтором зацикливает сам источник —
стык конца с началом без щели; несколько треков переключает наблюдатель плейлиста.

Музыку можно сменить из кода — своя композиция у сцены, у босса, у меню:

```csharp
PRUnitySDK.Managers.Sound.PlayBackgroundMusic(new[] { bossTheme }, loop: true);
PRUnitySDK.Managers.Sound.StopBackgroundMusic();
```

`PlayBackgroundMusic()` без аргументов запускает текущий трек заново — в том числе после `StopBackgroundMusic()`
и после доигранного плейлиста без повтора. Во время `PRUnitySDK.PauseManager.IsLogicPaused` музыка ставится
на паузу и затем продолжается с прежней позиции.

Длинному треку в настройках импорта ставят `Load Type: Compressed In Memory`: распакованный целиком он занимает
в памяти в десять раз больше файла.

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

Для пользовательского или системного mute предпочтительно использовать `AudioMixerManager`: он раздельно хранит пользовательскую и системную причины отключения, управляет `SoundManager` и синхронизируется с музыкальной паузой.

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
| `PlayBackgroundMusic()` | запустить текущий трек фонового плейлиста |
| `PlayBackgroundMusic(tracks, loop, shuffle)` | заменить плейлист и запустить его с начала |
| `StopBackgroundMusic` | остановить фоновую музыку, не теряя плейлист |
| `IsMute` | проверить общее состояние mute |
| `Mute` / `UnMute` | напрямую изменить громкость источников менеджера |

## Ограничения

- `SoundManagerData.RegisterSound()` пока не регистрирует наборы: внутри метода оставлен `TODO`. Регистрируйте `AudioSet` напрямую через `SoundManager`.
- `AudioMixerManager.SetMusicVolume`, `SetEffectVolume`, `SetUIVolume` и `SetMasterVolume` сейчас не меняют exposed-параметры mixer: вызовы `AudioMixer.SetFloat` закомментированы. Фактическая громкость поддерживается самим `SoundManager` через `GameSettings` и значения `AudioSource`.
- `AudioSet.RandomPitch` не имеет настраиваемого диапазона и при включении выбирает значение от `-3` до `3`, игнорируя поле `Pitch`.
- Повторная регистрация той же пары «владелец + категория» игнорируется и не заменяет существующий `AudioSet`.
- Пулы растут при нехватке свободных источников и не уменьшаются автоматически. Обычный позиционный пул растёт до своего предела голосов (с запасом для важных звуков); отдельный пул эффектов с `ignoreLimits: true` ограничений не имеет.
- Для вызовов через `PRUnitySDK.Managers.Sound` дождитесь завершения инициализации SDK.
