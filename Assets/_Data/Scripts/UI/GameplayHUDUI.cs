using UnityEngine;
using Sirenix.OdinInspector;
using TMPro;
using DG.Tweening;

public class GameplayHUDUI : MenuBase
{
    public override MenuType menuType => MenuType.GameplayHUD;

    [Header("Score Display")]
    [SerializeField] private TMPro.TextMeshProUGUI jumpScoreText;
    [SerializeField] private TMPro.TextMeshProUGUI knowledgeScoreText;

    [Header("Pause Button")]
    [SerializeField] private UnityEngine.UI.Button pauseButton;

    private bool _subscribed;

    protected override void LoadComponent()
    {
        if (jumpScoreText == null) jumpScoreText = FindTmpByName("JumpScoreText");
        if (knowledgeScoreText == null) knowledgeScoreText = FindTmpByName("KnowledgeScoreText");
        if (pauseButton == null) pauseButton = transform.Find("HUDCanvas/PauseButton")?.GetComponent<UnityEngine.UI.Button>();
    }

    protected override void LoadComponentRuntime()
    {
        pauseButton?.onClick.AddListener(OnPauseClicked);
        EnsureSubscriptions();
        UpdateScoreDisplay();
    }

    private void OnEnable()
    {
        EnsureSubscriptions();
        UpdateScoreDisplay();
    }

    private void OnDisable()
    {
        RemoveSubscriptions();
    }

    private void OnScoreChanged(object data)
    {
        if (PersistentData.Instance != null)
            PersistentData.Instance.JumpScore = ScoreManager.Instance.JumpScore;

        UpdateScoreDisplay();
    }

    private void OnQuizTriggered(object data)
    {
        // QuizManager gọi UIManager.ChangeMenu(QuizPanel) — không Close HUD ở đây để tránh tắt cả nhánh UI chứa QuizPanel.
    }

    private void OnQuizClosed(object data)
    {
        if (UIManager.Instance != null)
            UIManager.Instance.ChangeMenu(MenuType.GameplayHUD);
        else
            Open();
        UpdateScoreDisplay();
    }

    private void OnGameOver(object data)
    {
        Close();
    }

    private void OnGameStarted(object data)
    {
        Open();
        UpdateScoreDisplay();
    }

    private void UpdateScoreDisplay()
    {
        if (ScoreManager.Instance == null)
            return;

        if (jumpScoreText != null)
            jumpScoreText.text = $"Số bậc: {ScoreManager.Instance.JumpScore}";

        if (knowledgeScoreText != null)
            knowledgeScoreText.text = $"Điểm kiến thức: {ScoreManager.Instance.KnowledgeScore}";
    }

    private TMPro.TextMeshProUGUI FindTmpByName(string childName)
    {
        var tmps = GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
        for (int i = 0; i < tmps.Length; i++)
        {
            if (tmps[i].name == childName)
                return tmps[i];
        }
        return null;
    }

    private void OnPauseClicked()
    {
        Time.timeScale = 0f;
        UIManager.Instance.ChangeMenu(MenuType.PauseMenu);
    }

    public override void Open(object data = null)
    {
        base.Open(data);
        UpdateScoreDisplay();
    }

    private void OnDestroy()
    {
        RemoveSubscriptions();
    }

    private void EnsureSubscriptions()
    {
        if (_subscribed || EventManager.Instance == null) return;

        EventManager.Instance.Subscribe(GameEvent.ScoreChanged, OnScoreChanged);
        EventManager.Instance.Subscribe(GameEvent.QuizTriggered, OnQuizTriggered);
        EventManager.Instance.Subscribe(GameEvent.QuizClosed, OnQuizClosed);
        EventManager.Instance.Subscribe(GameEvent.GameOver, OnGameOver);
        EventManager.Instance.Subscribe(GameEvent.GameStarted, OnGameStarted);
        _subscribed = true;
    }

    private void RemoveSubscriptions()
    {
        if (!_subscribed || EventManager.Instance == null) return;

        EventManager.Instance.Unsubscribe(GameEvent.ScoreChanged, OnScoreChanged);
        EventManager.Instance.Unsubscribe(GameEvent.QuizTriggered, OnQuizTriggered);
        EventManager.Instance.Unsubscribe(GameEvent.QuizClosed, OnQuizClosed);
        EventManager.Instance.Unsubscribe(GameEvent.GameOver, OnGameOver);
        EventManager.Instance.Unsubscribe(GameEvent.GameStarted, OnGameStarted);
        _subscribed = false;
    }
}
