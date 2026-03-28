using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;

public class QuizUI : MenuBase
{
    public override MenuType menuType => MenuType.QuizPanel;

    [Header("Question Display")]
    [SerializeField] private TMPro.TextMeshProUGUI questionText;

    [Header("Buttons")]
    [SerializeField] private Transform answersContainer;
    [SerializeField] private UnityEngine.UI.Button answerButtonPrefab;

    [Header("Countdown")]
    [SerializeField] private TMPro.TextMeshProUGUI countdownText;
    [SerializeField] private GameObject countdownPanel;

    private readonly System.Collections.Generic.List<UnityEngine.UI.Button> _optionButtons = new System.Collections.Generic.List<UnityEngine.UI.Button>();
    private UnityEngine.UI.ColorBlock _defaultColors;
    private bool _defaultColorsSet;
    private Question _currentQuestion;
    private bool _hasAnswered;
    private Coroutine _timerRoutine;
    private Coroutine _answerRoutine;

    protected override void LoadComponent()
    {
        if (answersContainer == null)
            answersContainer = transform.Find("ContentPanel/AnswerButtons") ?? transform.Find("ContentPanel");

        if (questionText == null)
            questionText = transform.Find("ContentPanel/QuestionText")?.GetComponent<TMPro.TextMeshProUGUI>();
        if (countdownText == null)
            countdownText = transform.Find("ContentPanel/CountdownText")?.GetComponent<TMPro.TextMeshProUGUI>();
        if (countdownPanel == null)
            countdownPanel = transform.Find("ContentPanel/CountdownPanel")?.gameObject;
    }

    protected override void LoadComponentRuntime()
    {
        EventManager.Instance.Subscribe(GameEvent.QuizTriggered, OnQuizTriggered);
        EventManager.Instance.Subscribe(GameEvent.QuizClosed, OnQuizClosed);
    }

    private void OnQuizTriggered(object data)
    {
        if (data is Question question)
        {
            ShowQuestion(question);
        }
    }

    private void OnQuizClosed(object data)
    {
        Close();
    }

    public void ShowQuestion(Question question)
    {
        _currentQuestion = question;
        _hasAnswered = false;

        ResetCountdownStyle();
        questionText.text = question.question;
        BuildAnswerButtons(question.options);
        ResetOptionColors();
        Open();
    }

    private void OnOptionSelected(int index)
    {
        if (_hasAnswered || _currentQuestion == null) return;
        _hasAnswered = true;

        CancelQuestionTimer();
        CancelAnswerRoutine();

        bool isCorrect = index == _currentQuestion.correctIndex;

        if (isCorrect)
            AudioManager.Instance?.PlayCorrect();
        else
            AudioManager.Instance?.PlayWrong();

        HighlightAnswer(index, isCorrect);
        QuizManager.Instance.OnAnswerSelected(isCorrect);

        ShowAnswerResult(isCorrect);
    }

    private void HighlightAnswer(int selectedIndex, bool isCorrect)
    {
        for (int i = 0; i < _optionButtons.Count; i++)
        {
            _optionButtons[i].interactable = false;

            if (i == _currentQuestion.correctIndex)
                SetOptionColor(i, new Color(0.2f, 0.8f, 0.2f, 1f));
            else if (i == selectedIndex && !isCorrect)
                SetOptionColor(i, new Color(0.8f, 0.2f, 0.2f, 1f));
        }
    }

    private void SetOptionColor(int index, Color color)
    {
        var colors = _optionButtons[index].colors;
        colors.highlightedColor = color;
        colors.pressedColor = color;
        _optionButtons[index].colors = colors;
    }

    private void ResetOptionColors()
    {
        if (_optionButtons.Count == 0) return;
        var defaultColors = _defaultColorsSet ? _defaultColors : _optionButtons[0].colors;
        for (int i = 0; i < _optionButtons.Count; i++)
        {
            _optionButtons[i].colors = defaultColors;
        }
    }

    private void BuildAnswerButtons(System.Collections.Generic.List<string> options)
    {
        ClearAnswerButtons();
        if (answerButtonPrefab == null || answersContainer == null || options == null) return;

        for (int i = 0; i < options.Count; i++)
        {
            var button = Instantiate(answerButtonPrefab, answersContainer);
            button.name = $"AnswerButton_{i}";
            BindButtonLabel(button, options[i]);
            int index = i;
            button.onClick.AddListener(() => OnOptionSelected(index));
            button.interactable = true;
            _optionButtons.Add(button);

            if (!_defaultColorsSet)
            {
                _defaultColors = button.colors;
                _defaultColorsSet = true;
            }
        }
    }

    private void BindButtonLabel(UnityEngine.UI.Button button, string label)
    {
        var tmp = button.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (tmp != null)
        {
            tmp.text = label;
        }
    }

    private void ClearAnswerButtons()
    {
        for (int i = 0; i < _optionButtons.Count; i++)
        {
            if (_optionButtons[i] != null)
                Destroy(_optionButtons[i].gameObject);
        }
        _optionButtons.Clear();
    }

    public void StartQuestionTimer(float durationSeconds)
    {
        CancelQuestionTimer();
        _timerRoutine = StartCoroutine(QuestionTimerCoroutine(durationSeconds));
    }

    public void CancelQuestionTimer()
    {
        if (_timerRoutine != null)
        {
            StopCoroutine(_timerRoutine);
            _timerRoutine = null;
        }
    }

    private void CancelAnswerRoutine()
    {
        if (_answerRoutine != null)
        {
            StopCoroutine(_answerRoutine);
            _answerRoutine = null;
        }
    }

    private System.Collections.IEnumerator QuestionTimerCoroutine(float durationSeconds)
    {
        countdownPanel?.SetActive(true);
        float remaining = Mathf.Max(0f, durationSeconds);

        while (remaining > 0f)
        {
            if (countdownText != null)
                countdownText.text = Mathf.CeilToInt(remaining).ToString();
            yield return new WaitForSecondsRealtime(1f);
            remaining -= 1f;
        }

        countdownPanel?.SetActive(false);
        Close();
        QuizManager.Instance.OnTimeExpired();
    }

    private void ShowAnswerResult(bool isCorrect)
    {
        if (!gameObject.activeInHierarchy)
        {
            Open();
        }

        if (countdownPanel != null)
            countdownPanel.SetActive(true);

        if (countdownText != null)
        {
            countdownText.text = isCorrect ? "Correct" : "Incorrect";
            countdownText.color = isCorrect ? new Color(0.2f, 0.8f, 0.2f, 1f) : new Color(0.9f, 0.2f, 0.2f, 1f);
        }

        _answerRoutine = StartCoroutine(AnswerResultCoroutine());
    }

    private System.Collections.IEnumerator AnswerResultCoroutine()
    {
        yield return new WaitForSecondsRealtime(3f);
        countdownPanel?.SetActive(false);
        Close();
        QuizManager.Instance.HandleAnswerResultComplete();
    }

    private void ResetCountdownStyle()
    {
        if (countdownText != null)
            countdownText.color = Color.white;
    }

    public override void Close()
    {
        base.Close();
        _currentQuestion = null;
        _hasAnswered = false;
        CancelQuestionTimer();
        CancelAnswerRoutine();
    }

    private void OnDestroy()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.Unsubscribe(GameEvent.QuizTriggered, OnQuizTriggered);
            EventManager.Instance.Unsubscribe(GameEvent.QuizClosed, OnQuizClosed);
        }
    }
}
