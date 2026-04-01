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

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        [Header("References")]
        public PlayerController player;
        public GameObject playerPrefab;
        public LevelSpawner levelSpawner;
        public QuizManager quizManager;
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

        private const string HighscoreKey = "PolyJump_Highscore_Local";

        public float RemainingTime => _remainingTime;
        public int CurrentScore => Mathf.Max(0, Mathf.RoundToInt(_maxPlayerY));
        public int Highscore => _highscore;

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
        }

        private void Start()
        {
            EnsureUiReferences();

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera != null)
            {
                _cameraMinY = mainCamera.transform.position.y;
            }

            if (playerSpawnPoint == null)
            {
                GameObject spawnObj = GameObject.Find("PlayerSpawnPoint");
                if (spawnObj != null)
                {
                    playerSpawnPoint = spawnObj.transform;
                }
            }

            _highscore = PlayerPrefs.GetInt(HighscoreKey, 0);
            _remainingTime = startTimeSeconds;

            if (player != null)
            {
                player.gameObject.SetActive(false);
            }

            WireButtons();
            SetState(GameState.Menu);
            RefreshUI();
        }

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
                if (_remainingTime <= 0f)
                {
                    _remainingTime = 0f;
                    GameOver();
                    return;
                }
            }

            if (CurrentState == GameState.Playing)
            {
                TrackBestHeight();
                CheckFallOut();
            }

            if (CurrentState == GameState.Playing || CurrentState == GameState.Quiz)
            {
                UpdateCameraFollow();
            }

            RefreshHudText();
        }

        private void WireButtons()
        {
            if (playButton != null)
            {
                playButton.onClick.RemoveListener(OnPlayPressed);
                playButton.onClick.AddListener(OnPlayPressed);
            }

            if (replayButton != null)
            {
                replayButton.onClick.RemoveListener(OnReplayPressed);
                replayButton.onClick.AddListener(OnReplayPressed);
            }
        }

        public void OnPlayPressed()
        {
            if (CurrentState == GameState.Playing || CurrentState == GameState.Quiz)
            {
                return;
            }

            StartGameplay();
        }

        public void OnReplayPressed()
        {
            Scene active = SceneManager.GetActiveScene();
            SceneManager.LoadScene(active.buildIndex);
        }

        public void StartGameplay()
        {
            EnsureUiReferences();
            WireButtons();

            SpawnOrActivatePlayer();
            if (player == null)
            {
                Debug.LogError("[PolyJump] Khong the bat dau game vi chua co Player.");
                return;
            }

            _remainingTime = startTimeSeconds;
            _maxPlayerY = Mathf.Max(0f, player.transform.position.y);

            if (mainCamera != null)
            {
                _cameraMinY = mainCamera.transform.position.y;
            }

            if (levelSpawner != null)
            {
                levelSpawner.playerTransform = player.transform;
                levelSpawner.ResetLevelAroundPlayer();
                levelSpawner.SetPaused(false);
            }

            player.ResetForNewRun();
            player.SetGameplayPaused(false);

            if (quizManager != null)
            {
                quizManager.HideQuizPanel();
            }

            SetState(GameState.Playing);
            RefreshUI();
        }

        public void RequestQuiz()
        {
            if (CurrentState != GameState.Playing)
            {
                return;
            }

            SetState(GameState.Quiz);

            if (player != null)
            {
                player.SetGameplayPaused(true);
            }

            if (levelSpawner != null)
            {
                levelSpawner.SetPaused(true);
            }

            if (quizManager != null)
            {
                quizManager.ShowRandomQuestion();
            }

            RefreshUI();
        }

        public void RegisterPendingQuizPlatform(Collider2D quizPlatformCollider)
        {
            if (quizPlatformCollider == null)
            {
                return;
            }

            if (!quizPlatformCollider.CompareTag("QuizPlatform"))
            {
                return;
            }

            _pendingQuizPlatformCollider = quizPlatformCollider;
        }

        public void ResolveQuiz(float timeDelta)
        {
            if (timeDelta >= 0f)
            {
                AddTime(timeDelta);
            }
            else
            {
                SubtractTime(-timeDelta);
            }

            if (quizManager != null)
            {
                quizManager.HideQuizPanel();
            }

            if (player != null)
            {
                player.ForceNormalAnimation();
            }

            ConvertPendingQuizPlatformToNormal();

            if (CurrentState != GameState.GameOver)
            {
                SetState(GameState.Playing);

                if (player != null)
                {
                    player.SetGameplayPaused(false);
                }

                if (levelSpawner != null)
                {
                    levelSpawner.SetPaused(false);
                }
            }

            RefreshUI();
        }

        private void ConvertPendingQuizPlatformToNormal()
        {
            if (_pendingQuizPlatformCollider == null)
            {
                return;
            }

            GameObject obj = _pendingQuizPlatformCollider.gameObject;
            obj.tag = "Platform";

            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = new Color32(0xF3, 0x70, 0x21, 0xFF);
            }

            _pendingQuizPlatformCollider.enabled = true;
            _pendingQuizPlatformCollider = null;
        }

        public void AddTime(float seconds)
        {
            _remainingTime = Mathf.Max(0f, _remainingTime + Mathf.Abs(seconds));
            RefreshHudText();
        }

        public void SubtractTime(float seconds)
        {
            _remainingTime = Mathf.Max(0f, _remainingTime - Mathf.Abs(seconds));
            RefreshHudText();
        }

        private void SpawnOrActivatePlayer()
        {
            Vector3 spawnPos = GetPlayerSpawnPosition();

            if (player != null)
            {
                player.gameObject.SetActive(true);
                player.transform.position = spawnPos;
                player.transform.rotation = Quaternion.identity;
                return;
            }

            if (playerPrefab == null)
            {
                return;
            }

            GameObject playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            playerObj.name = "Player";
            player = playerObj.GetComponent<PlayerController>();

            if (player == null)
            {
                player = playerObj.AddComponent<PlayerController>();
            }
        }

        private Vector3 GetPlayerSpawnPosition()
        {
            if (playerSpawnPoint != null)
            {
                return playerSpawnPoint.position;
            }

            return playerSpawnPosition;
        }

        private void TrackBestHeight()
        {
            if (player == null)
            {
                return;
            }

            _maxPlayerY = Mathf.Max(_maxPlayerY, player.transform.position.y);
        }

        private void UpdateCameraFollow()
        {
            if (mainCamera == null || player == null)
            {
                return;
            }

            float cameraBottom = mainCamera.transform.position.y - mainCamera.orthographicSize;
            float triggerY = cameraBottom + (mainCamera.orthographicSize * 2f * cameraFollowTriggerNormalizedY);
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

        private void CheckFallOut()
        {
            if (player == null || mainCamera == null)
            {
                return;
            }

            float cameraBottom = mainCamera.transform.position.y - mainCamera.orthographicSize;
            if (player.transform.position.y < cameraBottom - fallOutOffset)
            {
                GameOver();
            }
        }

        private void GameOver()
        {
            if (CurrentState == GameState.GameOver)
            {
                return;
            }

            SetState(GameState.GameOver);

            if (player != null)
            {
                player.SetGameplayPaused(true);
            }

            if (levelSpawner != null)
            {
                levelSpawner.SetPaused(true);
            }

            if (quizManager != null)
            {
                quizManager.HideQuizPanel();
            }

            int finalScore = CurrentScore;
            if (finalScore > _highscore)
            {
                _highscore = finalScore;
                PlayerPrefs.SetInt(HighscoreKey, _highscore);
                PlayerPrefs.Save();
            }

            if (gameOverScoreText != null)
            {
                gameOverScoreText.text = "Điểm: " + finalScore;
            }

            if (gameOverHighscoreText != null)
            {
                gameOverHighscoreText.text = "Highscore: " + _highscore;
            }

            RefreshUI();
        }

        private void RefreshUI()
        {
            bool isMenu = CurrentState == GameState.Menu;
            bool isPlaying = CurrentState == GameState.Playing;
            bool isQuiz = CurrentState == GameState.Quiz;
            bool isGameOver = CurrentState == GameState.GameOver;

            if (panelStart != null)
            {
                panelStart.SetActive(isMenu);
            }

            if (panelHud != null)
            {
                panelHud.SetActive(isPlaying || isQuiz);
            }

            if (panelQuiz != null)
            {
                panelQuiz.SetActive(isQuiz);
            }

            if (panelGameOver != null)
            {
                panelGameOver.SetActive(isGameOver);
            }

            RefreshHudText();
        }

        private void RefreshHudText()
        {
            if (hudScoreText != null)
            {
                hudScoreText.text = "Điểm: " + CurrentScore;
            }

            if (hudTimeText != null)
            {
                hudTimeText.text = FormatTime(_remainingTime);
            }
        }

        private static string FormatTime(float totalSeconds)
        {
            int seconds = Mathf.Max(0, Mathf.FloorToInt(totalSeconds));
            int minutes = seconds / 60;
            int remain = seconds % 60;
            return string.Format("{0:00}:{1:00}", minutes, remain);
        }

        private void EnsureUiReferences()
        {
            if (panelStart == null)
            {
                panelStart = GameObject.Find("Panel_Start");
            }

            if (panelHud == null)
            {
                panelHud = GameObject.Find("Panel_HUD");
            }

            if (panelQuiz == null)
            {
                panelQuiz = GameObject.Find("Panel_Quiz");
            }

            if (panelGameOver == null)
            {
                panelGameOver = GameObject.Find("Panel_GameOver");
            }

            if (playButton == null && panelStart != null)
            {
                Transform t = panelStart.transform.Find("Btn_Play");
                if (t != null)
                {
                    playButton = t.GetComponent<Button>();
                }
            }

            // Fallback để chịu được trường hợp đổi tên object hoặc mất reference Inspector.
            if (playButton == null)
            {
                Button[] buttons = Object.FindObjectsOfType<Button>(true);
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

            if (replayButton == null && panelGameOver != null)
            {
                Transform t = panelGameOver.transform.Find("Btn_Replay");
                if (t != null)
                {
                    replayButton = t.GetComponent<Button>();
                }
            }

            if (hudScoreText == null && panelHud != null)
            {
                Transform t = panelHud.transform.Find("Txt_Score");
                if (t != null)
                {
                    hudScoreText = t.GetComponent<Text>();
                }
            }

            if (hudTimeText == null && panelHud != null)
            {
                Transform t = panelHud.transform.Find("Txt_Time");
                if (t != null)
                {
                    hudTimeText = t.GetComponent<Text>();
                }
            }

            if (gameOverScoreText == null && panelGameOver != null)
            {
                Transform t = panelGameOver.transform.Find("Txt_FinalScore");
                if (t != null)
                {
                    gameOverScoreText = t.GetComponent<Text>();
                }
            }

            if (gameOverHighscoreText == null && panelGameOver != null)
            {
                Transform t = panelGameOver.transform.Find("Txt_Highscore");
                if (t != null)
                {
                    gameOverHighscoreText = t.GetComponent<Text>();
                }
            }
        }

        private void SetState(GameState nextState)
        {
            CurrentState = nextState;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void TryStartFromPlayButtonInput()
        {
            if (EventSystem.current == null)
            {
                return;
            }

            EnsureUiReferences();

            if (playButton == null || !playButton.gameObject.activeInHierarchy || !playButton.interactable)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0) && IsPointerOnPlayButton(Input.mousePosition, -1))
            {
                OnPlayPressed();
                return;
            }

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase != TouchPhase.Began)
                {
                    continue;
                }

                if (IsPointerOnPlayButton(touch.position, touch.fingerId))
                {
                    OnPlayPressed();
                    return;
                }
            }
        }

        private bool IsPointerOnPlayButton(Vector2 screenPosition, int pointerId)
        {
            PointerEventData data = new PointerEventData(EventSystem.current)
            {
                position = screenPosition,
                pointerId = pointerId
            };

            _uiRaycastResults.Clear();
            EventSystem.current.RaycastAll(data, _uiRaycastResults);

            for (int i = 0; i < _uiRaycastResults.Count; i++)
            {
                GameObject hit = _uiRaycastResults[i].gameObject;
                if (hit == null)
                {
                    continue;
                }

                if (hit == playButton.gameObject || hit.transform.IsChildOf(playButton.transform))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
