using UnityEngine;
using Sirenix.OdinInspector;

public class ScoreManager : Singleton<ScoreManager>
{
    public int JumpScore { get; private set; }
    public int KnowledgeScore { get; private set; }
    public bool IsQuizActive { get; private set; }

    [SerializeField, Tooltip("Mở quiz mỗi khi JumpScore chia hết cho giá trị này (ví dụ 20 → mở ở 20, 40, 60...)")]
    private int quizThreshold = 20;

    public int QuizThreshold => quizThreshold;

    [Header("Debug")]
    [SerializeField] private bool logScoreEvents = false;

    protected override void Awake()
    {
        base.Awake();
        ResetScore();
    }

    protected override void LoadComponentRuntime()
    {
        EventManager.Instance.Subscribe(GameEvent.QuizClosed, OnQuizClosed);
        EventManager.Instance.Subscribe(GameEvent.GameOver, OnGameOver);
    }

    /// <summary>
    /// Gọi trực tiếp từ PlayerController khi qua platform — không qua EventManager.
    /// Chỉ tăng điểm, rồi bắn ScoreChanged để HUD cập nhật.
    /// </summary>
    public void AddPlatformPass(int platformId)
    {
        if (IsQuizActive) return;

        JumpScore++;

        // UI: bắn event để GameplayHUDUI cập nhật text
        EventManager.Instance.Notify(GameEvent.ScoreChanged, JumpScore);

        if (quizThreshold > 0 && JumpScore % quizThreshold == 0)
            TriggerQuiz();
    }

    private void TriggerQuiz()
    {
        IsQuizActive = true;
        EventManager.Instance.Notify(GameEvent.QuizTriggered);
    }

    private void OnQuizClosed(object data)
    {
        IsQuizActive = false;
        Time.timeScale = 1f;
    }

    public void RecordAnswer(bool correct)
    {
        if (correct)
        {
            KnowledgeScore++;
            PersistentData.Instance.CorrectAnswers++;
        }
    }

    private void OnGameOver(object data)
    {
        enabled = false;
    }

    private void OnDestroy()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.Unsubscribe(GameEvent.QuizClosed, OnQuizClosed);
            EventManager.Instance.Unsubscribe(GameEvent.GameOver, OnGameOver);
        }
    }

    public void ResetScore()
    {
        JumpScore = 0;
        KnowledgeScore = 0;
        IsQuizActive = false;
        enabled = true;
    }
}
