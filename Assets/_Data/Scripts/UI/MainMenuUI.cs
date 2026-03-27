using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;

public class MainMenuUI : MenuBase
{
    public override MenuType menuType => MenuType.MainMenu;

    [Header("Buttons")]
    [SerializeField] private UnityEngine.UI.Button playButton;
    [SerializeField] private UnityEngine.UI.Button continueButton;
    [SerializeField] private UnityEngine.UI.Button settingsButton;

    [Header("Animation")]
    [SerializeField] private float buttonAnimDuration = 0.3f;

    protected override void LoadComponent()
    {
        if (playButton == null) playButton = transform.Find("ButtonPanel/PlayButton")?.GetComponent<UnityEngine.UI.Button>();
        if (continueButton == null) continueButton = transform.Find("ButtonPanel/ContinueButton")?.GetComponent<UnityEngine.UI.Button>();
        if (settingsButton == null) settingsButton = transform.Find("ButtonPanel/SettingsButton")?.GetComponent<UnityEngine.UI.Button>();
    }

    protected override void LoadComponentRuntime()
    {
        playButton?.onClick.AddListener(OnPlayClicked);
        continueButton?.onClick.AddListener(OnContinueClicked);
        settingsButton?.onClick.AddListener(OnSettingsClicked);

        EventManager.Instance.Subscribe(GameEvent.GameStarted, OnGameStarted);
    }

    private void OnPlayClicked()
    {
        UIManager.Instance.ChangeMenu(MenuType.MapSelection);
    }

    private void OnContinueClicked()
    {
        PersistentData.Instance.ResetSession();
        EventManager.Instance.Notify(GameEvent.GameStarted);
    }

    private void OnSettingsClicked()
    {
        Debug.Log("Settings clicked (chua implement)");
    }

    private void OnGameStarted(object data)
    {
        Close();
    }

    public override void Open(object data = null)
    {
        base.Open(data);
        AnimateButtonsIn();
    }

    private void AnimateButtonsIn()
    {
        playButton?.transform.DOScale(0f, 0f);
        continueButton?.transform.DOScale(0f, 0f);
        settingsButton?.transform.DOScale(0f, 0f);

        playButton?.transform.DOScale(1f, buttonAnimDuration).SetEase(Ease.OutBack).SetDelay(0.1f);
        continueButton?.transform.DOScale(1f, buttonAnimDuration).SetEase(Ease.OutBack).SetDelay(0.2f);
        settingsButton?.transform.DOScale(1f, buttonAnimDuration).SetEase(Ease.OutBack).SetDelay(0.3f);
    }

    private void OnDestroy()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.Unsubscribe(GameEvent.GameStarted, OnGameStarted);
        }
    }
}
