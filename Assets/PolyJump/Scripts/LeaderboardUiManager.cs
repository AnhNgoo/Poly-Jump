using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.UI;

namespace PolyJump.Scripts
{
    public class LeaderboardUiManager : MonoBehaviour
    {
        private enum LeaderboardTab
        {
            Normal,
            Event
        }

        private enum EventWindowState
        {
            Unknown,
            Disabled,
            Upcoming,
            Active,
            Ended,
            Invalid
        }

        private sealed class LeaderboardCache
        {
            public readonly List<PlayerLeaderboardEntry> topEntries = new List<PlayerLeaderboardEntry>();
            public bool hasData;
            public bool isFetching;
            public float lastFetchRealtime;
            public string playerName = string.Empty;
            public int playerScore;
            public int playerRank;
            public bool hasPlayerInfo;
            public string emptyMessage = "Chưa có xếp hạng";

            public void Clear()
            {
                topEntries.Clear();
                hasData = false;
                isFetching = false;
                lastFetchRealtime = 0f;
                playerName = string.Empty;
                playerScore = 0;
                playerRank = 0;
                hasPlayerInfo = false;
                emptyMessage = "Chưa có xếp hạng";
            }
        }

        [Header("References")]
        public PlayFabAuthManager playFabAuthManager;
        public Canvas targetCanvas;

        [Header("Leaderboard")]
        public string raceTopStatisticName = "LeaderBoard_Event";
        public float cacheRefreshIntervalSeconds = 180f;
        public float manualRefreshCooldownSeconds = 30f;
        public int maxLeaderboardEntries = 10;

        private const string CanvasName = "Canvas_PolyJump";
        private const string PanelStartName = "Panel_Start";
        private const string PanelLeaderboardName = "Panel_Leaderboard";
        private const string ButtonOpenLeaderboardName = "Btn_Leaderboard";
        private const string EventEnabledKey = "EventEnabled";
        private const string EventStartKey = "EventStart";
        private const string EventEndKey = "EventEnd";
        private const string EventStartUtcKey = "EventStartUtc";
        private const string EventEndUtcKey = "EventEndUtc";
        private const string EventRewardsKey = "EventRewards";
        private const float EventInfoCacheSeconds = 10f;

        private static Font _cachedFont;
        private static LeaderboardUiManager _instance;

        private bool _wasLoggedIn;
        private float _refreshCooldownEndRealtime;
        private LeaderboardTab _activeTab = LeaderboardTab.Normal;

        private readonly LeaderboardCache _normalCache = new LeaderboardCache();
        private readonly LeaderboardCache _eventCache = new LeaderboardCache();

        private GameObject _panelStart;
        private Button _btnPlay;
        private Button _btnOpenLeaderboard;

        private GameObject _panelLeaderboard;
        private Button _btnBack;
        private Button _btnTabNormal;
        private Button _btnTabRaceTop;
        private Button _btnRefresh;
        private Text _txtRefresh;
        private Text _txtTabTitle;
        private Text _txtCurrentUser;
        private Text _txtCurrentScore;
        private Text _txtCurrentRank;
        private RectTransform _rowsContainer;
        private Text _txtEmpty;
        private GameObject _rowTemplate;
        private Text _txtEventTime;
        private Text _txtEventRewards;

        private readonly List<GameObject> _spawnedRows = new List<GameObject>();
        private readonly List<System.Action> _pendingEventInfoCallbacks = new List<System.Action>();

        private bool _eventInfoLoaded;
        private bool _eventInfoFetching;
        private bool _eventEnabled;
        private string _resolvedEventStatisticName = string.Empty;
        private string _eventStartRaw = string.Empty;
        private string _eventEndRaw = string.Empty;
        private DateTime _eventStartUtc = DateTime.MinValue;
        private DateTime _eventEndUtc = DateTime.MinValue;
        private string _eventRewards = "Chưa công bố";
        private float _eventInfoLastFetchRealtime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            LeaderboardUiManager existing = UnityEngine.Object.FindObjectOfType<LeaderboardUiManager>(true);
            if (existing != null)
            {
                return;
            }

            GameObject host = GameObject.Find("Managers");
            if (host == null)
            {
                host = new GameObject("Managers");
            }

            host.AddComponent<LeaderboardUiManager>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }

            _instance = this;
            ResolveRuntimeReferences();
            BuildOrBindUi();
            WireEvents();
            SyncAuthVisibility();
        }

        private void OnEnable()
        {
            ResolveRuntimeReferences();
            BuildOrBindUi();
            WireEvents();
            SyncAuthVisibility();
            UpdateRefreshButtonVisual();
        }

        private void Update()
        {
            ResolveRuntimeReferences();

            if (_btnOpenLeaderboard == null || _panelLeaderboard == null)
            {
                BuildOrBindUi();
                WireEvents();
            }

            bool loggedIn = PlayFabClientAPI.IsClientLoggedIn();
            if (loggedIn && !_wasLoggedIn)
            {
                OnLoginDetected();
            }
            else if (!loggedIn && _wasLoggedIn)
            {
                OnLogoutDetected();
            }

            _wasLoggedIn = loggedIn;
            SyncAuthVisibility();

            if (_panelLeaderboard != null && _panelLeaderboard.activeSelf)
            {
                bool shouldPollEventInfo = !_eventInfoFetching
                    && (!_eventInfoLoaded || (Time.unscaledTime - _eventInfoLastFetchRealtime) >= EventInfoCacheSeconds);
                if (shouldPollEventInfo)
                {
                    TryRefreshEventInfo(force: true);
                }

                if (_activeTab == LeaderboardTab.Normal && ShouldAutoRefresh(_normalCache))
                {
                    FetchNormalLeaderboard(force: false);
                }
                else if (_activeTab == LeaderboardTab.Event && ShouldAutoRefresh(_eventCache))
                {
                    FetchEventLeaderboard(force: false);
                }
            }

            UpdateRefreshButtonVisual();
        }

        private void ResolveRuntimeReferences()
        {
            if (playFabAuthManager == null)
            {
                playFabAuthManager = UnityEngine.Object.FindObjectOfType<PlayFabAuthManager>(true);
            }

            if (targetCanvas == null)
            {
                GameObject canvasObj = GameObject.Find(CanvasName);
                if (canvasObj != null)
                {
                    targetCanvas = canvasObj.GetComponent<Canvas>();
                }

                if (targetCanvas == null)
                {
                    targetCanvas = UnityEngine.Object.FindObjectOfType<Canvas>(true);
                }
            }

            if (_panelStart == null)
            {
                _panelStart = GameObject.Find(PanelStartName);
            }

            if (_btnPlay == null && _panelStart != null)
            {
                Transform play = _panelStart.transform.Find("Btn_Play");
                if (play != null)
                {
                    _btnPlay = play.GetComponent<Button>();
                }
            }
        }

        private void BuildOrBindUi()
        {
            if (targetCanvas == null)
            {
                return;
            }

            if (_panelStart == null)
            {
                _panelStart = GameObject.Find(PanelStartName);
            }

            BuildOrBindOpenLeaderboardButton();
            BuildOrBindLeaderboardPanel();
        }

        private void BuildOrBindOpenLeaderboardButton()
        {
            if (_panelStart == null)
            {
                return;
            }

            Transform existing = _panelStart.transform.Find(ButtonOpenLeaderboardName);
            bool wasExisting = existing != null;
            if (existing != null)
            {
                _btnOpenLeaderboard = existing.GetComponent<Button>();
            }

            if (_btnOpenLeaderboard == null)
            {
                _btnOpenLeaderboard = CreateButton(
                    ButtonOpenLeaderboardName,
                    _panelStart.transform,
                    "Bảng xếp hạng",
                    new Vector2(0.5f, 0.33f),
                    new Vector2(360f, 96f),
                    new Color32(0x12, 0x1A, 0x2F, 0xFF),
                    Color.white,
                    42);
            }

            if (!wasExisting && _btnOpenLeaderboard != null)
            {
                RectTransform rt = _btnOpenLeaderboard.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);

                    if (_btnPlay != null)
                    {
                        RectTransform playRt = _btnPlay.GetComponent<RectTransform>();
                        if (playRt != null)
                        {
                            rt.anchoredPosition = playRt.anchoredPosition + new Vector2(0f, -132f);
                        }
                    }
                }
            }
        }

        private void BuildOrBindLeaderboardPanel()
        {
            Transform existing = targetCanvas.transform.Find(PanelLeaderboardName);
            bool panelCreated = existing == null;
            if (existing != null)
            {
                _panelLeaderboard = existing.gameObject;
            }

            if (_panelLeaderboard == null)
            {
                _panelLeaderboard = new GameObject(PanelLeaderboardName, typeof(RectTransform), typeof(Image));
                _panelLeaderboard.transform.SetParent(targetCanvas.transform, false);
                panelCreated = true;
            }

            if (panelCreated)
            {
                RectTransform rootRt = _panelLeaderboard.GetComponent<RectTransform>();
                rootRt.anchorMin = Vector2.zero;
                rootRt.anchorMax = Vector2.one;
                rootRt.offsetMin = Vector2.zero;
                rootRt.offsetMax = Vector2.zero;

                Image bg = _panelLeaderboard.GetComponent<Image>();
                bg.color = new Color(0.04f, 0.09f, 0.15f, 0.93f);
            }

            _panelLeaderboard.SetActive(false);

            EnsureLeaderboardChildren();
        }

        private void EnsureLeaderboardChildren()
        {
            if (_panelLeaderboard == null)
            {
                return;
            }

            RectTransform root = _panelLeaderboard.GetComponent<RectTransform>();

            bool cardCreated;
            GameObject card = FindOrCreateChild(_panelLeaderboard.transform, "Card", out cardCreated, typeof(RectTransform), typeof(Image));
            if (cardCreated)
            {
                RectTransform cardRt = card.GetComponent<RectTransform>();
                cardRt.anchorMin = new Vector2(0.5f, 0.5f);
                cardRt.anchorMax = new Vector2(0.5f, 0.5f);
                cardRt.pivot = new Vector2(0.5f, 0.5f);
                cardRt.sizeDelta = new Vector2(1020f, 1740f);
                cardRt.anchoredPosition = Vector2.zero;
                Image cardImage = card.GetComponent<Image>();
                cardImage.color = new Color32(0xF6, 0xF8, 0xFC, 0xFF);
            }

            _btnBack = EnsureButton(card.transform, "Btn_Back", "Quay lại", new Vector2(0.12f, 0.93f), new Vector2(220f, 74f), new Color32(0x12, 0x1A, 0x2F, 0xFF), Color.white, 30);

            Transform legacyTitle = card.transform.Find("Txt_Title");
            if (legacyTitle != null)
            {
                Destroy(legacyTitle.gameObject);
            }

            EnsureLabel(card.transform, "Txt_LeaderboardTitle", "Bảng Xếp Hạng", 58, new Color32(0xF3, 0x70, 0x21, 0xFF), new Vector2(0.5f, 0.93f), new Vector2(620f, 96f), TextAnchor.MiddleCenter, FontStyle.Bold);

            _txtTabTitle = EnsureLabel(card.transform, "Txt_TabTitle", "Thường", 34, new Color32(0x12, 0x1A, 0x2F, 0xFF), new Vector2(0.5f, 0.865f), new Vector2(520f, 68f), TextAnchor.MiddleCenter, FontStyle.Bold);

            _txtCurrentUser = EnsureLabel(card.transform, "Txt_CurrentUser", "Người chơi: --", 30, new Color32(0x12, 0x1A, 0x2F, 0xFF), new Vector2(0.5f, 0.815f), new Vector2(900f, 58f), TextAnchor.MiddleCenter, FontStyle.Normal);
            _txtCurrentScore = EnsureLabel(card.transform, "Txt_CurrentScore", "Điểm: --", 30, new Color32(0x12, 0x1A, 0x2F, 0xFF), new Vector2(0.36f, 0.775f), new Vector2(360f, 58f), TextAnchor.MiddleCenter, FontStyle.Bold);
            _txtCurrentRank = EnsureLabel(card.transform, "Txt_CurrentRank", "Hạng: --", 30, new Color32(0x12, 0x1A, 0x2F, 0xFF), new Vector2(0.64f, 0.775f), new Vector2(360f, 58f), TextAnchor.MiddleCenter, FontStyle.Bold);

            bool headerCreated;
            GameObject headerRow = FindOrCreateChild(card.transform, "Header_Row", out headerCreated, typeof(RectTransform), typeof(Image));
            if (headerCreated)
            {
                RectTransform headerRt = headerRow.GetComponent<RectTransform>();
                headerRt.anchorMin = new Vector2(0.08f, 0.73f);
                headerRt.anchorMax = new Vector2(0.92f, 0.775f);
                headerRt.offsetMin = Vector2.zero;
                headerRt.offsetMax = Vector2.zero;
                Image headerImage = headerRow.GetComponent<Image>();
                headerImage.color = new Color32(0xD9, 0xE4, 0xFA, 0xFF);
            }

            EnsureColumnText(headerRow.transform, "Txt_HeaderRank", "Thứ hạng", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(220f, 52f), TextAnchor.MiddleLeft, 26, new Color32(0x12, 0x1A, 0x2F, 0xFF), FontStyle.Bold);
            EnsureColumnText(headerRow.transform, "Txt_HeaderName", "Người chơi", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 52f), TextAnchor.MiddleCenter, 26, new Color32(0x12, 0x1A, 0x2F, 0xFF), FontStyle.Bold);
            EnsureColumnText(headerRow.transform, "Txt_HeaderScore", "Điểm", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(180f, 52f), TextAnchor.MiddleRight, 26, new Color32(0x12, 0x1A, 0x2F, 0xFF), FontStyle.Bold);

            _txtEventTime = EnsureLabel(card.transform, "Txt_EventTime", "Thời gian: --", 24, new Color32(0x12, 0x1A, 0x2F, 0xFF), new Vector2(0.5f, 0.695f), new Vector2(860f, 42f), TextAnchor.MiddleLeft, FontStyle.Normal);
            _txtEventRewards = EnsureLabel(card.transform, "Txt_EventRewards", "Quà sự kiện: --", 24, new Color32(0x12, 0x1A, 0x2F, 0xFF), new Vector2(0.5f, 0.665f), new Vector2(860f, 42f), TextAnchor.MiddleLeft, FontStyle.Normal);

            bool scrollRootCreated;
            GameObject scrollRoot = FindOrCreateChild(card.transform, "ScrollRoot", out scrollRootCreated, typeof(RectTransform), typeof(Image));
            if (scrollRootCreated)
            {
                RectTransform scrollRootRt = scrollRoot.GetComponent<RectTransform>();
                scrollRootRt.anchorMin = new Vector2(0.08f, 0.17f);
                scrollRootRt.anchorMax = new Vector2(0.92f, 0.62f);
                scrollRootRt.offsetMin = Vector2.zero;
                scrollRootRt.offsetMax = Vector2.zero;
                Image scrollBg = scrollRoot.GetComponent<Image>();
                scrollBg.color = new Color32(0xE9, 0xEE, 0xF7, 0xFF);
            }

            bool viewportCreated;
            GameObject viewport = FindOrCreateChild(scrollRoot.transform, "Viewport", out viewportCreated, typeof(RectTransform), typeof(Image), typeof(Mask));
            RectTransform viewportRt = viewport.GetComponent<RectTransform>();
            if (viewportCreated)
            {
                viewportRt.anchorMin = Vector2.zero;
                viewportRt.anchorMax = Vector2.one;
                viewportRt.offsetMin = new Vector2(14f, 14f);
                viewportRt.offsetMax = new Vector2(-14f, -14f);
                Image viewportImage = viewport.GetComponent<Image>();
                viewportImage.color = new Color32(0xF9, 0xFB, 0xFF, 0xFF);
                Mask mask = viewport.GetComponent<Mask>();
                mask.showMaskGraphic = true;
            }

            bool contentCreated;
            GameObject content = FindOrCreateChild(viewport.transform, "Content", out contentCreated, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            _rowsContainer = content.GetComponent<RectTransform>();
            if (contentCreated)
            {
                _rowsContainer.anchorMin = new Vector2(0f, 1f);
                _rowsContainer.anchorMax = new Vector2(1f, 1f);
                _rowsContainer.pivot = new Vector2(0.5f, 1f);
                _rowsContainer.anchoredPosition = Vector2.zero;
                _rowsContainer.sizeDelta = new Vector2(0f, 0f);

                VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
                layout.spacing = 8f;
                layout.padding = new RectOffset(8, 8, 8, 8);
                layout.childControlHeight = true;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;

                ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            }

            ScrollRect scrollRect = scrollRoot.GetComponent<ScrollRect>();
            if (scrollRect == null)
            {
                scrollRect = scrollRoot.AddComponent<ScrollRect>();
            }

            if (scrollRect.viewport == null)
            {
                scrollRect.viewport = viewportRt;
            }

            if (scrollRect.content == null)
            {
                scrollRect.content = _rowsContainer;
            }

            if (scrollRootCreated)
            {
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.scrollSensitivity = 20f;
            }

            Transform template = _rowsContainer.Find("Row_1");
            if (template != null)
            {
                _rowTemplate = template.gameObject;
            }

            if (_rowTemplate == null)
            {
                _rowTemplate = CreateDefaultRowTemplate(_rowsContainer);
            }

            if (_rowTemplate != null)
            {
                _rowTemplate.name = "Row_1";
                _rowTemplate.SetActive(false);
                _rowTemplate.transform.SetAsFirstSibling();
            }

            _txtEmpty = EnsureLabel(viewport.transform, "Txt_Empty", "Chưa có xếp hạng", 34, new Color32(0x5D, 0x6B, 0x85, 0xFF), new Vector2(0.5f, 0.5f), new Vector2(600f, 110f), TextAnchor.MiddleCenter, FontStyle.Bold);

            _btnTabNormal = EnsureButton(card.transform, "Btn_TabNormal", "Thường", new Vector2(0.33f, 0.09f), new Vector2(230f, 78f), new Color32(0x12, 0x1A, 0x2F, 0xFF), Color.white, 30);
            _btnTabRaceTop = EnsureButton(card.transform, "Btn_TabRaceTop", "Sự kiện", new Vector2(0.56f, 0.09f), new Vector2(230f, 78f), new Color32(0x12, 0x1A, 0x2F, 0xFF), Color.white, 30);
            _btnRefresh = EnsureButton(card.transform, "Btn_RefreshLeaderboard", "Làm mới", new Vector2(0.79f, 0.09f), new Vector2(230f, 78f), new Color32(0xF3, 0x70, 0x21, 0xFF), Color.white, 30);
            _txtRefresh = _btnRefresh != null ? _btnRefresh.GetComponentInChildren<Text>(true) : null;

            SetButtonText(_btnTabNormal, "Thường");
            SetButtonText(_btnTabRaceTop, "Sự kiện");
            ApplyEventInfoToUi();
            UpdateEventInfoVisibility();

            root.SetAsLastSibling();
        }

        private void WireEvents()
        {
            if (_btnOpenLeaderboard != null)
            {
                _btnOpenLeaderboard.onClick.RemoveListener(OnOpenLeaderboardPressed);
                _btnOpenLeaderboard.onClick.AddListener(OnOpenLeaderboardPressed);
            }

            if (_btnBack != null)
            {
                _btnBack.onClick.RemoveListener(OnBackPressed);
                _btnBack.onClick.AddListener(OnBackPressed);
            }

            if (_btnTabNormal != null)
            {
                _btnTabNormal.onClick.RemoveListener(OnTabNormalPressed);
                _btnTabNormal.onClick.AddListener(OnTabNormalPressed);
            }

            if (_btnTabRaceTop != null)
            {
                _btnTabRaceTop.onClick.RemoveListener(OnTabRacePressed);
                _btnTabRaceTop.onClick.AddListener(OnTabRacePressed);
            }

            if (_btnRefresh != null)
            {
                _btnRefresh.onClick.RemoveListener(OnRefreshPressed);
                _btnRefresh.onClick.AddListener(OnRefreshPressed);
            }
        }

        private void OnOpenLeaderboardPressed()
        {
            if (_panelLeaderboard == null)
            {
                return;
            }

            if (_panelStart != null)
            {
                _panelStart.SetActive(false);
            }

            if (playFabAuthManager != null && playFabAuthManager.panelStart != null)
            {
                playFabAuthManager.panelStart.SetActive(false);
            }

            _panelLeaderboard.SetActive(true);
            _panelLeaderboard.transform.SetAsLastSibling();
            SetActiveTab(_activeTab);
            TryRefreshEventInfo(force: true);

            if (!PlayFabClientAPI.IsClientLoggedIn())
            {
                _normalCache.emptyMessage = "Vui lòng đăng nhập để xem xếp hạng";
                RefreshCurrentTabUi();
                return;
            }

            if (_activeTab == LeaderboardTab.Normal && ShouldAutoRefresh(_normalCache))
            {
                FetchNormalLeaderboard(force: false);
            }
            else
            {
                RefreshCurrentTabUi();
            }
        }

        private void OnBackPressed()
        {
            if (_panelLeaderboard != null)
            {
                _panelLeaderboard.SetActive(false);
            }

            bool canShowStart = true;
            if (playFabAuthManager != null && playFabAuthManager.panelAuth != null)
            {
                canShowStart = !playFabAuthManager.panelAuth.activeSelf;
            }

            if (GameManager.Instance != null)
            {
                canShowStart = canShowStart && GameManager.Instance.CurrentState == GameState.Menu;
            }

            if (_panelStart != null)
            {
                _panelStart.SetActive(canShowStart);
            }

            if (playFabAuthManager != null && playFabAuthManager.panelStart != null)
            {
                playFabAuthManager.panelStart.SetActive(canShowStart);
            }
        }

        private void OnTabNormalPressed()
        {
            SetActiveTab(LeaderboardTab.Normal);
            if (ShouldAutoRefresh(_normalCache))
            {
                FetchNormalLeaderboard(force: false);
            }
            else
            {
                RefreshCurrentTabUi();
            }
        }

        private void OnTabRacePressed()
        {
            SetActiveTab(LeaderboardTab.Event);
            if (ShouldAutoRefresh(_eventCache))
            {
                FetchEventLeaderboard(force: false);
            }
            else
            {
                RefreshCurrentTabUi();
            }
        }

        private void OnRefreshPressed()
        {
            if (Time.unscaledTime < _refreshCooldownEndRealtime)
            {
                return;
            }

            _refreshCooldownEndRealtime = Time.unscaledTime + Mathf.Max(5f, manualRefreshCooldownSeconds);

            if (_activeTab == LeaderboardTab.Normal)
            {
                FetchNormalLeaderboard(force: true);
            }
            else
            {
                FetchEventLeaderboard(force: true);
            }

            UpdateRefreshButtonVisual();
        }

        private void SetActiveTab(LeaderboardTab tab)
        {
            _activeTab = tab;
            if (_btnTabNormal != null)
            {
                _btnTabNormal.interactable = _activeTab != LeaderboardTab.Normal;
            }

            if (_btnTabRaceTop != null)
            {
                _btnTabRaceTop.interactable = _activeTab != LeaderboardTab.Event;
            }

            if (_txtTabTitle != null)
            {
                _txtTabTitle.text = _activeTab == LeaderboardTab.Normal ? "Bảng xếp hạng thường" : "Bảng xếp hạng sự kiện";
            }

            UpdateEventInfoVisibility();

            UpdateRefreshButtonVisual();
        }

        private void UpdateEventInfoVisibility()
        {
            bool showEventInfo = _activeTab == LeaderboardTab.Event && GetEventWindowState() == EventWindowState.Active;

            if (_txtEventTime != null)
            {
                _txtEventTime.gameObject.SetActive(showEventInfo);
            }

            if (_txtEventRewards != null)
            {
                _txtEventRewards.gameObject.SetActive(showEventInfo);
            }
        }

        private void OnLoginDetected()
        {
            _normalCache.Clear();
            _eventCache.Clear();
            _normalCache.emptyMessage = "Đang tải bảng xếp hạng...";
            _eventCache.emptyMessage = "Đang tải bảng xếp hạng...";
            SyncAuthVisibility();
            TryRefreshEventInfo(force: true);
            FetchNormalLeaderboard(force: true);
            FetchEventLeaderboard(force: true);
        }

        private void OnLogoutDetected()
        {
            _normalCache.Clear();
            _eventCache.Clear();

            if (_panelLeaderboard != null)
            {
                _panelLeaderboard.SetActive(false);
            }

            ClearRows();
            SetCurrentPlayerInfo("--", 0, 0, false);
            UpdateEmptyText("Chưa có xếp hạng");
            UpdateRefreshButtonVisual();
            SyncAuthVisibility();
        }

        private bool ShouldAutoRefresh(LeaderboardCache cache)
        {
            if (!PlayFabClientAPI.IsClientLoggedIn())
            {
                return false;
            }

            if (cache.isFetching)
            {
                return false;
            }

            if (!cache.hasData)
            {
                return true;
            }

            return Time.unscaledTime - cache.lastFetchRealtime >= Mathf.Max(30f, cacheRefreshIntervalSeconds);
        }

        private void FetchNormalLeaderboard(bool force)
        {
            if (!PlayFabClientAPI.IsClientLoggedIn())
            {
                _normalCache.isFetching = false;
                _normalCache.emptyMessage = "Vui lòng đăng nhập để xem xếp hạng";
                RefreshCurrentTabUi();
                return;
            }

            if (_normalCache.isFetching)
            {
                return;
            }

            if (!force && !ShouldAutoRefresh(_normalCache))
            {
                return;
            }

            _normalCache.isFetching = true;
            _normalCache.emptyMessage = "Đang tải bảng xếp hạng...";
            UpdateRefreshButtonVisual();

            string statisticName = ResolveNormalStatisticName();
            var topRequest = new GetLeaderboardRequest
            {
                StatisticName = statisticName,
                StartPosition = 0,
                MaxResultsCount = Mathf.Clamp(maxLeaderboardEntries, 1, 100)
            };

            try
            {
                PlayFabClientAPI.GetLeaderboard(topRequest,
                    topResult =>
                    {
                        _normalCache.topEntries.Clear();
                        if (topResult != null && topResult.Leaderboard != null)
                        {
                            _normalCache.topEntries.AddRange(topResult.Leaderboard);
                        }

                        FetchCurrentPlayerPosition(statisticName);
                    },
                    error =>
                    {
                        _normalCache.isFetching = false;
                        _normalCache.emptyMessage = "Không thể tải bảng xếp hạng";
                        Debug.LogError(error.GenerateErrorReport());
                        UpdateRefreshButtonVisual();
                        RefreshCurrentTabUi();
                    });
            }
            catch (PlayFabException)
            {
                _normalCache.isFetching = false;
                _normalCache.emptyMessage = "Vui lòng đăng nhập để xem xếp hạng";
                UpdateRefreshButtonVisual();
                RefreshCurrentTabUi();
            }
        }

        private void FetchCurrentPlayerPosition(string statisticName)
        {
            if (!PlayFabClientAPI.IsClientLoggedIn())
            {
                FinalizeNormalCacheWithoutRank();
                return;
            }

            var aroundRequest = new GetLeaderboardAroundPlayerRequest
            {
                StatisticName = statisticName,
                MaxResultsCount = 1
            };

            try
            {
                PlayFabClientAPI.GetLeaderboardAroundPlayer(aroundRequest,
                    aroundResult =>
                    {
                        _normalCache.hasPlayerInfo = false;
                        bool hasTopEntries = _normalCache.topEntries.Count > 0;
                        if (hasTopEntries && aroundResult != null && aroundResult.Leaderboard != null && aroundResult.Leaderboard.Count > 0)
                        {
                            PlayerLeaderboardEntry entry = aroundResult.Leaderboard[0];
                            if (entry != null)
                            {
                                _normalCache.playerName = NormalizeDisplayName(entry.DisplayName, entry.PlayFabId);
                                _normalCache.playerScore = Mathf.Max(0, entry.StatValue);
                                _normalCache.playerRank = entry.Position + 1;
                                _normalCache.hasPlayerInfo = true;
                            }
                        }

                        if (!_normalCache.hasPlayerInfo)
                        {
                            _normalCache.playerName = ResolveCurrentUserName();
                            _normalCache.playerScore = playFabAuthManager != null ? playFabAuthManager.GetCachedLeaderboardHighscore() : 0;
                            _normalCache.playerRank = 0;
                        }

                        _normalCache.hasData = true;
                        _normalCache.lastFetchRealtime = Time.unscaledTime;
                        _normalCache.isFetching = false;
                        _normalCache.emptyMessage = _normalCache.topEntries.Count > 0 ? string.Empty : "Chưa có xếp hạng";

                        UpdateRefreshButtonVisual();
                        RefreshCurrentTabUi();
                    },
                    error =>
                    {
                        FinalizeNormalCacheWithoutRank();
                        Debug.LogError(error.GenerateErrorReport());
                    });
            }
            catch (PlayFabException)
            {
                FinalizeNormalCacheWithoutRank();
            }
        }

        private void FinalizeNormalCacheWithoutRank()
        {
            _normalCache.playerName = ResolveCurrentUserName();
            _normalCache.playerScore = playFabAuthManager != null ? playFabAuthManager.GetCachedLeaderboardHighscore() : 0;
            _normalCache.playerRank = 0;
            _normalCache.hasPlayerInfo = false;
            _normalCache.hasData = true;
            _normalCache.lastFetchRealtime = Time.unscaledTime;
            _normalCache.isFetching = false;
            _normalCache.emptyMessage = _normalCache.topEntries.Count > 0 ? string.Empty : "Chưa có xếp hạng";

            UpdateRefreshButtonVisual();
            RefreshCurrentTabUi();
        }

        private void RefreshCurrentTabUi()
        {
            if (_panelLeaderboard == null || !_panelLeaderboard.activeSelf)
            {
                return;
            }

            if (_activeTab == LeaderboardTab.Normal)
            {
                RefreshTabUi(_normalCache, "Bảng xếp hạng thường");
            }
            else
            {
                RefreshTabUi(_eventCache, "Bảng xếp hạng sự kiện");
            }
        }

        private void FetchEventLeaderboard(bool force)
        {
            if (!PlayFabClientAPI.IsClientLoggedIn())
            {
                _eventCache.isFetching = false;
                _eventCache.emptyMessage = "Vui lòng đăng nhập để xem xếp hạng";
                RefreshCurrentTabUi();
                return;
            }

            bool shouldRefreshEventInfo = !_eventInfoLoaded
                || (Time.unscaledTime - _eventInfoLastFetchRealtime) >= EventInfoCacheSeconds;

            if (shouldRefreshEventInfo)
            {
                TryRefreshEventInfo(
                    force: true,
                    onCompleted: () => FetchEventLeaderboard(force));
                return;
            }

            if (!IsEventOpenNow())
            {
                _eventCache.topEntries.Clear();
                _eventCache.playerName = ResolveCurrentUserName();
                _eventCache.playerScore = 0;
                _eventCache.playerRank = 0;
                _eventCache.hasPlayerInfo = false;
                _eventCache.hasData = true;
                _eventCache.isFetching = false;
                _eventCache.lastFetchRealtime = Time.unscaledTime;
                _eventCache.emptyMessage = BuildEventUnavailableMessage();

                UpdateRefreshButtonVisual();
                RefreshCurrentTabUi();
                return;
            }

            if (_eventCache.isFetching)
            {
                return;
            }

            if (!force && !ShouldAutoRefresh(_eventCache))
            {
                return;
            }

            _eventCache.isFetching = true;
            _eventCache.emptyMessage = "Đang tải bảng xếp hạng...";
            UpdateRefreshButtonVisual();

            string statisticName = ResolveEventStatisticName();
            if (!string.Equals(_resolvedEventStatisticName, statisticName, StringComparison.Ordinal))
            {
                _resolvedEventStatisticName = statisticName;
                _eventCache.topEntries.Clear();
                _eventCache.hasData = false;
                _eventCache.lastFetchRealtime = 0f;
            }

            var topRequest = new GetLeaderboardRequest
            {
                StatisticName = statisticName,
                StartPosition = 0,
                MaxResultsCount = Mathf.Clamp(maxLeaderboardEntries, 1, 100)
            };

            try
            {
                PlayFabClientAPI.GetLeaderboard(topRequest,
                    topResult =>
                    {
                        _eventCache.topEntries.Clear();
                        if (topResult != null && topResult.Leaderboard != null)
                        {
                            _eventCache.topEntries.AddRange(topResult.Leaderboard);
                        }

                        FetchEventPlayerPosition(statisticName);
                    },
                    error =>
                    {
                        _eventCache.isFetching = false;
                        _eventCache.emptyMessage = "Không thể tải bảng xếp hạng";
                        Debug.LogError(error.GenerateErrorReport());
                        UpdateRefreshButtonVisual();
                        RefreshCurrentTabUi();
                    });
            }
            catch (PlayFabException)
            {
                _eventCache.isFetching = false;
                _eventCache.emptyMessage = "Vui lòng đăng nhập để xem xếp hạng";
                UpdateRefreshButtonVisual();
                RefreshCurrentTabUi();
            }
        }

        private void FetchEventPlayerPosition(string statisticName)
        {
            if (!PlayFabClientAPI.IsClientLoggedIn())
            {
                FinalizeEventCacheWithoutRank();
                return;
            }

            var aroundRequest = new GetLeaderboardAroundPlayerRequest
            {
                StatisticName = statisticName,
                MaxResultsCount = 1
            };

            try
            {
                PlayFabClientAPI.GetLeaderboardAroundPlayer(aroundRequest,
                    aroundResult =>
                    {
                        _eventCache.hasPlayerInfo = false;
                        bool hasTopEntries = _eventCache.topEntries.Count > 0;
                        if (hasTopEntries && aroundResult != null && aroundResult.Leaderboard != null && aroundResult.Leaderboard.Count > 0)
                        {
                            PlayerLeaderboardEntry entry = aroundResult.Leaderboard[0];
                            if (entry != null)
                            {
                                _eventCache.playerName = NormalizeDisplayName(entry.DisplayName, entry.PlayFabId);
                                _eventCache.playerScore = Mathf.Max(0, entry.StatValue);
                                _eventCache.playerRank = entry.Position + 1;
                                _eventCache.hasPlayerInfo = true;
                            }
                        }

                        if (!_eventCache.hasPlayerInfo)
                        {
                            _eventCache.playerName = ResolveCurrentUserName();
                            _eventCache.playerScore = 0;
                            _eventCache.playerRank = 0;
                        }

                        _eventCache.hasData = true;
                        _eventCache.lastFetchRealtime = Time.unscaledTime;
                        _eventCache.isFetching = false;
                        _eventCache.emptyMessage = _eventCache.topEntries.Count > 0 ? string.Empty : "Chưa có xếp hạng";

                        UpdateRefreshButtonVisual();
                        RefreshCurrentTabUi();
                    },
                    error =>
                    {
                        FinalizeEventCacheWithoutRank();
                        Debug.LogError(error.GenerateErrorReport());
                    });
            }
            catch (PlayFabException)
            {
                FinalizeEventCacheWithoutRank();
            }
        }

        private void FinalizeEventCacheWithoutRank()
        {
            _eventCache.playerName = ResolveCurrentUserName();
            _eventCache.playerScore = 0;
            _eventCache.playerRank = 0;
            _eventCache.hasPlayerInfo = false;
            _eventCache.hasData = true;
            _eventCache.lastFetchRealtime = Time.unscaledTime;
            _eventCache.isFetching = false;
            _eventCache.emptyMessage = _eventCache.topEntries.Count > 0 ? string.Empty : "Chưa có xếp hạng";

            UpdateRefreshButtonVisual();
            RefreshCurrentTabUi();
        }

        private void RefreshTabUi(LeaderboardCache cache, string tabTitle)
        {
            if (_txtTabTitle != null)
            {
                _txtTabTitle.text = tabTitle;
            }

            int score = cache.hasPlayerInfo ? cache.playerScore : 0;
            int rank = cache.hasPlayerInfo ? cache.playerRank : 0;
            string name = cache.hasPlayerInfo ? cache.playerName : ResolveCurrentUserName();
            SetCurrentPlayerInfo(name, score, rank, cache.hasPlayerInfo);

            ClearRows();
            if (cache.topEntries.Count == 0)
            {
                UpdateEmptyText(string.IsNullOrWhiteSpace(cache.emptyMessage) ? "Chưa có xếp hạng" : cache.emptyMessage);
                return;
            }

            UpdateEmptyText(string.Empty);
            for (int i = 0; i < cache.topEntries.Count; i++)
            {
                PlayerLeaderboardEntry entry = cache.topEntries[i];
                if (entry == null)
                {
                    continue;
                }

                CreateLeaderboardRow(i, entry);
            }
        }

        private void SetCurrentPlayerInfo(string userName, int score, int rank, bool hasRank)
        {
            if (_txtCurrentUser != null)
            {
                _txtCurrentUser.text = "Người chơi: " + (string.IsNullOrWhiteSpace(userName) ? "--" : userName);
            }

            if (_txtCurrentScore != null)
            {
                _txtCurrentScore.text = "Điểm: " + Mathf.Max(0, score);
            }

            if (_txtCurrentRank != null)
            {
                _txtCurrentRank.text = hasRank && rank > 0 ? "Hạng: " + rank : "Hạng: --";
            }
        }

        private void CreateLeaderboardRow(int index, PlayerLeaderboardEntry entry)
        {
            if (_rowsContainer == null || _rowTemplate == null)
            {
                return;
            }

            GameObject row = Instantiate(_rowTemplate, _rowsContainer);
            row.name = "Row_" + (index + 1);
            row.SetActive(true);

            Image bg = row.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = index % 2 == 0
                    ? new Color32(0xF2, 0xF6, 0xFF, 0xFF)
                    : new Color32(0xE7, 0xEE, 0xFD, 0xFF);
            }

            SetRowText(row.transform, "Txt_Rank", "#" + (entry.Position + 1));

            string displayName = NormalizeDisplayName(entry.DisplayName, entry.PlayFabId);
            SetRowText(row.transform, "Txt_Name", displayName);

            SetRowText(row.transform, "Txt_Score", Mathf.Max(0, entry.StatValue).ToString());

            _spawnedRows.Add(row);
        }

        private void ClearRows()
        {
            for (int i = 0; i < _spawnedRows.Count; i++)
            {
                GameObject row = _spawnedRows[i];
                if (row != null)
                {
                    Destroy(row);
                }
            }

            _spawnedRows.Clear();

            if (_rowTemplate != null)
            {
                _rowTemplate.SetActive(false);
                _rowTemplate.transform.SetAsFirstSibling();
            }
        }

        private static void SetRowText(Transform rowTransform, string textName, string value)
        {
            if (rowTransform == null)
            {
                return;
            }

            Transform t = rowTransform.Find(textName);
            if (t == null)
            {
                return;
            }

            Text txt = t.GetComponent<Text>();
            if (txt != null)
            {
                txt.text = value;
            }
        }

        private static GameObject CreateDefaultRowTemplate(Transform parent)
        {
            if (parent == null)
            {
                return null;
            }

            GameObject row = new GameObject("Row_1", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(parent, false);

            RectTransform rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 74f);

            Image bg = row.GetComponent<Image>();
            bg.color = new Color32(0xF2, 0xF6, 0xFF, 0xFF);

            EnsureColumnText(row.transform, "Txt_Rank", "#1", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(220f, 56f), TextAnchor.MiddleLeft, 28, new Color32(0x12, 0x1A, 0x2F, 0xFF), FontStyle.Normal);
            EnsureColumnText(row.transform, "Txt_Name", "Người chơi", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440f, 56f), TextAnchor.MiddleCenter, 28, new Color32(0x12, 0x1A, 0x2F, 0xFF), FontStyle.Normal);
            EnsureColumnText(row.transform, "Txt_Score", "0", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(180f, 56f), TextAnchor.MiddleRight, 28, new Color32(0xF3, 0x70, 0x21, 0xFF), FontStyle.Bold);

            row.SetActive(false);
            return row;
        }

        private void UpdateEmptyText(string message)
        {
            if (_txtEmpty == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                _txtEmpty.gameObject.SetActive(false);
                _txtEmpty.text = string.Empty;
            }
            else
            {
                _txtEmpty.gameObject.SetActive(true);
                _txtEmpty.text = message;
            }
        }

        private void UpdateRefreshButtonVisual()
        {
            if (_btnRefresh == null || _txtRefresh == null)
            {
                return;
            }

            bool fetching = _activeTab == LeaderboardTab.Normal ? _normalCache.isFetching : _eventCache.isFetching;
            float remain = Mathf.Max(0f, _refreshCooldownEndRealtime - Time.unscaledTime);

            if (fetching)
            {
                _btnRefresh.interactable = false;
                _txtRefresh.text = "Đang tải...";
                return;
            }

            if (remain > 0f)
            {
                _btnRefresh.interactable = false;
                _txtRefresh.text = "Làm mới (" + Mathf.CeilToInt(remain) + "s)";
                return;
            }

            _btnRefresh.interactable = true;
            _txtRefresh.text = "Làm mới";
        }

        private void SyncAuthVisibility()
        {
            if (_btnOpenLeaderboard != null)
            {
                bool isAuthMenuHidden = playFabAuthManager == null
                    || playFabAuthManager.panelAuth == null
                    || !playFabAuthManager.panelAuth.activeSelf;
                _btnOpenLeaderboard.interactable = isAuthMenuHidden;
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private string ResolveNormalStatisticName()
        {
            if (playFabAuthManager != null && !string.IsNullOrWhiteSpace(playFabAuthManager.leaderboardStatisticName))
            {
                return playFabAuthManager.leaderboardStatisticName;
            }

            return "LeaderBoard_Normal";
        }

        private string ResolveEventStatisticName()
        {
            string baseName;
            if (playFabAuthManager != null && !string.IsNullOrWhiteSpace(playFabAuthManager.eventLeaderboardStatisticName))
            {
                baseName = playFabAuthManager.eventLeaderboardStatisticName;
            }
            else if (!string.IsNullOrWhiteSpace(raceTopStatisticName))
            {
                baseName = raceTopStatisticName;
            }
            else
            {
                baseName = "LeaderBoard_Event";
            }

            string suffix = GetEventStatisticSuffix();
            if (string.IsNullOrWhiteSpace(suffix))
            {
                return baseName;
            }

            return baseName + "_" + suffix;
        }

        private string GetEventStatisticSuffix()
        {
            if (TryNormalizeEventSuffixFromRaw(_eventStartRaw, out string normalizedRaw))
            {
                return normalizedRaw;
            }

            if (_eventStartUtc != DateTime.MinValue)
            {
                return FormatEventTimeForDisplay(_eventStartUtc);
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

        private void TryRefreshEventInfo(bool force, System.Action onCompleted = null)
        {
            if (!PlayFabClientAPI.IsClientLoggedIn())
            {
                ApplyEventInfoToUi();
                if (onCompleted != null)
                {
                    onCompleted();
                }
                return;
            }

            bool hasFreshCache = _eventInfoLoaded
                && !force
                && (Time.unscaledTime - _eventInfoLastFetchRealtime) < EventInfoCacheSeconds;

            if (hasFreshCache)
            {
                ApplyEventInfoToUi();
                if (onCompleted != null)
                {
                    onCompleted();
                }
                return;
            }

            if (onCompleted != null)
            {
                _pendingEventInfoCallbacks.Add(onCompleted);
            }

            if (_eventInfoFetching)
            {
                return;
            }

            _eventInfoFetching = true;

            var request = new GetTitleDataRequest
            {
                Keys = new List<string>
                {
                    EventEnabledKey,
                    EventStartKey,
                    EventEndKey,
                    EventStartUtcKey,
                    EventEndUtcKey,
                    EventRewardsKey
                }
            };

            PlayFabClientAPI.GetTitleData(
                request,
                result =>
                {
                    _eventInfoFetching = false;
                    Dictionary<string, string> data = result != null ? result.Data : null;

                    _eventEnabled = ParseTitleDataBool(GetTitleDataValue(data, EventEnabledKey, "false"));
                    ResolveEventTimeFromTitleData(data, EventStartUtcKey, EventStartKey, out _eventStartRaw, out _eventStartUtc);
                    ResolveEventTimeFromTitleData(data, EventEndUtcKey, EventEndKey, out _eventEndRaw, out _eventEndUtc);
                    _eventRewards = GetTitleDataValue(data, EventRewardsKey, "Chưa công bố");

                    _eventInfoLoaded = true;
                    _eventInfoLastFetchRealtime = Time.unscaledTime;

                    ApplyEventInfoToUi();
                    OnEventConfigUpdated();
                    FlushPendingEventInfoCallbacks();
                },
                error =>
                {
                    _eventInfoFetching = false;
                    _eventInfoLoaded = true;
                    _eventEnabled = false;
                    _resolvedEventStatisticName = string.Empty;
                    _eventStartRaw = string.Empty;
                    _eventEndRaw = string.Empty;
                    _eventStartUtc = DateTime.MinValue;
                    _eventEndUtc = DateTime.MinValue;
                    _eventInfoLastFetchRealtime = Time.unscaledTime;

                    Debug.LogError(error.GenerateErrorReport());
                    ApplyEventInfoToUi();
                    OnEventConfigUpdated();
                    FlushPendingEventInfoCallbacks();
                });
        }

        private void OnEventConfigUpdated()
        {
            if (_panelLeaderboard == null || !_panelLeaderboard.activeSelf || _activeTab != LeaderboardTab.Event)
            {
                return;
            }

            if (!IsEventOpenNow())
            {
                _eventCache.topEntries.Clear();
                _eventCache.playerName = ResolveCurrentUserName();
                _eventCache.playerScore = 0;
                _eventCache.playerRank = 0;
                _eventCache.hasPlayerInfo = false;
                _eventCache.hasData = true;
                _eventCache.isFetching = false;
                _eventCache.lastFetchRealtime = Time.unscaledTime;
                _eventCache.emptyMessage = BuildEventUnavailableMessage();
                UpdateRefreshButtonVisual();
                RefreshCurrentTabUi();
                return;
            }

            if (!_eventCache.isFetching
                && (_eventCache.topEntries.Count == 0
                    || Time.unscaledTime - _eventCache.lastFetchRealtime >= Mathf.Max(30f, cacheRefreshIntervalSeconds)))
            {
                FetchEventLeaderboard(force: true);
                return;
            }

            RefreshCurrentTabUi();
        }

        private void FlushPendingEventInfoCallbacks()
        {
            if (_pendingEventInfoCallbacks.Count == 0)
            {
                return;
            }

            List<System.Action> callbacks = new List<System.Action>(_pendingEventInfoCallbacks);
            _pendingEventInfoCallbacks.Clear();

            for (int i = 0; i < callbacks.Count; i++)
            {
                System.Action callback = callbacks[i];
                if (callback != null)
                {
                    callback();
                }
            }
        }

        private bool IsEventOpenNow()
        {
            return GetEventWindowState() == EventWindowState.Active;
        }

        private EventWindowState GetEventWindowState()
        {
            if (!_eventInfoLoaded)
            {
                return EventWindowState.Unknown;
            }

            if (!_eventEnabled)
            {
                return EventWindowState.Disabled;
            }

            if (_eventStartUtc == DateTime.MinValue || _eventEndUtc == DateTime.MinValue)
            {
                return EventWindowState.Invalid;
            }

            if (_eventEndUtc < _eventStartUtc)
            {
                return EventWindowState.Invalid;
            }

            DateTime now = DateTime.UtcNow;
            if (now < _eventStartUtc)
            {
                return EventWindowState.Upcoming;
            }

            if (now > _eventEndUtc)
            {
                return EventWindowState.Ended;
            }

            return EventWindowState.Active;
        }

        private string BuildEventUnavailableMessage()
        {
            EventWindowState state = GetEventWindowState();
            if (state == EventWindowState.Upcoming)
            {
                return "Sắp có sự kiện vào lúc " + GetEventStartDisplayText();
            }

            return "Chưa có sự kiện";
        }

        private string GetEventStartDisplayText()
        {
            if (!string.IsNullOrWhiteSpace(_eventStartRaw))
            {
                return _eventStartRaw;
            }

            if (_eventStartUtc != DateTime.MinValue)
            {
                return FormatEventTimeForDisplay(_eventStartUtc);
            }

            return "--";
        }

        private void ApplyEventInfoToUi()
        {
            if (_txtEventTime != null)
            {
                string startText = !string.IsNullOrWhiteSpace(_eventStartRaw)
                    ? _eventStartRaw
                    : (_eventStartUtc == DateTime.MinValue ? "--" : FormatEventTimeForDisplay(_eventStartUtc));
                string endText = !string.IsNullOrWhiteSpace(_eventEndRaw)
                    ? _eventEndRaw
                    : (_eventEndUtc == DateTime.MinValue ? "--" : FormatEventTimeForDisplay(_eventEndUtc));
                _txtEventTime.text = "Thời gian sự kiện: " + startText + " - " + endText;
            }

            if (_txtEventRewards != null)
            {
                _txtEventRewards.text = "Quà sự kiện: " + (string.IsNullOrWhiteSpace(_eventRewards) ? "Chưa công bố" : _eventRewards);
            }

            UpdateEventInfoVisibility();
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

            // Accept both "HH:mm:ss-yyyy:MM:dd" and variants like "HH-mm-ss - yyyy-MM-dd".
            Match timeDate = Regex.Match(text, @"^\s*(\d{1,2})\D+(\d{1,2})\D+(\d{1,2})\s*-\s*(\d{4})\D+(\d{1,2})\D+(\d{1,2})\s*$");
            if (timeDate.Success)
            {
                if (!TryBuildUtcFromParts(
                    timeDate.Groups[4].Value,
                    timeDate.Groups[5].Value,
                    timeDate.Groups[6].Value,
                    timeDate.Groups[1].Value,
                    timeDate.Groups[2].Value,
                    timeDate.Groups[3].Value,
                    out utc))
                {
                    return false;
                }

                return true;
            }

            // Also accept reversed order: "yyyy:MM:dd-HH:mm:ss".
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

        private static string FormatEventTimeForDisplay(DateTime utc)
        {
            DateTime vn = DateTime.SpecifyKind(utc, DateTimeKind.Utc).AddHours(7);
            return vn.ToString("HH:mm:ss-yyyy:MM:dd");
        }

        private static void SetButtonText(Button button, string value)
        {
            if (button == null)
            {
                return;
            }

            Text txt = button.GetComponentInChildren<Text>(true);
            if (txt != null)
            {
                txt.text = value;
            }
        }

        private string ResolveCurrentUserName()
        {
            if (playFabAuthManager != null && playFabAuthManager.txt_StartUserName != null)
            {
                string uiName = playFabAuthManager.txt_StartUserName.text;
                if (!string.IsNullOrWhiteSpace(uiName))
                {
                    return uiName.Trim();
                }
            }

            return "--";
        }

        private static string NormalizeDisplayName(string displayName, string playFabId)
        {
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(playFabId))
            {
                if (playFabId.Length <= 6)
                {
                    return playFabId;
                }

                return "Người chơi " + playFabId.Substring(playFabId.Length - 6);
            }

            return "Người chơi";
        }

        private static GameObject FindOrCreateChild(Transform parent, string name, params Type[] components)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                GameObject obj = existing.gameObject;
                for (int i = 0; i < components.Length; i++)
                {
                    if (obj.GetComponent(components[i]) == null)
                    {
                        obj.AddComponent(components[i]);
                    }
                }

                return obj;
            }

            GameObject created = new GameObject(name, components);
            created.transform.SetParent(parent, false);
            return created;
        }

        private static Button EnsureButton(Transform parent, string name, string label, Vector2 anchor, Vector2 size, Color bg, Color textColor, int fontSize)
        {
            Transform existing = parent.Find(name);
            Button button = existing != null ? existing.GetComponent<Button>() : null;
            if (button == null)
            {
                button = CreateButton(name, parent, label, anchor, size, bg, textColor, fontSize);
            }

            return button;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 anchor, Vector2 size, Color bg, Color textColor, int fontSize)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;

            Image image = obj.GetComponent<Image>();
            image.color = bg;

            Button button = obj.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.95f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.85f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.5f);
            button.colors = colors;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(obj.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            Text text = textObj.GetComponent<Text>();
            text.text = label;
            text.font = GetUiFont();
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = textColor;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            return button;
        }

        private static Text EnsureLabel(Transform parent, string name, string value, int size, Color color, Vector2 anchor, Vector2 rectSize, TextAnchor alignment, FontStyle style)
        {
            Transform existing = parent.Find(name);
            bool created = existing == null;
            Text text = existing != null ? existing.GetComponent<Text>() : null;
            if (text == null)
            {
                GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
                obj.transform.SetParent(parent, false);
                text = obj.GetComponent<Text>();
                created = true;
            }

            if (created)
            {
                RectTransform rt = text.GetComponent<RectTransform>();
                rt.anchorMin = anchor;
                rt.anchorMax = anchor;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = rectSize;
                rt.anchoredPosition = Vector2.zero;

                text.text = value;
                text.font = GetUiFont();
                text.fontSize = size;
                text.fontStyle = style;
                text.color = color;
                text.alignment = alignment;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate;
                text.raycastTarget = false;
            }
            return text;
        }

        private static Text EnsureColumnText(
            Transform parent,
            string name,
            string value,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPos,
            Vector2 size,
            TextAnchor alignment,
            int fontSize,
            Color color,
            FontStyle style)
        {
            Transform existing = parent.Find(name);
            bool created = existing == null;
            Text text;
            if (existing == null)
            {
                GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
                obj.transform.SetParent(parent, false);
                text = obj.GetComponent<Text>();
            }
            else
            {
                text = existing.GetComponent<Text>();
                if (text == null)
                {
                    text = existing.gameObject.AddComponent<Text>();
                    created = true;
                }
            }

            if (created)
            {
                RectTransform rt = text.GetComponent<RectTransform>();
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.pivot = pivot;
                rt.anchoredPosition = anchoredPos;
                rt.sizeDelta = size;

                text.text = value;
                text.font = GetUiFont();
                text.fontSize = fontSize;
                text.fontStyle = style;
                text.color = color;
                text.alignment = alignment;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Truncate;
                text.raycastTarget = false;
            }
            return text;
        }

        private static GameObject FindOrCreateChild(Transform parent, string name, out bool created, params Type[] components)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                created = false;
                GameObject obj = existing.gameObject;
                for (int i = 0; i < components.Length; i++)
                {
                    if (obj.GetComponent(components[i]) == null)
                    {
                        obj.AddComponent(components[i]);
                    }
                }

                return obj;
            }

            created = true;
            GameObject newObj = new GameObject(name, components);
            newObj.transform.SetParent(parent, false);
            return newObj;
        }

        private static Font GetUiFont()
        {
            if (_cachedFont != null)
            {
                return _cachedFont;
            }

            _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_cachedFont == null)
            {
                _cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return _cachedFont;
        }
    }
}
