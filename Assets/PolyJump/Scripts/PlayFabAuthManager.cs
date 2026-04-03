using System.Collections.Generic;
using System.Collections;
using System;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PolyJump.Scripts
{
    [ExecuteAlways]
    /// <summary>
    /// Xử lý đăng ký, đăng nhập, lưu hồ sơ người chơi và gửi điểm lên PlayFab/CloudScript.
    /// </summary>
    public class PlayFabAuthManager : MonoBehaviour
    {
        [Header("PlayFab")]
        public string titleId = "141838";
        public string leaderboardStatisticName = "LeaderBoard_Normal";
        public string eventLeaderboardStatisticName = "LeaderBoard_Event";

        [Header("Register UI")]
        public TMP_InputField reg_Name;
        public TMP_InputField reg_Email;
        public TMP_InputField reg_Phone;
        public TMP_InputField reg_Pass;
        public Button Btn_ConfirmRegister;

        [Header("Login UI")]
        public TMP_InputField login_Email;
        public TMP_InputField login_Pass;
        public Button Btn_ConfirmLogin;

        [Header("Status UI")]
        public TMP_Text auth_Status;

        [Header("Menu Panels")]
        public GameObject panelAuth;
        public GameObject panelStart;
        public GameObject panelLogin;
        public GameObject panelRegister;

        [Header("Auth Tab Buttons")]
        public Button Btn_OpenRegister;
        public Button Btn_BackToLogin;

        [Header("Start Menu UI")]
        public TMP_Text txt_StartUserName;
        public Button Btn_Logout;

        private bool _isAuthenticated;
        private GameObject _panelLeaderboard;
        private bool _eventWindowLoaded;
        private bool _eventWindowFetching;
        private bool _eventWindowEnabled;
        private string _eventWindowStartRaw = string.Empty;
        private DateTime _eventWindowStartUtc = DateTime.MinValue;
        private DateTime _eventWindowEndUtc = DateTime.MinValue;
        private float _eventWindowLastFetchRealtime;
        private readonly List<Action<bool>> _pendingEventWindowCallbacks = new List<Action<bool>>();
        private bool _tabsInitialized;
        private Coroutine _statusHideCoroutine;
        private static bool _hasCachedLeaderboardHighscore;
        private static int _cachedLeaderboardHighscore;
        private static bool _isFetchingLeaderboardHighscore;
        private static readonly List<System.Action<int>> _pendingHighscoreCallbacks = new List<System.Action<int>>();

        private const float StatusAutoHideSeconds = 1f;
        private const string StartUserTextObjectName = "Txt_StartUserName";
        private const string StartLogoutButtonObjectName = "Btn_Logout";
        private const string ReplayPreserveKey = "PolyJump_Replay_PreserveSession";
        private const string ReplaySessionTicketKey = "PolyJump_Replay_ClientSessionTicket";
        private const string ReplayPlayFabIdKey = "PolyJump_Replay_PlayFabId";
        private const string ReplayEntityTokenKey = "PolyJump_Replay_EntityToken";
        private const string ReplayEntityIdKey = "PolyJump_Replay_EntityId";
        private const string ReplayEntityTypeKey = "PolyJump_Replay_EntityType";
        private const string ReplayTelemetryKey = "PolyJump_Replay_TelemetryKey";
        private const string ReplayRuntimeSessionKey = "PolyJump_Replay_RuntimeSession";
        private const string EventEnabledKey = "EventEnabled";
        private const string EventStartKey = "EventStart";
        private const string EventEndKey = "EventEnd";
        private const string EventStartUtcKey = "EventStartUtc";
        private const string EventEndUtcKey = "EventEndUtc";
        private const float EventWindowCacheSeconds = 30f;
        private static bool _preserveSessionOnNextStart;
        private static string _runtimeSessionId = Guid.NewGuid().ToString("N");
        private static readonly Regex GmailRegex = new Regex(@"^[a-z0-9._%+-]+@gmail\.com$", RegexOptions.Compiled);
        private static readonly Regex PhoneRegex = new Regex(@"^0\d{9}$", RegexOptions.Compiled);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        /// <summary>
        /// Thực hiện nghiệp vụ Reset Runtime Session theo ngữ cảnh sử dụng của script.
        /// </summary>
        private static void ResetRuntimeSession()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            _preserveSessionOnNextStart = false;
            _runtimeSessionId = Guid.NewGuid().ToString("N");
            _hasCachedLeaderboardHighscore = false;
            _cachedLeaderboardHighscore = 0;
            _isFetchingLeaderboardHighscore = false;
            _pendingHighscoreCallbacks.Clear();
        }

        /// <summary>
        /// Giữ lại Session For Next Scene Load phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        public static void PreserveSessionForNextSceneLoad()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            _preserveSessionOnNextStart = true;
            PlayerPrefs.SetInt(ReplayPreserveKey, 1);
            PlayerPrefs.SetString(ReplayRuntimeSessionKey, _runtimeSessionId);
            CaptureCurrentStaticAuthContext();
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Tiêu thụ Preserve Session Flag phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static bool ConsumePreserveSessionFlag()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            bool shouldPreserve = _preserveSessionOnNextStart;
            _preserveSessionOnNextStart = false;
            return shouldPreserve;
        }

        /// <summary>
        /// Đăng ký sự kiện và kích hoạt các liên kết runtime khi đối tượng được bật.
        /// </summary>
        private void OnEnable()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (Application.isPlaying)
            {
                return;
            }

            AutoBindUiReferences();
            EnsureTabButtons();
            EnsureStartMenuWidgets();
            LocalizeUiTexts();
            EnsureCenteredAuthLayout();
            EnsureUiInteractionSetup();
            ApplyEditorDefaultAuthState();
        }

        /// <summary>
        /// Khởi tạo tham chiếu, trạng thái ban đầu và các cấu hình cần thiết khi đối tượng được tạo.
        /// </summary>
        private void Awake()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            AutoBindUiReferences();
            LocalizeUiTexts();
            EnsureTabButtons();
            EnsureStartMenuWidgets();
            EnsureCenteredAuthLayout();
            EnsureUiInteractionSetup();

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!Application.isPlaying)
            {
                ApplyEditorDefaultAuthState();
                return;
            }

            EnsureTitleIdConfigured();
            WireUiEvents();
        }

        /// <summary>
        /// Thiết lập dữ liệu và liên kết cần dùng ngay trước khi vòng lặp gameplay bắt đầu.
        /// </summary>
        private IEnumerator Start()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (!Application.isPlaying)
            {
                yield break;
            }

            // Delay 1 frame so this gate runs after GameManager.Start refreshes menu panels.
            yield return null;

            bool preserveSession = ConsumePreserveSessionFlag() || ConsumeReplayPreserveFlag();
            bool hasCachedLogin = false;

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (preserveSession)
            {
                hasCachedLogin = TryRestoreAuthContextFromReplayCache();
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (hasCachedLogin)
            {
                _isAuthenticated = true;
                LoadAndShowCurrentUserName();
                EnsureLeaderboardHighscoreCached();
            }
            else
            {
                // Always require login when opening the game normally.
                _isAuthenticated = false;
                PlayFabClientAPI.ForgetAllCredentials();
                SetStartUserText(string.Empty);
                ShowLoginTab();
                ClearReplaySessionCache();
            }

            AutoBindUiReferences();
            EnsureTabButtons();
            EnsureStartMenuWidgets();
            EnsureUiInteractionSetup();
            WireUiEvents();
            EnsureCenteredAuthLayout();
            RefreshAuthGateUi();
        }

        /// <summary>
        /// Cập nhật hậu kỳ sau Update để đồng bộ hiển thị hoặc trạng thái phụ thuộc.
        /// </summary>
        private void LateUpdate()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (!Application.isPlaying)
            {
                return;
            }

            RefreshAuthGateUi();
        }

        /// <summary>
        /// Chuẩn hóa dữ liệu trong Editor khi giá trị Inspector thay đổi.
        /// </summary>
        private void OnValidate()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (Application.isPlaying)
            {
                return;
            }

            AutoBindUiReferences();
            EnsureTabButtons();
            EnsureStartMenuWidgets();
            LocalizeUiTexts();
            EnsureCenteredAuthLayout();
            EnsureUiInteractionSetup();
            ApplyEditorDefaultAuthState();
        }

        /// <summary>
        /// Đăng ký User phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        public void RegisterUser(string email, string password, string fullName, string phone)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            string trimmedName = TrimInput(fullName);
            string trimmedEmail = NormalizeEmail(email);
            string trimmedPassword = TrimInput(password);
            string trimmedPhone = TrimInput(phone);

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!ValidateRegisterInput(trimmedName, trimmedEmail, trimmedPassword, trimmedPhone))
            {
                return;
            }

            string resolvedTitleId = EnsureTitleIdConfigured();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (string.IsNullOrWhiteSpace(resolvedTitleId))
            {
                return;
            }

            InvalidateLeaderboardHighscoreCache();
            InvalidateEventWindowCache();

            string username = BuildUsernameFromName(trimmedName);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (string.IsNullOrWhiteSpace(username))
            {
                SetStatus("Tên người dùng không hợp lệ", StatusAutoHideSeconds);
                return;
            }

            var request = new RegisterPlayFabUserRequest
            {
                Email = trimmedEmail,
                Password = trimmedPassword,
                Username = username,
                DisplayName = trimmedName,
                TitleId = resolvedTitleId,
                RequireBothUsernameAndEmail = false
            };

            SetStatus("Đang đăng ký...", 0f);
            PlayFabClientAPI.RegisterPlayFabUser(request,
                result =>
                {
                    SaveAuthContextForReplay(result != null ? result.AuthenticationContext : null);

                    if (login_Email != null)
                    {
                        login_Email.text = trimmedEmail;
                    }

                    SetStatus("Đăng ký thành công", StatusAutoHideSeconds);
                    SaveExtraInfo(trimmedName, trimmedPhone, trimmedEmail, username);
                    MarkAuthenticatedAndOpenStartMenu(trimmedName);
                },
                HandleRegisterError);
        }

        /// <summary>
        /// Đăng nhập User phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        public void LoginUser(string email, string password)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            string normalizedEmail = NormalizeEmail(email);
            string trimmedPassword = TrimInput(password);

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!ValidateLoginInput(normalizedEmail, trimmedPassword))
            {
                return;
            }

            string resolvedTitleId = EnsureTitleIdConfigured();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (string.IsNullOrWhiteSpace(resolvedTitleId))
            {
                return;
            }

            InvalidateLeaderboardHighscoreCache();
            InvalidateEventWindowCache();

            PlayFabClientAPI.ForgetAllCredentials();

            var request = new LoginWithEmailAddressRequest
            {
                Email = normalizedEmail,
                Password = trimmedPassword,
                TitleId = resolvedTitleId
            };

            SetStatus("Đang đăng nhập...", 0f);
            PlayFabClientAPI.LoginWithEmailAddress(request,
                result =>
                {
                    SaveAuthContextForReplay(result != null ? result.AuthenticationContext : null);
                    SetStatus("Đăng nhập thành công", StatusAutoHideSeconds);
                    MarkAuthenticatedAndOpenStartMenu();
                },
                HandleLoginError);
        }

        /// <summary>
        /// Lấy Cached Leaderboard Highscore phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        public int GetCachedLeaderboardHighscore()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            return _hasCachedLeaderboardHighscore ? _cachedLeaderboardHighscore : 0;
        }

        /// <summary>
        /// Đảm bảo Leaderboard Highscore Cached phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        public void EnsureLeaderboardHighscoreCached(System.Action<int> onReady = null)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (!PlayFabClientAPI.IsClientLoggedIn())
            {
                InvalidateLeaderboardHighscoreCache();
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (onReady != null)
                {
                    onReady(0);
                }
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_hasCachedLeaderboardHighscore)
            {
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (onReady != null)
                {
                    onReady(_cachedLeaderboardHighscore);
                }
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (onReady != null)
            {
                _pendingHighscoreCallbacks.Add(onReady);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_isFetchingLeaderboardHighscore)
            {
                return;
            }

            _isFetchingLeaderboardHighscore = true;

            var getRequest = new GetPlayerStatisticsRequest
            {
                StatisticNames = new List<string> { leaderboardStatisticName }
            };

            PlayFabClientAPI.GetPlayerStatistics(getRequest,
                result =>
                {
                    _isFetchingLeaderboardHighscore = false;
                    int fetchedValue = ExtractLeaderboardHighscore(result);
                    UpdateLeaderboardHighscoreCache(fetchedValue, true);
                    FlushPendingHighscoreCallbacks(_cachedLeaderboardHighscore);
                },
                error =>
                {
                    _isFetchingLeaderboardHighscore = false;
                    Debug.LogError(error.GenerateErrorReport());
                    FlushPendingHighscoreCallbacks(GetCachedLeaderboardHighscore());
                });
        }

        /// <summary>
        /// Gửi Score phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        public void SubmitScore(int score)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            SubmitScore(score, null);
        }

        /// <summary>
        /// Gửi Score phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        public void SubmitScore(int score, System.Action<int> onHighscoreResolved)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            int finalScore = Mathf.Max(0, score);

            SubmitEventScoreWhenEventOpen(finalScore);

            EnsureLeaderboardHighscoreCached(cachedHighscore =>
            {
                int currentHighscore = Mathf.Max(0, cachedHighscore);
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (finalScore <= currentHighscore)
                {
                    if (onHighscoreResolved != null)
                    {
                        onHighscoreResolved(currentHighscore);
                    }
                    return;
                }

                SubmitStatisticValue(finalScore, success =>
                {
                    if (success)
                    {
                        UpdateLeaderboardHighscoreCache(finalScore, true);
                    }

                    if (onHighscoreResolved != null)
                    {
                        onHighscoreResolved(GetCachedLeaderboardHighscore());
                    }
                });
            });
        }

        /// <summary>
        /// Gửi Event Score When Event Open phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void SubmitEventScoreWhenEventOpen(int value)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (value <= 0)
            {
                return;
            }

            EnsureEventWindowStatus(isOpen =>
            {
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (!isOpen)
                {
                    Debug.Log("[PolyJump] Event is closed, skip SubmitEventScore.");
                    return;
                }

                string statisticName = ResolveEventStatisticNameForWindow();
                SubmitEventScoreViaCloudScript(value, statisticName);
            }, forceRefresh: true);
        }

        /// <summary>
        /// Đảm bảo Event Window Status phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void EnsureEventWindowStatus(Action<bool> onResolved, bool forceRefresh = false)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (!PlayFabClientAPI.IsClientLoggedIn())
            {
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (onResolved != null)
                {
                    onResolved(false);
                }
                return;
            }

            bool cacheFresh = _eventWindowLoaded
                && !forceRefresh
                && (Time.unscaledTime - _eventWindowLastFetchRealtime) < Mathf.Max(10f, EventWindowCacheSeconds);

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (cacheFresh)
            {
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (onResolved != null)
                {
                    onResolved(IsEventWindowOpenNow());
                }
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (onResolved != null)
            {
                _pendingEventWindowCallbacks.Add(onResolved);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_eventWindowFetching)
            {
                return;
            }

            _eventWindowFetching = true;

            var request = new GetTitleDataRequest
            {
                Keys = new List<string>
                {
                    EventEnabledKey,
                    EventStartKey,
                    EventEndKey,
                    EventStartUtcKey,
                    EventEndUtcKey
                }
            };

            PlayFabClientAPI.GetTitleData(
                request,
                result =>
                {
                    _eventWindowFetching = false;
                    Dictionary<string, string> data = result != null ? result.Data : null;

                    _eventWindowEnabled = ParseTitleDataBool(GetTitleDataValue(data, EventEnabledKey, "false"));
                    ResolveEventTimeFromTitleData(data, EventStartUtcKey, EventStartKey, out _eventWindowStartRaw, out _eventWindowStartUtc);
                    ResolveEventTimeFromTitleData(data, EventEndUtcKey, EventEndKey, out _, out _eventWindowEndUtc);

                    _eventWindowLoaded = true;
                    _eventWindowLastFetchRealtime = Time.unscaledTime;

                    FlushPendingEventWindowCallbacks(IsEventWindowOpenNow());
                },
                error =>
                {
                    _eventWindowFetching = false;
                    _eventWindowLoaded = true;
                    _eventWindowEnabled = false;
                    _eventWindowStartRaw = string.Empty;
                    _eventWindowStartUtc = DateTime.MinValue;
                    _eventWindowEndUtc = DateTime.MinValue;
                    _eventWindowLastFetchRealtime = Time.unscaledTime;

                    Debug.LogError(error.GenerateErrorReport());
                    FlushPendingEventWindowCallbacks(false);
                });
        }

        /// <summary>
        /// Gửi Event Score Via Cloud Script phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void SubmitEventScoreViaCloudScript(int value, string statisticName)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            var request = new ExecuteCloudScriptRequest
            {
                FunctionName = "SubmitEventScore",
                FunctionParameter = new Dictionary<string, object>
                {
                    { "score", value },
                    { "statisticName", statisticName }
                },
                GeneratePlayStreamEvent = true
            };

            PlayFabClientAPI.ExecuteCloudScript(
                request,
                result =>
                {
                    if (TryReadCloudScriptBoolean(result != null ? result.FunctionResult : null, "success", out bool cloudSuccess) && !cloudSuccess)
                    {
                        string reason = TryReadCloudScriptString(result != null ? result.FunctionResult : null, "message", out string message)
                            ? message
                            : "CloudScript rejected score by event rule.";
                        Debug.Log("[PolyJump] SubmitEventScore rejected by CloudScript: " + reason);
                        return;
                    }

                    if (ShouldFallbackEventSubmit(result, out string fallbackReason))
                    {
                        Debug.LogWarning("[PolyJump] SubmitEventScore fallback to direct statistic update: " + fallbackReason);
                        SubmitEventScoreDirect(value, statisticName);
                        return;
                    }

                    if (result != null && result.FunctionResult == null)
                    {
                        Debug.LogWarning("[PolyJump] SubmitEventScore CloudScript executed without return payload. Fallback to direct statistic update.");
                        SubmitEventScoreDirect(value, statisticName);
                        return;
                    }

                    string functionResult = result != null && result.FunctionResult != null
                        ? result.FunctionResult.ToString()
                        : "null";
                    Debug.Log("[PolyJump] SubmitEventScore result: " + functionResult);
                },
                error =>
                {
                    Debug.LogError(error.GenerateErrorReport());
                    SubmitEventScoreDirect(value, statisticName);
                });
        }

        /// <summary>
        /// Gửi Event Score Direct phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void SubmitEventScoreDirect(int value, string statisticName)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            SubmitStatisticValue(statisticName, value, success =>
            {
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (success)
                {
                    Debug.Log("[PolyJump] Event leaderboard updated directly: " + value + " (" + statisticName + ")");
                }
            });
        }

        /// <summary>
        /// Xác định Event Statistic Name For Window phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private string ResolveEventStatisticNameForWindow()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            string baseName = string.IsNullOrWhiteSpace(eventLeaderboardStatisticName)
                ? "LeaderBoard_Event"
                : eventLeaderboardStatisticName;

            string suffix = GetEventStatisticSuffix(_eventWindowStartRaw, _eventWindowStartUtc);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (string.IsNullOrWhiteSpace(suffix))
            {
                return baseName;
            }

            return baseName + "_" + suffix;
        }

        /// <summary>
        /// Lấy Event Statistic Suffix phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static string GetEventStatisticSuffix(string startRaw, DateTime startUtc)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (TryNormalizeEventSuffixFromRaw(startRaw, out string normalizedRaw))
            {
                return normalizedRaw;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (startUtc != DateTime.MinValue)
            {
                DateTime vn = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc).AddHours(7);
                return vn.ToString("HH:mm:ss-yyyy:MM:dd");
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!string.IsNullOrWhiteSpace(startRaw))
            {
                return startRaw.Trim();
            }

            return string.Empty;
        }

        /// <summary>
        /// Thử xử lý Normalize Event Suffix From Raw phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static bool TryNormalizeEventSuffixFromRaw(string raw, out string normalized)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            normalized = string.Empty;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            string text = raw.Trim();
            Match timeDate = Regex.Match(text, @"^\s*(\d{1,2})\D+(\d{1,2})\D+(\d{1,2})\s*-\s*(\d{4})\D+(\d{1,2})\D+(\d{1,2})\s*$");
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (timeDate.Success)
            {
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (!TryParseEventSuffixParts(
                    timeDate.Groups[1].Value,
                    timeDate.Groups[2].Value,
                    timeDate.Groups[3].Value,
                    timeDate.Groups[4].Value,
                    timeDate.Groups[5].Value,
                    timeDate.Groups[6].Value,
                    out normalized))
                {
                    return false;
                }

                return true;
            }

            Match dateTime = Regex.Match(text, @"^\s*(\d{4})\D+(\d{1,2})\D+(\d{1,2})\s*-\s*(\d{1,2})\D+(\d{1,2})\D+(\d{1,2})\s*$");
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!dateTime.Success)
            {
                return false;
            }

            return TryParseEventSuffixParts(
                dateTime.Groups[4].Value,
                dateTime.Groups[5].Value,
                dateTime.Groups[6].Value,
                dateTime.Groups[1].Value,
                dateTime.Groups[2].Value,
                dateTime.Groups[3].Value,
                out normalized);
        }

        /// <summary>
        /// Thử xử lý Parse Event Suffix Parts phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static bool TryParseEventSuffixParts(
            string hourText,
            string minuteText,
            string secondText,
            string yearText,
            string monthText,
            string dayText,
            out string normalized)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            normalized = string.Empty;

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!int.TryParse(hourText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int hour)
                || !int.TryParse(minuteText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int minute)
                || !int.TryParse(secondText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int second)
                || !int.TryParse(yearText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int year)
                || !int.TryParse(monthText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int month)
                || !int.TryParse(dayText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int day))
            {
                return false;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (hour < 0 || hour > 24)
            {
                return false;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (minute < 0 || minute > 59 || second < 0 || second > 59)
            {
                return false;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (hour == 24 && (minute != 0 || second != 0))
            {
                return false;
            }

            // Khối an toàn: thực thi tác vụ có khả năng phát sinh lỗi và sẽ xử lý ngoại lệ đi kèm.
            try
            {
                _ = new DateTime(year, month, day, hour == 24 ? 0 : hour, minute, second, DateTimeKind.Unspecified);
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }

            normalized = string.Format(
                CultureInfo.InvariantCulture,
                "{0:D2}:{1:D2}:{2:D2}-{3:D4}:{4:D2}:{5:D2}",
                hour,
                minute,
                second,
                year,
                month,
                day);
            return true;
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Should Fallback Event Submit theo ngữ cảnh sử dụng của script.
        /// </summary>
        private static bool ShouldFallbackEventSubmit(ExecuteCloudScriptResult result, out string reason)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            reason = string.Empty;

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (result == null)
            {
                reason = "ExecuteCloudScriptResult is null";
                return true;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (result.Error != null)
            {
                reason = BuildCloudScriptErrorDetails(result);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Xây dựng Cloud Script Error Details phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static string BuildCloudScriptErrorDetails(ExecuteCloudScriptResult result)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (result == null || result.Error == null)
            {
                return "CloudScript error (unknown details)";
            }

            var builder = new StringBuilder();
            builder.Append("CloudScript error: ").Append(result.Error.Error);

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!string.IsNullOrWhiteSpace(result.Error.Message))
            {
                builder.Append(" - ").Append(result.Error.Message.Trim());
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!string.IsNullOrWhiteSpace(result.Error.StackTrace))
            {
                builder.Append(" | stack: ").Append(result.Error.StackTrace.Trim());
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (result.Logs != null && result.Logs.Count > 0)
            {
                int count = Math.Min(3, result.Logs.Count);
                builder.Append(" | logs: ");
                // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
                for (int i = 0; i < count; i++)
                {
                    LogStatement log = result.Logs[i];
                    if (log == null)
                    {
                        continue;
                    }

                    if (i > 0)
                    {
                        builder.Append(" || ");
                    }

                    builder.Append("[")
                        .Append(string.IsNullOrWhiteSpace(log.Level) ? "Log" : log.Level)
                        .Append("] ")
                        .Append(string.IsNullOrWhiteSpace(log.Message) ? "(empty)" : log.Message.Trim());
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Thử xử lý Read Cloud Script Boolean phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static bool TryReadCloudScriptBoolean(object functionResult, string key, out bool value)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            value = false;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (functionResult == null || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            IDictionary<string, object> dict = functionResult as IDictionary<string, object>;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (dict == null || !dict.ContainsKey(key))
            {
                return false;
            }

            object raw = dict[key];
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (raw is bool rawBool)
            {
                value = rawBool;
                return true;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (raw is string rawString)
            {
                string normalized = rawString.Trim();
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (bool.TryParse(normalized, out bool parsed))
                {
                    value = parsed;
                    return true;
                }

                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (normalized == "1" || normalized == "0")
                {
                    value = normalized == "1";
                    return true;
                }

                return false;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (raw is int rawInt)
            {
                value = rawInt != 0;
                return true;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (raw is long rawLong)
            {
                value = rawLong != 0L;
                return true;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (raw is float rawFloat)
            {
                value = Mathf.Abs(rawFloat) > Mathf.Epsilon;
                return true;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (raw is double rawDouble)
            {
                value = Math.Abs(rawDouble) > double.Epsilon;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Thử xử lý Read Cloud Script String phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static bool TryReadCloudScriptString(object functionResult, string key, out string value)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            value = string.Empty;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (functionResult == null || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            IDictionary<string, object> dict = functionResult as IDictionary<string, object>;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (dict == null || !dict.ContainsKey(key) || dict[key] == null)
            {
                return false;
            }

            string text = dict[key].ToString();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            value = text.Trim();
            return true;
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Is Event Window Open Now theo ngữ cảnh sử dụng của script.
        /// </summary>
        private bool IsEventWindowOpenNow()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (!_eventWindowLoaded)
            {
                return false;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!_eventWindowEnabled)
            {
                return false;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_eventWindowStartUtc == DateTime.MinValue || _eventWindowEndUtc == DateTime.MinValue)
            {
                return false;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_eventWindowEndUtc < _eventWindowStartUtc)
            {
                return false;
            }

            DateTime now = DateTime.UtcNow;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (now < _eventWindowStartUtc)
            {
                return false;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (now > _eventWindowEndUtc)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Lấy Title Data Value phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static string GetTitleDataValue(Dictionary<string, string> data, string key, string fallback)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (data != null && data.ContainsKey(key) && !string.IsNullOrWhiteSpace(data[key]))
            {
                return data[key].Trim();
            }

            return fallback;
        }

        /// <summary>
        /// Phân tích Title Data Bool phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static bool ParseTitleDataBool(string value)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string normalized = value.Trim();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (bool.TryParse(normalized, out bool parsed))
            {
                return parsed;
            }

            return normalized == "1" || normalized.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Xác định Event Time From Title Data phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void ResolveEventTimeFromTitleData(
            Dictionary<string, string> data,
            string utcKey,
            string legacyKey,
            out string selectedRaw,
            out DateTime selectedUtc)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            selectedRaw = string.Empty;
            selectedUtc = DateTime.MinValue;

            string utcRaw = GetTitleDataValue(data, utcKey, string.Empty);
            string legacyRaw = GetTitleDataValue(data, legacyKey, string.Empty);

            DateTime utcParsed = ParseEventUtcTitleData(utcRaw);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (utcParsed != DateTime.MinValue)
            {
                selectedRaw = utcRaw;
                selectedUtc = utcParsed;
                return;
            }

            DateTime legacyParsed = ParseEventUtcTitleData(legacyRaw);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (legacyParsed != DateTime.MinValue)
            {
                selectedRaw = legacyRaw;
                selectedUtc = legacyParsed;
                return;
            }

            selectedRaw = !string.IsNullOrWhiteSpace(utcRaw) ? utcRaw : legacyRaw;
        }

        /// <summary>
        /// Phân tích Event Utc Title Data phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static DateTime ParseEventUtcTitleData(string value)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (string.IsNullOrWhiteSpace(value))
            {
                return DateTime.MinValue;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (TryParseVnEventTextToUtc(value, out DateTime parsedFromCustom))
            {
                return parsedFromCustom;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (DateTime.TryParseExact(
                value.Trim(),
                "HH:mm:ss-yyyy:MM:dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime vnTime))
            {
                DateTime utc = DateTime.SpecifyKind(vnTime, DateTimeKind.Unspecified).AddHours(-7);
                return DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (DateTime.TryParse(
                value.Trim(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTime parsed))
            {
                return parsed.ToUniversalTime();
            }

            return DateTime.MinValue;
        }

        /// <summary>
        /// Thử xử lý Parse Vn Event Text To Utc phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static bool TryParseVnEventTextToUtc(string value, out DateTime utc)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            utc = DateTime.MinValue;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string text = value.Trim();

            Match timeDate = Regex.Match(text, @"^\s*(\d{1,2})\D+(\d{1,2})\D+(\d{1,2})\s*-\s*(\d{4})\D+(\d{1,2})\D+(\d{1,2})\s*$");
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (timeDate.Success)
            {
                return TryBuildUtcFromParts(
                    timeDate.Groups[4].Value,
                    timeDate.Groups[5].Value,
                    timeDate.Groups[6].Value,
                    timeDate.Groups[1].Value,
                    timeDate.Groups[2].Value,
                    timeDate.Groups[3].Value,
                    out utc);
            }

            Match dateTime = Regex.Match(text, @"^\s*(\d{4})\D+(\d{1,2})\D+(\d{1,2})\s*-\s*(\d{1,2})\D+(\d{1,2})\D+(\d{1,2})\s*$");
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!dateTime.Success)
            {
                return false;
            }

            return TryBuildUtcFromParts(
                dateTime.Groups[1].Value,
                dateTime.Groups[2].Value,
                dateTime.Groups[3].Value,
                dateTime.Groups[4].Value,
                dateTime.Groups[5].Value,
                dateTime.Groups[6].Value,
                out utc);
        }

        /// <summary>
        /// Thử xử lý Build Utc From Parts phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static bool TryBuildUtcFromParts(
            string yearText,
            string monthText,
            string dayText,
            string hourText,
            string minuteText,
            string secondText,
            out DateTime utc)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            utc = DateTime.MinValue;

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!int.TryParse(hourText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int hour)
                || !int.TryParse(minuteText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int minute)
                || !int.TryParse(secondText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int second)
                || !int.TryParse(yearText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int year)
                || !int.TryParse(monthText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int month)
                || !int.TryParse(dayText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int day))
            {
                return false;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (hour < 0 || hour > 24)
            {
                return false;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (minute < 0 || minute > 59 || second < 0 || second > 59)
            {
                return false;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (hour == 24 && (minute != 0 || second != 0))
            {
                return false;
            }

            // Khối an toàn: thực thi tác vụ có khả năng phát sinh lỗi và sẽ xử lý ngoại lệ đi kèm.
            try
            {
                int normalizedHour = hour == 24 ? 0 : hour;
                DateTime vnTime = new DateTime(year, month, day, normalizedHour, minute, second, DateTimeKind.Unspecified);
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (hour == 24)
                {
                    vnTime = vnTime.AddDays(1);
                }

                DateTime utcTime = vnTime.AddHours(-7);
                utc = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Flush Pending Event Window Callbacks theo ngữ cảnh sử dụng của script.
        /// </summary>
        private void FlushPendingEventWindowCallbacks(bool isOpen)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_pendingEventWindowCallbacks.Count == 0)
            {
                return;
            }

            List<Action<bool>> callbacks = new List<Action<bool>>(_pendingEventWindowCallbacks);
            _pendingEventWindowCallbacks.Clear();

            // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
            for (int i = 0; i < callbacks.Count; i++)
            {
                Action<bool> callback = callbacks[i];
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (callback != null)
                {
                    callback(isOpen);
                }
            }
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Invalidate Event Window Cache theo ngữ cảnh sử dụng của script.
        /// </summary>
        private void InvalidateEventWindowCache()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            _eventWindowLoaded = false;
            _eventWindowFetching = false;
            _eventWindowEnabled = false;
            _eventWindowStartRaw = string.Empty;
            _eventWindowStartUtc = DateTime.MinValue;
            _eventWindowEndUtc = DateTime.MinValue;
            _eventWindowLastFetchRealtime = 0f;
            _pendingEventWindowCallbacks.Clear();
        }

        /// <summary>
        /// Gửi Statistic Value phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void SubmitStatisticValue(int value, System.Action<bool> onComplete)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            SubmitStatisticValue(leaderboardStatisticName, value, onComplete);
        }

        /// <summary>
        /// Gửi Statistic Value phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void SubmitStatisticValue(string statisticName, int value, System.Action<bool> onComplete)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (!PlayFabClientAPI.IsClientLoggedIn())
            {
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (onComplete != null)
                {
                    onComplete(false);
                }
                return;
            }

            var request = new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate>
                {
                    new StatisticUpdate
                    {
                        StatisticName = statisticName,
                        Value = value
                    }
                }
            };

            PlayFabClientAPI.UpdatePlayerStatistics(request,
                result =>
                {
                    Debug.Log("[PolyJump] Leaderboard updated: " + value);
                    if (onComplete != null)
                    {
                        onComplete(true);
                    }
                },
                error =>
                {
                    Debug.LogError(error.GenerateErrorReport());
                    if (onComplete != null)
                    {
                        onComplete(false);
                    }
                });
        }

        /// <summary>
        /// Xử lý sự kiện khi người dùng nhấn nút/chức năng tương ứng trên giao diện.
        /// </summary>
        public void OnRegisterButtonPressed()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            RegisterUser(
                reg_Email != null ? TrimInput(reg_Email.text) : string.Empty,
                reg_Pass != null ? TrimInput(reg_Pass.text) : string.Empty,
                reg_Name != null ? TrimInput(reg_Name.text) : string.Empty,
                reg_Phone != null ? TrimInput(reg_Phone.text) : string.Empty);
        }

        /// <summary>
        /// Xử lý sự kiện khi người dùng nhấn nút/chức năng tương ứng trên giao diện.
        /// </summary>
        public void OnLoginButtonPressed()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            LoginUser(
                login_Email != null ? TrimInput(login_Email.text) : string.Empty,
                login_Pass != null ? TrimInput(login_Pass.text) : string.Empty);
        }

        /// <summary>
        /// Lưu Extra Info phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void SaveExtraInfo(string fullName, string phone, string email, string username)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            var data = new Dictionary<string, string>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                data["UserName"] = fullName;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!string.IsNullOrWhiteSpace(phone))
            {
                data["Phone"] = phone;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!string.IsNullOrWhiteSpace(email))
            {
                data["Email"] = email;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (data.Count > 0)
            {
                var dataRequest = new UpdateUserDataRequest
                {
                    Data = data,
                    KeysToRemove = new List<string> { "Username" }
                };
                PlayFabClientAPI.UpdateUserData(dataRequest,
                    result => { Debug.Log("[PolyJump] Saved extra user data"); },
                    OnError);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                var displayRequest = new UpdateUserTitleDisplayNameRequest
                {
                    DisplayName = fullName
                };
                PlayFabClientAPI.UpdateUserTitleDisplayName(displayRequest,
                    result => { Debug.Log("[PolyJump] DisplayName updated"); },
                    OnError);
            }
        }

        /// <summary>
        /// Xác thực Register Input phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private bool ValidateRegisterInput(string fullName, string email, string password, string phone)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (string.IsNullOrWhiteSpace(fullName))
            {
                SetStatus("Tên người dùng không được bỏ trống", StatusAutoHideSeconds);
                return false;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (string.IsNullOrWhiteSpace(email))
            {
                SetStatus("Email không được bỏ trống", StatusAutoHideSeconds);
                return false;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!IsValidGmail(email))
            {
                SetStatus("Email phải có dạng @gmail.com", StatusAutoHideSeconds);
                return false;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (string.IsNullOrWhiteSpace(phone))
            {
                SetStatus("Số điện thoại không được bỏ trống", StatusAutoHideSeconds);
                return false;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!IsValidPhoneNumber(phone))
            {
                SetStatus("Số điện thoại không hợp lệ (10 số, bắt đầu bằng 0)", StatusAutoHideSeconds);
                return false;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6 || password.Length > 100)
            {
                SetStatus("Mật khẩu phải từ 6 đến 100 ký tự", StatusAutoHideSeconds);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Xác thực Login Input phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private bool ValidateLoginInput(string email, string password)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (string.IsNullOrWhiteSpace(email))
            {
                SetStatus("Email không được bỏ trống", StatusAutoHideSeconds);
                return false;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!IsValidGmail(email))
            {
                SetStatus("Email phải có dạng @gmail.com", StatusAutoHideSeconds);
                return false;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (string.IsNullOrWhiteSpace(password))
            {
                SetStatus("Mật khẩu không được bỏ trống", StatusAutoHideSeconds);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Xử lý Register Error phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void HandleRegisterError(PlayFabError error)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (error == null)
            {
                SetStatus("Đăng ký thất bại", StatusAutoHideSeconds);
                return;
            }

            // Khối phân nhánh: chọn nhánh xử lý theo từng trường hợp cụ thể.
            switch (error.Error)
            {
                case PlayFabErrorCode.UsernameNotAvailable:
                case PlayFabErrorCode.DuplicateUsername:
                case PlayFabErrorCode.NameNotAvailable:
                    SetStatus("Tên người dùng đã tồn tại", StatusAutoHideSeconds);
                    break;
                case PlayFabErrorCode.EmailAddressNotAvailable:
                case PlayFabErrorCode.DuplicateEmail:
                    SetStatus("Email đã tồn tại", StatusAutoHideSeconds);
                    break;
                case PlayFabErrorCode.InvalidEmailAddress:
                    SetStatus("Email phải có dạng @gmail.com", StatusAutoHideSeconds);
                    break;
                default:
                    OnError(error);
                    return;
            }

            Debug.Log("[PolyJump] Handled register validation from PlayFab: " + error.Error);
            RefreshAuthGateUi();
        }

        /// <summary>
        /// Xử lý Login Error phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void HandleLoginError(PlayFabError error)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (error == null)
            {
                SetStatus("Đăng nhập thất bại", StatusAutoHideSeconds);
                return;
            }

            // Khối phân nhánh: chọn nhánh xử lý theo từng trường hợp cụ thể.
            switch (error.Error)
            {
                case PlayFabErrorCode.AccountNotFound:
                    SetStatus("Email chưa tồn tại trên hệ thống", StatusAutoHideSeconds);
                    break;
                case PlayFabErrorCode.InvalidEmailOrPassword:
                case PlayFabErrorCode.InvalidUsernameOrPassword:
                    SetStatus("Email hoặc mật khẩu không đúng", StatusAutoHideSeconds);
                    break;
                default:
                    OnError(error);
                    return;
            }

            Debug.Log("[PolyJump] Handled login validation from PlayFab: " + error.Error);
            RefreshAuthGateUi();
        }

        /// <summary>
        /// Xử lý callback sự kiện hệ thống hoặc gameplay phát sinh trong vòng đời đối tượng.
        /// </summary>
        private void OnError(PlayFabError error)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            string report = error != null ? error.GenerateErrorReport() : "Unknown PlayFab error";
            Debug.LogError(report);
            string msg = error != null && !string.IsNullOrWhiteSpace(error.ErrorMessage)
                ? error.ErrorMessage
                : "Không xác định";
            SetStatus("Lỗi: " + msg, StatusAutoHideSeconds);
            RefreshAuthGateUi();
        }

        /// <summary>
        /// Đánh dấu Authenticated And Open Start Menu phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void MarkAuthenticatedAndOpenStartMenu()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            _isAuthenticated = true;
            LoadAndShowCurrentUserName();
            NormalizeUserNamePlayerData();
            EnsureLeaderboardHighscoreCached();
            RefreshAuthGateUi();
        }

        /// <summary>
        /// Đánh dấu Authenticated And Open Start Menu phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void MarkAuthenticatedAndOpenStartMenu(string userName)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            _isAuthenticated = true;
            SetStartUserText(userName);
            NormalizeUserNamePlayerData(userName);
            EnsureLeaderboardHighscoreCached();
            RefreshAuthGateUi();
        }

        /// <summary>
        /// Chuẩn hóa User Name Player Data phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void NormalizeUserNamePlayerData(string canonicalName = null)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (!PlayFabClientAPI.IsClientLoggedIn())
            {
                return;
            }

            var request = new UpdateUserDataRequest
            {
                KeysToRemove = new List<string> { "Username" }
            };

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!string.IsNullOrWhiteSpace(canonicalName))
            {
                request.Data = new Dictionary<string, string>
                {
                    { "UserName", canonicalName }
                };
            }

            PlayFabClientAPI.UpdateUserData(request,
                result => { },
                error => { Debug.LogWarning("[PolyJump] Failed to normalize UserName key: " + error.ErrorMessage); });
        }

        /// <summary>
        /// Xử lý sự kiện khi người dùng nhấn nút/chức năng tương ứng trên giao diện.
        /// </summary>
        public void OnLogoutPressed()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (Application.isPlaying)
            {
                PlayFabClientAPI.ForgetAllCredentials();
            }

            _isAuthenticated = false;
            InvalidateLeaderboardHighscoreCache();
            InvalidateEventWindowCache();
            ClearReplaySessionCache();
            SetStartUserText(string.Empty);
            ShowLoginTab();
            RefreshAuthGateUi();
            SetStatus("Đã đăng xuất", StatusAutoHideSeconds);
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Extract Leaderboard Highscore theo ngữ cảnh sử dụng của script.
        /// </summary>
        private int ExtractLeaderboardHighscore(GetPlayerStatisticsResult result)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            int highscore = 0;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (result == null || result.Statistics == null)
            {
                return highscore;
            }

            // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
            for (int i = 0; i < result.Statistics.Count; i++)
            {
                StatisticValue stat = result.Statistics[i];
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (stat == null || stat.StatisticName != leaderboardStatisticName)
                {
                    continue;
                }

                highscore = Mathf.Max(0, stat.Value);
                break;
            }

            return highscore;
        }

        /// <summary>
        /// Cập nhật Leaderboard Highscore Cache phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void UpdateLeaderboardHighscoreCache(int value, bool hasValue)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            _cachedLeaderboardHighscore = Mathf.Max(0, value);
            _hasCachedLeaderboardHighscore = hasValue;
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Invalidate Leaderboard Highscore Cache theo ngữ cảnh sử dụng của script.
        /// </summary>
        private static void InvalidateLeaderboardHighscoreCache()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            _cachedLeaderboardHighscore = 0;
            _hasCachedLeaderboardHighscore = false;
            _isFetchingLeaderboardHighscore = false;
            _pendingHighscoreCallbacks.Clear();
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Flush Pending Highscore Callbacks theo ngữ cảnh sử dụng của script.
        /// </summary>
        private static void FlushPendingHighscoreCallbacks(int highscore)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_pendingHighscoreCallbacks.Count == 0)
            {
                return;
            }

            // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
            for (int i = 0; i < _pendingHighscoreCallbacks.Count; i++)
            {
                System.Action<int> callback = _pendingHighscoreCallbacks[i];
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (callback != null)
                {
                    callback(highscore);
                }
            }

            _pendingHighscoreCallbacks.Clear();
        }

        /// <summary>
        /// Lưu Auth Context For Replay phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void SaveAuthContextForReplay(PlayFabAuthenticationContext context)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (context == null)
            {
                CaptureCurrentStaticAuthContext();
                PlayerPrefs.Save();
                return;
            }

            SaveStringOrDelete(ReplaySessionTicketKey, context.ClientSessionTicket);
            SaveStringOrDelete(ReplayPlayFabIdKey, context.PlayFabId);
            SaveStringOrDelete(ReplayEntityTokenKey, context.EntityToken);
            SaveStringOrDelete(ReplayEntityIdKey, context.EntityId);
            SaveStringOrDelete(ReplayEntityTypeKey, context.EntityType);
            SaveStringOrDelete(ReplayTelemetryKey, context.TelemetryKey);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Thu thập Current Static Auth Context phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void CaptureCurrentStaticAuthContext()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            PlayFabAuthenticationContext context = PlayFabSettings.staticPlayer;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (context == null)
            {
                return;
            }

            SaveStringOrDelete(ReplaySessionTicketKey, context.ClientSessionTicket);
            SaveStringOrDelete(ReplayPlayFabIdKey, context.PlayFabId);
            SaveStringOrDelete(ReplayEntityTokenKey, context.EntityToken);
            SaveStringOrDelete(ReplayEntityIdKey, context.EntityId);
            SaveStringOrDelete(ReplayEntityTypeKey, context.EntityType);
            SaveStringOrDelete(ReplayTelemetryKey, context.TelemetryKey);
        }

        /// <summary>
        /// Tiêu thụ Replay Preserve Flag phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static bool ConsumeReplayPreserveFlag()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            bool shouldPreserve = PlayerPrefs.GetInt(ReplayPreserveKey, 0) == 1;
            string replaySession = PlayerPrefs.GetString(ReplayRuntimeSessionKey, string.Empty);
            bool sameRuntime = !string.IsNullOrWhiteSpace(replaySession) && replaySession == _runtimeSessionId;

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (shouldPreserve)
            {
                PlayerPrefs.DeleteKey(ReplayPreserveKey);
                PlayerPrefs.DeleteKey(ReplayRuntimeSessionKey);
                PlayerPrefs.Save();
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!shouldPreserve)
            {
                return false;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!sameRuntime)
            {
                ClearReplaySessionCache();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Thử xử lý Restore Auth Context From Replay Cache phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static bool TryRestoreAuthContextFromReplayCache()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (PlayFabClientAPI.IsClientLoggedIn())
            {
                return true;
            }

            string ticket = PlayerPrefs.GetString(ReplaySessionTicketKey, string.Empty);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (string.IsNullOrWhiteSpace(ticket))
            {
                return false;
            }

            PlayFabAuthenticationContext context = PlayFabSettings.staticPlayer;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (context == null)
            {
                return false;
            }

            context.ClientSessionTicket = ticket;
            context.PlayFabId = PlayerPrefs.GetString(ReplayPlayFabIdKey, string.Empty);
            context.EntityToken = PlayerPrefs.GetString(ReplayEntityTokenKey, string.Empty);
            context.EntityId = PlayerPrefs.GetString(ReplayEntityIdKey, string.Empty);
            context.EntityType = PlayerPrefs.GetString(ReplayEntityTypeKey, string.Empty);
            context.TelemetryKey = PlayerPrefs.GetString(ReplayTelemetryKey, string.Empty);

            return PlayFabClientAPI.IsClientLoggedIn();
        }

        /// <summary>
        /// Xóa Replay Session Cache phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void ClearReplaySessionCache()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            PlayerPrefs.DeleteKey(ReplayPreserveKey);
            PlayerPrefs.DeleteKey(ReplayRuntimeSessionKey);
            PlayerPrefs.DeleteKey(ReplaySessionTicketKey);
            PlayerPrefs.DeleteKey(ReplayPlayFabIdKey);
            PlayerPrefs.DeleteKey(ReplayEntityTokenKey);
            PlayerPrefs.DeleteKey(ReplayEntityIdKey);
            PlayerPrefs.DeleteKey(ReplayEntityTypeKey);
            PlayerPrefs.DeleteKey(ReplayTelemetryKey);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Lưu String Or Delete phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void SaveStringOrDelete(string key, string value)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (string.IsNullOrWhiteSpace(value))
            {
                PlayerPrefs.DeleteKey(key);
                return;
            }

            PlayerPrefs.SetString(key, value);
        }

        /// <summary>
        /// Làm mới Auth Gate Ui phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void RefreshAuthGateUi()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (panelAuth == null || panelStart == null)
            {
                AutoBindUiReferences();
            }

            bool loggedIn = _isAuthenticated;

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (panelAuth != null)
            {
                panelAuth.SetActive(!loggedIn);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (panelStart != null)
            {
                bool shouldShowStart = loggedIn;
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (GameManager.Instance != null)
                {
                    shouldShowStart = shouldShowStart && GameManager.Instance.CurrentState == GameState.Menu;
                }

                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (shouldShowStart)
                {
                    shouldShowStart = !IsLeaderboardPanelVisible();
                }

                panelStart.SetActive(shouldShowStart);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!loggedIn && !_tabsInitialized)
            {
                ShowLoginTab();
            }
        }

        /// <summary>
        /// Áp dụng Editor Default Auth State phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void ApplyEditorDefaultAuthState()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            _isAuthenticated = false;

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (panelAuth != null)
            {
                panelAuth.SetActive(true);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (panelStart != null)
            {
                panelStart.SetActive(false);
            }

            SetStartUserText(string.Empty);
            ShowLoginTab();
        }

        /// <summary>
        /// Đảm bảo Title Id Configured phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private string EnsureTitleIdConfigured()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            string resolvedTitleId = string.IsNullOrWhiteSpace(titleId)
                ? PlayFabSettings.TitleId
                : titleId.Trim();

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (string.IsNullOrWhiteSpace(resolvedTitleId))
            {
                SetStatus("Thiếu TitleId PlayFab", StatusAutoHideSeconds);
                Debug.LogError("[PolyJump] PlayFab TitleId is empty.");
                return string.Empty;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (PlayFabSettings.TitleId != resolvedTitleId)
            {
                PlayFabSettings.TitleId = resolvedTitleId;
            }

            return resolvedTitleId;
        }

        /// <summary>
        /// Chuẩn hóa Email phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static string NormalizeEmail(string email)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            return string.IsNullOrWhiteSpace(email)
                ? string.Empty
                : email.Trim().ToLowerInvariant();
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Trim Input theo ngữ cảnh sử dụng của script.
        /// </summary>
        private static string TrimInput(string value)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        /// <summary>
        /// Xây dựng Username From Name phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static string BuildUsernameFromName(string fullName)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            string source = TrimInput(fullName);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (string.IsNullOrWhiteSpace(source))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(source.Length);
            // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (char.IsWhiteSpace(c))
                {
                    sb.Append('_');
                }
                else if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                }
                else if (c == '_' || c == '.' || c == '-')
                {
                    sb.Append(c);
                }
            }

            string username = sb.ToString().Trim('_');
            // Khối lặp điều kiện: tiếp tục xử lý cho đến khi đạt điều kiện dừng.
            while (username.Contains("__"))
            {
                username = username.Replace("__", "_");
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (username.Length < 3)
            {
                return string.Empty;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (username.Length > 20)
            {
                username = username.Substring(0, 20);
            }

            return username;
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Is Valid Gmail theo ngữ cảnh sử dụng của script.
        /// </summary>
        private static bool IsValidGmail(string email)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            return GmailRegex.IsMatch(email);
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Is Valid Phone Number theo ngữ cảnh sử dụng của script.
        /// </summary>
        private static bool IsValidPhoneNumber(string phone)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            return PhoneRegex.IsMatch(phone);
        }

        /// <summary>
        /// Thiết lập Status phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void SetStatus(string message, float hideAfterSeconds)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (auth_Status != null)
            {
                auth_Status.text = message;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!Application.isPlaying)
            {
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_statusHideCoroutine != null)
            {
                StopCoroutine(_statusHideCoroutine);
                _statusHideCoroutine = null;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (hideAfterSeconds > 0f)
            {
                _statusHideCoroutine = StartCoroutine(ClearStatusAfterDelay(hideAfterSeconds));
            }
        }

        /// <summary>
        /// Xóa Status After Delay phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private IEnumerator ClearStatusAfterDelay(float seconds)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            yield return new WaitForSeconds(seconds);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (auth_Status != null)
            {
                auth_Status.text = string.Empty;
            }

            _statusHideCoroutine = null;
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Auto Bind Ui References theo ngữ cảnh sử dụng của script.
        /// </summary>
        private void AutoBindUiReferences()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (reg_Name == null)
            {
                reg_Name = FindTmpInput("reg_Name");
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (reg_Email == null)
            {
                reg_Email = FindTmpInput("reg_Email");
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (reg_Phone == null)
            {
                reg_Phone = FindTmpInput("reg_Phone");
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (reg_Pass == null)
            {
                reg_Pass = FindTmpInput("reg_Pass");
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (login_Email == null)
            {
                login_Email = FindTmpInput("login_Email");
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (login_Pass == null)
            {
                login_Pass = FindTmpInput("login_Pass");
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (Btn_ConfirmRegister == null)
            {
                Btn_ConfirmRegister = FindButton("Btn_ConfirmRegister");
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (Btn_ConfirmLogin == null)
            {
                Btn_ConfirmLogin = FindButton("Btn_ConfirmLogin");
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (auth_Status == null)
            {
                auth_Status = FindTmpText("auth_Status");
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (panelAuth == null)
            {
                panelAuth = GameObject.Find("Panel_Auth");
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (panelStart == null)
            {
                panelStart = GameObject.Find("Panel_Start");
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (panelLogin == null)
            {
                GameObject obj = GameObject.Find("Panel_Login");
                panelLogin = obj;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (panelRegister == null)
            {
                GameObject obj = GameObject.Find("Panel_Register");
                panelRegister = obj;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (Btn_OpenRegister == null)
            {
                Btn_OpenRegister = FindButton("Btn_OpenRegister");
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (Btn_BackToLogin == null)
            {
                Btn_BackToLogin = FindButton("Btn_BackToLogin");
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_panelLeaderboard == null)
            {
                _panelLeaderboard = GameObject.Find("Panel_Leaderboard");
            }
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Is Leaderboard Panel Visible theo ngữ cảnh sử dụng của script.
        /// </summary>
        private bool IsLeaderboardPanelVisible()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_panelLeaderboard == null)
            {
                _panelLeaderboard = GameObject.Find("Panel_Leaderboard");
            }

            return _panelLeaderboard != null && _panelLeaderboard.activeInHierarchy;
        }

        /// <summary>
        /// Liên kết Ui Events phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void WireUiEvents()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (Btn_ConfirmRegister != null)
            {
                Btn_ConfirmRegister.onClick.RemoveListener(OnRegisterButtonPressed);
                Btn_ConfirmRegister.onClick.AddListener(OnRegisterButtonPressed);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (Btn_ConfirmLogin != null)
            {
                Btn_ConfirmLogin.onClick.RemoveListener(OnLoginButtonPressed);
                Btn_ConfirmLogin.onClick.AddListener(OnLoginButtonPressed);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (Btn_OpenRegister != null)
            {
                Btn_OpenRegister.onClick.RemoveListener(OnOpenRegisterPressed);
                Btn_OpenRegister.onClick.AddListener(OnOpenRegisterPressed);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (Btn_BackToLogin != null)
            {
                Btn_BackToLogin.onClick.RemoveListener(OnBackToLoginPressed);
                Btn_BackToLogin.onClick.AddListener(OnBackToLoginPressed);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (Btn_Logout != null)
            {
                Btn_Logout.onClick.RemoveListener(OnLogoutPressed);
                Btn_Logout.onClick.AddListener(OnLogoutPressed);
            }
        }

        /// <summary>
        /// Xử lý sự kiện khi người dùng nhấn nút/chức năng tương ứng trên giao diện.
        /// </summary>
        public void OnOpenRegisterPressed()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            ShowRegisterTab();
        }

        /// <summary>
        /// Xử lý sự kiện khi người dùng nhấn nút/chức năng tương ứng trên giao diện.
        /// </summary>
        public void OnBackToLoginPressed()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            ShowLoginTab();
        }

        /// <summary>
        /// Hiển thị Login Tab phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void ShowLoginTab()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (panelLogin != null)
            {
                panelLogin.SetActive(true);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (panelRegister != null)
            {
                panelRegister.SetActive(false);
            }

            _tabsInitialized = true;
        }

        /// <summary>
        /// Hiển thị Register Tab phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void ShowRegisterTab()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (panelLogin != null)
            {
                panelLogin.SetActive(false);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (panelRegister != null)
            {
                panelRegister.SetActive(true);
            }

            _tabsInitialized = true;
        }

        /// <summary>
        /// Đảm bảo Centered Auth Layout phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void EnsureCenteredAuthLayout()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            EnsurePanelRectDefaultsWhenMissing(panelAuth, Vector2.zero);
            EnsurePanelRectDefaultsWhenMissing(panelLogin, new Vector2(0f, 16f));
            EnsurePanelRectDefaultsWhenMissing(panelRegister, new Vector2(0f, 16f));
        }

        /// <summary>
        /// Đảm bảo Panel Rect Defaults When Missing phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void EnsurePanelRectDefaultsWhenMissing(GameObject panelObj, Vector2 anchoredPos)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (panelObj == null)
            {
                return;
            }

            RectTransform rt = panelObj.GetComponent<RectTransform>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (rt != null)
            {
                return;
            }

            rt = panelObj.AddComponent<RectTransform>();

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
        }

        /// <summary>
        /// Đảm bảo Tab Buttons phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void EnsureTabButtons()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (panelLogin == null || panelRegister == null)
            {
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (Btn_OpenRegister == null)
            {
                Btn_OpenRegister = CreateTabButton(panelLogin.transform, "Btn_OpenRegister", "Đăng ký", new Vector2(0f, -214f));
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (Btn_BackToLogin == null)
            {
                Btn_BackToLogin = CreateTabButton(panelRegister.transform, "Btn_BackToLogin", "Quay lại đăng nhập", new Vector2(0f, -254f));
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (Btn_OpenRegister != null)
            {
                Btn_OpenRegister.interactable = true;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (Btn_BackToLogin != null)
            {
                Btn_BackToLogin.interactable = true;
            }
        }

        /// <summary>
        /// Tạo Tab Button phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private Button CreateTabButton(Transform parent, string objName, string label, Vector2 anchoredPos)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            GameObject btnObj = new GameObject(objName, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(260f, 40f);
            rt.anchoredPosition = anchoredPos;

            Image image = btnObj.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.1f);

            Button button = btnObj.GetComponent<Button>();

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 26;
            tmp.color = new Color32(0x12, 0x1A, 0x2F, 0xFF);

            return button;
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Localize Ui Texts theo ngữ cảnh sử dụng của script.
        /// </summary>
        private void LocalizeUiTexts()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            SetTmpTextByName("reg_Title", "Đăng ký");
            SetTmpTextByName("login_Title", "Đăng nhập");

            SetButtonLabel(Btn_ConfirmRegister, "Đăng ký");
            SetButtonLabel(Btn_ConfirmLogin, "Đăng nhập");

            Button playBtn = FindButton("Btn_Play");
            SetButtonLabel(playBtn, "Chơi thôi");
            SetButtonLabel(Btn_Logout, "Đăng xuất");

            SetInputPlaceholder(reg_Name, "Tên người dùng");
            SetInputPlaceholder(reg_Email, "Email");
            SetInputPlaceholder(reg_Phone, "Số điện thoại");
            SetInputPlaceholder(reg_Pass, "Mật khẩu");
            SetInputPlaceholder(login_Email, "Email");
            SetInputPlaceholder(login_Pass, "Mật khẩu");

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!_isAuthenticated)
            {
                SetStartUserText(string.Empty);
            }
        }

        /// <summary>
        /// Đảm bảo Ui Interaction Setup phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void EnsureUiInteractionSetup()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (auth_Status != null)
            {
                auth_Status.raycastTarget = false;
            }

            EnsureInputEditable(login_Pass);
            EnsureInputEditable(reg_Pass);
        }

        /// <summary>
        /// Đảm bảo Start Menu Widgets phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void EnsureStartMenuWidgets()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (panelStart == null)
            {
                return;
            }

            bool createdStartUserText = false;
            bool createdLogoutButton = false;

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (txt_StartUserName == null)
            {
                Transform child = panelStart.transform.Find(StartUserTextObjectName);
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (child != null)
                {
                    txt_StartUserName = child.GetComponent<TMP_Text>();
                    if (txt_StartUserName == null)
                    {
                        txt_StartUserName = child.gameObject.AddComponent<TextMeshProUGUI>();
                        txt_StartUserName.alignment = TextAlignmentOptions.Left;
                        txt_StartUserName.fontSize = 28;
                        txt_StartUserName.color = new Color32(0x12, 0x1A, 0x2F, 0xFF);
                        txt_StartUserName.raycastTarget = false;
                        txt_StartUserName.text = string.Empty;
                    }
                }
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (txt_StartUserName == null)
            {
                txt_StartUserName = CreateStartUserText(panelStart.transform);
                createdStartUserText = true;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (Btn_Logout == null)
            {
                Transform child = panelStart.transform.Find(StartLogoutButtonObjectName);
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (child != null)
                {
                    Btn_Logout = child.GetComponent<Button>();
                    if (Btn_Logout == null)
                    {
                        if (child.GetComponent<Image>() == null)
                        {
                            child.gameObject.AddComponent<Image>();
                        }

                        Btn_Logout = child.gameObject.AddComponent<Button>();
                    }

                    EnsureLogoutButtonLabel(child);
                }
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (Btn_Logout == null)
            {
                Btn_Logout = CreateStartLogoutButton(panelStart.transform);
                createdLogoutButton = true;
            }

            UpdateStartMenuWidgetLayout(createdStartUserText, createdLogoutButton);
        }

        /// <summary>
        /// Cập nhật Start Menu Widget Layout phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void UpdateStartMenuWidgetLayout(bool applyTextLayout, bool applyButtonLayout)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (applyTextLayout && txt_StartUserName != null)
            {
                RectTransform textRt = txt_StartUserName.GetComponent<RectTransform>();
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (textRt != null)
                {
                    textRt.anchorMin = new Vector2(0f, 1f);
                    textRt.anchorMax = new Vector2(0f, 1f);
                    textRt.pivot = new Vector2(0f, 1f);
                    textRt.sizeDelta = new Vector2(420f, 40f);
                    textRt.anchoredPosition = new Vector2(24f, -20f);
                }

                txt_StartUserName.alignment = TextAlignmentOptions.Left;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (applyButtonLayout && Btn_Logout != null)
            {
                RectTransform btnRt = Btn_Logout.GetComponent<RectTransform>();
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (btnRt != null)
                {
                    btnRt.anchorMin = new Vector2(0f, 1f);
                    btnRt.anchorMax = new Vector2(0f, 1f);
                    btnRt.pivot = new Vector2(0f, 1f);
                    btnRt.sizeDelta = new Vector2(180f, 40f);
                    btnRt.anchoredPosition = new Vector2(24f, -66f);
                }
            }
        }

        /// <summary>
        /// Đảm bảo Logout Button Label phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void EnsureLogoutButtonLabel(Transform buttonTransform)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (buttonTransform == null)
            {
                return;
            }

            Transform textChild = buttonTransform.Find("Text");
            TextMeshProUGUI tmp = textChild != null ? textChild.GetComponent<TextMeshProUGUI>() : null;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (tmp == null)
            {
                GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(buttonTransform, false);

                RectTransform textRt = textObj.GetComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = Vector2.zero;
                textRt.offsetMax = Vector2.zero;

                tmp = textObj.GetComponent<TextMeshProUGUI>();
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 24;
                tmp.color = new Color32(0x12, 0x1A, 0x2F, 0xFF);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (string.IsNullOrWhiteSpace(tmp.text))
            {
                tmp.text = "Đăng xuất";
            }
        }

        /// <summary>
        /// Tạo Start User Text phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private TMP_Text CreateStartUserText(Transform parent)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            GameObject textObj = new GameObject(StartUserTextObjectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(parent, false);

            RectTransform rt = textObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(520f, 52f);
            rt.anchoredPosition = new Vector2(0f, -82f);

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.fontSize = 28;
            tmp.color = new Color32(0x12, 0x1A, 0x2F, 0xFF);
            tmp.text = string.Empty;
            tmp.raycastTarget = false;
            return tmp;
        }

        /// <summary>
        /// Tạo Start Logout Button phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private Button CreateStartLogoutButton(Transform parent)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            GameObject btnObj = new GameObject(StartLogoutButtonObjectName, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(260f, 44f);
            rt.anchoredPosition = new Vector2(0f, -132f);

            Image image = btnObj.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.1f);

            Button button = btnObj.GetComponent<Button>();
            button.interactable = true;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = "Đăng xuất";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 24;
            tmp.color = new Color32(0x12, 0x1A, 0x2F, 0xFF);

            return button;
        }

        /// <summary>
        /// Nạp And Show Current User Name phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void LoadAndShowCurrentUserName()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (!Application.isPlaying || !PlayFabClientAPI.IsClientLoggedIn())
            {
                SetStartUserText(string.Empty);
                return;
            }

            PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest(),
                result =>
                {
                    string displayName = string.Empty;
                    if (result != null && result.AccountInfo != null)
                    {
                        if (result.AccountInfo.TitleInfo != null)
                        {
                            displayName = result.AccountInfo.TitleInfo.DisplayName;
                        }

                        if (string.IsNullOrWhiteSpace(displayName))
                        {
                            displayName = result.AccountInfo.Username;
                        }
                    }

                    SetStartUserText(displayName);
                },
                error =>
                {
                    Debug.LogError(error.GenerateErrorReport());
                    SetStartUserText(string.Empty);
                });
        }

        /// <summary>
        /// Thiết lập Start User Text phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void SetStartUserText(string userName)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (txt_StartUserName == null)
            {
                return;
            }

            string normalized = string.IsNullOrWhiteSpace(userName) ? string.Empty : userName.Trim();
            txt_StartUserName.text = normalized;
        }

        /// <summary>
        /// Đảm bảo Input Editable phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void EnsureInputEditable(TMP_InputField input)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (input == null)
            {
                return;
            }

            input.interactable = true;
            input.readOnly = false;
        }

        /// <summary>
        /// Thiết lập Tmp Text By Name phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void SetTmpTextByName(string objectName, string value)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            TMP_Text text = FindTmpText(objectName);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (text != null)
            {
                text.text = value;
            }
        }

        /// <summary>
        /// Thiết lập Input Placeholder phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void SetInputPlaceholder(TMP_InputField inputField, string value)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (inputField == null)
            {
                return;
            }

            TMP_Text placeholder = inputField.placeholder as TMP_Text;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (placeholder != null)
            {
                placeholder.text = value;
            }
        }

        /// <summary>
        /// Thiết lập Button Label phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void SetButtonLabel(Button button, string value)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (button == null)
            {
                return;
            }

            TMP_Text tmp = button.GetComponentInChildren<TMP_Text>(true);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (tmp != null)
            {
                tmp.text = value;
                return;
            }

            Text legacy = button.GetComponentInChildren<Text>(true);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (legacy != null)
            {
                legacy.text = value;
            }
        }

        /// <summary>
        /// Tìm Tmp Input phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static TMP_InputField FindTmpInput(string objectName)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            GameObject obj = GameObject.Find(objectName);
            return obj != null ? obj.GetComponent<TMP_InputField>() : null;
        }

        /// <summary>
        /// Tìm Tmp Text phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static TMP_Text FindTmpText(string objectName)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            GameObject obj = GameObject.Find(objectName);
            return obj != null ? obj.GetComponent<TMP_Text>() : null;
        }

        /// <summary>
        /// Tìm Button phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static Button FindButton(string objectName)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            GameObject obj = GameObject.Find(objectName);
            return obj != null ? obj.GetComponent<Button>() : null;
        }
    }
}
