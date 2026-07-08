using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardNotifier : PRMonoBehaviour
{
    [SerializeField] protected GameObject container;
    [SerializeField] protected GameObject rewardContainer;
    [SerializeField] protected Image qualityBorder;
    [SerializeField] protected Image rays;
    [SerializeField] protected CanvasGroup raysCanvasGroup;
    [SerializeField] protected Image icon;
    [SerializeField] protected LocalizationObserver nameItem;
    [SerializeField] protected LocalizationObserver quality;
    [SerializeField] protected TextMeshProUGUI countResource;
    [SerializeField] protected Button actionButton;

    [SerializeField] private float scaleDuration = 0.5f; 
    [SerializeField] private float delayBetween = 1f; 
    [SerializeField] private float fadeDuration = 0.5f;

    [SerializeField] private AudioClip rewardSound;

    private Action callback;

    private bool unsetPaused;
    private bool restoreCursorState;

    public void ShowObjectReward(RewardItemBase reward, Action callback, bool unsetPaused = true, bool restoreCursorState = false)
    {
        Show(reward.Icon, reward.Item, reward.GetQuality(), callback, null, unsetPaused, restoreCursorState);
        //DashboardMessageSender.OpenObjectReward(reward);
    }

    public void ShowResourceReward(RewardResource reward, Action callback, long? count = null, bool unsetPaused = true, bool restoreCursorState = false)
    {
        Show(reward.Icon, reward.Item, reward.GetQuality(), callback, count.HasValue ? count : reward.Count, unsetPaused, restoreCursorState);
        //DashboardMessageSender.AddResource(reward, count);
    }

    public void ShowObject(ItemDefinitionBase item, Action callback, bool unsetPaused = true, bool restoreCursorState = false)
    {
        Show(item.Icon, item, item.Quality, callback, null, unsetPaused, restoreCursorState);
        //DashboardMessageSender.OpenObject(item);
    }

    public void ShowRewardAction(RewardAction acionReward, Action callback, bool unsetPaused = true, bool restoreCursorState = false)
    {
        Show(acionReward.Icon, acionReward, acionReward.GetQuality(), callback, null, unsetPaused, restoreCursorState);
    }

    public void ShowResource(RewardResource resource, Action callback, long count, bool unsetPaused = true, bool restoreCursorState = false)
    {
        Show(resource.Icon, resource, resource.GetQuality(), callback, count, unsetPaused, restoreCursorState);
    }

    public void Show(Sprite iconReward, ILocalizationProvider translate, QualityType qualityReward, Action callback, long? count = null, bool unsetPaused = true, bool restoreCursorState = false)
    {
        actionButton.interactable = false;
        PRUnitySDK.PauseManager.SetLogicPaused(true, this);
        CursorManager.ActiveCursor();
        countResource.gameObject.SetActive(count.HasValue);
        if (count.HasValue)
        {
            var textValue = $"+ {count}";
            countResource.SetText(textValue);
        }

        quality.TextMeshProUGUI.color = QualityUtils.GetColor(qualityReward);
        rays.color = QualityUtils.GetColor(qualityReward);
        qualityBorder.color = QualityUtils.GetColor(qualityReward);
        quality.SetLocalization(new QualityLocalizationProvider(qualityReward));
        nameItem.SetLocalization(translate);
        icon.sprite = iconReward;
        this.callback = callback;
        container.gameObject.SetActive(true);
        rewardContainer.transform.localScale = Vector3.zero;
        raysCanvasGroup.alpha = 0;
        PRUnitySDK.Managers.Sound.PlaySoundUIOneShot(rewardSound);
        this.unsetPaused = unsetPaused;
        this.restoreCursorState = restoreCursorState;
        // Анимируем появление контейнера
        rewardContainer.transform.DOScale(1, scaleDuration)
            .SetEase(Ease.OutBack) // Даем плавность
            .OnComplete(() =>
            {
                // После появления контейнера ждем delayBetween и показываем CanvasGroup
                raysCanvasGroup.DOFade(1, fadeDuration).SetEase(Ease.Linear).SetDelay(delayBetween);
                actionButton.interactable = true;
            });
    }

    public void Hide()
    {
        callback?.Invoke();
        if(this.unsetPaused)
            PRUnitySDK.PauseManager.SetLogicPaused(false, this);
        container.gameObject.SetActive(false);
    }

    protected override void OnEnable()
    {
        actionButton.onClick.AddListener(() => 
        {
            if(restoreCursorState)
                GameManager.Instance.LoadingUserCursorState();
            Hide(); 
        });
        base.OnEnable();
    }

    protected override void OnDisable()
    {
        actionButton.onClick.RemoveAllListeners();
        base.OnDisable();
    }
}
