using System;
using System.Collections;
using UnityEngine;
using Sirenix.OdinInspector;

public class QuizManager : Singleton<QuizManager>
{
    [SerializeField] private QuizUI quizUI;
    [SerializeField] private string currentMajor = "GameProgramming";
    [SerializeField] private float countdownDuration = 3f;

    private Question _currentQuestion;
    private bool _isWaitingForCountdown;
    private Action _onQuizEnded;

    protected override void LoadComponentRuntime()
    {
        EventManager.Instance.Subscribe(GameEvent.QuizTriggered, OnQuizTriggered);
    }

    private void OnQuizTriggered(object data)
    {
        TriggerQuiz();
    }

    public void SetMajor(string major)
    {
        currentMajor = major;
    }

    private void TriggerQuiz()
    {

        Time.timeScale = 0f;
        _currentQuestion = DataManager.Instance.GetRandomQuestion(currentMajor);
        if (_currentQuestion == null)
        {
            Debug.LogWarning("No questions available for major: " + currentMajor);
            ResumeGame();
            return;
        }

        PersistentData.Instance.TotalQuestions++;

        if (quizUI != null)
        {
            quizUI.ShowQuestion(_currentQuestion);
        }
        else
        {
            Debug.LogWarning("QuizUI not assigned in QuizManager!");
            ResumeGame();
        }
    }

    public void OnAnswerSelected(bool isCorrect)
    {
        ScoreManager.Instance.RecordAnswer(isCorrect);
        _isWaitingForCountdown = true;

        if (quizUI != null)
        {
            quizUI.StartCountdown(() =>
            {
                _isWaitingForCountdown = false;
                ResumeGame();
            });
        }
        else
        {
            ResumeGame();
        }
    }

    private void ResumeGame()
    {
        EventManager.Instance.Notify(GameEvent.QuizClosed);
    }

    public Question GetCurrentQuestion() => _currentQuestion;

    private void OnDestroy()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.Unsubscribe(GameEvent.QuizTriggered, OnQuizTriggered);
        }
    }
}
