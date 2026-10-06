using System;
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

    public void SceneChange(int id)
    {
        SceneChange(() => SceneManager.LoadScene(id));
    }

    /// <summary>
    /// Выполняет загрузку сцены под настроенным затемнением.
    /// </summary>
    public void SceneChange(Action loadScene)
    {

        if (GetSettings().UseFadeOnChange)
            ScreenFade.Instance.FadeIn(() => StartScene(loadScene));
        else
            StartScene(loadScene);
    }

    private void StartSceneWithLoadingScreen(int id)
    {
        SceneDataChanger.Instance.SetData<int>(SceneDataChanger.NEXT_SCENE_KEY, id);
        SceneManager.LoadScene(SceneIds.LOADING_SCENE_INDEX);
    }

    private void StartScene(Action loadScene)
    {
        loadScene();

        if (GetSettings().UseFadeOnChange)
            ScreenFade.Instance.FadeOut();
    }

    private SceneTransitionSettings GetSettings() 
        => PRUnitySDK.Settings.SceneTransition;
}
