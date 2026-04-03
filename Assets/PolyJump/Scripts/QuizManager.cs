using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PolyJump.Scripts
{
    [Serializable]
    /// <summary>
    /// Mô tả vai trò chính của lớp QuizQuestion trong hệ thống PolyJump.
    /// </summary>
    public class QuizQuestion
    {
        public string q;
        public string[] a;
        public int correct;
    }

    [Serializable]
    /// <summary>
    /// Mô tả vai trò chính của lớp QuizQuestionCollection trong hệ thống PolyJump.
    /// </summary>
    public class QuizQuestionCollection
    {
        public QuizQuestion[] items;
    }

    /// <summary>
    /// Quản lý dữ liệu câu hỏi, hiển thị quiz, chấm đáp án và trả kết quả về game chính.
    /// </summary>
    public class QuizManager : MonoBehaviour
    {
        [Header("UI")]
        public GameObject panelQuiz;
        public Text questionText;
        public Button[] answerButtons = new Button[4];

        [Header("Config")]
        public string resourceFileName = "QuizData";

        [Header("Time Reward/Penalty")]
        [Tooltip("Số giây cộng/trừ khi trả lời đúng. Dương để cộng, âm để trừ.")]
        public float correctAnswerTimeDeltaSeconds = 5f;
        [Tooltip("Số giây cộng/trừ khi trả lời sai. Dương để cộng, âm để trừ.")]
        public float wrongAnswerTimeDeltaSeconds = -5f;

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
        private int[] _questionOrder = Array.Empty<int>();
        private int _questionOrderCursor;
        private int _lastQuestionIndex = -1;

        /// <summary>
        /// Khởi tạo tham chiếu, trạng thái ban đầu và các cấu hình cần thiết khi đối tượng được tạo.
        /// </summary>
        private void Awake()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            LoadQuestions();
            NormalizeFeedbackVietnameseText();
            BindAnswerButtons();

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (questionText != null)
            {
                _questionDefaultAlignment = questionText.alignment;
                _hasCachedQuestionAlignment = true;
            }

            HideQuizPanel();
        }

        /// <summary>
        /// Chuẩn hóa Feedback Vietnamese Text phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void NormalizeFeedbackVietnameseText()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (string.IsNullOrWhiteSpace(correctFeedbackText) || string.Equals(correctFeedbackText, "Dung roi! +5s", StringComparison.Ordinal))
            {
                correctFeedbackText = "Đúng rồi! +5s";
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (string.IsNullOrWhiteSpace(wrongFeedbackText) || string.Equals(wrongFeedbackText, "Sai roi! -5s", StringComparison.Ordinal))
            {
                wrongFeedbackText = "Sai rồi! -5s";
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (string.IsNullOrWhiteSpace(correctHeadlineText) || string.Equals(correctHeadlineText, "CHINH XAC", StringComparison.Ordinal))
            {
                correctHeadlineText = "CHÍNH XÁC";
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (string.IsNullOrWhiteSpace(wrongHeadlineText) || string.Equals(wrongHeadlineText, "SAI ROI", StringComparison.Ordinal))
            {
                wrongHeadlineText = "SAI RỒI";
            }
        }

        /// <summary>
        /// Nạp Questions phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void LoadQuestions()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            TextAsset jsonAsset = Resources.Load<TextAsset>(resourceFileName);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (jsonAsset == null)
            {
                Debug.LogError("[PolyJump] Khong tim thay file quiz trong Resources: " + resourceFileName);
                _questions = Array.Empty<QuizQuestion>();
                return;
            }

            string wrapped = "{\"items\":" + jsonAsset.text + "}";
            QuizQuestionCollection data = JsonUtility.FromJson<QuizQuestionCollection>(wrapped);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (data == null || data.items == null)
            {
                _questions = Array.Empty<QuizQuestion>();
                return;
            }

            _questions = data.items;
            _questionOrder = Array.Empty<int>();
            _questionOrderCursor = 0;
            _lastQuestionIndex = -1;
        }

        /// <summary>
        /// Gắn Answer Buttons phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void BindAnswerButtons()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (answerButtons == null)
            {
                return;
            }

            // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
            for (int i = 0; i < answerButtons.Length; i++)
            {
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (answerButtons[i] == null)
                {
                    continue;
                }

                int captured = i;
                answerButtons[i].onClick.RemoveAllListeners();
                answerButtons[i].onClick.AddListener(() => OnAnswerButtonPressed(captured));
            }
        }

        /// <summary>
        /// Hiển thị Random Question phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        public void ShowRandomQuestion()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_resolveRoutine != null)
            {
                StopCoroutine(_resolveRoutine);
                _resolveRoutine = null;
            }

            _isResolvingAnswer = false;

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_questions.Length == 0)
            {
                HideQuizPanel();
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ResolveQuiz(0f);
                }
                return;
            }

            _currentQuestion = GetNextQuestion();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_currentQuestion == null)
            {
                HideQuizPanel();
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ResolveQuiz(0f);
                }
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (panelQuiz != null)
            {
                panelQuiz.SetActive(true);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (questionText != null)
            {
                RestoreQuestionTextAlignment();
                questionText.text = _currentQuestion.q;
            }

            // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
            for (int i = 0; i < answerButtons.Length; i++)
            {
                Button button = answerButtons[i];
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (button == null)
                {
                    continue;
                }

                bool visible = _currentQuestion.a != null && i < _currentQuestion.a.Length;
                button.gameObject.SetActive(visible);
                button.interactable = visible;

                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
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

        /// <summary>
        /// Ẩn Quiz Panel phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        public void HideQuizPanel()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_resolveRoutine != null)
            {
                StopCoroutine(_resolveRoutine);
                _resolveRoutine = null;
            }

            _isResolvingAnswer = false;

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (panelQuiz != null)
            {
                panelQuiz.SetActive(false);
            }

            RestoreQuestionTextAlignment();
        }

        /// <summary>
        /// Xử lý sự kiện khi người dùng nhấn nút/chức năng tương ứng trên giao diện.
        /// </summary>
        public void OnAnswerButtonPressed(int answerIndex)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_isResolvingAnswer)
            {
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_currentQuestion == null || _currentQuestion.a == null)
            {
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (answerIndex < 0 || answerIndex >= _currentQuestion.a.Length)
            {
                return;
            }

            bool isCorrect = answerIndex == _currentQuestion.correct;
            float timeDelta = isCorrect ? correctAnswerTimeDeltaSeconds : wrongAnswerTimeDeltaSeconds;

            _isResolvingAnswer = true;
            SetAnswersInteractable(false);
            ShowAnswerFeedback(isCorrect);
            _resolveRoutine = StartCoroutine(ResolveAfterDelay(timeDelta));
        }

        /// <summary>
        /// Xác định After Delay phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private IEnumerator ResolveAfterDelay(float timeDelta)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, feedbackDelaySeconds));

            _resolveRoutine = null;
            _isResolvingAnswer = false;

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResolveQuiz(timeDelta);
            }
        }

        /// <summary>
        /// Hiển thị Answer Feedback phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void ShowAnswerFeedback(bool isCorrect)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (questionText == null)
            {
                return;
            }

            string message = isCorrect ? correctFeedbackText : wrongFeedbackText;
            string headline = isCorrect ? correctHeadlineText : wrongHeadlineText;
            Color feedbackColor = isCorrect ? correctFeedbackColor : wrongFeedbackColor;
            string baseQuestion = _currentQuestion != null ? _currentQuestion.q : string.Empty;

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
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

        /// <summary>
        /// Thực hiện nghiệp vụ Restore Question Text Alignment theo ngữ cảnh sử dụng của script.
        /// </summary>
        private void RestoreQuestionTextAlignment()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (questionText == null || !_hasCachedQuestionAlignment)
            {
                return;
            }

            questionText.alignment = _questionDefaultAlignment;
        }

        /// <summary>
        /// Thiết lập Answers Interactable phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void SetAnswersInteractable(bool interactable)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (answerButtons == null)
            {
                return;
            }

            // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
            for (int i = 0; i < answerButtons.Length; i++)
            {
                Button button = answerButtons[i];
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (button == null || !button.gameObject.activeSelf)
                {
                    continue;
                }

                button.interactable = interactable;
            }
        }

        /// <summary>
        /// Lấy Next Question phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private QuizQuestion GetNextQuestion()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_questions == null || _questions.Length == 0)
            {
                return null;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_questionOrder == null || _questionOrder.Length != _questions.Length || _questionOrderCursor >= _questionOrder.Length)
            {
                RebuildQuestionOrder();
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_questionOrder == null || _questionOrder.Length == 0 || _questionOrderCursor >= _questionOrder.Length)
            {
                return null;
            }

            int questionIndex = _questionOrder[_questionOrderCursor];
            _questionOrderCursor++;
            _lastQuestionIndex = questionIndex;
            return _questions[questionIndex];
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Rebuild Question Order theo ngữ cảnh sử dụng của script.
        /// </summary>
        private void RebuildQuestionOrder()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            int count = _questions != null ? _questions.Length : 0;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (count <= 0)
            {
                _questionOrder = Array.Empty<int>();
                _questionOrderCursor = 0;
                return;
            }

            _questionOrder = new int[count];
            // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
            for (int i = 0; i < count; i++)
            {
                _questionOrder[i] = i;
            }

            // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
            for (int i = 0; i < count - 1; i++)
            {
                int swapIndex = UnityEngine.Random.Range(i, count);
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (swapIndex == i)
                {
                    continue;
                }

                int temp = _questionOrder[i];
                _questionOrder[i] = _questionOrder[swapIndex];
                _questionOrder[swapIndex] = temp;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (count > 1 && _lastQuestionIndex >= 0 && _questionOrder[0] == _lastQuestionIndex)
            {
                int temp = _questionOrder[0];
                _questionOrder[0] = _questionOrder[1];
                _questionOrder[1] = temp;
            }

            _questionOrderCursor = 0;
        }

        /// <summary>
        /// Gỡ đăng ký sự kiện và giải phóng liên kết tạm khi đối tượng bị tắt.
        /// </summary>
        private void OnDisable()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_resolveRoutine != null)
            {
                StopCoroutine(_resolveRoutine);
                _resolveRoutine = null;
            }

            _isResolvingAnswer = false;
        }
    }
}
