using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace PolyJump.Scripts
{
    public enum GameState
    {
        Menu,
        Playing,
        Quiz,
        GameOver
    }

    /// <summary>
    /// Điều phối luồng chơi chính: trạng thái game, thời gian, điểm số, quiz và kết thúc màn chơi.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        private const int FixedTargetFps = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        /// <summary>
        /// Áp dụng Global Frame Rate On Boot phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void ApplyGlobalFrameRateOnBoot()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            ApplyFrameRateSettings();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        /// <summary>
        /// Thực hiện nghiệp vụ Reset Statics theo ngữ cảnh sử dụng của script.
        /// </summary>
        private static void ResetStatics()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            Instance = null;
        }

        [Header("References")]
        public PlayerController player;
        public GameObject playerPrefab;
        public LevelSpawner levelSpawner;
        public QuizManager quizManager;
        public PlayFabAuthManager playFabAuthManager;
        public Camera mainCamera;

        [Header("Spawn Config")]
        public Transform playerSpawnPoint;
        public Vector3 playerSpawnPosition = new Vector3(0f, -1.2f, 0f);

        [Header("UI Panels")]
        public GameObject panelStart;
        public GameObject panelHud;
        public GameObject panelQuiz;
        public GameObject panelGameOver;

        [Header("UI Text")]
        public Text hudScoreText;
        public Text hudTimeText;
        public Text gameOverScoreText;
        public Text gameOverHighscoreText;

        [Header("UI Buttons")]
        public Button playButton;
        public Button replayButton;

        [Header("Gameplay Config")]
        public float startTimeSeconds = 180f;
        public float fallOutOffset = 1.5f;

        [Header("Camera Follow")]
        [Range(0f, 1f)]
        public float cameraFollowTriggerNormalizedY = 0.5f;
        public float cameraFollowLerp = 10f;

        public GameState CurrentState { get; private set; } = GameState.Menu;

        private float _remainingTime;
        private float _maxPlayerY;
        private float _cameraMinY;
        private int _highscore;
        private Collider2D _pendingQuizPlatformCollider;
        private readonly List<RaycastResult> _uiRaycastResults = new List<RaycastResult>(8);

        public float RemainingTime => _remainingTime;
        public int CurrentScore => Mathf.Max(0, Mathf.RoundToInt(_maxPlayerY));
        public int Highscore => _highscore;

        /// <summary>
        /// Khởi tạo tham chiếu, trạng thái ban đầu và các cấu hình cần thiết khi đối tượng được tạo.
        /// </summary>
        private void Awake()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            ApplyFrameRateSettings();

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
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
        }

        /// <summary>
        /// Xử lý callback sự kiện hệ thống hoặc gameplay phát sinh trong vòng đời đối tượng.
        /// </summary>
        private void OnApplicationFocus(bool hasFocus)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (hasFocus)
            {
                ApplyFrameRateSettings();
            }
        }

        /// <summary>
        /// Áp dụng Frame Rate Settings phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void ApplyFrameRateSettings()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = FixedTargetFps;
        }

        /// <summary>
        /// Thiết lập dữ liệu và liên kết cần dùng ngay trước khi vòng lặp gameplay bắt đầu.
        /// </summary>
        private void Start()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            EnsureUiReferences();

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (mainCamera != null)
            {
                _cameraMinY = mainCamera.transform.position.y;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (playerSpawnPoint == null)
            {
                GameObject spawnObj = GameObject.Find("PlayerSpawnPoint");
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (spawnObj != null)
                {
                    playerSpawnPoint = spawnObj.transform;
                }
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (playFabAuthManager == null)
            {
                playFabAuthManager = Object.FindObjectOfType<PlayFabAuthManager>(true);
            }

            _highscore = playFabAuthManager != null ? playFabAuthManager.GetCachedLeaderboardHighscore() : 0;
            _remainingTime = startTimeSeconds;

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (player != null)
            {
                player.gameObject.SetActive(false);
            }

            WireButtons();
            SetState(GameState.Menu);
            RefreshUI();
        }

        /// <summary>
        /// Cập nhật logic theo từng khung hình để phản hồi trạng thái hiện tại của game.
        /// </summary>
        private void Update()
        {
            // Fail-safe: chỉ khi bấm đúng nút PLAY thì mới bắt đầu game.
            if (CurrentState == GameState.Menu)
            {
                TryStartFromPlayButtonInput();
            }

            // Khi đang chơi hoặc đang trả lời quiz, thời gian vẫn phải tiếp tục đếm.
            if (CurrentState == GameState.Playing || CurrentState == GameState.Quiz)
            {
                _remainingTime -= Time.unscaledDeltaTime;
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (_remainingTime <= 0f)
                {
                    _remainingTime = 0f;
                    GameOver();
                    return;
                }
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (CurrentState == GameState.Playing)
            {
                TrackBestHeight();
                CheckFallOut();
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (CurrentState == GameState.Playing || CurrentState == GameState.Quiz)
            {
                UpdateCameraFollow();
            }

            RefreshHudText();
        }

        /// <summary>
        /// Liên kết Buttons phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void WireButtons()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (playButton != null)
            {
                playButton.onClick.RemoveListener(OnPlayPressed);
                playButton.onClick.AddListener(OnPlayPressed);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (replayButton != null)
            {
                replayButton.onClick.RemoveListener(OnReplayPressed);
                replayButton.onClick.AddListener(OnReplayPressed);
            }
        }

        /// <summary>
        /// Xử lý sự kiện khi người dùng nhấn nút/chức năng tương ứng trên giao diện.
        /// </summary>
        public void OnPlayPressed()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (CurrentState == GameState.Playing || CurrentState == GameState.Quiz)
            {
                return;
            }

            StartGameplay();
        }

        /// <summary>
        /// Xử lý sự kiện khi người dùng nhấn nút/chức năng tương ứng trên giao diện.
        /// </summary>
        public void OnReplayPressed()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            PlayFabAuthManager.PreserveSessionForNextSceneLoad();
            Scene active = SceneManager.GetActiveScene();
            SceneManager.LoadScene(active.buildIndex);
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Start Gameplay theo ngữ cảnh sử dụng của script.
        /// </summary>
        public void StartGameplay()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            EnsureUiReferences();
            WireButtons();

            SpawnOrActivatePlayer();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (player == null)
            {
                Debug.LogError("[PolyJump] Khong the bat dau game vi chua co Player.");
                return;
            }

            _remainingTime = startTimeSeconds;
            _maxPlayerY = Mathf.Max(0f, player.transform.position.y);

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (mainCamera != null)
            {
                _cameraMinY = mainCamera.transform.position.y;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (levelSpawner != null)
            {
                levelSpawner.playerTransform = player.transform;
                levelSpawner.ResetLevelAroundPlayer();
                levelSpawner.SetPaused(false);
            }

            player.ResetForNewRun();
            player.SetGameplayPaused(false);

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (quizManager != null)
            {
                quizManager.HideQuizPanel();
            }

            SetState(GameState.Playing);
            RefreshUI();
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Request Quiz theo ngữ cảnh sử dụng của script.
        /// </summary>
        public void RequestQuiz()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (CurrentState != GameState.Playing)
            {
                return;
            }

            SetState(GameState.Quiz);

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (player != null)
            {
                player.SetGameplayPaused(true);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (levelSpawner != null)
            {
                levelSpawner.SetPaused(true);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (quizManager != null)
            {
                quizManager.ShowRandomQuestion();
            }

            RefreshUI();
        }

        /// <summary>
        /// Đăng ký Pending Quiz Platform phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        public void RegisterPendingQuizPlatform(Collider2D quizPlatformCollider)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (quizPlatformCollider == null)
            {
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!quizPlatformCollider.CompareTag("QuizPlatform"))
            {
                return;
            }

            _pendingQuizPlatformCollider = quizPlatformCollider;
        }

        /// <summary>
        /// Xác định Quiz phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        public void ResolveQuiz(float timeDelta)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (timeDelta >= 0f)
            {
                AddTime(timeDelta);
            }
            else
            {
                SubtractTime(-timeDelta);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (quizManager != null)
            {
                quizManager.HideQuizPanel();
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (player != null)
            {
                player.ForceNormalAnimation();
            }

            ConvertPendingQuizPlatformToNormal();

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (CurrentState != GameState.GameOver)
            {
                SetState(GameState.Playing);

                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (player != null)
                {
                    player.SetGameplayPaused(false);
                }

                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (levelSpawner != null)
                {
                    levelSpawner.SetPaused(false);
                }
            }

            RefreshUI();
        }

        /// <summary>
        /// Chuyển đổi Pending Quiz Platform To Normal phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void ConvertPendingQuizPlatformToNormal()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_pendingQuizPlatformCollider == null)
            {
                return;
            }

            GameObject obj = _pendingQuizPlatformCollider.gameObject;
            obj.tag = "Platform";

            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (sr != null)
            {
                sr.color = new Color32(0xF3, 0x70, 0x21, 0xFF);
            }

            _pendingQuizPlatformCollider.enabled = true;
            _pendingQuizPlatformCollider = null;
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Add Time theo ngữ cảnh sử dụng của script.
        /// </summary>
        public void AddTime(float seconds)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            _remainingTime = Mathf.Max(0f, _remainingTime + Mathf.Abs(seconds));
            RefreshHudText();
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Subtract Time theo ngữ cảnh sử dụng của script.
        /// </summary>
        public void SubtractTime(float seconds)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            _remainingTime = Mathf.Max(0f, _remainingTime - Mathf.Abs(seconds));
            RefreshHudText();
        }

        /// <summary>
        /// Sinh Or Activate Player phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void SpawnOrActivatePlayer()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            Vector3 spawnPos = GetPlayerSpawnPosition();

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (player != null)
            {
                player.gameObject.SetActive(true);
                player.transform.position = spawnPos;
                player.transform.rotation = Quaternion.identity;
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (playerPrefab == null)
            {
                return;
            }

            GameObject playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            playerObj.name = "Player";
            player = playerObj.GetComponent<PlayerController>();

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (player == null)
            {
                player = playerObj.AddComponent<PlayerController>();
            }
        }

        /// <summary>
        /// Lấy Player Spawn Position phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private Vector3 GetPlayerSpawnPosition()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (playerSpawnPoint != null)
            {
                return playerSpawnPoint.position;
            }

            return playerSpawnPosition;
        }

        /// <summary>
        /// Theo dõi Best Height phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void TrackBestHeight()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (player == null)
            {
                return;
            }

            _maxPlayerY = Mathf.Max(_maxPlayerY, player.transform.position.y);
        }

        /// <summary>
        /// Cập nhật Camera Follow phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void UpdateCameraFollow()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (mainCamera == null || player == null)
            {
                return;
            }

            float cameraBottom = mainCamera.transform.position.y - mainCamera.orthographicSize;
            float triggerY = cameraBottom + (mainCamera.orthographicSize * 2f * cameraFollowTriggerNormalizedY);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (player.transform.position.y <= triggerY)
            {
                return;
            }

            float offset = player.transform.position.y - triggerY;
            float targetY = mainCamera.transform.position.y + offset;
            targetY = Mathf.Max(_cameraMinY, targetY);

            float t = 1f - Mathf.Exp(-Mathf.Max(0.01f, cameraFollowLerp) * Time.unscaledDeltaTime);
            float nextY = Mathf.Lerp(mainCamera.transform.position.y, targetY, t);
            nextY = Mathf.Max(mainCamera.transform.position.y, nextY);

            Vector3 camPos = mainCamera.transform.position;
            camPos.y = nextY;
            mainCamera.transform.position = camPos;
            _cameraMinY = Mathf.Max(_cameraMinY, nextY);
        }

        /// <summary>
        /// Kiểm tra Fall Out phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void CheckFallOut()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (player == null || mainCamera == null)
            {
                return;
            }

            float cameraBottom = mainCamera.transform.position.y - mainCamera.orthographicSize;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (player.transform.position.y < cameraBottom - fallOutOffset)
            {
                GameOver();
            }
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Game Over theo ngữ cảnh sử dụng của script.
        /// </summary>
        private void GameOver()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (CurrentState == GameState.GameOver)
            {
                return;
            }

            SetState(GameState.GameOver);

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (player != null)
            {
                player.SetGameplayPaused(true);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (levelSpawner != null)
            {
                levelSpawner.SetPaused(true);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (quizManager != null)
            {
                quizManager.HideQuizPanel();
            }

            int finalScore = CurrentScore;

            int cachedHighscore = playFabAuthManager != null
                ? playFabAuthManager.GetCachedLeaderboardHighscore()
                : _highscore;
            _highscore = Mathf.Max(0, cachedHighscore);

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (gameOverScoreText != null)
            {
                gameOverScoreText.text = "Điểm: " + finalScore;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (gameOverHighscoreText != null)
            {
                gameOverHighscoreText.text = "Highscore: " + _highscore;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (playFabAuthManager != null)
            {
                playFabAuthManager.SubmitScore(finalScore, resolvedHighscore =>
                {
                    _highscore = Mathf.Max(0, resolvedHighscore);
                    if (gameOverHighscoreText != null)
                    {
                        gameOverHighscoreText.text = "Highscore: " + _highscore;
                    }
                });
            }

            RefreshUI();
        }

        /// <summary>
        /// Làm mới UI phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void RefreshUI()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            bool isMenu = CurrentState == GameState.Menu;
            bool isPlaying = CurrentState == GameState.Playing;
            bool isQuiz = CurrentState == GameState.Quiz;
            bool isGameOver = CurrentState == GameState.GameOver;

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (panelStart != null)
            {
                panelStart.SetActive(isMenu);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (panelHud != null)
            {
                panelHud.SetActive(isPlaying || isQuiz);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (panelQuiz != null)
            {
                panelQuiz.SetActive(isQuiz);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (panelGameOver != null)
            {
                panelGameOver.SetActive(isGameOver);
            }

            RefreshHudText();
        }

        /// <summary>
        /// Làm mới Hud Text phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void RefreshHudText()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (hudScoreText != null)
            {
                hudScoreText.text = "Điểm: " + CurrentScore;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (hudTimeText != null)
            {
                hudTimeText.text = FormatTime(_remainingTime);
            }
        }

        /// <summary>
        /// Định dạng Time phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static string FormatTime(float totalSeconds)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            int seconds = Mathf.Max(0, Mathf.FloorToInt(totalSeconds));
            int minutes = seconds / 60;
            int remain = seconds % 60;
            return string.Format("{0:00}:{1:00}", minutes, remain);
        }

        /// <summary>
        /// Đảm bảo Ui References phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void EnsureUiReferences()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (panelStart == null)
            {
                panelStart = GameObject.Find("Panel_Start");
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (panelHud == null)
            {
                panelHud = GameObject.Find("Panel_HUD");
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (panelQuiz == null)
            {
                panelQuiz = GameObject.Find("Panel_Quiz");
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (panelGameOver == null)
            {
                panelGameOver = GameObject.Find("Panel_GameOver");
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (playButton == null && panelStart != null)
            {
                Transform t = panelStart.transform.Find("Btn_Play");
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (t != null)
                {
                    playButton = t.GetComponent<Button>();
                }
            }

            // Fallback để chịu được trường hợp đổi tên object hoặc mất reference Inspector.
            if (playButton == null)
            {
                Button[] buttons = Object.FindObjectsOfType<Button>(true);
                // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i] == null)
                    {
                        continue;
                    }

                    string lower = buttons[i].name.ToLowerInvariant();
                    if (lower.Contains("play"))
                    {
                        playButton = buttons[i];
                        break;
                    }
                }
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (replayButton == null && panelGameOver != null)
            {
                Transform t = panelGameOver.transform.Find("Btn_Replay");
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (t != null)
                {
                    replayButton = t.GetComponent<Button>();
                }
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (hudScoreText == null && panelHud != null)
            {
                Transform t = panelHud.transform.Find("Txt_Score");
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (t != null)
                {
                    hudScoreText = t.GetComponent<Text>();
                }
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (hudTimeText == null && panelHud != null)
            {
                Transform t = panelHud.transform.Find("Txt_Time");
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (t != null)
                {
                    hudTimeText = t.GetComponent<Text>();
                }
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (gameOverScoreText == null && panelGameOver != null)
            {
                Transform t = panelGameOver.transform.Find("Txt_FinalScore");
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (t != null)
                {
                    gameOverScoreText = t.GetComponent<Text>();
                }
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (gameOverHighscoreText == null && panelGameOver != null)
            {
                Transform t = panelGameOver.transform.Find("Txt_Highscore");
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (t != null)
                {
                    gameOverHighscoreText = t.GetComponent<Text>();
                }
            }
        }

        /// <summary>
        /// Thiết lập State phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void SetState(GameState nextState)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            CurrentState = nextState;
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
        }

        /// <summary>
        /// Thử xử lý Start From Play Button Input phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void TryStartFromPlayButtonInput()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (EventSystem.current == null)
            {
                return;
            }

            EnsureUiReferences();

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (playButton == null || !playButton.gameObject.activeInHierarchy || !playButton.interactable)
            {
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (Input.GetMouseButtonDown(0) && IsPointerOnPlayButton(Input.mousePosition, -1))
            {
                OnPlayPressed();
                return;
            }

            // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (touch.phase != TouchPhase.Began)
                {
                    continue;
                }

                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (IsPointerOnPlayButton(touch.position, touch.fingerId))
                {
                    OnPlayPressed();
                    return;
                }
            }
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Is Pointer On Play Button theo ngữ cảnh sử dụng của script.
        /// </summary>
        private bool IsPointerOnPlayButton(Vector2 screenPosition, int pointerId)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            PointerEventData data = new PointerEventData(EventSystem.current)
            {
                position = screenPosition,
                pointerId = pointerId
            };

            _uiRaycastResults.Clear();
            EventSystem.current.RaycastAll(data, _uiRaycastResults);

            // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
            for (int i = 0; i < _uiRaycastResults.Count; i++)
            {
                GameObject hit = _uiRaycastResults[i].gameObject;
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (hit == null)
                {
                    continue;
                }

                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (hit == playButton.gameObject || hit.transform.IsChildOf(playButton.transform))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
