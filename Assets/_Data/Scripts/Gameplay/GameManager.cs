using UnityEngine;
using Sirenix.OdinInspector;

public enum GameState
{
    Menu,
    Playing,
    Paused,
    Quiz,
    GameOver
}

public class GameManager : Singleton<GameManager>
{
    [SerializeField] private PlayerController playerPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GameObject gameplayContainer;

    [ShowInInspector] public GameState CurrentState { get; private set; } = GameState.Menu;

    private PlayerController _currentPlayer;

    protected override void LoadComponentRuntime()
    {
        EventManager.Instance.Subscribe(GameEvent.GameStarted, OnGameStarted);
        EventManager.Instance.Subscribe(GameEvent.GameOver, OnGameOver);
        EventManager.Instance.Subscribe(GameEvent.QuizTriggered, OnQuizTriggered);
        EventManager.Instance.Subscribe(GameEvent.QuizClosed, OnQuizClosed);
        EventManager.Instance.Subscribe(GameEvent.GamePaused, OnGamePaused);
        EventManager.Instance.Subscribe(GameEvent.GameResumed, OnGameResumed);
        EventManager.Instance.Subscribe(GameEvent.MajorSelected, OnMajorSelected);
    }

    private void OnMajorSelected(object data)
    {
        if (CurrentState == GameState.Playing) return;
        if (data is string major) StartGameplay(major);
    }

    private void OnGameStarted(object data)
    {
        if (CurrentState == GameState.Menu)
        {
            StartGameplay(PersistentData.Instance.CurrentMajor);
        }
    }

    public void StartGameWithMajor(string majorId)
    {
        if (string.IsNullOrEmpty(majorId)) return;

        PersistentData.Instance.CurrentMajor = majorId;
        StartGameplay(majorId);
    }

    private void StartGameplay(string major)
    {
        CurrentState = GameState.Playing;

        if (spawnPoint != null && playerPrefab != null)
        {
            if (_currentPlayer != null) Destroy(_currentPlayer.gameObject);
            _currentPlayer = Instantiate(playerPrefab, spawnPoint.position, Quaternion.identity);

            // ResetSpawner phải chạy SAU khi player đã spawn
            // để AlignSpawnToPlayer(player) tìm thấy player qua tag.
            var platformSpawner = FindFirstObjectByType<PlatformSpawner>();
            if (platformSpawner != null)
                platformSpawner.AlignSpawnToPlayer(_currentPlayer.transform);
            if (platformSpawner != null) platformSpawner.ResetSpawner();
        }

        ScoreManager.Instance.ResetScore();

        var quizManager = FindFirstObjectByType<QuizManager>();
        if (quizManager != null) quizManager.SetMajor(major);

        var cameraFollow = FindFirstObjectByType<CameraFollow>();
        if (cameraFollow != null)
            cameraFollow.SetTarget(_currentPlayer != null ? _currentPlayer.transform : null, true);

        UIManager.Instance.ChangeMenu(MenuType.GameplayHUD);
    }

    private void OnGameOver(object data)
    {
        if (CurrentState != GameState.Playing && CurrentState != GameState.Quiz) return;
        CurrentState = GameState.GameOver;
        UIManager.Instance.ChangeMenu(MenuType.GameOverPanel);
    }

    private void OnQuizTriggered(object data)
    {
        CurrentState = GameState.Quiz;
    }

    private void OnQuizClosed(object data)
    {
        CurrentState = GameState.Playing;
    }

    private void OnGamePaused(object data)
    {
        CurrentState = GameState.Paused;
    }

    private void OnGameResumed(object data)
    {
        CurrentState = GameState.Playing;
    }

    public void PauseGame()
    {
        if (CurrentState != GameState.Playing) return;
        Time.timeScale = 0f;
        CurrentState = GameState.Paused;
        EventManager.Instance.Notify(GameEvent.GamePaused);
    }

    public void ResumeGame()
    {
        if (CurrentState != GameState.Paused) return;
        Time.timeScale = 1f;
        CurrentState = GameState.Playing;
        EventManager.Instance.Notify(GameEvent.GameResumed);
    }

    private void OnDestroy()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.Unsubscribe(GameEvent.GameStarted, OnGameStarted);
            EventManager.Instance.Unsubscribe(GameEvent.GameOver, OnGameOver);
            EventManager.Instance.Unsubscribe(GameEvent.QuizTriggered, OnQuizTriggered);
            EventManager.Instance.Unsubscribe(GameEvent.QuizClosed, OnQuizClosed);
            EventManager.Instance.Unsubscribe(GameEvent.GamePaused, OnGamePaused);
            EventManager.Instance.Unsubscribe(GameEvent.GameResumed, OnGameResumed);
            EventManager.Instance.Unsubscribe(GameEvent.MajorSelected, OnMajorSelected);
        }
    }
}
