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
    }

    private void OnQuizTriggered(object data)
    {
        if (data is Question question)
        {
            ShowQuestion(question);
        }
    }

    public void ShowQuestion(Question question)
    {
        _currentQuestion = question;
        _hasAnswered = false;

        questionText.text = question.question;
        BuildAnswerButtons(question.options);
        ResetOptionColors();
        Open();
    }

    private void OnOptionSelected(int index)
    {
        if (_hasAnswered || _currentQuestion == null) return;
        _hasAnswered = true;

        bool isCorrect = index == _currentQuestion.correctIndex;

        HighlightAnswer(index, isCorrect);
        QuizManager.Instance.OnAnswerSelected(isCorrect);
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

    public void StartCountdown(System.Action onComplete)
    {
        StartCoroutine(CountdownCoroutine(onComplete));
    }

    private System.Collections.IEnumerator CountdownCoroutine(System.Action onComplete)
    {
        countdownPanel?.SetActive(true);
        int count = 3;
        countdownText.text = count.ToString();

        for (int i = count; i > 0; i--)
        {
            countdownText.text = i.ToString();
            countdownText.transform.localScale = Vector3.one * 1.5f;
            countdownText.transform.DOScale(1f, 0.3f).SetUpdate(true);
            yield return new WaitForSecondsRealtime(1f);
        }

        countdownPanel?.SetActive(false);
        Close();
        onComplete?.Invoke();
    }

    public override void Close()
    {
        base.Close();
        _currentQuestion = null;
        _hasAnswered = false;
    }

    private void OnDestroy()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.Unsubscribe(GameEvent.QuizTriggered, OnQuizTriggered);
        }
    }
}
