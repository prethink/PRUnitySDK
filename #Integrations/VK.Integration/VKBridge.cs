#if !PRSDK_DISABLE_VK
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Мост к VK Bridge — площадке VK Games: игра во ВКонтакте и в Одноклассниках одним кодом.
/// </summary>
/// <remarks>
/// Своего плагина нет: вызовы идут в <c>Plugins/VK/VKBridge.jslib</c>, ответы возвращаются через <c>SendMessage</c>
/// на объект этого компонента. Запрос получает номер, и ответ с этим номером находит свой обработчик.
/// <para>
/// Параметры запуска, профиль игрока и сохранение приходят вместе с запуском: игре не нужно ждать их отдельно.
/// Сохранение пишет сам JavaScript — сжатым, частями и не чаще раза в 30 секунд (<see cref="StorageSave"/>):
/// у VK Storage значение до 4096 символов и 1000 записей в час на игрока.
/// </para>
/// <para>
/// Одноклассники видны по платформе запуска (<see cref="IsOk"/>): игра, заведённая в VK Games, привязывается
/// к ним в настройках («Другие площадки»), и тот же VK Bridge работает на ok.ru.
/// </para>
/// <para>
/// В редакторе и вне браузера площадки нет, и мост её изображает: гость, сохранение в <c>PlayerPrefs</c>
/// (ключи <c>VKEditorStorage.*</c>), реклама «досматривается» за секунду.
/// </para>
/// </remarks>
public sealed class VKBridge : MonoBehaviour
{
    /// <summary>
    /// Имя объекта-приёмника: на него JavaScript шлёт ответы.
    /// </summary>
    public const string ReceiverName = "VKBridge";

    #region Поля и свойства

    private static VKBridge receiver;
    private static int nextRequestId;
    private static readonly Dictionary<int, Action<string>> requests = new();
    private static readonly Dictionary<int, Action> adOpenings = new();

    /// <summary>
    /// Чем закончился запуск площадки.
    /// </summary>
    public static VKBridgeState State { get; private set; }

    /// <summary>
    /// Площадка запущена, и к ней можно обращаться.
    /// </summary>
    public static bool IsReady => State == VKBridgeState.Ready;

    /// <summary>
    /// Что площадка сообщила при запуске: игрок, язык, платформа, сохранение. Без площадки — <see langword="null"/>.
    /// </summary>
    public static VKInitResult Launch { get; private set; }

    /// <summary>
    /// Игра запущена в Одноклассниках, а не во ВКонтакте.
    /// </summary>
    public static bool IsOk => Launch is { isOk: true };

    /// <summary>
    /// Ролик за награду готов к показу: площадка проверяет это сама раз в 30 секунд и после каждого ролика.
    /// </summary>
    public static bool IsRewardAvailable { get; private set; }

    /// <summary>
    /// Запуск закончился — успешно или нет (<see cref="State"/>).
    /// </summary>
    public static event Action Initialized;

    /// <summary>
    /// Запись сохранения закончилась: удалась ли и причина, если нет.
    /// </summary>
    public static event Action<bool, string> SaveWritten;

    // Без перезагрузки домена статика пережила бы прошлый запуск игры в редакторе.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        receiver = null;
        nextRequestId = 0;
        requests.Clear();
        adOpenings.Clear();
        State = VKBridgeState.NotStarted;
        Launch = null;
        IsRewardAvailable = false;
        Initialized = null;
        SaveWritten = null;
    }

    #endregion

    #region Запуск

    /// <summary>
    /// Запускает площадку. Итог приходит событием <see cref="Initialized"/>; повторный вызов ничего не делает.
    /// </summary>
    public static void Init()
    {
        if (State != VKBridgeState.NotStarted)
            return;

        State = VKBridgeState.Starting;

        if (receiver == null)
        {
            var host = new GameObject(ReceiverName);
            DontDestroyOnLoad(host);
            receiver = host.AddComponent<VKBridge>();
        }

        Native.Init(ReceiverName);
    }

    #endregion

    #region Хранилище

    /// <summary>
    /// Отдаёт сохранение на запись. Пишется последнее отданное, когда позволят интервал и лимит площадки;
    /// на скрытой странице — сразу.
    /// </summary>
    public static void StorageSave(string text)
    {
        if (IsReady)
            Native.StorageSave(text ?? string.Empty);
    }

    /// <summary>
    /// Записывает короткое значение под своим ключом сразу. Для редких записей: каждая тратит лимит площадки.
    /// </summary>
    public static void StorageSetValue(string key, string value)
    {
        if (IsReady)
            Native.StorageSetValue(key, value ?? string.Empty);
    }

    #endregion

    #region Реклама

    /// <summary>
    /// Показывает рекламу, если у площадки она есть.
    /// </summary>
    /// <param name="format">Вид рекламы.</param>
    /// <param name="opening">Реклама сейчас откроется: пора ставить паузу. Не зовётся, если показа не будет.</param>
    /// <param name="done">Итог показа.</param>
    public static void ShowAd(VKAdFormat format, Action opening, Action<VKAdResult> done)
    {
        if (!IsReady)
        {
            done?.Invoke(new VKAdResult { reason = "NO_SDK" });
            return;
        }

        int id = ++nextRequestId;
        requests[id] = json =>
        {
            adOpenings.Remove(id);
            done?.Invoke(Parse<VKAdResult>(json) ?? new VKAdResult { reason = "BAD_RESPONSE" });
        };
        if (opening != null)
            adOpenings[id] = opening;

        Native.ShowAd(format == VKAdFormat.Reward ? "reward" : "interstitial", id);
    }

    /// <summary>
    /// Возвращает фокус браузера в игру после окна площадки.
    /// </summary>
    public static void FocusGame() => Native.FocusGame();

    #endregion

    #region Ответы JavaScript

    private static T Parse<T>(string json) where T : class
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonUtility.FromJson<T>(json);
        }
        catch (Exception exception)
        {
            PRLog.WriteWarning(typeof(VKBridge), $"Не разобран ответ VK Bridge: {exception.Message}\n{json}");
            return null;
        }
    }

    /// <summary>
    /// Итог запуска. Зовёт JavaScript.
    /// </summary>
    private void OnInitialized(string json)
    {
        VKInitResult result = Parse<VKInitResult>(json);

        if (result is { ok: true })
        {
            Launch = result;
            State = VKBridgeState.Ready;

            if (result.saveLoadFailed)
                PRLog.WriteWarning(this, "Сохранение VK не прочитано: в этой сессии облако не пишется, чтобы не затереть прогресс.");
        }
        else
        {
            PRLog.WriteWarning(this, $"VK Bridge не запустился: {result?.error}");
            State = VKBridgeState.Failed;
        }

        Initialized?.Invoke();
    }

    /// <summary>
    /// Ответ на запрос: «номер|json». Зовёт JavaScript.
    /// </summary>
    private void OnResponse(string message)
    {
        int separator = message?.IndexOf('|') ?? -1;
        if (separator <= 0 || !int.TryParse(message.Substring(0, separator), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
        {
            PRLog.WriteWarning(this, $"Ответ VK Bridge без номера: {message}");
            return;
        }

        if (!requests.Remove(id, out Action<string> handler))
            return;

        try
        {
            handler(message.Substring(separator + 1));
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }

    /// <summary>
    /// Событие площадки: «тип|данные». Зовёт JavaScript.
    /// </summary>
    private void OnEvent(string message)
    {
        string[] parts = (message ?? string.Empty).Split(new[] { '|' }, 3);
        string data = parts.Length > 1 ? parts[1] : string.Empty;

        switch (parts[0])
        {
            case "adOpening":
                if (int.TryParse(data, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) && adOpenings.Remove(id, out Action opening))
                    opening();
                break;

            case "visibility":
                // Скрытая вкладка или свёрнутое приложение — пауза, как при потере фокуса.
                PRUnitySDK.PauseManager?.SetFocusPaused(data != "1", this);
                break;

            case "reward":
                IsRewardAvailable = data == "1";
                break;

            case "saved":
                bool success = data == "1";
                string reason = parts.Length > 2 ? parts[2] : string.Empty;
                if (!success)
                    PRLog.WriteWarning(this, $"Сохранение VK не записано: {reason}. Повторится со следующей записью.");
                SaveWritten?.Invoke(success, reason);
                break;
        }
    }

    #endregion

    #region Вызовы JavaScript

    private static class Native
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void VKBridge_Init(string receiverName);
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void VKBridge_StorageSave(string text);
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void VKBridge_StorageSetValue(string key, string value);
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void VKBridge_ShowAd(string format, int id);
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void VKBridge_FocusGame();

        public static void Init(string receiverName) => VKBridge_Init(receiverName);
        public static void StorageSave(string text) => VKBridge_StorageSave(text);
        public static void StorageSetValue(string key, string value) => VKBridge_StorageSetValue(key, value);
        public static void ShowAd(string format, int id) => VKBridge_ShowAd(format, id);
        public static void FocusGame() => VKBridge_FocusGame();
#else
        // Вне браузера площадка изображается здесь же: ответы приходят тем же путём, что и из JavaScript.
        // Хранилище — файл, а не PlayerPrefs: те общие на все игры, файл переключается вместе с проектом SDK.
        private static readonly PRSDKEditorStorage storage = new("VK");

        public static void Init(string receiverName)
        {
            receiver.OnInitialized(JsonUtility.ToJson(new VKInitResult
            {
                ok = true,
                userId = "editor",
                language = "ru",
                platform = "desktop_web",
                save = storage.Get("prsave"),
                savedLanguage = storage.Get("prlang"),
            }));
            receiver.OnEvent("reward|1");
        }

        public static void StorageSave(string text)
        {
            storage.Set("prsave", text);
            storage.Flush();
            receiver.OnEvent("saved|1");
        }

        public static void StorageSetValue(string key, string value)
        {
            storage.Set(key, value);
            storage.Flush();
        }

        public static void ShowAd(string format, int id)
        {
            receiver.OnEvent("adOpening|" + id.ToString(CultureInfo.InvariantCulture));
            receiver.StartCoroutine(RespondLater(id + "|" + JsonUtility.ToJson(new VKAdResult { shown = true }), 1f));
        }

        public static void FocusGame()
        {
        }

        private static IEnumerator RespondLater(string message, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);

            if (receiver != null)
                receiver.OnResponse(message);
        }
#endif
    }

    #endregion
}

/// <summary>
/// Чем закончился запуск VK Bridge.
/// </summary>
public enum VKBridgeState
{
    NotStarted,
    Starting,
    Ready,
    Failed,
}

/// <summary>
/// Вид рекламы VK.
/// </summary>
public enum VKAdFormat
{
    Interstitial,
    Reward,
}

// Поля — в нижнем регистре, как в ответах JavaScript: их разбирает JsonUtility.
#pragma warning disable IDE1006

/// <summary>
/// Что площадка сообщила при запуске.
/// </summary>
/// <remarks>
/// <see cref="platform"/> — <c>vk_platform</c>: <c>desktop_web</c>, <c>mobile_web</c>, <c>mobile_android</c>,
/// <c>mobile_iphone</c>, <c>mobile_ipad</c>…; в Одноклассниках с окончанием <c>_ok</c>.
/// <see cref="save"/> — прочитанное сохранение, <see cref="savedLanguage"/> — язык, выбранный игроком.
/// </remarks>
[Serializable]
public sealed class VKInitResult
{
    public bool ok;
    public string userId;
    public string firstName;
    public string lastName;
    public string language;
    public string platform;
    public bool isOk;
    public string save;
    public string savedLanguage;
    public bool saveLoadFailed;
    public string error;
}

/// <summary>
/// Итог показа рекламы: была ли она показана и причина, если нет.
/// </summary>
[Serializable]
public sealed class VKAdResult
{
    public bool shown;
    public string reason;
}

#pragma warning restore IDE1006
#endif
