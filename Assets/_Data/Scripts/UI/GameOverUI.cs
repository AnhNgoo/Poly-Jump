using UnityEngine;
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
        restartButton?.onClick.AddListener(OnRestartClicked);
        mainMenuButton?.onClick.AddListener(OnMainMenuClicked);

        EventManager.Instance.Subscribe(GameEvent.GameOver, OnGameOver);
    }

    private void OnGameOver(object data)
    {
        ShowResults();
        Open();
    }

    private void ShowResults()
    {
        var persistent = PersistentData.Instance;
        var dataManager = DataManager.Instance;

        int jumpScore = persistent.JumpScore;
        int correct = persistent.CorrectAnswers;
        int total = persistent.TotalQuestions;
        float percentage = persistent.CalculatePercentage();

        jumpScoreText.text = $"Platforms Passed: {jumpScore}";
        knowledgeScoreText.text = $"Correct Answers: {correct}/{total}";
        percentageText.text = $"Knowledge Score: {percentage:F1}%";

        dataManager.SavePlayerProgress(persistent.CurrentMajor, percentage);
    }

    private void OnRestartClicked()
    {
        Time.timeScale = 1f;
        PersistentData.Instance.ResetSession();
        EventManager.Instance.Notify(GameEvent.GameStarted);
    }

    private void OnMainMenuClicked()
    {
        Time.timeScale = 1f;
        PersistentData.Instance.ResetSession();
        UIManager.Instance.ChangeMenu(MenuType.MainMenu);
    }

    public override void Open(object data = null)
    {
        base.Open(data);
        transform.localScale = Vector3.zero;
        transform.DOScale(1f, scaleAnimDuration).SetEase(Ease.OutBack);
    }

    private void OnDestroy()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.Unsubscribe(GameEvent.GameOver, OnGameOver);
        }
    }
}
