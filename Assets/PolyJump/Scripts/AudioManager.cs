using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PolyJump.Scripts
{
    [DisallowMultipleComponent]
    /// <summary>
    /// Điều phối toàn bộ âm thanh nền, hiệu ứng, nút toggle và liên kết sự kiện âm thanh trong game.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        public AudioSource musicSource;
        public AudioSource sfxSource;

        [Header("Audio Clips")]
        public AudioClip backgroundMusic;
        public AudioClip buttonClickClip;
        public AudioClip correctAnswerClip;
        public AudioClip wrongAnswerClip;
        public AudioClip gameOverClip;
        public AudioClip jumpClip;

        [Header("Audio State")]
        [SerializeField]
        private bool musicEnabled = true;
        [SerializeField]
        private bool sfxEnabled = true;

        [Header("Toggle Sprites Asset (Assign Here)")]
        public AudioToggleSpriteSet toggleSpriteSet;

        [Header("Toggle Sprite Override (Optional)")]
        public Sprite musicOnSprite;
        public Sprite musicOffSprite;
        public Sprite sfxOnSprite;
        public Sprite sfxOffSprite;

        [Header("Auto Setup")]
        public bool autoHookButtons = true;
        public float buttonRescanInterval = 0.8f;
        public bool autoHookPlayerJump = true;
        public string gameOverPanelName = "Panel_GameOver";

        private GameObject _gameOverPanel;
        private bool _gameOverPanelWasActive;
        private float _nextButtonScanAt;
        private float _nextPanelLookupAt;

        private QuizManager _boundQuizManager;
        private readonly Dictionary<Button, UnityEngine.Events.UnityAction> _quizButtonListeners = new Dictionary<Button, UnityEngine.Events.UnityAction>();

        private const string ToggleMusicButtonObjectName = "Btn_ToggleMusic";
        private const string ToggleSfxButtonObjectName = "Btn_ToggleSfx";

        private Button _toggleMusicButton;
        private Button _toggleSfxButton;
        private float _nextToggleButtonsRefreshAt;

        private Sprite _fallbackMusicOnSprite;
        private Sprite _fallbackMusicOffSprite;
        private Sprite _fallbackSfxOnSprite;
        private Sprite _fallbackSfxOffSprite;

        public bool IsMusicEnabled => musicEnabled;
        public bool IsSfxEnabled => sfxEnabled;

        /// <summary>
        /// Khởi tạo tham chiếu, trạng thái ban đầu và các cấu hình cần thiết khi đối tượng được tạo.
        /// </summary>
        private void Awake()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (Instance == this)
            {
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureAudioSources();
            ApplyAudioState();
        }

        /// <summary>
        /// Đăng ký sự kiện và kích hoạt các liên kết runtime khi đối tượng được bật.
        /// </summary>
        private void OnEnable()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        /// <summary>
        /// Thiết lập dữ liệu và liên kết cần dùng ngay trước khi vòng lặp gameplay bắt đầu.
        /// </summary>
        private void Start()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            RefreshAllHooks(resetPanelState: true);
            PlayBackgroundMusic();
        }

        /// <summary>
        /// Cập nhật logic theo từng khung hình để phản hồi trạng thái hiện tại của game.
        /// </summary>
        private void Update()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (autoHookButtons && Time.unscaledTime >= _nextButtonScanAt)
            {
                HookAllButtons();
                _nextButtonScanAt = Time.unscaledTime + Mathf.Max(0.2f, buttonRescanInterval);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (autoHookPlayerJump)
            {
                EnsurePlayerJumpHooks();
            }

            PollGameOverPanel();

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (Time.unscaledTime >= _nextToggleButtonsRefreshAt)
            {
                BindAudioToggleButtons();
                RefreshToggleButtonsVisual();
                _nextToggleButtonsRefreshAt = Time.unscaledTime + 0.35f;
            }
        }

        /// <summary>
        /// Gỡ đăng ký sự kiện và giải phóng liên kết tạm khi đối tượng bị tắt.
        /// </summary>
        private void OnDisable()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            SceneManager.sceneLoaded -= OnSceneLoaded;
            UnbindQuizButtons();
            UnbindAudioToggleButtons();
        }

        /// <summary>
        /// Dọn dẹp tài nguyên và hủy các ràng buộc còn tồn tại trước khi đối tượng bị hủy.
        /// </summary>
        private void OnDestroy()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (Instance == this)
            {
                Instance = null;
            }

            UnbindQuizButtons();
            UnbindAudioToggleButtons();
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Play Background Music theo ngữ cảnh sử dụng của script.
        /// </summary>
        public void PlayBackgroundMusic()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (!musicEnabled)
            {
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (musicSource == null || backgroundMusic == null)
            {
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (musicSource.clip != backgroundMusic)
            {
                musicSource.clip = backgroundMusic;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Play Button Click theo ngữ cảnh sử dụng của script.
        /// </summary>
        public void PlayButtonClick()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            PlaySfx(buttonClickClip);
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Play Correct Answer theo ngữ cảnh sử dụng của script.
        /// </summary>
        public void PlayCorrectAnswer()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            PlaySfx(correctAnswerClip);
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Play Wrong Answer theo ngữ cảnh sử dụng của script.
        /// </summary>
        public void PlayWrongAnswer()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            PlaySfx(wrongAnswerClip);
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Play Game Over theo ngữ cảnh sử dụng của script.
        /// </summary>
        public void PlayGameOver()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            PlaySfx(gameOverClip);
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Play Jump theo ngữ cảnh sử dụng của script.
        /// </summary>
        public void PlayJump()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            PlaySfx(jumpClip);
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Toggle Music theo ngữ cảnh sử dụng của script.
        /// </summary>
        public void ToggleMusic()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            SetMusicEnabled(!musicEnabled);
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Toggle Sfx theo ngữ cảnh sử dụng của script.
        /// </summary>
        public void ToggleSfx()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            SetSfxEnabled(!sfxEnabled);
        }

        /// <summary>
        /// Thiết lập Music Enabled phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        public void SetMusicEnabled(bool enabled)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            musicEnabled = enabled;

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (musicSource == null)
            {
                return;
            }

            musicSource.mute = !musicEnabled;

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!musicEnabled)
            {
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (musicSource.isPlaying)
                {
                    musicSource.Pause();
                }

                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (backgroundMusic == null)
            {
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (musicSource.clip != backgroundMusic)
            {
                musicSource.clip = backgroundMusic;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }

            RefreshToggleButtonsVisual();
        }

        /// <summary>
        /// Thiết lập Sfx Enabled phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        public void SetSfxEnabled(bool enabled)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            sfxEnabled = enabled;

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (sfxSource == null)
            {
                return;
            }

            sfxSource.mute = !sfxEnabled;
            RefreshToggleButtonsVisual();
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Play Sfx theo ngữ cảnh sử dụng của script.
        /// </summary>
        private void PlaySfx(AudioClip clip)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (!sfxEnabled || clip == null || sfxSource == null)
            {
                return;
            }

            sfxSource.PlayOneShot(clip);
        }

        /// <summary>
        /// Xử lý callback sự kiện hệ thống hoặc gameplay phát sinh trong vòng đời đối tượng.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            RefreshAllHooks(resetPanelState: true);
            ApplyAudioState();
            PlayBackgroundMusic();
        }

        /// <summary>
        /// Làm mới All Hooks phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void RefreshAllHooks(bool resetPanelState)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (autoHookButtons)
            {
                HookAllButtons();
                _nextButtonScanAt = Time.unscaledTime + Mathf.Max(0.2f, buttonRescanInterval);
            }

            BindQuizButtons();

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (autoHookPlayerJump)
            {
                EnsurePlayerJumpHooks();
            }

            ResolveGameOverPanel(resetPanelState);
            BindAudioToggleButtons();
            RefreshToggleButtonsVisual();
        }

        /// <summary>
        /// Gắn Audio Toggle Buttons phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void BindAudioToggleButtons()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            Button musicButton = FindButtonByName(ToggleMusicButtonObjectName);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (musicButton != _toggleMusicButton)
            {
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (_toggleMusicButton != null)
                {
                    _toggleMusicButton.onClick.RemoveListener(OnMusicTogglePressed);
                }

                _toggleMusicButton = musicButton;

                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (_toggleMusicButton != null)
                {
                    RemoveLegacyToggleRelay(_toggleMusicButton);
                    _toggleMusicButton.onClick.RemoveListener(OnMusicTogglePressed);
                    _toggleMusicButton.onClick.AddListener(OnMusicTogglePressed);
                }
            }

            Button sfxButton = FindButtonByName(ToggleSfxButtonObjectName);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (sfxButton != _toggleSfxButton)
            {
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (_toggleSfxButton != null)
                {
                    _toggleSfxButton.onClick.RemoveListener(OnSfxTogglePressed);
                }

                _toggleSfxButton = sfxButton;

                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (_toggleSfxButton != null)
                {
                    RemoveLegacyToggleRelay(_toggleSfxButton);
                    _toggleSfxButton.onClick.RemoveListener(OnSfxTogglePressed);
                    _toggleSfxButton.onClick.AddListener(OnSfxTogglePressed);
                }
            }
        }

        /// <summary>
        /// Gỡ liên kết Audio Toggle Buttons phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void UnbindAudioToggleButtons()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_toggleMusicButton != null)
            {
                _toggleMusicButton.onClick.RemoveListener(OnMusicTogglePressed);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_toggleSfxButton != null)
            {
                _toggleSfxButton.onClick.RemoveListener(OnSfxTogglePressed);
            }
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Remove Legacy Toggle Relay theo ngữ cảnh sử dụng của script.
        /// </summary>
        private static void RemoveLegacyToggleRelay(Button button)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (button == null)
            {
                return;
            }

            AudioStartToggleRelay relay = button.GetComponent<AudioStartToggleRelay>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (relay != null)
            {
                UnityEngine.Object.Destroy(relay);
            }
        }

        /// <summary>
        /// Xử lý sự kiện khi người dùng nhấn nút/chức năng tương ứng trên giao diện.
        /// </summary>
        private void OnMusicTogglePressed()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            ToggleMusic();
        }

        /// <summary>
        /// Xử lý sự kiện khi người dùng nhấn nút/chức năng tương ứng trên giao diện.
        /// </summary>
        private void OnSfxTogglePressed()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            ToggleSfx();
        }

        /// <summary>
        /// Làm mới Toggle Buttons Visual phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void RefreshToggleButtonsVisual()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            ApplyToggleButtonVisual(_toggleMusicButton, AudioToggleType.Music, musicEnabled);
            ApplyToggleButtonVisual(_toggleSfxButton, AudioToggleType.Sfx, sfxEnabled);
        }

        /// <summary>
        /// Áp dụng Toggle Button Visual phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void ApplyToggleButtonVisual(Button button, AudioToggleType toggleType, bool enabled)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (button == null)
            {
                return;
            }

            Text text = button.GetComponentInChildren<Text>(true);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (text != null)
            {
                text.text = string.Empty;
                text.enabled = false;
            }

            Image image = button.GetComponent<Image>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (image != null)
            {
                image.sprite = ResolveToggleSprite(toggleType, enabled);
                image.color = Color.white;
                image.type = Image.Type.Sliced;
            }
        }

        /// <summary>
        /// Xác định Toggle Sprite phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private Sprite ResolveToggleSprite(AudioToggleType toggleType, bool enabled)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            Sprite fromSet = GetSpriteFromSet(toggleType, enabled);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (fromSet != null)
            {
                return fromSet;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (toggleType == AudioToggleType.Music)
            {
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (enabled && musicOnSprite != null)
                {
                    return musicOnSprite;
                }

                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (!enabled && musicOffSprite != null)
                {
                    return musicOffSprite;
                }
            }
            else
            {
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (enabled && sfxOnSprite != null)
                {
                    return sfxOnSprite;
                }

                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (!enabled && sfxOffSprite != null)
                {
                    return sfxOffSprite;
                }
            }

            return GetFallbackToggleSprite(toggleType, enabled);
        }

        /// <summary>
        /// Lấy Sprite From Set phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private Sprite GetSpriteFromSet(AudioToggleType toggleType, bool enabled)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (toggleSpriteSet == null)
            {
                return null;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (toggleType == AudioToggleType.Music)
            {
                return enabled ? toggleSpriteSet.musicOnSprite : toggleSpriteSet.musicOffSprite;
            }

            return enabled ? toggleSpriteSet.sfxOnSprite : toggleSpriteSet.sfxOffSprite;
        }

        /// <summary>
        /// Lấy Fallback Toggle Sprite phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private Sprite GetFallbackToggleSprite(AudioToggleType toggleType, bool enabled)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (toggleType == AudioToggleType.Music)
            {
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (enabled)
                {
                    if (_fallbackMusicOnSprite == null)
                    {
                        _fallbackMusicOnSprite = CreateFallbackToggleSprite(toggleType, true);
                    }

                    return _fallbackMusicOnSprite;
                }

                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (_fallbackMusicOffSprite == null)
                {
                    _fallbackMusicOffSprite = CreateFallbackToggleSprite(toggleType, false);
                }

                return _fallbackMusicOffSprite;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (enabled)
            {
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (_fallbackSfxOnSprite == null)
                {
                    _fallbackSfxOnSprite = CreateFallbackToggleSprite(toggleType, true);
                }

                return _fallbackSfxOnSprite;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_fallbackSfxOffSprite == null)
            {
                _fallbackSfxOffSprite = CreateFallbackToggleSprite(toggleType, false);
            }

            return _fallbackSfxOffSprite;
        }

        /// <summary>
        /// Tạo Fallback Toggle Sprite phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static Sprite CreateFallbackToggleSprite(AudioToggleType toggleType, bool enabled)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color bg = toggleType == AudioToggleType.Music
                ? (enabled ? new Color32(0x2B, 0x93, 0xFF, 0xFF) : new Color32(0x6F, 0x83, 0x99, 0xFF))
                : (enabled ? new Color32(0xF3, 0x70, 0x21, 0xFF) : new Color32(0x7F, 0x8D, 0x9D, 0xFF));

            Color border = new Color32(0x12, 0x1A, 0x2F, 0xFF);
            Color icon = Color.white;

            // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
            for (int y = 0; y < size; y++)
            {
                // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
                for (int x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, bg);
                }
            }

            // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
            for (int i = 0; i < size; i++)
            {
                tex.SetPixel(i, 0, border);
                tex.SetPixel(i, 1, border);
                tex.SetPixel(i, size - 1, border);
                tex.SetPixel(i, size - 2, border);
                tex.SetPixel(0, i, border);
                tex.SetPixel(1, i, border);
                tex.SetPixel(size - 1, i, border);
                tex.SetPixel(size - 2, i, border);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (toggleType == AudioToggleType.Music)
            {
                DrawRect(tex, 26, 20, 5, 24, icon);
                DrawRect(tex, 30, 38, 14, 4, icon);
                DrawCircle(tex, 24, 18, 7, icon);
            }
            else
            {
                DrawRect(tex, 14, 24, 8, 16, icon);
                // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
                for (int x = 22; x <= 38; x++)
                {
                    int half = (x - 22) / 2 + 2;
                    DrawRect(tex, x, 32 - half, 1, half * 2, icon);
                }
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (enabled)
            {
                DrawCircle(tex, 52, 52, 6, new Color32(0x34, 0xC7, 0x59, 0xFF));
            }
            else
            {
                DrawThickLine(tex, 14, 14, 50, 50, 4, new Color32(0xFF, 0x45, 0x3A, 0xFF));
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Draw Rect theo ngữ cảnh sử dụng của script.
        /// </summary>
        private static void DrawRect(Texture2D tex, int x, int y, int w, int h, Color color)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (tex == null)
            {
                return;
            }

            // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
            for (int py = y; py < y + h; py++)
            {
                // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
                for (int px = x; px < x + w; px++)
                {
                    if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                    {
                        tex.SetPixel(px, py, color);
                    }
                }
            }
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Draw Circle theo ngữ cảnh sử dụng của script.
        /// </summary>
        private static void DrawCircle(Texture2D tex, int cx, int cy, int r, Color color)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (tex == null)
            {
                return;
            }

            int rr = r * r;
            // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
            for (int y = -r; y <= r; y++)
            {
                // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
                for (int x = -r; x <= r; x++)
                {
                    if (x * x + y * y > rr)
                    {
                        continue;
                    }

                    int px = cx + x;
                    int py = cy + y;
                    if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                    {
                        tex.SetPixel(px, py, color);
                    }
                }
            }
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Draw Thick Line theo ngữ cảnh sử dụng của script.
        /// </summary>
        private static void DrawThickLine(Texture2D tex, int x0, int y0, int x1, int y1, int thickness, Color color)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (tex == null)
            {
                return;
            }

            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            // Khối lặp điều kiện: tiếp tục xử lý cho đến khi đạt điều kiện dừng.
            while (true)
            {
                DrawCircle(tex, x0, y0, Mathf.Max(1, thickness), color);

                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (x0 == x1 && y0 == y1)
                {
                    break;
                }

                int e2 = err * 2;
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }

                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        /// <summary>
        /// Tìm Button By Name phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static Button FindButtonByName(string objectName)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            Button[] allButtons = FindObjectsOfType<Button>(true);
            // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
            for (int i = 0; i < allButtons.Length; i++)
            {
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (allButtons[i] != null && allButtons[i].name == objectName)
                {
                    return allButtons[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Đăng ký hook All Buttons phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void HookAllButtons()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            Button[] allButtons = FindObjectsOfType<Button>(true);
            // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
            for (int i = 0; i < allButtons.Length; i++)
            {
                Button button = allButtons[i];
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (button == null)
                {
                    continue;
                }

                AudioButtonClickRelay relay = button.GetComponent<AudioButtonClickRelay>();
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (relay == null)
                {
                    relay = button.gameObject.AddComponent<AudioButtonClickRelay>();
                }

                relay.SetManager(this);
            }
        }

        /// <summary>
        /// Gắn Quiz Buttons phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void BindQuizButtons()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            QuizManager quiz = FindObjectOfType<QuizManager>(true);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (quiz == null)
            {
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_boundQuizManager != quiz)
            {
                UnbindQuizButtons();
                _boundQuizManager = quiz;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (quiz.answerButtons == null)
            {
                return;
            }

            // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
            for (int i = 0; i < quiz.answerButtons.Length; i++)
            {
                Button answerButton = quiz.answerButtons[i];
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (answerButton == null || _quizButtonListeners.ContainsKey(answerButton))
                {
                    continue;
                }

                UnityEngine.Events.UnityAction action = () => StartCoroutine(PlayQuizAnswerSfxDeferred(quiz));
                answerButton.onClick.AddListener(action);
                _quizButtonListeners[answerButton] = action;
            }
        }

        /// <summary>
        /// Gỡ liên kết Quiz Buttons phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void UnbindQuizButtons()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            foreach (KeyValuePair<Button, UnityEngine.Events.UnityAction> pair in _quizButtonListeners)
            {
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (pair.Key != null)
                {
                    pair.Key.onClick.RemoveListener(pair.Value);
                }
            }

            _quizButtonListeners.Clear();
            _boundQuizManager = null;
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Play Quiz Answer Sfx Deferred theo ngữ cảnh sử dụng của script.
        /// </summary>
        private IEnumerator PlayQuizAnswerSfxDeferred(QuizManager quiz)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            yield return null;

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (quiz == null || quiz.questionText == null)
            {
                yield break;
            }

            string questionText = quiz.questionText.text ?? string.Empty;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (ContainsIgnoreCase(questionText, quiz.correctHeadlineText) || ContainsIgnoreCase(questionText, quiz.correctFeedbackText))
            {
                PlayCorrectAnswer();
                yield break;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (ContainsIgnoreCase(questionText, quiz.wrongHeadlineText) || ContainsIgnoreCase(questionText, quiz.wrongFeedbackText))
            {
                PlayWrongAnswer();
            }
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Contains Ignore Case theo ngữ cảnh sử dụng của script.
        /// </summary>
        private static bool ContainsIgnoreCase(string source, string value)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value))
            {
                return false;
            }

            return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Đảm bảo Player Jump Hooks phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void EnsurePlayerJumpHooks()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            PlayerController[] players = FindObjectsOfType<PlayerController>(true);
            // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
            for (int i = 0; i < players.Length; i++)
            {
                PlayerController player = players[i];
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (player == null)
                {
                    continue;
                }

                AudioPlayerJumpRelay relay = player.GetComponent<AudioPlayerJumpRelay>();
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (relay == null)
                {
                    relay = player.gameObject.AddComponent<AudioPlayerJumpRelay>();
                }

                relay.SetManager(this);
            }
        }

        /// <summary>
        /// Xác định Game Over Panel phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void ResolveGameOverPanel(bool resetPanelState)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            GameObject panel = null;

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (GameManager.Instance != null && GameManager.Instance.panelGameOver != null)
            {
                panel = GameManager.Instance.panelGameOver;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (panel == null)
            {
                panel = GameObject.Find(gameOverPanelName);
            }

            _gameOverPanel = panel;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (resetPanelState)
            {
                _gameOverPanelWasActive = _gameOverPanel != null && _gameOverPanel.activeInHierarchy;
            }

            _nextPanelLookupAt = Time.unscaledTime + 1f;
        }

        /// <summary>
        /// Thăm dò Game Over Panel phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void PollGameOverPanel()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_gameOverPanel == null && Time.unscaledTime >= _nextPanelLookupAt)
            {
                ResolveGameOverPanel(resetPanelState: true);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_gameOverPanel == null)
            {
                return;
            }

            bool isActive = _gameOverPanel.activeInHierarchy;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (isActive && !_gameOverPanelWasActive)
            {
                PlayGameOver();
            }

            _gameOverPanelWasActive = isActive;
        }

        /// <summary>
        /// Đảm bảo Audio Sources phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void EnsureAudioSources()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            AudioSource[] sources = GetComponents<AudioSource>();
            bool addedMusicSource = false;
            bool addedSfxSource = false;

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (musicSource == null)
            {
                musicSource = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();
                addedMusicSource = sources.Length == 0;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (sfxSource == null || sfxSource == musicSource)
            {
                AudioSource candidate = null;
                AudioSource[] allSources = GetComponents<AudioSource>();
                // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
                for (int i = 0; i < allSources.Length; i++)
                {
                    if (allSources[i] != null && allSources[i] != musicSource)
                    {
                        candidate = allSources[i];
                        break;
                    }
                }

                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (candidate == null)
                {
                    candidate = gameObject.AddComponent<AudioSource>();
                    addedSfxSource = true;
                }

                sfxSource = candidate;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (addedMusicSource)
            {
                ConfigureMusicSourceDefaults(musicSource);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (addedSfxSource)
            {
                ConfigureSfxSourceDefaults(sfxSource);
            }
        }

        /// <summary>
        /// Cấu hình Music Source Defaults phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void ConfigureMusicSourceDefaults(AudioSource source)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (source == null)
            {
                return;
            }

            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0.7f;
        }

        /// <summary>
        /// Cấu hình Sfx Source Defaults phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void ConfigureSfxSourceDefaults(AudioSource source)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (source == null)
            {
                return;
            }

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.volume = 1f;
        }

        /// <summary>
        /// Áp dụng Audio State phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void ApplyAudioState()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            SetSfxEnabled(sfxEnabled);
            SetMusicEnabled(musicEnabled);
        }
    }

    public enum AudioToggleType
    {
        Music,
        Sfx
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    /// <summary>
    /// Mô tả vai trò chính của lớp AudioButtonClickRelay trong hệ thống PolyJump.
    /// </summary>
    public class AudioButtonClickRelay : MonoBehaviour
    {
        private AudioManager _manager;
        private Button _button;

        /// <summary>
        /// Khởi tạo tham chiếu, trạng thái ban đầu và các cấu hình cần thiết khi đối tượng được tạo.
        /// </summary>
        private void Awake()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            _button = GetComponent<Button>();
        }

        /// <summary>
        /// Đăng ký sự kiện và kích hoạt các liên kết runtime khi đối tượng được bật.
        /// </summary>
        private void OnEnable()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_button == null)
            {
                return;
            }

            _button.onClick.RemoveListener(OnButtonClicked);
            _button.onClick.AddListener(OnButtonClicked);
        }

        /// <summary>
        /// Gỡ đăng ký sự kiện và giải phóng liên kết tạm khi đối tượng bị tắt.
        /// </summary>
        private void OnDisable()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnButtonClicked);
            }
        }

        /// <summary>
        /// Thiết lập Manager phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        public void SetManager(AudioManager manager)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            _manager = manager;
        }

        /// <summary>
        /// Xử lý callback sự kiện hệ thống hoặc gameplay phát sinh trong vòng đời đối tượng.
        /// </summary>
        private void OnButtonClicked()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            AudioManager manager = _manager != null ? _manager : AudioManager.Instance;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (manager != null)
            {
                manager.PlayButtonClick();
            }
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    /// <summary>
    /// Mô tả vai trò chính của lớp AudioStartToggleRelay trong hệ thống PolyJump.
    /// </summary>
    public class AudioStartToggleRelay : MonoBehaviour
    {
        public AudioToggleType toggleType = AudioToggleType.Music;

        private Button _button;
        private Text _text;
        private Image _image;
        private float _nextRefreshAt;

        private static readonly Color EnabledColor = new Color32(0x12, 0x1A, 0x2F, 0xFF);
        private static readonly Color DisabledColor = new Color32(0x5D, 0x6B, 0x85, 0xFF);

        /// <summary>
        /// Khởi tạo tham chiếu, trạng thái ban đầu và các cấu hình cần thiết khi đối tượng được tạo.
        /// </summary>
        private void Awake()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            _button = GetComponent<Button>();
            _text = GetComponentInChildren<Text>(true);
            _image = GetComponent<Image>();
        }

        /// <summary>
        /// Đăng ký sự kiện và kích hoạt các liên kết runtime khi đối tượng được bật.
        /// </summary>
        private void OnEnable()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnTogglePressed);
                _button.onClick.AddListener(OnTogglePressed);
            }

            RefreshVisual();
        }

        /// <summary>
        /// Gỡ đăng ký sự kiện và giải phóng liên kết tạm khi đối tượng bị tắt.
        /// </summary>
        private void OnDisable()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnTogglePressed);
            }
        }

        /// <summary>
        /// Cập nhật logic theo từng khung hình để phản hồi trạng thái hiện tại của game.
        /// </summary>
        private void Update()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (Time.unscaledTime < _nextRefreshAt)
            {
                return;
            }

            _nextRefreshAt = Time.unscaledTime + 0.35f;
            RefreshVisual();
        }

        /// <summary>
        /// Xử lý sự kiện khi người dùng nhấn nút/chức năng tương ứng trên giao diện.
        /// </summary>
        private void OnTogglePressed()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            AudioManager manager = AudioManager.Instance;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (manager == null)
            {
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (toggleType == AudioToggleType.Music)
            {
                manager.ToggleMusic();
            }
            else
            {
                manager.ToggleSfx();
            }

            RefreshVisual();
        }

        /// <summary>
        /// Làm mới Visual phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void RefreshVisual()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            AudioManager manager = AudioManager.Instance;

            bool isEnabled = true;
            string prefix = toggleType == AudioToggleType.Music ? "M" : "S";

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (manager != null)
            {
                isEnabled = toggleType == AudioToggleType.Music ? manager.IsMusicEnabled : manager.IsSfxEnabled;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_text == null)
            {
                _text = GetComponentInChildren<Text>(true);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_text != null)
            {
                _text.text = prefix + (isEnabled ? " ON" : " OFF");
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_image == null)
            {
                _image = GetComponent<Image>();
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_image != null)
            {
                _image.color = isEnabled ? EnabledColor : DisabledColor;
            }
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    /// <summary>
    /// Mô tả vai trò chính của lớp AudioPlayerJumpRelay trong hệ thống PolyJump.
    /// </summary>
    public class AudioPlayerJumpRelay : MonoBehaviour
    {
        private AudioManager _manager;
        private PlayerController _player;
        private Rigidbody2D _rb;
        private float _lastKnownVerticalVelocity;
        private int _lastPlatformObjectId = int.MinValue;
        private float _lastJumpSfxAt = -10f;

        private const float MinRepeatSeconds = 0.1f;

        /// <summary>
        /// Khởi tạo tham chiếu, trạng thái ban đầu và các cấu hình cần thiết khi đối tượng được tạo.
        /// </summary>
        private void Awake()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            _player = GetComponent<PlayerController>();
            _rb = _player != null ? _player.rb : null;

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_rb == null)
            {
                _rb = GetComponent<Rigidbody2D>();
            }

            _lastKnownVerticalVelocity = _rb != null ? _rb.velocity.y : 0f;
        }

        /// <summary>
        /// Cập nhật logic vật lý theo nhịp cố định để đảm bảo chuyển động ổn định.
        /// </summary>
        private void FixedUpdate()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_rb == null)
            {
                _rb = _player != null && _player.rb != null ? _player.rb : GetComponent<Rigidbody2D>();
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_rb != null)
            {
                _lastKnownVerticalVelocity = _rb.velocity.y;
            }
        }

        /// <summary>
        /// Thiết lập Manager phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        public void SetManager(AudioManager manager)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            _manager = manager;
        }

        /// <summary>
        /// Xử lý callback sự kiện hệ thống hoặc gameplay phát sinh trong vòng đời đối tượng.
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (other == null)
            {
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!other.CompareTag("Platform") && !other.CompareTag("QuizPlatform") && !other.CompareTag("Ground"))
            {
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!IsDescendingOrAlmostStill())
            {
                return;
            }

            int platformObjectId = other.gameObject.GetInstanceID();
            float now = Time.time;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (platformObjectId == _lastPlatformObjectId && now - _lastJumpSfxAt < MinRepeatSeconds)
            {
                return;
            }

            _lastPlatformObjectId = platformObjectId;
            _lastJumpSfxAt = now;

            AudioManager manager = _manager != null ? _manager : AudioManager.Instance;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (manager != null)
            {
                manager.PlayJump();
            }
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Is Descending Or Almost Still theo ngữ cảnh sử dụng của script.
        /// </summary>
        private bool IsDescendingOrAlmostStill()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_rb == null)
            {
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (_player != null && _player.rb != null)
                {
                    _rb = _player.rb;
                }
                else
                {
                    _rb = GetComponent<Rigidbody2D>();
                }
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_rb == null)
            {
                return true;
            }

            // current velocity can already be flipped upward by PlayerController before this relay callback,
            // so we also trust the last cached pre-collision velocity from FixedUpdate.
            return _rb.velocity.y <= 0.05f || _lastKnownVerticalVelocity <= 0.05f;
        }
    }
}
