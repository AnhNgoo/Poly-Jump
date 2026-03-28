using UnityEngine;

public class SettingMenuUI : MenuBase
{
    public override MenuType menuType => MenuType.SettingsMenu;

    [Header("Controls")]
    [SerializeField] private UnityEngine.UI.Slider musicSlider;
    [SerializeField] private UnityEngine.UI.Slider vfxSlider;
    [SerializeField] private UnityEngine.UI.Button backButton;

    private MenuType _returnMenu = MenuType.MainMenu;
    private bool _ignoreSliderEvent;

    protected override void LoadComponent()
    {
        if (musicSlider == null) musicSlider = transform.Find("ContentPanel/MusicSlider")?.GetComponent<UnityEngine.UI.Slider>();
        if (vfxSlider == null) vfxSlider = transform.Find("ContentPanel/VFXSlider")?.GetComponent<UnityEngine.UI.Slider>();
        if (backButton == null) backButton = transform.Find("ContentPanel/BackButton")?.GetComponent<UnityEngine.UI.Button>();
        if (backButton == null) backButton = transform.Find("ButtonPanel/BackButton")?.GetComponent<UnityEngine.UI.Button>();
    }

    protected override void LoadComponentRuntime()
    {
        backButton?.onClick.AddListener(OnBackClicked);
        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        if (vfxSlider != null)
            vfxSlider.onValueChanged.AddListener(OnVfxVolumeChanged);
    }

    public override void Open(object data = null)
    {
        base.Open(data);
        _returnMenu = data is MenuType menuType ? menuType : UIManager.Instance.PreviousMenuType;
        if (_returnMenu == MenuType.None)
            _returnMenu = MenuType.MainMenu;

        SyncSliders();
    }

    private void SyncSliders()
    {
        if (AudioManager.Instance == null) return;

        _ignoreSliderEvent = true;
        if (musicSlider != null)
            musicSlider.value = AudioManager.Instance.MusicVolume;
        if (vfxSlider != null)
            vfxSlider.value = AudioManager.Instance.VfxVolume;
        _ignoreSliderEvent = false;
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (_ignoreSliderEvent) return;
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(value);
    }

    private void OnVfxVolumeChanged(float value)
    {
        if (_ignoreSliderEvent) return;
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetVfxVolume(value);
    }

    private void OnBackClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        if (_returnMenu == MenuType.PauseMenu)
            Time.timeScale = 0f;
        else
            Time.timeScale = 1f;

        UIManager.Instance.ChangeMenu(_returnMenu);
    }
}
