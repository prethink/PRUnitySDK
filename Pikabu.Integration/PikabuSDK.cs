#if !PRSDK_DISABLE_PIKABU
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Мост к SDK «Пикабу Игр» (<c>PkbSDK</c>): запуск, игрок, облачное хранилище, реклама, покупки.
/// </summary>
/// <remarks>
/// Своего плагина у площадки нет: вызовы идут в <c>Plugins/Pikabu/PikabuSDK.jslib</c>, а ответы
/// возвращаются через <c>SendMessage</c> на объект этого компонента. Почти всё у SDK асинхронное, поэтому
/// запрос получает номер, и ответ с этим номером находит свой обработчик.
/// <para>
/// В редакторе и вне браузера SDK нет, и мост его изображает: игрок-гость, хранилище в <c>PlayerPrefs</c>,
/// реклама «досматривается» за секунду, покупка проходит сразу. Так интеграцию можно гонять в редакторе.
/// </para>
/// <para>
/// Реклама до вызова <see cref="GameStarted"/> (preloader) показывается шаблоном страницы, ещё до запуска Unity.
/// </para>
/// </remarks>
public sealed class PikabuSDK : MonoBehaviour
{
    /// <summary>
    /// Имя объекта-приёмника: на него JavaScript шлёт ответы.
    /// </summary>
    public const string ReceiverName = "PikabuSDK";

    #region Поля и свойства

    private static PikabuSDK receiver;
    private static int nextRequestId;
    private static readonly Dictionary<int, Action<string>> requests = new();
    private static readonly Dictionary<int, Action> adOpenings = new();

    /// <summary>
    /// Чем закончился запуск SDK.
    /// </summary>
    public static PikabuSDKState State { get; private set; }

    /// <summary>
    /// SDK запущен, и к нему можно обращаться.
    /// </summary>
    public static bool IsReady => State == PikabuSDKState.Ready;

    /// <summary>
    /// Игрок: идентификатор в этой игре, имя, аватар, вошёл ли он в аккаунт Пикабу.
    /// </summary>
    /// <remarks>
    /// Без SDK — <see langword="null"/>. После входа в аккаунт данные обновляются (<see cref="Authorized"/>).
    /// </remarks>
    public static PikabuPlayer Player { get; private set; }

    /// <summary>
    /// Площадка показывает игроку ролики за награду. Не меняется за сессию.
    /// </summary>
    public static bool IsRewardedSupported { get; private set; }

    /// <summary>
    /// Площадка показывает игроку полноэкранную рекламу. Не меняется за сессию.
    /// </summary>
    public static bool IsFullscreenSupported { get; private set; }

    /// <summary>
    /// Товары игры из Студии Пикабу. Приходят вместе с запуском SDK; обновить — <see cref="GetProducts"/>.
    /// </summary>
    public static IReadOnlyList<PikabuProduct> Products => products;

    private static List<PikabuProduct> products = new();

    /// <summary>
    /// Каталог товаров получен: при запуске площадка могла не успеть его отдать.
    /// </summary>
    public static bool ProductsLoaded { get; private set; }

    /// <summary>
    /// Запуск SDK закончился — успешно или нет (<see cref="State"/>).
    /// </summary>
    public static event Action Initialized;

    /// <summary>
    /// Игрок вошёл в аккаунт Пикабу посреди игры. <see cref="Player"/> уже обновлён.
    /// </summary>
    public static event Action<PikabuPlayer> Authorized;

    /// <summary>
    /// Облако не приняло сохранение; в аргументе код площадки (<c>REVISION_CONFLICT</c>, <c>QUOTA_EXCEEDED</c>…).
    /// </summary>
    public static event Action<string> StorageSaveFailed;

    // Без перезагрузки домена статика пережила бы прошлый запуск игры в редакторе.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        receiver = null;
        nextRequestId = 0;
        requests.Clear();
        adOpenings.Clear();
        State = PikabuSDKState.NotStarted;
        Player = null;
        IsRewardedSupported = IsFullscreenSupported = false;
        products = new List<PikabuProduct>();
        ProductsLoaded = false;
        Initialized = null;
        Authorized = null;
        StorageSaveFailed = null;
    }

    #endregion

    #region Запуск

    /// <summary>
    /// Запускает SDK. Итог приходит событием <see cref="Initialized"/>; повторный вызов ничего не делает.
    /// </summary>
    public static void Init()
    {
        if (State != PikabuSDKState.NotStarted)
            return;

        State = PikabuSDKState.Starting;
        EnsureReceiver();
        Native.Init(ReceiverName);
    }

    /// <summary>
    /// Сообщает площадке, что игра загружена и игрок может играть.
    /// </summary>
    public static void GameStarted()
    {
        if (IsReady)
            Native.GameStarted();
    }

    private static void EnsureReceiver()
    {
        if (receiver != null)
            return;

        var host = new GameObject(ReceiverName);
        DontDestroyOnLoad(host);
        receiver = host.AddComponent<PikabuSDK>();
    }

    #endregion

    #region Хранилище

    /// <summary>
    /// Значение ключа облачного хранилища; пусто, если его нет или SDK не запущен.
    /// </summary>
    /// <remarks>
    /// Хранилище приходит вместе с запуском SDK, поэтому чтение синхронное.
    /// </remarks>
    public static string StorageGet(string key) => IsReady ? Native.StorageGet(key) ?? string.Empty : string.Empty;

    /// <summary>
    /// Записывает значение. На сервер оно уйдёт само, с небольшой задержкой; дождаться — <see cref="StorageFlush"/>.
    /// </summary>
    public static void StorageSet(string key, string value)
    {
        if (IsReady)
            Native.StorageSet(key, value ?? string.Empty);
    }

    /// <summary>
    /// Отправляет накопленные изменения на сервер и сообщает, принял ли он их.
    /// </summary>
    /// <remarks>
    /// Не чаще 100 раз за 5 минут: лимит площадки считает именно эти вызовы, а не <see cref="StorageSet"/>.
    /// </remarks>
    public static void StorageFlush(Action<PikabuResult> done)
    {
        if (!IsReady)
        {
            done?.Invoke(new PikabuResult { reason = "NO_SDK" });
            return;
        }

        Native.StorageFlush(Request<PikabuResult>(done));
    }

    #endregion

    #region Реклама

    /// <summary>
    /// Показывает рекламу, если площадка готова её показать.
    /// </summary>
    /// <param name="format">Вид рекламы.</param>
    /// <param name="opening">Реклама сейчас откроется: пора ставить паузу. Не зовётся, если показа не будет.</param>
    /// <param name="done">Итог показа.</param>
    public static void ShowAd(PikabuAdFormat format, Action opening, Action<PikabuAdResult> done)
    {
        if (!IsReady)
        {
            done?.Invoke(new PikabuAdResult { reason = "NO_SDK" });
            return;
        }

        int id = Request<PikabuAdResult>(result =>
        {
            adOpenings.Remove(result.requestId);
            done?.Invoke(result);
        });
        if (opening != null)
            adOpenings[id] = opening;

        Native.ShowAd(format == PikabuAdFormat.Rewarded ? "rewarded" : "fullscreen", id);
    }

    #endregion

    #region Покупки

    /// <summary>
    /// Товары игры из Студии Пикабу.
    /// </summary>
    public static void GetProducts(Action<PikabuProductList> done)
    {
        if (!IsReady)
        {
            done?.Invoke(new PikabuProductList { reason = "NO_SDK" });
            return;
        }

        Native.GetProducts(Request<PikabuProductList>(result =>
        {
            if (result.success)
            {
                products = result.items ?? new List<PikabuProduct>();
                ProductsLoaded = true;
            }

            done?.Invoke(result);
        }));
    }

    /// <summary>
    /// Покупки игрока: постоянные и ещё не подтверждённые расходуемые.
    /// </summary>
    public static void GetPurchases(Action<PikabuPurchaseList> done)
    {
        if (!IsReady)
        {
            done?.Invoke(new PikabuPurchaseList { reason = "NO_SDK" });
            return;
        }

        Native.GetPurchases(Request<PikabuPurchaseList>(done));
    }

    /// <summary>
    /// Открывает окно оплаты. Игроку без аккаунта площадка сначала предложит войти.
    /// </summary>
    public static void Purchase(string productId, string developerPayload, Action<PikabuPurchaseResult> done)
    {
        if (!IsReady)
        {
            done?.Invoke(new PikabuPurchaseResult { reason = "NO_SDK" });
            return;
        }

        Native.Purchase(productId, developerPayload ?? string.Empty, Request<PikabuPurchaseResult>(done));
    }

    /// <summary>
    /// Подтверждает обработку расходуемой покупки: площадка убирает её из списка покупок игрока.
    /// </summary>
    public static void ConsumePurchase(string purchaseId, Action<PikabuResult> done)
    {
        if (!IsReady)
        {
            done?.Invoke(new PikabuResult { reason = "NO_SDK" });
            return;
        }

        Native.ConsumePurchase(purchaseId, Request<PikabuResult>(done));
    }

    #endregion

    #region Игрок

    /// <summary>
    /// Открывает окно входа в аккаунт Пикабу и сообщает, вошёл ли игрок.
    /// </summary>
    public static void OpenAuthDialog(Action<PikabuAuthResult> done)
    {
        if (!IsReady)
        {
            done?.Invoke(new PikabuAuthResult { reason = "NO_SDK" });
            return;
        }

        Native.OpenAuthDialog(Request<PikabuAuthResult>(result =>
        {
            if (result.player != null && !string.IsNullOrEmpty(result.player.id))
                Player = result.player;
            done?.Invoke(result);
        }));
    }

    /// <summary>
    /// Возвращает фокус браузера в игру после окна площадки: оплаты, входа, рекламы.
    /// </summary>
    /// <remarks>
    /// Без фокуса игра стоит на паузе по фокусу (<see cref="GameManager"/>), и игрок видел бы замерший экран,
    /// пока не кликнет по игре.
    /// </remarks>
    public static void FocusGame() => Native.FocusGame();

    #endregion

    #region Ответы JavaScript

    private static int Request<T>(Action<T> done) where T : PikabuResponse, new()
    {
        int id = ++nextRequestId;
        requests[id] = json =>
        {
            T result = Parse<T>(json) ?? new T { reason = "BAD_RESPONSE" };
            result.requestId = id;
            done?.Invoke(result);
        };
        return id;
    }

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
            PRLog.WriteWarning(typeof(PikabuSDK), $"Не разобран ответ SDK Пикабу: {exception.Message}\n{json}");
            return null;
        }
    }

    /// <summary>
    /// Итог запуска SDK. Зовёт JavaScript.
    /// </summary>
    private void OnInitialized(string json)
    {
        PikabuInitResult result = Parse<PikabuInitResult>(json);

        if (result is { ok: true })
        {
            Player = result.player;
            IsRewardedSupported = result.rewardedSupported;
            IsFullscreenSupported = result.fullscreenSupported;
            products = result.products ?? new List<PikabuProduct>();
            ProductsLoaded = result.productsLoaded;
            State = PikabuSDKState.Ready;
        }
        else
        {
            PRLog.WriteWarning(this, $"SDK Пикабу не запустился: {result?.error}");
            State = PikabuSDKState.Failed;
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
            PRLog.WriteWarning(this, $"Ответ SDK Пикабу без номера: {message}");
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
        int separator = message?.IndexOf('|') ?? -1;
        string type = separator < 0 ? message : message.Substring(0, separator);
        string data = separator < 0 ? string.Empty : message.Substring(separator + 1);

        switch (type)
        {
            case "adOpening":
                if (int.TryParse(data, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) && adOpenings.Remove(id, out Action opening))
                    opening();
                break;

            case "visibility":
                // Скрытая вкладка — пауза, как при потере фокуса: игра и музыка стоят, пока игрок не вернётся.
                PRUnitySDK.PauseManager?.SetFocusPaused(data != "1", this);
                break;

            case "auth":
                PikabuPlayer player = Parse<PikabuPlayer>(data);
                if (player != null)
                    Player = player;
                Authorized?.Invoke(Player);
                break;

            case "storageError":
                StorageSaveFailed?.Invoke(data);
                break;
        }
    }

    #endregion

    #region Вызовы JavaScript

    private static class Native
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void PikabuSDK_Init(string receiverName);
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void PikabuSDK_GameStarted();
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern string PikabuSDK_StorageGet(string key);
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void PikabuSDK_StorageSet(string key, string value);
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void PikabuSDK_StorageFlush(int id);
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void PikabuSDK_ShowAd(string format, int id);
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void PikabuSDK_GetProducts(int id);
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void PikabuSDK_GetPurchases(int id);
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void PikabuSDK_Purchase(string productId, string payload, int id);
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void PikabuSDK_ConsumePurchase(string purchaseId, int id);
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void PikabuSDK_OpenAuthDialog(int id);
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void PikabuSDK_FocusGame();

        public static void Init(string receiverName) => PikabuSDK_Init(receiverName);
        public static void GameStarted() => PikabuSDK_GameStarted();
        public static string StorageGet(string key) => PikabuSDK_StorageGet(key);
        public static void StorageSet(string key, string value) => PikabuSDK_StorageSet(key, value);
        public static void StorageFlush(int id) => PikabuSDK_StorageFlush(id);
        public static void ShowAd(string format, int id) => PikabuSDK_ShowAd(format, id);
        public static void GetProducts(int id) => PikabuSDK_GetProducts(id);
        public static void GetPurchases(int id) => PikabuSDK_GetPurchases(id);
        public static void Purchase(string productId, string payload, int id) => PikabuSDK_Purchase(productId, payload, id);
        public static void ConsumePurchase(string purchaseId, int id) => PikabuSDK_ConsumePurchase(purchaseId, id);
        public static void OpenAuthDialog(int id) => PikabuSDK_OpenAuthDialog(id);
        public static void FocusGame() => PikabuSDK_FocusGame();
#else
        // Вне браузера SDK изображается здесь же: ответы приходят тем же путём, что и из JavaScript.
        private const string StoragePrefix = "PikabuEditorStorage.";

        public static void Init(string receiverName) =>
            receiver.OnInitialized(JsonUtility.ToJson(new PikabuInitResult
            {
                ok = true,
                player = new PikabuPlayer { id = "editor", name = string.Empty, isAuthorized = false },
                rewardedSupported = true,
                fullscreenSupported = true,
                productsLoaded = true,
            }));

        public static void GameStarted() => PRLog.WriteDebug(typeof(PikabuSDK), "Пикабу: gameStarted().");
        public static string StorageGet(string key) => PlayerPrefs.GetString(StoragePrefix + key, string.Empty);
        public static void StorageSet(string key, string value) => PlayerPrefs.SetString(StoragePrefix + key, value);

        public static void StorageFlush(int id)
        {
            PlayerPrefs.Save();
            Respond(id, new PikabuResult { success = true }, 0f);
        }

        public static void ShowAd(string format, int id)
        {
            receiver.OnEvent("adOpening|" + id.ToString(CultureInfo.InvariantCulture));
            Respond(id, new PikabuAdResult { success = true, rendered = true, reward = format == "rewarded" }, 1f);
        }

        public static void GetProducts(int id) => Respond(id, new PikabuProductList { success = true }, 0f);
        public static void GetPurchases(int id) => Respond(id, new PikabuPurchaseList { success = true }, 0f);

        public static void Purchase(string productId, string payload, int id) =>
            Respond(id, new PikabuPurchaseResult
            {
                success = true,
                purchase = new PikabuPurchase
                {
                    purchaseId = Guid.NewGuid().ToString("N"),
                    productId = productId,
                    isConsumable = true,
                    developerPayload = payload,
                },
            }, 0.5f);

        public static void ConsumePurchase(string purchaseId, int id) => Respond(id, new PikabuResult { success = true }, 0f);
        public static void OpenAuthDialog(int id) => Respond(id, new PikabuAuthResult { player = Player }, 0f);

        public static void FocusGame()
        {
        }

        /// <remarks>
        /// Не сразу, а следующим кадром или позже: у настоящего SDK ответ всегда асинхронный, и код игры
        /// не должен полагаться на обратное.
        /// </remarks>
        private static void Respond(int id, object response, float delay) =>
            receiver.StartCoroutine(RespondLater(id + "|" + JsonUtility.ToJson(response), delay));

        private static IEnumerator RespondLater(string message, float delay)
        {
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);
            else
                yield return null;

            if (receiver != null)
                receiver.OnResponse(message);
        }
#endif
    }

    #endregion
}

/// <summary>
/// Чем закончился запуск SDK Пикабу.
/// </summary>
public enum PikabuSDKState
{
    NotStarted,
    Starting,
    Ready,
    Failed,
}

/// <summary>
/// Вид рекламы Пикабу, который показывает игра. Рекламу на загрузке показывает шаблон страницы.
/// </summary>
public enum PikabuAdFormat
{
    Fullscreen,
    Rewarded,
}

// Поля — в нижнем регистре, как в ответах JavaScript: их разбирает JsonUtility.
#pragma warning disable IDE1006

/// <summary>
/// Ответ SDK на запрос: удался ли он и код причины, если нет.
/// </summary>
[Serializable]
public class PikabuResponse
{
    public bool success;
    public string reason;

    /// <summary>
    /// Номер запроса, на который пришёл ответ.
    /// </summary>
    [NonSerialized] public int requestId;
}

[Serializable]
public sealed class PikabuResult : PikabuResponse
{
}

[Serializable]
public sealed class PikabuPlayer
{
    public string id;
    public string name;
    public string avatar;
    public bool isAuthorized;
}

[Serializable]
public sealed class PikabuInitResult
{
    public bool ok;
    public PikabuPlayer player;
    public bool rewardedSupported;
    public bool fullscreenSupported;
    public bool productsLoaded;
    public List<PikabuProduct> products = new();
    public string error;
}

/// <summary>
/// Итог показа рекламы.
/// </summary>
/// <remarks>
/// <see cref="rendered"/> — окно с рекламой было на экране. <see cref="reward"/> — ролик досмотрен, награду выдать.
/// Без показа в <see cref="PikabuResponse.reason"/> — код площадки (<c>COOLDOWN_ACTIVE</c>, <c>UI_BUSY</c>…)
/// или <c>CANNOT_SHOW</c>, если площадка ответила, что показать сейчас нечего.
/// </remarks>
[Serializable]
public sealed class PikabuAdResult : PikabuResponse
{
    public bool rendered;
    public bool reward;
}

/// <summary>
/// Товар из Студии Пикабу. Цена — в копейках; <see cref="formattedPrice"/> — готовая подпись, «50 ₽».
/// </summary>
[Serializable]
public sealed class PikabuProduct
{
    public string id;
    public string name;
    public int price;
    public string currencyCode;
    public string formattedPrice;
}

[Serializable]
public sealed class PikabuProductList : PikabuResponse
{
    public List<PikabuProduct> items = new();
}

[Serializable]
public sealed class PikabuPurchase
{
    public string purchaseId;
    public string productId;
    public bool isConsumable;
    public string developerPayload;
}

[Serializable]
public sealed class PikabuPurchaseList : PikabuResponse
{
    public List<PikabuPurchase> items = new();
}

[Serializable]
public sealed class PikabuPurchaseResult : PikabuResponse
{
    public PikabuPurchase purchase;
}

[Serializable]
public sealed class PikabuAuthResult : PikabuResponse
{
    public PikabuPlayer player;
}

#pragma warning restore IDE1006
#endif
