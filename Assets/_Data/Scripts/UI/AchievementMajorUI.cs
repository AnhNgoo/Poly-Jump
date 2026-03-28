using UnityEngine;

public class AchievementMajorUI : MenuBase
{
    public override MenuType menuType => MenuType.AchievementMajor;

    [Header("Major Scores")]
    [SerializeField] private Transform contentContainer;
    [SerializeField] private TMPro.TextMeshProUGUI scoreTextPrefab;
    [SerializeField] private UnityEngine.UI.Button backButton;
    [SerializeField] private TMPro.TextMeshProUGUI titleText;

    private readonly System.Collections.Generic.List<TMPro.TextMeshProUGUI> _spawnedTexts = new System.Collections.Generic.List<TMPro.TextMeshProUGUI>();
    private string _currentFacultyId;

    protected override void LoadComponent()
    {
        if (contentContainer == null)
            contentContainer = transform.Find("ScrollView/Viewport/Content")
                ?? transform.Find("Content/ScrollView/Viewport/Content")
                ?? transform.Find("ContentPanel");
        if (backButton == null) backButton = transform.Find("BackButton")?.GetComponent<UnityEngine.UI.Button>();
        if (backButton == null) backButton = transform.Find("ButtonPanel/BackButton")?.GetComponent<UnityEngine.UI.Button>();
        if (titleText == null) titleText = transform.Find("TitleText")?.GetComponent<TMPro.TextMeshProUGUI>();
    }

    protected override void LoadComponentRuntime()
    {
        if (contentContainer == null || backButton == null)
            LoadComponent();

        backButton?.onClick.AddListener(OnBackClicked);
    }

    public override void Open(object data = null)
    {
        base.Open(data);
        _currentFacultyId = data as string ?? PersistentData.Instance.CurrentFaculty;
        UpdateTitle();
        RebuildMajorScores();
    }

    private void UpdateTitle()
    {
        if (titleText == null) return;
        var faculty = DataManager.Instance.GetFaculty(_currentFacultyId);
        var label = faculty != null && !string.IsNullOrEmpty(faculty.displayName)
            ? faculty.displayName
            : _currentFacultyId;
        titleText.text = string.IsNullOrEmpty(label) ? "Thành tích" : $"Thành tích ngành {label}";
    }

    private void RebuildMajorScores()
    {
        ClearSpawnedTexts();
        if (contentContainer == null) return;

        var majors = DataManager.Instance.GetMajorsForFaculty(_currentFacultyId);
        if (majors == null || majors.Count == 0) return;

        if (scoreTextPrefab == null)
        {
            BindExistingTexts(majors);
            return;
        }

        foreach (var major in majors)
        {
            var text = Instantiate(scoreTextPrefab, contentContainer);
            text.name = string.IsNullOrEmpty(major.id) ? "MajorScoreText" : $"{major.id}ScoreText";
            string label = string.IsNullOrEmpty(major.displayName) ? major.id : major.displayName;
            float score = DataManager.Instance.GetBestScore(_currentFacultyId, major.id);
            text.text = $"{label}: {score:F1}%";
            _spawnedTexts.Add(text);
        }
    }

    private void BindExistingTexts(System.Collections.Generic.List<MajorInfo> majors)
    {
        var existingTexts = contentContainer.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
        int index = 0;

        foreach (var text in existingTexts)
        {
            if (text == null || text == titleText) continue;
            if (index >= majors.Count)
            {
                text.gameObject.SetActive(false);
                continue;
            }

            var major = majors[index++];
            string label = string.IsNullOrEmpty(major.displayName) ? major.id : major.displayName;
            float score = DataManager.Instance.GetBestScore(_currentFacultyId, major.id);
            text.gameObject.SetActive(true);
            text.name = string.IsNullOrEmpty(major.id) ? "MajorScoreText" : $"{major.id}ScoreText";
            text.text = $"{label}: {score:F1}%";
        }
    }

    private void ClearSpawnedTexts()
    {
        for (int i = 0; i < _spawnedTexts.Count; i++)
        {
            if (_spawnedTexts[i] != null)
                Destroy(_spawnedTexts[i].gameObject);
        }
        _spawnedTexts.Clear();
    }

    private void OnBackClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        UIManager.Instance.ChangeMenu(MenuType.AchievementMenu);
    }
}
