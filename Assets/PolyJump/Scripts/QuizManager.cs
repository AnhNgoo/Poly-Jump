using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PolyJump.Scripts
{
    [Serializable]
    public class QuizQuestion
    {
        public string q;
        public string[] a;
        public int correct;
    }

    [Serializable]
    public class QuizQuestionCollection
    {
        public QuizQuestion[] items;
    }

    public class QuizManager : MonoBehaviour
    {
        [Header("UI")]
        public GameObject panelQuiz;
        public Text questionText;
        public Button[] answerButtons = new Button[4];

        [Header("Config")]
        public string resourceFileName = "QuizData";

        [Header("Answer Feedback")]
        public string correctFeedbackText = "Đúng rồi! +5s";
        public string wrongFeedbackText = "Sai rồi! -5s";
        public float feedbackDelaySeconds = 1f;
        public Color correctFeedbackColor = new Color32(0x34, 0xC7, 0x59, 0xFF);
        public Color wrongFeedbackColor = new Color32(0xFF, 0x3B, 0x30, 0xFF);
        public string correctHeadlineText = "CHÍNH XÁC";
        public string wrongHeadlineText = "SAI RỒI";

        private QuizQuestion[] _questions = Array.Empty<QuizQuestion>();
        private QuizQuestion _currentQuestion;
        private bool _isResolvingAnswer;
        private Coroutine _resolveRoutine;
        private bool _hasCachedQuestionAlignment;
        private TextAnchor _questionDefaultAlignment;

        private void Awake()
        {
            LoadQuestions();
            NormalizeFeedbackVietnameseText();
            BindAnswerButtons();

            if (questionText != null)
            {
                _questionDefaultAlignment = questionText.alignment;
                _hasCachedQuestionAlignment = true;
            }

            HideQuizPanel();
        }

        private void NormalizeFeedbackVietnameseText()
        {
            if (string.IsNullOrWhiteSpace(correctFeedbackText) || string.Equals(correctFeedbackText, "Dung roi! +5s", StringComparison.Ordinal))
            {
                correctFeedbackText = "Đúng rồi! +5s";
            }

            if (string.IsNullOrWhiteSpace(wrongFeedbackText) || string.Equals(wrongFeedbackText, "Sai roi! -5s", StringComparison.Ordinal))
            {
                wrongFeedbackText = "Sai rồi! -5s";
            }

            if (string.IsNullOrWhiteSpace(correctHeadlineText) || string.Equals(correctHeadlineText, "CHINH XAC", StringComparison.Ordinal))
            {
                correctHeadlineText = "CHÍNH XÁC";
            }

            if (string.IsNullOrWhiteSpace(wrongHeadlineText) || string.Equals(wrongHeadlineText, "SAI ROI", StringComparison.Ordinal))
            {
                wrongHeadlineText = "SAI RỒI";
            }
        }

        private void LoadQuestions()
        {
            TextAsset jsonAsset = Resources.Load<TextAsset>(resourceFileName);
            if (jsonAsset == null)
            {
                Debug.LogError("[PolyJump] Khong tim thay file quiz trong Resources: " + resourceFileName);
                _questions = Array.Empty<QuizQuestion>();
                return;
            }

            string wrapped = "{\"items\":" + jsonAsset.text + "}";
            QuizQuestionCollection data = JsonUtility.FromJson<QuizQuestionCollection>(wrapped);
            if (data == null || data.items == null)
            {
                _questions = Array.Empty<QuizQuestion>();
                return;
            }

            _questions = data.items;
        }

        private void BindAnswerButtons()
        {
            if (answerButtons == null)
            {
                return;
            }

            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i] == null)
                {
                    continue;
                }

                int captured = i;
                answerButtons[i].onClick.RemoveAllListeners();
                answerButtons[i].onClick.AddListener(() => OnAnswerButtonPressed(captured));
            }
        }

        public void ShowRandomQuestion()
        {
            if (_resolveRoutine != null)
            {
                StopCoroutine(_resolveRoutine);
                _resolveRoutine = null;
            }

            _isResolvingAnswer = false;

            if (_questions.Length == 0)
            {
                HideQuizPanel();
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ResolveQuiz(0f);
                }
                return;
            }

            _currentQuestion = _questions[UnityEngine.Random.Range(0, _questions.Length)];

            if (panelQuiz != null)
            {
                panelQuiz.SetActive(true);
            }

            if (questionText != null)
            {
                RestoreQuestionTextAlignment();
                questionText.text = _currentQuestion.q;
            }

            for (int i = 0; i < answerButtons.Length; i++)
            {
                Button button = answerButtons[i];
                if (button == null)
                {
                    continue;
                }

                bool visible = _currentQuestion.a != null && i < _currentQuestion.a.Length;
                button.gameObject.SetActive(visible);
                button.interactable = visible;

                if (visible)
                {
                    Text btnText = button.GetComponentInChildren<Text>();
                    if (btnText != null)
                    {
                        btnText.text = _currentQuestion.a[i];
                    }
                }
            }
        }

        public void HideQuizPanel()
        {
            if (_resolveRoutine != null)
            {
                StopCoroutine(_resolveRoutine);
                _resolveRoutine = null;
            }

            _isResolvingAnswer = false;

            if (panelQuiz != null)
            {
                panelQuiz.SetActive(false);
            }

            RestoreQuestionTextAlignment();
        }

        public void OnAnswerButtonPressed(int answerIndex)
        {
            if (_isResolvingAnswer)
            {
                return;
            }

            if (_currentQuestion == null || _currentQuestion.a == null)
            {
                return;
            }

            if (answerIndex < 0 || answerIndex >= _currentQuestion.a.Length)
            {
                return;
            }

            bool isCorrect = answerIndex == _currentQuestion.correct;
            float timeDelta = isCorrect ? 5f : -5f;

            _isResolvingAnswer = true;
            SetAnswersInteractable(false);
            ShowAnswerFeedback(isCorrect);
            _resolveRoutine = StartCoroutine(ResolveAfterDelay(timeDelta));
        }

        private IEnumerator ResolveAfterDelay(float timeDelta)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, feedbackDelaySeconds));

            _resolveRoutine = null;
            _isResolvingAnswer = false;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResolveQuiz(timeDelta);
            }
        }

        private void ShowAnswerFeedback(bool isCorrect)
        {
            if (questionText == null)
            {
                return;
            }

            string message = isCorrect ? correctFeedbackText : wrongFeedbackText;
            string headline = isCorrect ? correctHeadlineText : wrongHeadlineText;
            Color feedbackColor = isCorrect ? correctFeedbackColor : wrongFeedbackColor;
            string baseQuestion = _currentQuestion != null ? _currentQuestion.q : string.Empty;

            if (!_hasCachedQuestionAlignment)
            {
                _questionDefaultAlignment = questionText.alignment;
                _hasCachedQuestionAlignment = true;
            }

            questionText.alignment = TextAnchor.MiddleCenter;

            string hex = ColorUtility.ToHtmlStringRGB(feedbackColor);
            questionText.text =
                baseQuestion +
                "\n\n<size=52><b><color=#" + hex + ">" + headline + "</color></b></size>" +
                "\n<size=42><b><color=#" + hex + ">" + message + "</color></b></size>";
        }

        private void RestoreQuestionTextAlignment()
        {
            if (questionText == null || !_hasCachedQuestionAlignment)
            {
                return;
            }

            questionText.alignment = _questionDefaultAlignment;
        }

        private void SetAnswersInteractable(bool interactable)
        {
            if (answerButtons == null)
            {
                return;
            }

            for (int i = 0; i < answerButtons.Length; i++)
            {
                Button button = answerButtons[i];
                if (button == null || !button.gameObject.activeSelf)
                {
                    continue;
                }

                button.interactable = interactable;
            }
        }

        private void OnDisable()
        {
            if (_resolveRoutine != null)
            {
                StopCoroutine(_resolveRoutine);
                _resolveRoutine = null;
            }

            _isResolvingAnswer = false;
        }
    }
}
