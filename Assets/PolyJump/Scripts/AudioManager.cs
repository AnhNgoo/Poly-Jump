using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PolyJump.Scripts
{
    [DisallowMultipleComponent]
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

        private void Awake()
        {
            if (Instance == this)
            {
                return;
            }

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureAudioSources();
            ApplyAudioState();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            RefreshAllHooks(resetPanelState: true);
            PlayBackgroundMusic();
        }

        private void Update()
        {
            if (autoHookButtons && Time.unscaledTime >= _nextButtonScanAt)
            {
                HookAllButtons();
                _nextButtonScanAt = Time.unscaledTime + Mathf.Max(0.2f, buttonRescanInterval);
            }

            if (autoHookPlayerJump)
            {
                EnsurePlayerJumpHooks();
            }

            PollGameOverPanel();

            if (Time.unscaledTime >= _nextToggleButtonsRefreshAt)
            {
                BindAudioToggleButtons();
                RefreshToggleButtonsVisual();
                _nextToggleButtonsRefreshAt = Time.unscaledTime + 0.35f;
            }
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            UnbindQuizButtons();
            UnbindAudioToggleButtons();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            UnbindQuizButtons();
            UnbindAudioToggleButtons();
        }

        public void PlayBackgroundMusic()
        {
            if (!musicEnabled)
            {
                return;
            }

            if (musicSource == null || backgroundMusic == null)
            {
                return;
            }

            if (musicSource.clip != backgroundMusic)
            {
                musicSource.clip = backgroundMusic;
            }

            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }
        }

        public void PlayButtonClick()
        {
            PlaySfx(buttonClickClip);
        }

        public void PlayCorrectAnswer()
        {
            PlaySfx(correctAnswerClip);
        }

        public void PlayWrongAnswer()
        {
            PlaySfx(wrongAnswerClip);
        }

        public void PlayGameOver()
        {
            PlaySfx(gameOverClip);
        }

        public void PlayJump()
        {
            PlaySfx(jumpClip);
        }

        public void ToggleMusic()
        {
            SetMusicEnabled(!musicEnabled);
        }

        public void ToggleSfx()
        {
            SetSfxEnabled(!sfxEnabled);
        }

        public void SetMusicEnabled(bool enabled)
        {
            musicEnabled = enabled;

            if (musicSource == null)
            {
                return;
            }

            musicSource.mute = !musicEnabled;

            if (!musicEnabled)
            {
                if (musicSource.isPlaying)
                {
                    musicSource.Pause();
                }

                return;
            }

            if (backgroundMusic == null)
            {
                return;
            }

            if (musicSource.clip != backgroundMusic)
            {
                musicSource.clip = backgroundMusic;
            }

            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }

            RefreshToggleButtonsVisual();
        }

        public void SetSfxEnabled(bool enabled)
        {
            sfxEnabled = enabled;

            if (sfxSource == null)
            {
                return;
            }

            sfxSource.mute = !sfxEnabled;
            RefreshToggleButtonsVisual();
        }

        private void PlaySfx(AudioClip clip)
        {
            if (!sfxEnabled || clip == null || sfxSource == null)
            {
                return;
            }

            sfxSource.PlayOneShot(clip);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RefreshAllHooks(resetPanelState: true);
            ApplyAudioState();
            PlayBackgroundMusic();
        }

        private void RefreshAllHooks(bool resetPanelState)
        {
            if (autoHookButtons)
            {
                HookAllButtons();
                _nextButtonScanAt = Time.unscaledTime + Mathf.Max(0.2f, buttonRescanInterval);
            }

            BindQuizButtons();

            if (autoHookPlayerJump)
            {
                EnsurePlayerJumpHooks();
            }

            ResolveGameOverPanel(resetPanelState);
            BindAudioToggleButtons();
            RefreshToggleButtonsVisual();
        }

        private void BindAudioToggleButtons()
        {
            Button musicButton = FindButtonByName(ToggleMusicButtonObjectName);
            if (musicButton != _toggleMusicButton)
            {
                if (_toggleMusicButton != null)
                {
                    _toggleMusicButton.onClick.RemoveListener(OnMusicTogglePressed);
                }

                _toggleMusicButton = musicButton;

                if (_toggleMusicButton != null)
                {
                    RemoveLegacyToggleRelay(_toggleMusicButton);
                    _toggleMusicButton.onClick.RemoveListener(OnMusicTogglePressed);
                    _toggleMusicButton.onClick.AddListener(OnMusicTogglePressed);
                }
            }

            Button sfxButton = FindButtonByName(ToggleSfxButtonObjectName);
            if (sfxButton != _toggleSfxButton)
            {
                if (_toggleSfxButton != null)
                {
                    _toggleSfxButton.onClick.RemoveListener(OnSfxTogglePressed);
                }

                _toggleSfxButton = sfxButton;

                if (_toggleSfxButton != null)
                {
                    RemoveLegacyToggleRelay(_toggleSfxButton);
                    _toggleSfxButton.onClick.RemoveListener(OnSfxTogglePressed);
                    _toggleSfxButton.onClick.AddListener(OnSfxTogglePressed);
                }
            }
        }

        private void UnbindAudioToggleButtons()
        {
            if (_toggleMusicButton != null)
            {
                _toggleMusicButton.onClick.RemoveListener(OnMusicTogglePressed);
            }

            if (_toggleSfxButton != null)
            {
                _toggleSfxButton.onClick.RemoveListener(OnSfxTogglePressed);
            }
        }

        private static void RemoveLegacyToggleRelay(Button button)
        {
            if (button == null)
            {
                return;
            }

            AudioStartToggleRelay relay = button.GetComponent<AudioStartToggleRelay>();
            if (relay != null)
            {
                UnityEngine.Object.Destroy(relay);
            }
        }

        private void OnMusicTogglePressed()
        {
            ToggleMusic();
        }

        private void OnSfxTogglePressed()
        {
            ToggleSfx();
        }

        private void RefreshToggleButtonsVisual()
        {
            ApplyToggleButtonVisual(_toggleMusicButton, AudioToggleType.Music, musicEnabled);
            ApplyToggleButtonVisual(_toggleSfxButton, AudioToggleType.Sfx, sfxEnabled);
        }

        private void ApplyToggleButtonVisual(Button button, AudioToggleType toggleType, bool enabled)
        {
            if (button == null)
            {
                return;
            }

            Text text = button.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = string.Empty;
                text.enabled = false;
            }

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = ResolveToggleSprite(toggleType, enabled);
                image.color = Color.white;
                image.type = Image.Type.Sliced;
            }
        }

        private Sprite ResolveToggleSprite(AudioToggleType toggleType, bool enabled)
        {
            Sprite fromSet = GetSpriteFromSet(toggleType, enabled);
            if (fromSet != null)
            {
                return fromSet;
            }

            if (toggleType == AudioToggleType.Music)
            {
                if (enabled && musicOnSprite != null)
                {
                    return musicOnSprite;
                }

                if (!enabled && musicOffSprite != null)
                {
                    return musicOffSprite;
                }
            }
            else
            {
                if (enabled && sfxOnSprite != null)
                {
                    return sfxOnSprite;
                }

                if (!enabled && sfxOffSprite != null)
                {
                    return sfxOffSprite;
                }
            }

            return GetFallbackToggleSprite(toggleType, enabled);
        }

        private Sprite GetSpriteFromSet(AudioToggleType toggleType, bool enabled)
        {
            if (toggleSpriteSet == null)
            {
                return null;
            }

            if (toggleType == AudioToggleType.Music)
            {
                return enabled ? toggleSpriteSet.musicOnSprite : toggleSpriteSet.musicOffSprite;
            }

            return enabled ? toggleSpriteSet.sfxOnSprite : toggleSpriteSet.sfxOffSprite;
        }

        private Sprite GetFallbackToggleSprite(AudioToggleType toggleType, bool enabled)
        {
            if (toggleType == AudioToggleType.Music)
            {
                if (enabled)
                {
                    if (_fallbackMusicOnSprite == null)
                    {
                        _fallbackMusicOnSprite = CreateFallbackToggleSprite(toggleType, true);
                    }

                    return _fallbackMusicOnSprite;
                }

                if (_fallbackMusicOffSprite == null)
                {
                    _fallbackMusicOffSprite = CreateFallbackToggleSprite(toggleType, false);
                }

                return _fallbackMusicOffSprite;
            }

            if (enabled)
            {
                if (_fallbackSfxOnSprite == null)
                {
                    _fallbackSfxOnSprite = CreateFallbackToggleSprite(toggleType, true);
                }

                return _fallbackSfxOnSprite;
            }

            if (_fallbackSfxOffSprite == null)
            {
                _fallbackSfxOffSprite = CreateFallbackToggleSprite(toggleType, false);
            }

            return _fallbackSfxOffSprite;
        }

        private static Sprite CreateFallbackToggleSprite(AudioToggleType toggleType, bool enabled)
        {
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color bg = toggleType == AudioToggleType.Music
                ? (enabled ? new Color32(0x2B, 0x93, 0xFF, 0xFF) : new Color32(0x6F, 0x83, 0x99, 0xFF))
                : (enabled ? new Color32(0xF3, 0x70, 0x21, 0xFF) : new Color32(0x7F, 0x8D, 0x9D, 0xFF));

            Color border = new Color32(0x12, 0x1A, 0x2F, 0xFF);
            Color icon = Color.white;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, bg);
                }
            }

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

            if (toggleType == AudioToggleType.Music)
            {
                DrawRect(tex, 26, 20, 5, 24, icon);
                DrawRect(tex, 30, 38, 14, 4, icon);
                DrawCircle(tex, 24, 18, 7, icon);
            }
            else
            {
                DrawRect(tex, 14, 24, 8, 16, icon);
                for (int x = 22; x <= 38; x++)
                {
                    int half = (x - 22) / 2 + 2;
                    DrawRect(tex, x, 32 - half, 1, half * 2, icon);
                }
            }

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

        private static void DrawRect(Texture2D tex, int x, int y, int w, int h, Color color)
        {
            if (tex == null)
            {
                return;
            }

            for (int py = y; py < y + h; py++)
            {
                for (int px = x; px < x + w; px++)
                {
                    if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                    {
                        tex.SetPixel(px, py, color);
                    }
                }
            }
        }

        private static void DrawCircle(Texture2D tex, int cx, int cy, int r, Color color)
        {
            if (tex == null)
            {
                return;
            }

            int rr = r * r;
            for (int y = -r; y <= r; y++)
            {
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

        private static void DrawThickLine(Texture2D tex, int x0, int y0, int x1, int y1, int thickness, Color color)
        {
            if (tex == null)
            {
                return;
            }

            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                DrawCircle(tex, x0, y0, Mathf.Max(1, thickness), color);

                if (x0 == x1 && y0 == y1)
                {
                    break;
                }

                int e2 = err * 2;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        private static Button FindButtonByName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            Button[] allButtons = FindObjectsOfType<Button>(true);
            for (int i = 0; i < allButtons.Length; i++)
            {
                if (allButtons[i] != null && allButtons[i].name == objectName)
                {
                    return allButtons[i];
                }
            }

            return null;
        }

        private void HookAllButtons()
        {
            Button[] allButtons = FindObjectsOfType<Button>(true);
            for (int i = 0; i < allButtons.Length; i++)
            {
                Button button = allButtons[i];
                if (button == null)
                {
                    continue;
                }

                AudioButtonClickRelay relay = button.GetComponent<AudioButtonClickRelay>();
                if (relay == null)
                {
                    relay = button.gameObject.AddComponent<AudioButtonClickRelay>();
                }

                relay.SetManager(this);
            }
        }

        private void BindQuizButtons()
        {
            QuizManager quiz = FindObjectOfType<QuizManager>(true);
            if (quiz == null)
            {
                return;
            }

            if (_boundQuizManager != quiz)
            {
                UnbindQuizButtons();
                _boundQuizManager = quiz;
            }

            if (quiz.answerButtons == null)
            {
                return;
            }

            for (int i = 0; i < quiz.answerButtons.Length; i++)
            {
                Button answerButton = quiz.answerButtons[i];
                if (answerButton == null || _quizButtonListeners.ContainsKey(answerButton))
                {
                    continue;
                }

                UnityEngine.Events.UnityAction action = () => StartCoroutine(PlayQuizAnswerSfxDeferred(quiz));
                answerButton.onClick.AddListener(action);
                _quizButtonListeners[answerButton] = action;
            }
        }

        private void UnbindQuizButtons()
        {
            foreach (KeyValuePair<Button, UnityEngine.Events.UnityAction> pair in _quizButtonListeners)
            {
                if (pair.Key != null)
                {
                    pair.Key.onClick.RemoveListener(pair.Value);
                }
            }

            _quizButtonListeners.Clear();
            _boundQuizManager = null;
        }

        private IEnumerator PlayQuizAnswerSfxDeferred(QuizManager quiz)
        {
            yield return null;

            if (quiz == null || quiz.questionText == null)
            {
                yield break;
            }

            string questionText = quiz.questionText.text ?? string.Empty;
            if (ContainsIgnoreCase(questionText, quiz.correctHeadlineText) || ContainsIgnoreCase(questionText, quiz.correctFeedbackText))
            {
                PlayCorrectAnswer();
                yield break;
            }

            if (ContainsIgnoreCase(questionText, quiz.wrongHeadlineText) || ContainsIgnoreCase(questionText, quiz.wrongFeedbackText))
            {
                PlayWrongAnswer();
            }
        }

        private static bool ContainsIgnoreCase(string source, string value)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value))
            {
                return false;
            }

            return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void EnsurePlayerJumpHooks()
        {
            PlayerController[] players = FindObjectsOfType<PlayerController>(true);
            for (int i = 0; i < players.Length; i++)
            {
                PlayerController player = players[i];
                if (player == null)
                {
                    continue;
                }

                AudioPlayerJumpRelay relay = player.GetComponent<AudioPlayerJumpRelay>();
                if (relay == null)
                {
                    relay = player.gameObject.AddComponent<AudioPlayerJumpRelay>();
                }

                relay.SetManager(this);
            }
        }

        private void ResolveGameOverPanel(bool resetPanelState)
        {
            GameObject panel = null;

            if (GameManager.Instance != null && GameManager.Instance.panelGameOver != null)
            {
                panel = GameManager.Instance.panelGameOver;
            }

            if (panel == null)
            {
                panel = GameObject.Find(gameOverPanelName);
            }

            _gameOverPanel = panel;
            if (resetPanelState)
            {
                _gameOverPanelWasActive = _gameOverPanel != null && _gameOverPanel.activeInHierarchy;
            }

            _nextPanelLookupAt = Time.unscaledTime + 1f;
        }

        private void PollGameOverPanel()
        {
            if (_gameOverPanel == null && Time.unscaledTime >= _nextPanelLookupAt)
            {
                ResolveGameOverPanel(resetPanelState: true);
            }

            if (_gameOverPanel == null)
            {
                return;
            }

            bool isActive = _gameOverPanel.activeInHierarchy;
            if (isActive && !_gameOverPanelWasActive)
            {
                PlayGameOver();
            }

            _gameOverPanelWasActive = isActive;
        }

        private void EnsureAudioSources()
        {
            AudioSource[] sources = GetComponents<AudioSource>();
            bool addedMusicSource = false;
            bool addedSfxSource = false;

            if (musicSource == null)
            {
                musicSource = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();
                addedMusicSource = sources.Length == 0;
            }

            if (sfxSource == null || sfxSource == musicSource)
            {
                AudioSource candidate = null;
                AudioSource[] allSources = GetComponents<AudioSource>();
                for (int i = 0; i < allSources.Length; i++)
                {
                    if (allSources[i] != null && allSources[i] != musicSource)
                    {
                        candidate = allSources[i];
                        break;
                    }
                }

                if (candidate == null)
                {
                    candidate = gameObject.AddComponent<AudioSource>();
                    addedSfxSource = true;
                }

                sfxSource = candidate;
            }

            if (addedMusicSource)
            {
                ConfigureMusicSourceDefaults(musicSource);
            }

            if (addedSfxSource)
            {
                ConfigureSfxSourceDefaults(sfxSource);
            }
        }

        private static void ConfigureMusicSourceDefaults(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0.7f;
        }

        private static void ConfigureSfxSourceDefaults(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.volume = 1f;
        }

        private void ApplyAudioState()
        {
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
    public class AudioButtonClickRelay : MonoBehaviour
    {
        private AudioManager _manager;
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }

            if (_button == null)
            {
                return;
            }

            _button.onClick.RemoveListener(OnButtonClicked);
            _button.onClick.AddListener(OnButtonClicked);
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnButtonClicked);
            }
        }

        public void SetManager(AudioManager manager)
        {
            _manager = manager;
        }

        private void OnButtonClicked()
        {
            AudioManager manager = _manager != null ? _manager : AudioManager.Instance;
            if (manager != null)
            {
                manager.PlayButtonClick();
            }
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public class AudioStartToggleRelay : MonoBehaviour
    {
        public AudioToggleType toggleType = AudioToggleType.Music;

        private Button _button;
        private Text _text;
        private Image _image;
        private float _nextRefreshAt;

        private static readonly Color EnabledColor = new Color32(0x12, 0x1A, 0x2F, 0xFF);
        private static readonly Color DisabledColor = new Color32(0x5D, 0x6B, 0x85, 0xFF);

        private void Awake()
        {
            _button = GetComponent<Button>();
            _text = GetComponentInChildren<Text>(true);
            _image = GetComponent<Image>();
        }

        private void OnEnable()
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }

            if (_button != null)
            {
                _button.onClick.RemoveListener(OnTogglePressed);
                _button.onClick.AddListener(OnTogglePressed);
            }

            RefreshVisual();
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnTogglePressed);
            }
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextRefreshAt)
            {
                return;
            }

            _nextRefreshAt = Time.unscaledTime + 0.35f;
            RefreshVisual();
        }

        private void OnTogglePressed()
        {
            AudioManager manager = AudioManager.Instance;
            if (manager == null)
            {
                return;
            }

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

        private void RefreshVisual()
        {
            AudioManager manager = AudioManager.Instance;

            bool isEnabled = true;
            string prefix = toggleType == AudioToggleType.Music ? "M" : "S";

            if (manager != null)
            {
                isEnabled = toggleType == AudioToggleType.Music ? manager.IsMusicEnabled : manager.IsSfxEnabled;
            }

            if (_text == null)
            {
                _text = GetComponentInChildren<Text>(true);
            }

            if (_text != null)
            {
                _text.text = prefix + (isEnabled ? " ON" : " OFF");
            }

            if (_image == null)
            {
                _image = GetComponent<Image>();
            }

            if (_image != null)
            {
                _image.color = isEnabled ? EnabledColor : DisabledColor;
            }
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    public class AudioPlayerJumpRelay : MonoBehaviour
    {
        private AudioManager _manager;
        private PlayerController _player;
        private Rigidbody2D _rb;
        private float _lastKnownVerticalVelocity;
        private int _lastPlatformObjectId = int.MinValue;
        private float _lastJumpSfxAt = -10f;

        private const float MinRepeatSeconds = 0.1f;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            _rb = _player != null ? _player.rb : null;

            if (_rb == null)
            {
                _rb = GetComponent<Rigidbody2D>();
            }

            _lastKnownVerticalVelocity = _rb != null ? _rb.velocity.y : 0f;
        }

        private void FixedUpdate()
        {
            if (_rb == null)
            {
                _rb = _player != null && _player.rb != null ? _player.rb : GetComponent<Rigidbody2D>();
            }

            if (_rb != null)
            {
                _lastKnownVerticalVelocity = _rb.velocity.y;
            }
        }

        public void SetManager(AudioManager manager)
        {
            _manager = manager;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null)
            {
                return;
            }

            if (!other.CompareTag("Platform") && !other.CompareTag("QuizPlatform") && !other.CompareTag("Ground"))
            {
                return;
            }

            if (!IsDescendingOrAlmostStill())
            {
                return;
            }

            int platformObjectId = other.gameObject.GetInstanceID();
            float now = Time.time;
            if (platformObjectId == _lastPlatformObjectId && now - _lastJumpSfxAt < MinRepeatSeconds)
            {
                return;
            }

            _lastPlatformObjectId = platformObjectId;
            _lastJumpSfxAt = now;

            AudioManager manager = _manager != null ? _manager : AudioManager.Instance;
            if (manager != null)
            {
                manager.PlayJump();
            }
        }

        private bool IsDescendingOrAlmostStill()
        {
            if (_rb == null)
            {
                if (_player != null && _player.rb != null)
                {
                    _rb = _player.rb;
                }
                else
                {
                    _rb = GetComponent<Rigidbody2D>();
                }
            }

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