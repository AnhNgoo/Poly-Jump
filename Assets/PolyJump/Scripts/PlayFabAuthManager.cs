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
        private static void ResetRuntimeSession()
        {
            _preserveSessionOnNextStart = false;
            _runtimeSessionId = Guid.NewGuid().ToString("N");
            _hasCachedLeaderboardHighscore = false;
            _cachedLeaderboardHighscore = 0;
            _isFetchingLeaderboardHighscore = false;
            _pendingHighscoreCallbacks.Clear();
        }

        public static void PreserveSessionForNextSceneLoad()
        {
            _preserveSessionOnNextStart = true;
            PlayerPrefs.SetInt(ReplayPreserveKey, 1);
            PlayerPrefs.SetString(ReplayRuntimeSessionKey, _runtimeSessionId);
            CaptureCurrentStaticAuthContext();
            PlayerPrefs.Save();
        }

        private static bool ConsumePreserveSessionFlag()
        {
            bool shouldPreserve = _preserveSessionOnNextStart;
            _preserveSessionOnNextStart = false;
            return shouldPreserve;
        }

        private void OnEnable()
        {
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

        private void Awake()
        {
            AutoBindUiReferences();
            LocalizeUiTexts();
            EnsureTabButtons();
            EnsureStartMenuWidgets();
            EnsureCenteredAuthLayout();
            EnsureUiInteractionSetup();

            if (!Application.isPlaying)
            {
                ApplyEditorDefaultAuthState();
                return;
            }

            EnsureTitleIdConfigured();
            WireUiEvents();
        }

        private IEnumerator Start()
        {
            if (!Application.isPlaying)
            {
                yield break;
            }

            // Delay 1 frame so this gate runs after GameManager.Start refreshes menu panels.
            yield return null;

            bool preserveSession = ConsumePreserveSessionFlag() || ConsumeReplayPreserveFlag();
            bool hasCachedLogin = false;

            if (preserveSession)
            {
                hasCachedLogin = TryRestoreAuthContextFromReplayCache();
            }

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

        private void LateUpdate()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            RefreshAuthGateUi();
        }

        private void OnValidate()
        {
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

        public void RegisterUser(string email, string password, string fullName, string phone)
        {
            string trimmedName = TrimInput(fullName);
            string trimmedEmail = NormalizeEmail(email);
            string trimmedPassword = TrimInput(password);
            string trimmedPhone = TrimInput(phone);

            if (!ValidateRegisterInput(trimmedName, trimmedEmail, trimmedPassword, trimmedPhone))
            {
                return;
            }

            string resolvedTitleId = EnsureTitleIdConfigured();
            if (string.IsNullOrWhiteSpace(resolvedTitleId))
            {
                return;
            }

            InvalidateLeaderboardHighscoreCache();
            InvalidateEventWindowCache();

            string username = BuildUsernameFromName(trimmedName);
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

        public void LoginUser(string email, string password)
        {
            string normalizedEmail = NormalizeEmail(email);
            string trimmedPassword = TrimInput(password);

            if (!ValidateLoginInput(normalizedEmail, trimmedPassword))
            {
                return;
            }

            string resolvedTitleId = EnsureTitleIdConfigured();
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

        public int GetCachedLeaderboardHighscore()
        {
            return _hasCachedLeaderboardHighscore ? _cachedLeaderboardHighscore : 0;
        }

        public void EnsureLeaderboardHighscoreCached(System.Action<int> onReady = null)
        {
            if (!PlayFabClientAPI.IsClientLoggedIn())
            {
                InvalidateLeaderboardHighscoreCache();
                if (onReady != null)
                {
                    onReady(0);
                }
                return;
            }

            if (_hasCachedLeaderboardHighscore)
            {
                if (onReady != null)
                {
                    onReady(_cachedLeaderboardHighscore);
                }
                return;
            }

            if (onReady != null)
            {
                _pendingHighscoreCallbacks.Add(onReady);
            }

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

        public void SubmitScore(int score)
        {
            SubmitScore(score, null);
        }

        public void SubmitScore(int score, System.Action<int> onHighscoreResolved)
        {
            int finalScore = Mathf.Max(0, score);

            SubmitEventScoreWhenEventOpen(finalScore);

            EnsureLeaderboardHighscoreCached(cachedHighscore =>
            {
                int currentHighscore = Mathf.Max(0, cachedHighscore);
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

        private void SubmitEventScoreWhenEventOpen(int value)
        {
            if (value <= 0)
            {
                return;
            }

            EnsureEventWindowStatus(isOpen =>
            {
                if (!isOpen)
                {
                    Debug.Log("[PolyJump] Event is closed, skip SubmitEventScore.");
                    return;
                }

                string statisticName = ResolveEventStatisticNameForWindow();
                SubmitEventScoreViaCloudScript(value, statisticName);
            }, forceRefresh: true);
        }

        private void EnsureEventWindowStatus(Action<bool> onResolved, bool forceRefresh = false)
        {
            if (!PlayFabClientAPI.IsClientLoggedIn())
            {
                if (onResolved != null)
                {
                    onResolved(false);
                }
                return;
            }

            bool cacheFresh = _eventWindowLoaded
                && !forceRefresh
                && (Time.unscaledTime - _eventWindowLastFetchRealtime) < Mathf.Max(10f, EventWindowCacheSeconds);

            if (cacheFresh)
            {
                if (onResolved != null)
                {
                    onResolved(IsEventWindowOpenNow());
                }
                return;
            }

            if (onResolved != null)
            {
                _pendingEventWindowCallbacks.Add(onResolved);
            }

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

        private void SubmitEventScoreViaCloudScript(int value, string statisticName)
        {
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

        private void SubmitEventScoreDirect(int value, string statisticName)
        {
            SubmitStatisticValue(statisticName, value, success =>
            {
                if (success)
                {
                    Debug.Log("[PolyJump] Event leaderboard updated directly: " + value + " (" + statisticName + ")");
                }
            });
        }

        private string ResolveEventStatisticNameForWindow()
        {
            string baseName = string.IsNullOrWhiteSpace(eventLeaderboardStatisticName)
                ? "LeaderBoard_Event"
                : eventLeaderboardStatisticName;

            string suffix = GetEventStatisticSuffix(_eventWindowStartRaw, _eventWindowStartUtc);
            if (string.IsNullOrWhiteSpace(suffix))
            {
                return baseName;
            }

            return baseName + "_" + suffix;
        }

        private static string GetEventStatisticSuffix(string startRaw, DateTime startUtc)
        {
            if (TryNormalizeEventSuffixFromRaw(startRaw, out string normalizedRaw))
            {
                return normalizedRaw;
            }

            if (startUtc != DateTime.MinValue)
            {
                DateTime vn = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc).AddHours(7);
                return vn.ToString("HH:mm:ss-yyyy:MM:dd");
            }

            if (!string.IsNullOrWhiteSpace(startRaw))
            {
                return startRaw.Trim();
            }

            return string.Empty;
        }

        private static bool TryNormalizeEventSuffixFromRaw(string raw, out string normalized)
        {
            normalized = string.Empty;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            string text = raw.Trim();
            Match timeDate = Regex.Match(text, @"^\s*(\d{1,2})\D+(\d{1,2})\D+(\d{1,2})\s*-\s*(\d{4})\D+(\d{1,2})\D+(\d{1,2})\s*$");
            if (timeDate.Success)
            {
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

        private static bool TryParseEventSuffixParts(
            string hourText,
            string minuteText,
            string secondText,
            string yearText,
            string monthText,
            string dayText,
            out string normalized)
        {
            normalized = string.Empty;

            if (!int.TryParse(hourText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int hour)
                || !int.TryParse(minuteText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int minute)
                || !int.TryParse(secondText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int second)
                || !int.TryParse(yearText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int year)
                || !int.TryParse(monthText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int month)
                || !int.TryParse(dayText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int day))
            {
                return false;
            }

            if (hour < 0 || hour > 24)
            {
                return false;
            }

            if (minute < 0 || minute > 59 || second < 0 || second > 59)
            {
                return false;
            }

            if (hour == 24 && (minute != 0 || second != 0))
            {
                return false;
            }

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

        private static bool ShouldFallbackEventSubmit(ExecuteCloudScriptResult result, out string reason)
        {
            reason = string.Empty;

            if (result == null)
            {
                reason = "ExecuteCloudScriptResult is null";
                return true;
            }

            if (result.Error != null)
            {
                reason = BuildCloudScriptErrorDetails(result);
                return true;
            }

            return false;
        }

        private static string BuildCloudScriptErrorDetails(ExecuteCloudScriptResult result)
        {
            if (result == null || result.Error == null)
            {
                return "CloudScript error (unknown details)";
            }

            var builder = new StringBuilder();
            builder.Append("CloudScript error: ").Append(result.Error.Error);

            if (!string.IsNullOrWhiteSpace(result.Error.Message))
            {
                builder.Append(" - ").Append(result.Error.Message.Trim());
            }

            if (!string.IsNullOrWhiteSpace(result.Error.StackTrace))
            {
                builder.Append(" | stack: ").Append(result.Error.StackTrace.Trim());
            }

            if (result.Logs != null && result.Logs.Count > 0)
            {
                int count = Math.Min(3, result.Logs.Count);
                builder.Append(" | logs: ");
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

        private static bool TryReadCloudScriptBoolean(object functionResult, string key, out bool value)
        {
            value = false;
            if (functionResult == null || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            IDictionary<string, object> dict = functionResult as IDictionary<string, object>;
            if (dict == null || !dict.ContainsKey(key))
            {
                return false;
            }

            object raw = dict[key];
            if (raw is bool rawBool)
            {
                value = rawBool;
                return true;
            }

            if (raw is string rawString)
            {
                string normalized = rawString.Trim();
                if (bool.TryParse(normalized, out bool parsed))
                {
                    value = parsed;
                    return true;
                }

                if (normalized == "1" || normalized == "0")
                {
                    value = normalized == "1";
                    return true;
                }

                return false;
            }

            if (raw is int rawInt)
            {
                value = rawInt != 0;
                return true;
            }

            if (raw is long rawLong)
            {
                value = rawLong != 0L;
                return true;
            }

            if (raw is float rawFloat)
            {
                value = Mathf.Abs(rawFloat) > Mathf.Epsilon;
                return true;
            }

            if (raw is double rawDouble)
            {
                value = Math.Abs(rawDouble) > double.Epsilon;
                return true;
            }

            return false;
        }

        private static bool TryReadCloudScriptString(object functionResult, string key, out string value)
        {
            value = string.Empty;
            if (functionResult == null || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            IDictionary<string, object> dict = functionResult as IDictionary<string, object>;
            if (dict == null || !dict.ContainsKey(key) || dict[key] == null)
            {
                return false;
            }

            string text = dict[key].ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            value = text.Trim();
            return true;
        }

        private bool IsEventWindowOpenNow()
        {
            if (!_eventWindowLoaded)
            {
                return false;
            }

            if (!_eventWindowEnabled)
            {
                return false;
            }

            if (_eventWindowStartUtc == DateTime.MinValue || _eventWindowEndUtc == DateTime.MinValue)
            {
                return false;
            }

            if (_eventWindowEndUtc < _eventWindowStartUtc)
            {
                return false;
            }

            DateTime now = DateTime.UtcNow;
            if (now < _eventWindowStartUtc)
            {
                return false;
            }

            if (now > _eventWindowEndUtc)
            {
                return false;
            }

            return true;
        }

        private static string GetTitleDataValue(Dictionary<string, string> data, string key, string fallback)
        {
            if (data != null && data.ContainsKey(key) && !string.IsNullOrWhiteSpace(data[key]))
            {
                return data[key].Trim();
            }

            return fallback;
        }

        private static bool ParseTitleDataBool(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string normalized = value.Trim();
            if (bool.TryParse(normalized, out bool parsed))
            {
                return parsed;
            }

            return normalized == "1" || normalized.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }

        private static void ResolveEventTimeFromTitleData(
            Dictionary<string, string> data,
            string utcKey,
            string legacyKey,
            out string selectedRaw,
            out DateTime selectedUtc)
        {
            selectedRaw = string.Empty;
            selectedUtc = DateTime.MinValue;

            string utcRaw = GetTitleDataValue(data, utcKey, string.Empty);
            string legacyRaw = GetTitleDataValue(data, legacyKey, string.Empty);

            DateTime utcParsed = ParseEventUtcTitleData(utcRaw);
            if (utcParsed != DateTime.MinValue)
            {
                selectedRaw = utcRaw;
                selectedUtc = utcParsed;
                return;
            }

            DateTime legacyParsed = ParseEventUtcTitleData(legacyRaw);
            if (legacyParsed != DateTime.MinValue)
            {
                selectedRaw = legacyRaw;
                selectedUtc = legacyParsed;
                return;
            }

            selectedRaw = !string.IsNullOrWhiteSpace(utcRaw) ? utcRaw : legacyRaw;
        }

        private static DateTime ParseEventUtcTitleData(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DateTime.MinValue;
            }

            if (TryParseVnEventTextToUtc(value, out DateTime parsedFromCustom))
            {
                return parsedFromCustom;
            }

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

        private static bool TryParseVnEventTextToUtc(string value, out DateTime utc)
        {
            utc = DateTime.MinValue;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string text = value.Trim();

            Match timeDate = Regex.Match(text, @"^\s*(\d{1,2})\D+(\d{1,2})\D+(\d{1,2})\s*-\s*(\d{4})\D+(\d{1,2})\D+(\d{1,2})\s*$");
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

        private static bool TryBuildUtcFromParts(
            string yearText,
            string monthText,
            string dayText,
            string hourText,
            string minuteText,
            string secondText,
            out DateTime utc)
        {
            utc = DateTime.MinValue;

            if (!int.TryParse(hourText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int hour)
                || !int.TryParse(minuteText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int minute)
                || !int.TryParse(secondText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int second)
                || !int.TryParse(yearText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int year)
                || !int.TryParse(monthText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int month)
                || !int.TryParse(dayText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int day))
            {
                return false;
            }

            if (hour < 0 || hour > 24)
            {
                return false;
            }

            if (minute < 0 || minute > 59 || second < 0 || second > 59)
            {
                return false;
            }

            if (hour == 24 && (minute != 0 || second != 0))
            {
                return false;
            }

            try
            {
                int normalizedHour = hour == 24 ? 0 : hour;
                DateTime vnTime = new DateTime(year, month, day, normalizedHour, minute, second, DateTimeKind.Unspecified);
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

        private void FlushPendingEventWindowCallbacks(bool isOpen)
        {
            if (_pendingEventWindowCallbacks.Count == 0)
            {
                return;
            }

            List<Action<bool>> callbacks = new List<Action<bool>>(_pendingEventWindowCallbacks);
            _pendingEventWindowCallbacks.Clear();

            for (int i = 0; i < callbacks.Count; i++)
            {
                Action<bool> callback = callbacks[i];
                if (callback != null)
                {
                    callback(isOpen);
                }
            }
        }

        private void InvalidateEventWindowCache()
        {
            _eventWindowLoaded = false;
            _eventWindowFetching = false;
            _eventWindowEnabled = false;
            _eventWindowStartRaw = string.Empty;
            _eventWindowStartUtc = DateTime.MinValue;
            _eventWindowEndUtc = DateTime.MinValue;
            _eventWindowLastFetchRealtime = 0f;
            _pendingEventWindowCallbacks.Clear();
        }

        private void SubmitStatisticValue(int value, System.Action<bool> onComplete)
        {
            SubmitStatisticValue(leaderboardStatisticName, value, onComplete);
        }

        private void SubmitStatisticValue(string statisticName, int value, System.Action<bool> onComplete)
        {
            if (!PlayFabClientAPI.IsClientLoggedIn())
            {
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

        public void OnRegisterButtonPressed()
        {
            RegisterUser(
                reg_Email != null ? TrimInput(reg_Email.text) : string.Empty,
                reg_Pass != null ? TrimInput(reg_Pass.text) : string.Empty,
                reg_Name != null ? TrimInput(reg_Name.text) : string.Empty,
                reg_Phone != null ? TrimInput(reg_Phone.text) : string.Empty);
        }

        public void OnLoginButtonPressed()
        {
            LoginUser(
                login_Email != null ? TrimInput(login_Email.text) : string.Empty,
                login_Pass != null ? TrimInput(login_Pass.text) : string.Empty);
        }

        private void SaveExtraInfo(string fullName, string phone, string email, string username)
        {
            var data = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                data["UserName"] = fullName;
            }

            if (!string.IsNullOrWhiteSpace(phone))
            {
                data["Phone"] = phone;
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                data["Email"] = email;
            }

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

        private bool ValidateRegisterInput(string fullName, string email, string password, string phone)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                SetStatus("Tên người dùng không được bỏ trống", StatusAutoHideSeconds);
                return false;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                SetStatus("Email không được bỏ trống", StatusAutoHideSeconds);
                return false;
            }

            if (!IsValidGmail(email))
            {
                SetStatus("Email phải có dạng @gmail.com", StatusAutoHideSeconds);
                return false;
            }

            if (string.IsNullOrWhiteSpace(phone))
            {
                SetStatus("Số điện thoại không được bỏ trống", StatusAutoHideSeconds);
                return false;
            }

            if (!IsValidPhoneNumber(phone))
            {
                SetStatus("Số điện thoại không hợp lệ (10 số, bắt đầu bằng 0)", StatusAutoHideSeconds);
                return false;
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6 || password.Length > 100)
            {
                SetStatus("Mật khẩu phải từ 6 đến 100 ký tự", StatusAutoHideSeconds);
                return false;
            }

            return true;
        }

        private bool ValidateLoginInput(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                SetStatus("Email không được bỏ trống", StatusAutoHideSeconds);
                return false;
            }

            if (!IsValidGmail(email))
            {
                SetStatus("Email phải có dạng @gmail.com", StatusAutoHideSeconds);
                return false;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                SetStatus("Mật khẩu không được bỏ trống", StatusAutoHideSeconds);
                return false;
            }

            return true;
        }

        private void HandleRegisterError(PlayFabError error)
        {
            if (error == null)
            {
                SetStatus("Đăng ký thất bại", StatusAutoHideSeconds);
                return;
            }

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

        private void HandleLoginError(PlayFabError error)
        {
            if (error == null)
            {
                SetStatus("Đăng nhập thất bại", StatusAutoHideSeconds);
                return;
            }

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

        private void OnError(PlayFabError error)
        {
            string report = error != null ? error.GenerateErrorReport() : "Unknown PlayFab error";
            Debug.LogError(report);
            string msg = error != null && !string.IsNullOrWhiteSpace(error.ErrorMessage)
                ? error.ErrorMessage
                : "Không xác định";
            SetStatus("Lỗi: " + msg, StatusAutoHideSeconds);
            RefreshAuthGateUi();
        }

        private void MarkAuthenticatedAndOpenStartMenu()
        {
            _isAuthenticated = true;
            LoadAndShowCurrentUserName();
            NormalizeUserNamePlayerData();
            EnsureLeaderboardHighscoreCached();
            RefreshAuthGateUi();
        }

        private void MarkAuthenticatedAndOpenStartMenu(string userName)
        {
            _isAuthenticated = true;
            SetStartUserText(userName);
            NormalizeUserNamePlayerData(userName);
            EnsureLeaderboardHighscoreCached();
            RefreshAuthGateUi();
        }

        private void NormalizeUserNamePlayerData(string canonicalName = null)
        {
            if (!PlayFabClientAPI.IsClientLoggedIn())
            {
                return;
            }

            var request = new UpdateUserDataRequest
            {
                KeysToRemove = new List<string> { "Username" }
            };

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

        public void OnLogoutPressed()
        {
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

        private int ExtractLeaderboardHighscore(GetPlayerStatisticsResult result)
        {
            int highscore = 0;
            if (result == null || result.Statistics == null)
            {
                return highscore;
            }

            for (int i = 0; i < result.Statistics.Count; i++)
            {
                StatisticValue stat = result.Statistics[i];
                if (stat == null || stat.StatisticName != leaderboardStatisticName)
                {
                    continue;
                }

                highscore = Mathf.Max(0, stat.Value);
                break;
            }

            return highscore;
        }

        private static void UpdateLeaderboardHighscoreCache(int value, bool hasValue)
        {
            _cachedLeaderboardHighscore = Mathf.Max(0, value);
            _hasCachedLeaderboardHighscore = hasValue;
        }

        private static void InvalidateLeaderboardHighscoreCache()
        {
            _cachedLeaderboardHighscore = 0;
            _hasCachedLeaderboardHighscore = false;
            _isFetchingLeaderboardHighscore = false;
            _pendingHighscoreCallbacks.Clear();
        }

        private static void FlushPendingHighscoreCallbacks(int highscore)
        {
            if (_pendingHighscoreCallbacks.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _pendingHighscoreCallbacks.Count; i++)
            {
                System.Action<int> callback = _pendingHighscoreCallbacks[i];
                if (callback != null)
                {
                    callback(highscore);
                }
            }

            _pendingHighscoreCallbacks.Clear();
        }

        private static void SaveAuthContextForReplay(PlayFabAuthenticationContext context)
        {
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

        private static void CaptureCurrentStaticAuthContext()
        {
            PlayFabAuthenticationContext context = PlayFabSettings.staticPlayer;
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

        private static bool ConsumeReplayPreserveFlag()
        {
            bool shouldPreserve = PlayerPrefs.GetInt(ReplayPreserveKey, 0) == 1;
            string replaySession = PlayerPrefs.GetString(ReplayRuntimeSessionKey, string.Empty);
            bool sameRuntime = !string.IsNullOrWhiteSpace(replaySession) && replaySession == _runtimeSessionId;

            if (shouldPreserve)
            {
                PlayerPrefs.DeleteKey(ReplayPreserveKey);
                PlayerPrefs.DeleteKey(ReplayRuntimeSessionKey);
                PlayerPrefs.Save();
            }

            if (!shouldPreserve)
            {
                return false;
            }

            if (!sameRuntime)
            {
                ClearReplaySessionCache();
                return false;
            }

            return true;
        }

        private static bool TryRestoreAuthContextFromReplayCache()
        {
            if (PlayFabClientAPI.IsClientLoggedIn())
            {
                return true;
            }

            string ticket = PlayerPrefs.GetString(ReplaySessionTicketKey, string.Empty);
            if (string.IsNullOrWhiteSpace(ticket))
            {
                return false;
            }

            PlayFabAuthenticationContext context = PlayFabSettings.staticPlayer;
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

        private static void ClearReplaySessionCache()
        {
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

        private static void SaveStringOrDelete(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                PlayerPrefs.DeleteKey(key);
                return;
            }

            PlayerPrefs.SetString(key, value);
        }

        private void RefreshAuthGateUi()
        {
            if (panelAuth == null || panelStart == null)
            {
                AutoBindUiReferences();
            }

            bool loggedIn = _isAuthenticated;

            if (panelAuth != null)
            {
                panelAuth.SetActive(!loggedIn);
            }

            if (panelStart != null)
            {
                bool shouldShowStart = loggedIn;
                if (GameManager.Instance != null)
                {
                    shouldShowStart = shouldShowStart && GameManager.Instance.CurrentState == GameState.Menu;
                }

                if (shouldShowStart)
                {
                    shouldShowStart = !IsLeaderboardPanelVisible();
                }

                panelStart.SetActive(shouldShowStart);
            }

            if (!loggedIn && !_tabsInitialized)
            {
                ShowLoginTab();
            }
        }

        private void ApplyEditorDefaultAuthState()
        {
            _isAuthenticated = false;

            if (panelAuth != null)
            {
                panelAuth.SetActive(true);
            }

            if (panelStart != null)
            {
                panelStart.SetActive(false);
            }

            SetStartUserText(string.Empty);
            ShowLoginTab();
        }

        private string EnsureTitleIdConfigured()
        {
            string resolvedTitleId = string.IsNullOrWhiteSpace(titleId)
                ? PlayFabSettings.TitleId
                : titleId.Trim();

            if (string.IsNullOrWhiteSpace(resolvedTitleId))
            {
                SetStatus("Thiếu TitleId PlayFab", StatusAutoHideSeconds);
                Debug.LogError("[PolyJump] PlayFab TitleId is empty.");
                return string.Empty;
            }

            if (PlayFabSettings.TitleId != resolvedTitleId)
            {
                PlayFabSettings.TitleId = resolvedTitleId;
            }

            return resolvedTitleId;
        }

        private static string NormalizeEmail(string email)
        {
            return string.IsNullOrWhiteSpace(email)
                ? string.Empty
                : email.Trim().ToLowerInvariant();
        }

        private static string TrimInput(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string BuildUsernameFromName(string fullName)
        {
            string source = TrimInput(fullName);
            if (string.IsNullOrWhiteSpace(source))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(source.Length);
            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
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
            while (username.Contains("__"))
            {
                username = username.Replace("__", "_");
            }

            if (username.Length < 3)
            {
                return string.Empty;
            }

            if (username.Length > 20)
            {
                username = username.Substring(0, 20);
            }

            return username;
        }

        private static bool IsValidGmail(string email)
        {
            return GmailRegex.IsMatch(email);
        }

        private static bool IsValidPhoneNumber(string phone)
        {
            return PhoneRegex.IsMatch(phone);
        }

        private void SetStatus(string message, float hideAfterSeconds)
        {
            if (auth_Status != null)
            {
                auth_Status.text = message;
            }

            if (!Application.isPlaying)
            {
                return;
            }

            if (_statusHideCoroutine != null)
            {
                StopCoroutine(_statusHideCoroutine);
                _statusHideCoroutine = null;
            }

            if (hideAfterSeconds > 0f)
            {
                _statusHideCoroutine = StartCoroutine(ClearStatusAfterDelay(hideAfterSeconds));
            }
        }

        private IEnumerator ClearStatusAfterDelay(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (auth_Status != null)
            {
                auth_Status.text = string.Empty;
            }

            _statusHideCoroutine = null;
        }

        private void AutoBindUiReferences()
        {
            if (reg_Name == null)
            {
                reg_Name = FindTmpInput("reg_Name");
            }

            if (reg_Email == null)
            {
                reg_Email = FindTmpInput("reg_Email");
            }

            if (reg_Phone == null)
            {
                reg_Phone = FindTmpInput("reg_Phone");
            }

            if (reg_Pass == null)
            {
                reg_Pass = FindTmpInput("reg_Pass");
            }

            if (login_Email == null)
            {
                login_Email = FindTmpInput("login_Email");
            }

            if (login_Pass == null)
            {
                login_Pass = FindTmpInput("login_Pass");
            }

            if (Btn_ConfirmRegister == null)
            {
                Btn_ConfirmRegister = FindButton("Btn_ConfirmRegister");
            }

            if (Btn_ConfirmLogin == null)
            {
                Btn_ConfirmLogin = FindButton("Btn_ConfirmLogin");
            }

            if (auth_Status == null)
            {
                auth_Status = FindTmpText("auth_Status");
            }

            if (panelAuth == null)
            {
                panelAuth = GameObject.Find("Panel_Auth");
            }

            if (panelStart == null)
            {
                panelStart = GameObject.Find("Panel_Start");
            }

            if (panelLogin == null)
            {
                GameObject obj = GameObject.Find("Panel_Login");
                panelLogin = obj;
            }

            if (panelRegister == null)
            {
                GameObject obj = GameObject.Find("Panel_Register");
                panelRegister = obj;
            }

            if (Btn_OpenRegister == null)
            {
                Btn_OpenRegister = FindButton("Btn_OpenRegister");
            }

            if (Btn_BackToLogin == null)
            {
                Btn_BackToLogin = FindButton("Btn_BackToLogin");
            }

            if (_panelLeaderboard == null)
            {
                _panelLeaderboard = GameObject.Find("Panel_Leaderboard");
            }
        }

        private bool IsLeaderboardPanelVisible()
        {
            if (_panelLeaderboard == null)
            {
                _panelLeaderboard = GameObject.Find("Panel_Leaderboard");
            }

            return _panelLeaderboard != null && _panelLeaderboard.activeInHierarchy;
        }

        private void WireUiEvents()
        {
            if (Btn_ConfirmRegister != null)
            {
                Btn_ConfirmRegister.onClick.RemoveListener(OnRegisterButtonPressed);
                Btn_ConfirmRegister.onClick.AddListener(OnRegisterButtonPressed);
            }

            if (Btn_ConfirmLogin != null)
            {
                Btn_ConfirmLogin.onClick.RemoveListener(OnLoginButtonPressed);
                Btn_ConfirmLogin.onClick.AddListener(OnLoginButtonPressed);
            }

            if (Btn_OpenRegister != null)
            {
                Btn_OpenRegister.onClick.RemoveListener(OnOpenRegisterPressed);
                Btn_OpenRegister.onClick.AddListener(OnOpenRegisterPressed);
            }

            if (Btn_BackToLogin != null)
            {
                Btn_BackToLogin.onClick.RemoveListener(OnBackToLoginPressed);
                Btn_BackToLogin.onClick.AddListener(OnBackToLoginPressed);
            }

            if (Btn_Logout != null)
            {
                Btn_Logout.onClick.RemoveListener(OnLogoutPressed);
                Btn_Logout.onClick.AddListener(OnLogoutPressed);
            }
        }

        public void OnOpenRegisterPressed()
        {
            ShowRegisterTab();
        }

        public void OnBackToLoginPressed()
        {
            ShowLoginTab();
        }

        private void ShowLoginTab()
        {
            if (panelLogin != null)
            {
                panelLogin.SetActive(true);
            }

            if (panelRegister != null)
            {
                panelRegister.SetActive(false);
            }

            _tabsInitialized = true;
        }

        private void ShowRegisterTab()
        {
            if (panelLogin != null)
            {
                panelLogin.SetActive(false);
            }

            if (panelRegister != null)
            {
                panelRegister.SetActive(true);
            }

            _tabsInitialized = true;
        }

        private void EnsureCenteredAuthLayout()
        {
            EnsurePanelRectDefaultsWhenMissing(panelAuth, Vector2.zero);
            EnsurePanelRectDefaultsWhenMissing(panelLogin, new Vector2(0f, 16f));
            EnsurePanelRectDefaultsWhenMissing(panelRegister, new Vector2(0f, 16f));
        }

        private static void EnsurePanelRectDefaultsWhenMissing(GameObject panelObj, Vector2 anchoredPos)
        {
            if (panelObj == null)
            {
                return;
            }

            RectTransform rt = panelObj.GetComponent<RectTransform>();
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

        private void EnsureTabButtons()
        {
            if (panelLogin == null || panelRegister == null)
            {
                return;
            }

            if (Btn_OpenRegister == null)
            {
                Btn_OpenRegister = CreateTabButton(panelLogin.transform, "Btn_OpenRegister", "Đăng ký", new Vector2(0f, -214f));
            }

            if (Btn_BackToLogin == null)
            {
                Btn_BackToLogin = CreateTabButton(panelRegister.transform, "Btn_BackToLogin", "Quay lại đăng nhập", new Vector2(0f, -254f));
            }

            if (Btn_OpenRegister != null)
            {
                Btn_OpenRegister.interactable = true;
            }

            if (Btn_BackToLogin != null)
            {
                Btn_BackToLogin.interactable = true;
            }
        }

        private Button CreateTabButton(Transform parent, string objName, string label, Vector2 anchoredPos)
        {
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

        private void LocalizeUiTexts()
        {
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

            if (!_isAuthenticated)
            {
                SetStartUserText(string.Empty);
            }
        }

        private void EnsureUiInteractionSetup()
        {
            if (auth_Status != null)
            {
                auth_Status.raycastTarget = false;
            }

            EnsureInputEditable(login_Pass);
            EnsureInputEditable(reg_Pass);
        }

        private void EnsureStartMenuWidgets()
        {
            if (panelStart == null)
            {
                return;
            }

            bool createdStartUserText = false;
            bool createdLogoutButton = false;

            if (txt_StartUserName == null)
            {
                Transform child = panelStart.transform.Find(StartUserTextObjectName);
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

            if (txt_StartUserName == null)
            {
                txt_StartUserName = CreateStartUserText(panelStart.transform);
                createdStartUserText = true;
            }

            if (Btn_Logout == null)
            {
                Transform child = panelStart.transform.Find(StartLogoutButtonObjectName);
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

            if (Btn_Logout == null)
            {
                Btn_Logout = CreateStartLogoutButton(panelStart.transform);
                createdLogoutButton = true;
            }

            UpdateStartMenuWidgetLayout(createdStartUserText, createdLogoutButton);
        }

        private void UpdateStartMenuWidgetLayout(bool applyTextLayout, bool applyButtonLayout)
        {
            if (applyTextLayout && txt_StartUserName != null)
            {
                RectTransform textRt = txt_StartUserName.GetComponent<RectTransform>();
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

            if (applyButtonLayout && Btn_Logout != null)
            {
                RectTransform btnRt = Btn_Logout.GetComponent<RectTransform>();
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

        private static void EnsureLogoutButtonLabel(Transform buttonTransform)
        {
            if (buttonTransform == null)
            {
                return;
            }

            Transform textChild = buttonTransform.Find("Text");
            TextMeshProUGUI tmp = textChild != null ? textChild.GetComponent<TextMeshProUGUI>() : null;
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

            if (string.IsNullOrWhiteSpace(tmp.text))
            {
                tmp.text = "Đăng xuất";
            }
        }

        private TMP_Text CreateStartUserText(Transform parent)
        {
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

        private Button CreateStartLogoutButton(Transform parent)
        {
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

        private void LoadAndShowCurrentUserName()
        {
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

        private void SetStartUserText(string userName)
        {
            if (txt_StartUserName == null)
            {
                return;
            }

            string normalized = string.IsNullOrWhiteSpace(userName) ? string.Empty : userName.Trim();
            txt_StartUserName.text = normalized;
        }

        private static void EnsureInputEditable(TMP_InputField input)
        {
            if (input == null)
            {
                return;
            }

            input.interactable = true;
            input.readOnly = false;
        }

        private static void SetTmpTextByName(string objectName, string value)
        {
            TMP_Text text = FindTmpText(objectName);
            if (text != null)
            {
                text.text = value;
            }
        }

        private static void SetInputPlaceholder(TMP_InputField inputField, string value)
        {
            if (inputField == null)
            {
                return;
            }

            TMP_Text placeholder = inputField.placeholder as TMP_Text;
            if (placeholder != null)
            {
                placeholder.text = value;
            }
        }

        private static void SetButtonLabel(Button button, string value)
        {
            if (button == null)
            {
                return;
            }

            TMP_Text tmp = button.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                tmp.text = value;
                return;
            }

            Text legacy = button.GetComponentInChildren<Text>(true);
            if (legacy != null)
            {
                legacy.text = value;
            }
        }

        private static TMP_InputField FindTmpInput(string objectName)
        {
            GameObject obj = GameObject.Find(objectName);
            return obj != null ? obj.GetComponent<TMP_InputField>() : null;
        }

        private static TMP_Text FindTmpText(string objectName)
        {
            GameObject obj = GameObject.Find(objectName);
            return obj != null ? obj.GetComponent<TMP_Text>() : null;
        }

        private static Button FindButton(string objectName)
        {
            GameObject obj = GameObject.Find(objectName);
            return obj != null ? obj.GetComponent<Button>() : null;
        }
    }
}
