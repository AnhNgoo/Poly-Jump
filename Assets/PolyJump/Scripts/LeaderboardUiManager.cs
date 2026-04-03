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
    /// <summary>
    /// Quản lý giao diện bảng xếp hạng, tải dữ liệu top và đồng bộ trạng thái theo tab thường/sự kiện.
    /// </summary>
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

        /// <summary>
        /// Lưu trạng thái cache dữ liệu bảng xếp hạng để giảm số lần gọi API không cần thiết.
        /// </summary>
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

            /// <summary>
            /// Xóa nghiệp vụ tương ứng phục vụ luồng xử lý hiện tại của hệ thống.
            /// </summary>
            public void Clear()
            {
                // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
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
        /// <summary>
        /// Thực hiện nghiệp vụ Bootstrap theo ngữ cảnh sử dụng của script.
        /// </summary>
        private static void Bootstrap()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            LeaderboardUiManager existing = UnityEngine.Object.FindObjectOfType<LeaderboardUiManager>(true);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (existing != null)
            {
                return;
            }

            GameObject host = GameObject.Find("Managers");
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (host == null)
            {
                host = new GameObject("Managers");
            }

            host.AddComponent<LeaderboardUiManager>();
        }

        /// <summary>
        /// Khởi tạo tham chiếu, trạng thái ban đầu và các cấu hình cần thiết khi đối tượng được tạo.
        /// </summary>
        private void Awake()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
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

        /// <summary>
        /// Đăng ký sự kiện và kích hoạt các liên kết runtime khi đối tượng được bật.
        /// </summary>
        private void OnEnable()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            ResolveRuntimeReferences();
            BuildOrBindUi();
            WireEvents();
            SyncAuthVisibility();
            UpdateRefreshButtonVisual();
        }

        /// <summary>
        /// Cập nhật logic theo từng khung hình để phản hồi trạng thái hiện tại của game.
        /// </summary>
        private void Update()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            ResolveRuntimeReferences();

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_btnOpenLeaderboard == null || _panelLeaderboard == null)
            {
                BuildOrBindUi();
                WireEvents();
            }

            bool loggedIn = PlayFabClientAPI.IsClientLoggedIn();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
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

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_panelLeaderboard != null && _panelLeaderboard.activeSelf)
            {
                bool shouldPollEventInfo = !_eventInfoFetching
                    && (!_eventInfoLoaded || (Time.unscaledTime - _eventInfoLastFetchRealtime) >= EventInfoCacheSeconds);
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (shouldPollEventInfo)
                {
                    TryRefreshEventInfo(force: true);
                }

                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
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

        /// <summary>
        /// Xác định Runtime References phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void ResolveRuntimeReferences()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (playFabAuthManager == null)
            {
                playFabAuthManager = UnityEngine.Object.FindObjectOfType<PlayFabAuthManager>(true);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (targetCanvas == null)
            {
                GameObject canvasObj = GameObject.Find(CanvasName);
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (canvasObj != null)
                {
                    targetCanvas = canvasObj.GetComponent<Canvas>();
                }

                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (targetCanvas == null)
                {
                    targetCanvas = UnityEngine.Object.FindObjectOfType<Canvas>(true);
                }
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_panelStart == null)
            {
                _panelStart = GameObject.Find(PanelStartName);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_btnPlay == null && _panelStart != null)
            {
                Transform play = _panelStart.transform.Find("Btn_Play");
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (play != null)
                {
                    _btnPlay = play.GetComponent<Button>();
                }
            }
        }

        /// <summary>
        /// Xây dựng Or Bind Ui phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void BuildOrBindUi()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (targetCanvas == null)
            {
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_panelStart == null)
            {
                _panelStart = GameObject.Find(PanelStartName);
            }

            BuildOrBindOpenLeaderboardButton();
            BuildOrBindLeaderboardPanel();
        }

        /// <summary>
        /// Xây dựng Or Bind Open Leaderboard Button phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void BuildOrBindOpenLeaderboardButton()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_panelStart == null)
            {
                return;
            }

            Transform existing = _panelStart.transform.Find(ButtonOpenLeaderboardName);
            bool wasExisting = existing != null;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (existing != null)
            {
                _btnOpenLeaderboard = existing.GetComponent<Button>();
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
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

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!wasExisting && _btnOpenLeaderboard != null)
            {
                RectTransform rt = _btnOpenLeaderboard.GetComponent<RectTransform>();
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
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

        /// <summary>
        /// Xây dựng Or Bind Leaderboard Panel phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void BuildOrBindLeaderboardPanel()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            Transform existing = targetCanvas.transform.Find(PanelLeaderboardName);
            bool panelCreated = existing == null;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (existing != null)
            {
                _panelLeaderboard = existing.gameObject;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_panelLeaderboard == null)
            {
                _panelLeaderboard = new GameObject(PanelLeaderboardName, typeof(RectTransform), typeof(Image));
                _panelLeaderboard.transform.SetParent(targetCanvas.transform, false);
                panelCreated = true;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
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

        /// <summary>
        /// Đảm bảo Leaderboard Children phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void EnsureLeaderboardChildren()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_panelLeaderboard == null)
            {
                return;
            }

            RectTransform root = _panelLeaderboard.GetComponent<RectTransform>();

            bool cardCreated;
            GameObject card = FindOrCreateChild(_panelLeaderboard.transform, "Card", out cardCreated, typeof(RectTransform), typeof(Image));
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
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
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
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
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
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
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
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
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
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
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
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
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (scrollRect == null)
            {
                scrollRect = scrollRoot.AddComponent<ScrollRect>();
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (scrollRect.viewport == null)
            {
                scrollRect.viewport = viewportRt;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (scrollRect.content == null)
            {
                scrollRect.content = _rowsContainer;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (scrollRootCreated)
            {
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.scrollSensitivity = 20f;
            }

            Transform template = _rowsContainer.Find("Row_1");
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (template != null)
            {
                _rowTemplate = template.gameObject;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_rowTemplate == null)
            {
                _rowTemplate = CreateDefaultRowTemplate(_rowsContainer);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
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

        /// <summary>
        /// Liên kết Events phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void WireEvents()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_btnOpenLeaderboard != null)
            {
                _btnOpenLeaderboard.onClick.RemoveListener(OnOpenLeaderboardPressed);
                _btnOpenLeaderboard.onClick.AddListener(OnOpenLeaderboardPressed);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_btnBack != null)
            {
                _btnBack.onClick.RemoveListener(OnBackPressed);
                _btnBack.onClick.AddListener(OnBackPressed);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_btnTabNormal != null)
            {
                _btnTabNormal.onClick.RemoveListener(OnTabNormalPressed);
                _btnTabNormal.onClick.AddListener(OnTabNormalPressed);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_btnTabRaceTop != null)
            {
                _btnTabRaceTop.onClick.RemoveListener(OnTabRacePressed);
                _btnTabRaceTop.onClick.AddListener(OnTabRacePressed);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_btnRefresh != null)
            {
                _btnRefresh.onClick.RemoveListener(OnRefreshPressed);
                _btnRefresh.onClick.AddListener(OnRefreshPressed);
            }
        }

        /// <summary>
        /// Xử lý sự kiện khi người dùng nhấn nút/chức năng tương ứng trên giao diện.
        /// </summary>
        private void OnOpenLeaderboardPressed()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_panelLeaderboard == null)
            {
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_panelStart != null)
            {
                _panelStart.SetActive(false);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (playFabAuthManager != null && playFabAuthManager.panelStart != null)
            {
                playFabAuthManager.panelStart.SetActive(false);
            }

            _panelLeaderboard.SetActive(true);
            _panelLeaderboard.transform.SetAsLastSibling();
            SetActiveTab(_activeTab);
            TryRefreshEventInfo(force: true);

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!PlayFabClientAPI.IsClientLoggedIn())
            {
                _normalCache.emptyMessage = "Vui lòng đăng nhập để xem xếp hạng";
                RefreshCurrentTabUi();
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_activeTab == LeaderboardTab.Normal && ShouldAutoRefresh(_normalCache))
            {
                FetchNormalLeaderboard(force: false);
            }
            else
            {
                RefreshCurrentTabUi();
            }
        }

        /// <summary>
        /// Xử lý sự kiện khi người dùng nhấn nút/chức năng tương ứng trên giao diện.
        /// </summary>
        private void OnBackPressed()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_panelLeaderboard != null)
            {
                _panelLeaderboard.SetActive(false);
            }

            bool canShowStart = true;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (playFabAuthManager != null && playFabAuthManager.panelAuth != null)
            {
                canShowStart = !playFabAuthManager.panelAuth.activeSelf;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (GameManager.Instance != null)
            {
                canShowStart = canShowStart && GameManager.Instance.CurrentState == GameState.Menu;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_panelStart != null)
            {
                _panelStart.SetActive(canShowStart);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (playFabAuthManager != null && playFabAuthManager.panelStart != null)
            {
                playFabAuthManager.panelStart.SetActive(canShowStart);
            }
        }

        /// <summary>
        /// Xử lý sự kiện khi người dùng nhấn nút/chức năng tương ứng trên giao diện.
        /// </summary>
        private void OnTabNormalPressed()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            SetActiveTab(LeaderboardTab.Normal);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (ShouldAutoRefresh(_normalCache))
            {
                FetchNormalLeaderboard(force: false);
            }
            else
            {
                RefreshCurrentTabUi();
            }
        }

        /// <summary>
        /// Xử lý sự kiện khi người dùng nhấn nút/chức năng tương ứng trên giao diện.
        /// </summary>
        private void OnTabRacePressed()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            SetActiveTab(LeaderboardTab.Event);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (ShouldAutoRefresh(_eventCache))
            {
                FetchEventLeaderboard(force: false);
            }
            else
            {
                RefreshCurrentTabUi();
            }
        }

        /// <summary>
        /// Xử lý sự kiện khi người dùng nhấn nút/chức năng tương ứng trên giao diện.
        /// </summary>
        private void OnRefreshPressed()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (Time.unscaledTime < _refreshCooldownEndRealtime)
            {
                return;
            }

            _refreshCooldownEndRealtime = Time.unscaledTime + Mathf.Max(5f, manualRefreshCooldownSeconds);

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
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

        /// <summary>
        /// Thiết lập Active Tab phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void SetActiveTab(LeaderboardTab tab)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            _activeTab = tab;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_btnTabNormal != null)
            {
                _btnTabNormal.interactable = _activeTab != LeaderboardTab.Normal;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_btnTabRaceTop != null)
            {
                _btnTabRaceTop.interactable = _activeTab != LeaderboardTab.Event;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_txtTabTitle != null)
            {
                _txtTabTitle.text = _activeTab == LeaderboardTab.Normal ? "Bảng xếp hạng thường" : "Bảng xếp hạng sự kiện";
            }

            UpdateEventInfoVisibility();

            UpdateRefreshButtonVisual();
        }

        /// <summary>
        /// Cập nhật Event Info Visibility phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void UpdateEventInfoVisibility()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            bool showEventInfo = _activeTab == LeaderboardTab.Event && GetEventWindowState() == EventWindowState.Active;

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_txtEventTime != null)
            {
                _txtEventTime.gameObject.SetActive(showEventInfo);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_txtEventRewards != null)
            {
                _txtEventRewards.gameObject.SetActive(showEventInfo);
            }
        }

        /// <summary>
        /// Xử lý callback sự kiện hệ thống hoặc gameplay phát sinh trong vòng đời đối tượng.
        /// </summary>
        private void OnLoginDetected()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            _normalCache.Clear();
            _eventCache.Clear();
            _normalCache.emptyMessage = "Đang tải bảng xếp hạng...";
            _eventCache.emptyMessage = "Đang tải bảng xếp hạng...";
            SyncAuthVisibility();
            TryRefreshEventInfo(force: true);
            FetchNormalLeaderboard(force: true);
            FetchEventLeaderboard(force: true);
        }

        /// <summary>
        /// Xử lý callback sự kiện hệ thống hoặc gameplay phát sinh trong vòng đời đối tượng.
        /// </summary>
        private void OnLogoutDetected()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            _normalCache.Clear();
            _eventCache.Clear();

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
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

        /// <summary>
        /// Thực hiện nghiệp vụ Should Auto Refresh theo ngữ cảnh sử dụng của script.
        /// </summary>
        private bool ShouldAutoRefresh(LeaderboardCache cache)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (!PlayFabClientAPI.IsClientLoggedIn())
            {
                return false;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (cache.isFetching)
            {
                return false;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!cache.hasData)
            {
                return true;
            }

            return Time.unscaledTime - cache.lastFetchRealtime >= Mathf.Max(30f, cacheRefreshIntervalSeconds);
        }

        /// <summary>
        /// Tải Normal Leaderboard phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void FetchNormalLeaderboard(bool force)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (!PlayFabClientAPI.IsClientLoggedIn())
            {
                _normalCache.isFetching = false;
                _normalCache.emptyMessage = "Vui lòng đăng nhập để xem xếp hạng";
                RefreshCurrentTabUi();
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_normalCache.isFetching)
            {
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
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

            // Khối an toàn: thực thi tác vụ có khả năng phát sinh lỗi và sẽ xử lý ngoại lệ đi kèm.
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

        /// <summary>
        /// Tải Current Player Position phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void FetchCurrentPlayerPosition(string statisticName)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
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

            // Khối an toàn: thực thi tác vụ có khả năng phát sinh lỗi và sẽ xử lý ngoại lệ đi kèm.
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

        /// <summary>
        /// Thực hiện nghiệp vụ Finalize Normal Cache Without Rank theo ngữ cảnh sử dụng của script.
        /// </summary>
        private void FinalizeNormalCacheWithoutRank()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
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

        /// <summary>
        /// Làm mới Current Tab Ui phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void RefreshCurrentTabUi()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_panelLeaderboard == null || !_panelLeaderboard.activeSelf)
            {
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_activeTab == LeaderboardTab.Normal)
            {
                RefreshTabUi(_normalCache, "Bảng xếp hạng thường");
            }
            else
            {
                RefreshTabUi(_eventCache, "Bảng xếp hạng sự kiện");
            }
        }

        /// <summary>
        /// Tải Event Leaderboard phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void FetchEventLeaderboard(bool force)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (!PlayFabClientAPI.IsClientLoggedIn())
            {
                _eventCache.isFetching = false;
                _eventCache.emptyMessage = "Vui lòng đăng nhập để xem xếp hạng";
                RefreshCurrentTabUi();
                return;
            }

            bool shouldRefreshEventInfo = !_eventInfoLoaded
                || (Time.unscaledTime - _eventInfoLastFetchRealtime) >= EventInfoCacheSeconds;

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (shouldRefreshEventInfo)
            {
                TryRefreshEventInfo(
                    force: true,
                    onCompleted: () => FetchEventLeaderboard(force));
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
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

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_eventCache.isFetching)
            {
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!force && !ShouldAutoRefresh(_eventCache))
            {
                return;
            }

            _eventCache.isFetching = true;
            _eventCache.emptyMessage = "Đang tải bảng xếp hạng...";
            UpdateRefreshButtonVisual();

            string statisticName = ResolveEventStatisticName();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
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

            // Khối an toàn: thực thi tác vụ có khả năng phát sinh lỗi và sẽ xử lý ngoại lệ đi kèm.
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

        /// <summary>
        /// Tải Event Player Position phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void FetchEventPlayerPosition(string statisticName)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
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

            // Khối an toàn: thực thi tác vụ có khả năng phát sinh lỗi và sẽ xử lý ngoại lệ đi kèm.
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

        /// <summary>
        /// Thực hiện nghiệp vụ Finalize Event Cache Without Rank theo ngữ cảnh sử dụng của script.
        /// </summary>
        private void FinalizeEventCacheWithoutRank()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
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

        /// <summary>
        /// Làm mới Tab Ui phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void RefreshTabUi(LeaderboardCache cache, string tabTitle)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_txtTabTitle != null)
            {
                _txtTabTitle.text = tabTitle;
            }

            int score = cache.hasPlayerInfo ? cache.playerScore : 0;
            int rank = cache.hasPlayerInfo ? cache.playerRank : 0;
            string name = cache.hasPlayerInfo ? cache.playerName : ResolveCurrentUserName();
            SetCurrentPlayerInfo(name, score, rank, cache.hasPlayerInfo);

            ClearRows();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (cache.topEntries.Count == 0)
            {
                UpdateEmptyText(string.IsNullOrWhiteSpace(cache.emptyMessage) ? "Chưa có xếp hạng" : cache.emptyMessage);
                return;
            }

            UpdateEmptyText(string.Empty);
            // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
            for (int i = 0; i < cache.topEntries.Count; i++)
            {
                PlayerLeaderboardEntry entry = cache.topEntries[i];
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (entry == null)
                {
                    continue;
                }

                CreateLeaderboardRow(i, entry);
            }
        }

        /// <summary>
        /// Thiết lập Current Player Info phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void SetCurrentPlayerInfo(string userName, int score, int rank, bool hasRank)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_txtCurrentUser != null)
            {
                _txtCurrentUser.text = "Người chơi: " + (string.IsNullOrWhiteSpace(userName) ? "--" : userName);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_txtCurrentScore != null)
            {
                _txtCurrentScore.text = "Điểm: " + Mathf.Max(0, score);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_txtCurrentRank != null)
            {
                _txtCurrentRank.text = hasRank && rank > 0 ? "Hạng: " + rank : "Hạng: --";
            }
        }

        /// <summary>
        /// Tạo Leaderboard Row phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void CreateLeaderboardRow(int index, PlayerLeaderboardEntry entry)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_rowsContainer == null || _rowTemplate == null)
            {
                return;
            }

            GameObject row = Instantiate(_rowTemplate, _rowsContainer);
            row.name = "Row_" + (index + 1);
            row.SetActive(true);

            Image bg = row.GetComponent<Image>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
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

        /// <summary>
        /// Xóa Rows phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void ClearRows()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            for (int i = 0; i < _spawnedRows.Count; i++)
            {
                GameObject row = _spawnedRows[i];
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (row != null)
                {
                    Destroy(row);
                }
            }

            _spawnedRows.Clear();

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_rowTemplate != null)
            {
                _rowTemplate.SetActive(false);
                _rowTemplate.transform.SetAsFirstSibling();
            }
        }

        /// <summary>
        /// Thiết lập Row Text phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void SetRowText(Transform rowTransform, string textName, string value)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (rowTransform == null)
            {
                return;
            }

            Transform t = rowTransform.Find(textName);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (t == null)
            {
                return;
            }

            Text txt = t.GetComponent<Text>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (txt != null)
            {
                txt.text = value;
            }
        }

        /// <summary>
        /// Tạo Default Row Template phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static GameObject CreateDefaultRowTemplate(Transform parent)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
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

        /// <summary>
        /// Cập nhật Empty Text phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void UpdateEmptyText(string message)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_txtEmpty == null)
            {
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
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

        /// <summary>
        /// Cập nhật Refresh Button Visual phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void UpdateRefreshButtonVisual()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_btnRefresh == null || _txtRefresh == null)
            {
                return;
            }

            bool fetching = _activeTab == LeaderboardTab.Normal ? _normalCache.isFetching : _eventCache.isFetching;
            float remain = Mathf.Max(0f, _refreshCooldownEndRealtime - Time.unscaledTime);

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (fetching)
            {
                _btnRefresh.interactable = false;
                _txtRefresh.text = "Đang tải...";
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (remain > 0f)
            {
                _btnRefresh.interactable = false;
                _txtRefresh.text = "Làm mới (" + Mathf.CeilToInt(remain) + "s)";
                return;
            }

            _btnRefresh.interactable = true;
            _txtRefresh.text = "Làm mới";
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Sync Auth Visibility theo ngữ cảnh sử dụng của script.
        /// </summary>
        private void SyncAuthVisibility()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_btnOpenLeaderboard != null)
            {
                bool isAuthMenuHidden = playFabAuthManager == null
                    || playFabAuthManager.panelAuth == null
                    || !playFabAuthManager.panelAuth.activeSelf;
                _btnOpenLeaderboard.interactable = isAuthMenuHidden;
            }
        }

        /// <summary>
        /// Dọn dẹp tài nguyên và hủy các ràng buộc còn tồn tại trước khi đối tượng bị hủy.
        /// </summary>
        private void OnDestroy()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Xác định Normal Statistic Name phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private string ResolveNormalStatisticName()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (playFabAuthManager != null && !string.IsNullOrWhiteSpace(playFabAuthManager.leaderboardStatisticName))
            {
                return playFabAuthManager.leaderboardStatisticName;
            }

            return "LeaderBoard_Normal";
        }

        /// <summary>
        /// Xác định Event Statistic Name phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private string ResolveEventStatisticName()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            string baseName;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
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
        private string GetEventStatisticSuffix()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (TryNormalizeEventSuffixFromRaw(_eventStartRaw, out string normalizedRaw))
            {
                return normalizedRaw;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_eventStartUtc != DateTime.MinValue)
            {
                return FormatEventTimeForDisplay(_eventStartUtc);
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
        /// Thử xử lý Refresh Event Info phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void TryRefreshEventInfo(bool force, System.Action onCompleted = null)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (!PlayFabClientAPI.IsClientLoggedIn())
            {
                ApplyEventInfoToUi();
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (onCompleted != null)
                {
                    onCompleted();
                }
                return;
            }

            bool hasFreshCache = _eventInfoLoaded
                && !force
                && (Time.unscaledTime - _eventInfoLastFetchRealtime) < EventInfoCacheSeconds;

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (hasFreshCache)
            {
                ApplyEventInfoToUi();
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (onCompleted != null)
                {
                    onCompleted();
                }
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (onCompleted != null)
            {
                _pendingEventInfoCallbacks.Add(onCompleted);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
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

        /// <summary>
        /// Xử lý callback sự kiện hệ thống hoặc gameplay phát sinh trong vòng đời đối tượng.
        /// </summary>
        private void OnEventConfigUpdated()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_panelLeaderboard == null || !_panelLeaderboard.activeSelf || _activeTab != LeaderboardTab.Event)
            {
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
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

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!_eventCache.isFetching
                && (_eventCache.topEntries.Count == 0
                    || Time.unscaledTime - _eventCache.lastFetchRealtime >= Mathf.Max(30f, cacheRefreshIntervalSeconds)))
            {
                FetchEventLeaderboard(force: true);
                return;
            }

            RefreshCurrentTabUi();
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Flush Pending Event Info Callbacks theo ngữ cảnh sử dụng của script.
        /// </summary>
        private void FlushPendingEventInfoCallbacks()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_pendingEventInfoCallbacks.Count == 0)
            {
                return;
            }

            List<System.Action> callbacks = new List<System.Action>(_pendingEventInfoCallbacks);
            _pendingEventInfoCallbacks.Clear();

            // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
            for (int i = 0; i < callbacks.Count; i++)
            {
                System.Action callback = callbacks[i];
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (callback != null)
                {
                    callback();
                }
            }
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Is Event Open Now theo ngữ cảnh sử dụng của script.
        /// </summary>
        private bool IsEventOpenNow()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            return GetEventWindowState() == EventWindowState.Active;
        }

        /// <summary>
        /// Lấy Event Window State phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private EventWindowState GetEventWindowState()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (!_eventInfoLoaded)
            {
                return EventWindowState.Unknown;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!_eventEnabled)
            {
                return EventWindowState.Disabled;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_eventStartUtc == DateTime.MinValue || _eventEndUtc == DateTime.MinValue)
            {
                return EventWindowState.Invalid;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_eventEndUtc < _eventStartUtc)
            {
                return EventWindowState.Invalid;
            }

            DateTime now = DateTime.UtcNow;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (now < _eventStartUtc)
            {
                return EventWindowState.Upcoming;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (now > _eventEndUtc)
            {
                return EventWindowState.Ended;
            }

            return EventWindowState.Active;
        }

        /// <summary>
        /// Xây dựng Event Unavailable Message phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private string BuildEventUnavailableMessage()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            EventWindowState state = GetEventWindowState();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (state == EventWindowState.Upcoming)
            {
                return "Sắp có sự kiện vào lúc " + GetEventStartDisplayText();
            }

            return "Chưa có sự kiện";
        }

        /// <summary>
        /// Lấy Event Start Display Text phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private string GetEventStartDisplayText()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (!string.IsNullOrWhiteSpace(_eventStartRaw))
            {
                return _eventStartRaw;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_eventStartUtc != DateTime.MinValue)
            {
                return FormatEventTimeForDisplay(_eventStartUtc);
            }

            return "--";
        }

        /// <summary>
        /// Áp dụng Event Info To Ui phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void ApplyEventInfoToUi()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
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

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_txtEventRewards != null)
            {
                _txtEventRewards.text = "Quà sự kiện: " + (string.IsNullOrWhiteSpace(_eventRewards) ? "Chưa công bố" : _eventRewards);
            }

            UpdateEventInfoVisibility();
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

            // Accept both "HH:mm:ss-yyyy:MM:dd" and variants like "HH-mm-ss - yyyy-MM-dd".
            Match timeDate = Regex.Match(text, @"^\s*(\d{1,2})\D+(\d{1,2})\D+(\d{1,2})\s*-\s*(\d{4})\D+(\d{1,2})\D+(\d{1,2})\s*$");
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (timeDate.Success)
            {
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
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
        /// Định dạng Event Time For Display phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static string FormatEventTimeForDisplay(DateTime utc)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            DateTime vn = DateTime.SpecifyKind(utc, DateTimeKind.Utc).AddHours(7);
            return vn.ToString("HH:mm:ss-yyyy:MM:dd");
        }

        /// <summary>
        /// Thiết lập Button Text phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void SetButtonText(Button button, string value)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (button == null)
            {
                return;
            }

            Text txt = button.GetComponentInChildren<Text>(true);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (txt != null)
            {
                txt.text = value;
            }
        }

        /// <summary>
        /// Xác định Current User Name phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private string ResolveCurrentUserName()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (playFabAuthManager != null && playFabAuthManager.txt_StartUserName != null)
            {
                string uiName = playFabAuthManager.txt_StartUserName.text;
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (!string.IsNullOrWhiteSpace(uiName))
                {
                    return uiName.Trim();
                }
            }

            return "--";
        }

        /// <summary>
        /// Chuẩn hóa Display Name phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static string NormalizeDisplayName(string displayName, string playFabId)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName.Trim();
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!string.IsNullOrWhiteSpace(playFabId))
            {
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (playFabId.Length <= 6)
                {
                    return playFabId;
                }

                return "Người chơi " + playFabId.Substring(playFabId.Length - 6);
            }

            return "Người chơi";
        }

        /// <summary>
        /// Tìm Or Create Child phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static GameObject FindOrCreateChild(Transform parent, string name, params Type[] components)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            Transform existing = parent.Find(name);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (existing != null)
            {
                GameObject obj = existing.gameObject;
                // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
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

        /// <summary>
        /// Đảm bảo Button phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static Button EnsureButton(Transform parent, string name, string label, Vector2 anchor, Vector2 size, Color bg, Color textColor, int fontSize)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            Transform existing = parent.Find(name);
            Button button = existing != null ? existing.GetComponent<Button>() : null;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (button == null)
            {
                button = CreateButton(name, parent, label, anchor, size, bg, textColor, fontSize);
            }

            return button;
        }

        /// <summary>
        /// Tạo Button phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static Button CreateButton(string name, Transform parent, string label, Vector2 anchor, Vector2 size, Color bg, Color textColor, int fontSize)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
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

        /// <summary>
        /// Đảm bảo Label phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static Text EnsureLabel(Transform parent, string name, string value, int size, Color color, Vector2 anchor, Vector2 rectSize, TextAnchor alignment, FontStyle style)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            Transform existing = parent.Find(name);
            bool created = existing == null;
            Text text = existing != null ? existing.GetComponent<Text>() : null;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (text == null)
            {
                GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
                obj.transform.SetParent(parent, false);
                text = obj.GetComponent<Text>();
                created = true;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
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

        /// <summary>
        /// Đảm bảo Column Text phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
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
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            Transform existing = parent.Find(name);
            bool created = existing == null;
            Text text;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (existing == null)
            {
                GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
                obj.transform.SetParent(parent, false);
                text = obj.GetComponent<Text>();
            }
            else
            {
                text = existing.GetComponent<Text>();
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (text == null)
                {
                    text = existing.gameObject.AddComponent<Text>();
                    created = true;
                }
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
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

        /// <summary>
        /// Tìm Or Create Child phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static GameObject FindOrCreateChild(Transform parent, string name, out bool created, params Type[] components)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            Transform existing = parent.Find(name);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (existing != null)
            {
                created = false;
                GameObject obj = existing.gameObject;
                // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
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

        /// <summary>
        /// Lấy Ui Font phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static Font GetUiFont()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_cachedFont != null)
            {
                return _cachedFont;
            }

            _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_cachedFont == null)
            {
                _cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return _cachedFont;
        }
    }
}
