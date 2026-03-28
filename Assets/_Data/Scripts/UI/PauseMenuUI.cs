using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuUI : MenuBase
{
    public override MenuType menuType => MenuType.PauseMenu;

    [Header("Buttons")]
    [SerializeField] private UnityEngine.UI.Button mainMenuButton;
    [SerializeField] private UnityEngine.UI.Button resumeButton;
    [SerializeField] private UnityEngine.UI.Button settingsButton;

    protected override void LoadComponent()
    {
        if (mainMenuButton == null) mainMenuButton = transform.Find("ButtonPanel/MainMenuButton")?.GetComponent<UnityEngine.UI.Button>();
        if (resumeButton == null) resumeButton = transform.Find("ButtonPanel/ResumeButton")?.GetComponent<UnityEngine.UI.Button>();
        if (settingsButton == null) settingsButton = transform.Find("ButtonPanel/SettingsButton")?.GetComponent<UnityEngine.UI.Button>();
    }

    protected override void LoadComponentRuntime()
    {
        mainMenuButton?.onClick.AddListener(OnMainMenuClicked);
        resumeButton?.onClick.AddListener(OnResumeClicked);
        settingsButton?.onClick.AddListener(OnSettingsClicked);
    }

    public override void Open(object data = null)
    {
        base.Open(data);
        Time.timeScale = 0f;
    }

    private void OnMainMenuClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        Time.timeScale = 1f;
        PersistentData.Instance.ResetSession();
        GameManager.PendingRestart = false;
        GameManager.PendingMajor = null;
        PlayerPrefs.DeleteKey("PendingRestart");
        PlayerPrefs.DeleteKey("PendingMajor");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnResumeClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        Time.timeScale = 1f;
        UIManager.Instance.ChangeMenu(MenuType.GameplayHUD);
    }

    private void OnSettingsClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        UIManager.Instance.ChangeMenu(MenuType.SettingsMenu, MenuType.PauseMenu);
    }
}
