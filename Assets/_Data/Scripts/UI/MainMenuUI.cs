using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;

public class MainMenuUI : MenuBase
{
    public override MenuType menuType => MenuType.MainMenu;

    [Header("Buttons")]
    [SerializeField] private UnityEngine.UI.Button playButton;
    [SerializeField] private UnityEngine.UI.Button settingsButton;
    [SerializeField] private UnityEngine.UI.Button achievementButton;
    [SerializeField] private UnityEngine.UI.Button quitButton;

    [Header("Animation")]
    [SerializeField] private float buttonAnimDuration = 0.3f;

    protected override void LoadComponent()
    {
        if (playButton == null) playButton = transform.Find("ButtonPanel/PlayButton")?.GetComponent<UnityEngine.UI.Button>();
        if (settingsButton == null) settingsButton = transform.Find("ButtonPanel/SettingsButton")?.GetComponent<UnityEngine.UI.Button>();
        if (achievementButton == null) achievementButton = transform.Find("ButtonPanel/AchievementButton")?.GetComponent<UnityEngine.UI.Button>();
        if (quitButton == null) quitButton = transform.Find("ButtonPanel/QuitButton")?.GetComponent<UnityEngine.UI.Button>();
    }

    protected override void LoadComponentRuntime()
    {
        playButton?.onClick.AddListener(OnPlayClicked);
        settingsButton?.onClick.AddListener(OnSettingsClicked);
        achievementButton?.onClick.AddListener(OnAchievementClicked);
        quitButton?.onClick.AddListener(OnQuitClicked);

        EventManager.Instance.Subscribe(GameEvent.GameStarted, OnGameStarted);
    }

    private void OnPlayClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        UIManager.Instance.ChangeMenu(MenuType.MapSelection);
    }

    private void OnSettingsClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        Time.timeScale = 1f;
        UIManager.Instance.ChangeMenu(MenuType.SettingsMenu, MenuType.MainMenu);
    }

    private void OnAchievementClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        Time.timeScale = 1f;
        UIManager.Instance.ChangeMenu(MenuType.AchievementMenu);
    }

    private void OnQuitClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        Application.Quit();
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
        settingsButton?.transform.DOScale(0f, 0f);
        achievementButton?.transform.DOScale(0f, 0f);
        quitButton?.transform.DOScale(0f, 0f);

        playButton?.transform.DOScale(1f, buttonAnimDuration).SetEase(Ease.OutBack).SetDelay(0.1f);
        settingsButton?.transform.DOScale(1f, buttonAnimDuration).SetEase(Ease.OutBack).SetDelay(0.2f);
        achievementButton?.transform.DOScale(1f, buttonAnimDuration).SetEase(Ease.OutBack).SetDelay(0.3f);
        quitButton?.transform.DOScale(1f, buttonAnimDuration).SetEase(Ease.OutBack).SetDelay(0.4f);
    }

    private void OnDestroy()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.Unsubscribe(GameEvent.GameStarted, OnGameStarted);
        }
    }
}
