using UnityEngine;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;
using TMPro;
using DG.Tweening;

public class GameOverUI : MenuBase
{
    public override MenuType menuType => MenuType.GameOverPanel;

    [Header("Score Display")]
    [SerializeField] private TMPro.TextMeshProUGUI jumpScoreText;
    [SerializeField] private TMPro.TextMeshProUGUI knowledgeScoreText;
    [SerializeField] private TMPro.TextMeshProUGUI percentageText;

    [Header("Buttons")]
    [SerializeField] private UnityEngine.UI.Button restartButton;
    [SerializeField] private UnityEngine.UI.Button mainMenuButton;

    [Header("Animation")]
    [SerializeField] private float scaleAnimDuration = 0.5f;

    protected override void LoadComponent()
    {
        if (jumpScoreText == null) jumpScoreText = transform.Find("ContentPanel/JumpScoreText")?.GetComponent<TMPro.TextMeshProUGUI>();
        if (knowledgeScoreText == null) knowledgeScoreText = transform.Find("ContentPanel/KnowledgeScoreText")?.GetComponent<TMPro.TextMeshProUGUI>();
        if (percentageText == null) percentageText = transform.Find("ContentPanel/PercentageText")?.GetComponent<TMPro.TextMeshProUGUI>();
        if (restartButton == null) restartButton = transform.Find("ContentPanel/RestartButton")?.GetComponent<UnityEngine.UI.Button>();
        if (mainMenuButton == null) mainMenuButton = transform.Find("ContentPanel/MainMenuButton")?.GetComponent<UnityEngine.UI.Button>();
    }

    protected override void LoadComponentRuntime()
    {
        if (jumpScoreText == null || knowledgeScoreText == null || percentageText == null
            || restartButton == null || mainMenuButton == null)
        {
            LoadComponent();
        }

        restartButton?.onClick.AddListener(OnRestartClicked);
        mainMenuButton?.onClick.AddListener(OnMainMenuClicked);

        EventManager.Instance.Subscribe(GameEvent.GameOver, OnGameOver);
    }

    private void OnGameOver(object data)
    {
        Open();
    }

    private void ShowResults()
    {
        if (jumpScoreText == null || knowledgeScoreText == null || percentageText == null)
        {
            Debug.LogWarning("GameOverUI missing score text references.");
            return;
        }

        var persistent = PersistentData.Instance;
        if (persistent == null)
        {
            Debug.LogWarning("PersistentData not ready when showing results.");
            return;
        }

        int jumpScore = persistent.JumpScore;
        int correct = persistent.CorrectAnswers;
        int totalAsked = persistent.TotalQuestions;
        int totalForDisplay = totalAsked;
        var quizManager = QuizManager.Instance;
        if (quizManager != null && quizManager.MaxQuestionsPerRun > 0)
            totalForDisplay = quizManager.MaxQuestionsPerRun;

        float percentage = totalForDisplay > 0
            ? (float)correct / totalForDisplay * 100f
            : 0f;

        jumpScoreText.text = $"Số bậc: {jumpScore}";
        knowledgeScoreText.text = $"Điểm kiến thức: {correct}/{totalForDisplay}";
        percentageText.text = $"Tỷ lệ kiến thức: {percentage:F1}%";

        var dataManager = DataManager.Instance;
        if (dataManager != null)
            dataManager.SavePlayerProgress(persistent.CurrentFaculty, persistent.CurrentMajor, percentage);
    }

    private void OnRestartClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        Time.timeScale = 1f;
        PersistentData.Instance.ResetSession();
        GameManager.PendingRestart = true;
        GameManager.PendingMajor = PersistentData.Instance.CurrentMajor;
        PlayerPrefs.SetInt("PendingRestart", 1);
        PlayerPrefs.SetString("PendingMajor", PersistentData.Instance.CurrentMajor);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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

    public override void Open(object data = null)
    {
        base.Open(data);
        transform.localScale = Vector3.zero;
        transform.DOScale(1f, scaleAnimDuration).SetEase(Ease.OutBack);
        AudioManager.Instance?.PlayGameOver();
        ShowResults();
    }

    private void OnDestroy()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.Unsubscribe(GameEvent.GameOver, OnGameOver);
        }
    }
}
