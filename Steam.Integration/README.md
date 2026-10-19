# Интеграция со Steam

PC-сборка в Steam через [Steamworks.NET](https://github.com/rlabrecque/Steamworks.NET) 2025.164.1
(`Assets/com.rlabrecque.steamworks.net`, пример `Assets/Scripts/Steamworks.NET/SteamManager.cs` из пакета не используется).
Достижения и лидерборды лежат в `PRUnitySDKPrivate/Steam.Integration` и относятся к этому же модулю.

Модуль `STEAM`, раздел «PC — Windows, Linux, macOS», слот «Магазин».

## Важно

- **Включение.** *PRUnitySDK → Модули*, вкладка «Интеграции», раздел «PC», слот «Магазин»: выбрать «Steam» и нажать
  «Применить». Код собирается только под Windows, Linux и macOS (`UNITY_STANDALONE_*`): в WebGL и Android он не попадает,
  и при активной платформе WebGL Unity его не компилирует.
- **App ID** — `PRSDKSettings → Steam`. По умолчанию 480 — тестовая игра Valve (Spacewar): Steam запустится
  у любого, у кого он установлен. Для запуска из редактора и не из Steam рядом с проектом нужен `steam_appid.txt` с App ID
  (Steamworks.NET создаёт его сам); в релизную сборку этот файл не класть.
- **Перезапуск через Steam** (`RestartIfNotLaunchedFromSteam`) включать в релизе: игра, запущенная мимо Steam,
  перезапустится через него. В редакторе не действует.
- **Steam Cloud:** в Steamworks нужна квота (*Steam Cloud → Byte quota per user* и *Number of files*), иначе облако
  не примет файл. Имя файла — `SaveFileName` в настройках.
- **Достижения:** заведите их в Steamworks (*Stats & Achievements → Achievements*) и свяжите в *PRUnitySDK → Settings → Steam →
  Achievements*: GUID достижения из базы проекта и API-имя Steamworks. Без пары достижение в Steam не идёт.
- **Лидерборды:** имя таблицы в настройках рейтинга = имя лидерборда в Steamworks (*Leaderboards*), заводить заранее:
  сортировка по убыванию, для времени — тип «Time (milliseconds)».
- **Покупок нет:** микротранзакции Steam подтверждает только сервер игры.
- **Steam не запущен** — игра работает без него: сохранение на диске, имя и язык по умолчанию.

## Из чего состоит

| Раздел | Что | Steamworks |
| --- | --- | --- |
| `SteamPlatform` | `SteamAPI.InitEx`, `RunCallbacks` каждый кадр, `Shutdown` на выходе; оверлей (Shift+Tab) ставит паузу | `SteamAPI`, `GameOverlayActivated_t` |
| `Bootstrap.Steam` | SDK собирается после запуска Steam; экран загрузки — на языке Steam | — |
| `SteamGameDataStorager` | `IGameDataStorage`: файл в Steam Cloud и в `persistentDataPath`, при загрузке — более новый по дате изменения | `ISteamRemoteStorage` |
| `SteamLanguageManager` | `ILanguageManager`: язык игры в клиенте Steam (russian/ukrainian → ru, turkish → tr, иначе en), выбор игрока — `PlayerPrefs` | `SteamApps.GetCurrentGameLanguage` |
| `SteamPlayerNameService` | ник в Steam | `SteamFriends.GetPersonaName` |
| `SteamAchievementSync` (приватная часть) | выполненные достижения из списка связей → Steam; при запуске догоняет уже выполненные | `SteamUserStats.SetAchievement` |
| `SteamRatingSystem` (приватная часть) | `IRatingTableSource`: топ и соседи игрока, запись «лучший остаётся», время в миллисекундах | `FindLeaderboard`, `UploadLeaderboardScore`, `DownloadLeaderboardEntries` |
