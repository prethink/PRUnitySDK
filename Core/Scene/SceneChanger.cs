using System;
using System.Collections;
using UnityEngine.SceneManagement;

public class SceneChanger : SingletonProviderBase<SceneChanger>
{
    public static bool IsReady { get; private set; } = true;

    public event Action OnSceneReady;
    public event Action OnScenePrepared;

    //public static void Register(IScenePreloader element)
    //{
    //    IsReady = false;
    //    awaitableElements.Add(element);
    //    PauseManager.SetNotifyPauseChange();
    //}

    //public void Ready(IScenePreloader element)
    //{
    //    if (awaitableElements.All(x => x.IsReady))
    //        OnScenePrepared?.Invoke();
    //}

    public void InvokeSceneReady()
    {
        IsReady = true;
        OnSceneReady?.Invoke();
        EventBus.RaiseEvent<IReadySceneGameEvent>(x => x.OnReadyScene());
        PauseManager.SetNotifyPauseChange();
    }

    public void SceneChangeWithLoadingScreen(int id)
    {
        if (GetSettings().UseFadeOnChange)
            ScreenFade.Instance.FadeIn(() => StartSceneWithLoadingScreen(id));
        else
            StartSceneWithLoadingScreen(id);
    }

    /// <param name="onShown">Вызывается, когда новая сцена показана игроку.</param>
    public void SceneChange(int id, Action onShown = null)
    {
        SceneChange(() => SceneManager.LoadScene(id), onShown);
    }

    /// <summary>
    /// Выполняет загрузку сцены под настроенным затемнением.
    /// </summary>
    /// <param name="onShown">
    /// Вызывается, когда новая сцена показана игроку: её первый кадр отрисован, а затемнение сошло.
    /// </param>
    public void SceneChange(Action loadScene, Action onShown = null)
    {

        if (GetSettings().UseFadeOnChange)
            ScreenFade.Instance.FadeIn(() => StartScene(loadScene, onShown));
        else
            StartScene(loadScene, onShown);
    }

    private void StartSceneWithLoadingScreen(int id)
    {
        SceneDataChanger.Instance.SetData<int>(SceneDataChanger.NEXT_SCENE_KEY, id);
        SceneManager.LoadScene(SceneIds.LOADING_SCENE_INDEX);
    }

    private void StartScene(Action loadScene, Action onShown)
    {
        bool useFade = GetSettings().UseFadeOnChange;

        // Показана — значит и загружена, и видна. Порядок не задан: затемнение с нулевой
        // длительностью сходит раньше, чем сцена загрузилась.
        int pending = useFade ? 2 : 1;

        if (onShown != null)
            SceneManager.sceneLoaded += OnLoaded;

        loadScene();

        if (useFade)
            ScreenFade.Instance.FadeOut(Complete);

        void OnLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnLoaded;
            PRMonoBehaviourHost.Instance.StartCoroutine(AfterFirstFrame(Complete));
        }

        void Complete()
        {
            if (--pending == 0)
                onShown?.Invoke();
        }
    }

    /// <remarks>
    /// <c>sceneLoaded</c> приходит до <c>Start</c> объектов сцены и до её первого кадра;
    /// к следующему кадру тот уже на экране.
    /// </remarks>
    private static IEnumerator AfterFirstFrame(Action callback)
    {
        yield return null;
        callback();
    }

    private SceneTransitionSettings GetSettings() 
        => PRUnitySDK.Settings.SceneTransition;
}
