using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Флаги, которые сцена добавляет к проектным на время своей жизни.
/// </summary>
/// <remarks>
/// Компонент обобщённый, на объект вешается наследник с конкретным набором:
/// <c>public class SceneFlags : SceneFlagsBase&lt;GameFlagsEnumerations&gt; { }</c>.
/// </remarks>
public class SceneFlagsBase<T> : PRMonoBehaviour, IFlagProvider
    where T : FlagsProviderBase, new()
{
    [SerializedDictionary("Флаг", "Решение")]
    [SerializeField]
    private SerializedDictionary<EnumerationReference<T>, FlagDecision> flags = new();

    private readonly FlagResolver sceneFlags = new();

    /// <summary>
    /// Ключи, влияние на которые сейчас держит сам компонент.
    /// </summary>
    private readonly HashSet<Enumeration> appliedKeys = new();

    private readonly HashSet<Enumeration> desiredKeys = new();

    private bool isReadyRequested;

    private bool isRegistered;

    /// <summary>
    /// Resolver сцены. Влияния из кода добавляйте в него со своим source.
    /// </summary>
    public FlagResolver Resolver => sceneFlags;

    /// <summary>
    /// Зарегистрирован ли resolver сцены в <see cref="FlagsManager"/>.
    /// </summary>
    public bool IsRegistered => isRegistered;

    protected override void OnEnable()
    {
        base.OnEnable();

        // Объект на сцене включается раньше, чем поднимается SDK, и менеджера флагов
        // тогда ещё нет. Сигнал вызывает и опоздавшего, и пришедшего после готовности.
        if (!isReadyRequested)
        {
            isReadyRequested = true;
            PRUnitySDK.ReadySignal.SubscribeOnReady(OnSDKReady);
            return;
        }

        OnSDKReady();
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (isRegistered && PRUnitySDK.Managers.Flags != null)
            PRUnitySDK.Managers.Flags.RemoveSceneFlags(sceneFlags);

        isRegistered = false;
        appliedKeys.Clear();
        sceneFlags.Clear();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Снимаем подписку с сигнала: объект могли уничтожить до готовности SDK,
    /// и тогда делегат держал бы уничтоженный компонент до конца сессии.
    /// </remarks>
    protected override void UnRegisterEventsOnDestroy()
    {
        if (isReadyRequested && PRUnitySDK.ReadySignal != null)
            PRUnitySDK.ReadySignal.UnSubscribe(OnSDKReady);

        base.UnRegisterEventsOnDestroy();
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        // До регистрации применять некуда, а в Play Mode правку в инспекторе
        // видно сразу.
        if (isRegistered)
            ApplyFlags();
    }

    /// <summary>
    /// Переносит настройки инспектора в resolver сцены.
    /// </summary>
    /// <remarks>
    /// Влияния компонента, которых больше нет в списке, снимаются. Влияния с другим
    /// source остаются: их владелец удаляет сам.
    /// </remarks>
    public void ApplyFlags()
    {
        desiredKeys.Clear();

        foreach (var item in flags)
        {
            Enumeration key = EnumerationReference<T>.ToEnumeration(item.Key);
            if (key == null || item.Value == FlagDecision.Unspecified)
                continue;

            desiredKeys.Add(key);

            if (item.Value == FlagDecision.Allow)
                sceneFlags.Allow(key, this);
            else
                sceneFlags.Deny(key, this);
        }

        foreach (var key in appliedKeys)
        {
            if (!desiredKeys.Contains(key))
                sceneFlags.Remove(key, this);
        }

        appliedKeys.Clear();
        appliedKeys.UnionWith(desiredKeys);
    }

    private void OnSDKReady()
    {
        if (this == null || !isActiveAndEnabled)
            return;

        ApplyFlags();
        PRUnitySDK.Managers.Flags.AddSceneFlags(sceneFlags);
        isRegistered = true;
    }
}
