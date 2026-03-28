using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;

public class MajorSelectionUI : MenuBase
{
    public override MenuType menuType => MenuType.MajorSelection;

    [Header("Major Buttons")]
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private UnityEngine.UI.Button majorButtonPrefab;
    [SerializeField] private UnityEngine.UI.Button backButton;

    [Header("Labels")]
    [SerializeField] private TMPro.TextMeshProUGUI titleText;

    private string _currentFacultyId;
    private readonly System.Collections.Generic.List<UnityEngine.UI.Button> _spawnedButtons = new System.Collections.Generic.List<UnityEngine.UI.Button>();

    protected override void LoadComponent()
    {
        if (buttonContainer == null)
            buttonContainer = transform.Find("ButtonPanel/MajorButtons") ?? transform.Find("ButtonPanel");
        if (backButton == null) backButton = transform.Find("ButtonPanel/BackButton")?.GetComponent<UnityEngine.UI.Button>();
        if (titleText == null) titleText = transform.Find("TitleText")?.GetComponent<TMPro.TextMeshProUGUI>();
    }

    protected override void LoadComponentRuntime()
    {
        backButton?.onClick.AddListener(OnBackClicked);
    }

    public override void Open(object data = null)
    {
        base.Open(data);
        _currentFacultyId = data as string ?? PersistentData.Instance.CurrentFaculty;
        UpdateLabels();
        RebuildMajorButtons();
    }

    private void UpdateLabels()
    {
        if (titleText == null) return;

        var faculty = DataManager.Instance.GetFaculty(_currentFacultyId);
        var label = faculty != null && !string.IsNullOrEmpty(faculty.displayName)
            ? faculty.displayName
            : _currentFacultyId;
        titleText.text = string.IsNullOrEmpty(label) ? "Chọn chuyên ngành" : $"{label} - Chuyên ngành";
    }

    private void RebuildMajorButtons()
    {
        ClearSpawnedButtons();
        if (buttonContainer == null) return;

        var majors = DataManager.Instance.GetMajorsForFaculty(_currentFacultyId);
        if (majors == null || majors.Count == 0) return;

        if (majorButtonPrefab == null)
        {
            BindExistingButtons(majors);
            return;
        }

        foreach (var major in majors)
        {
            var button = Instantiate(majorButtonPrefab, buttonContainer);
            button.name = string.IsNullOrEmpty(major.id) ? "MajorButton" : $"{major.id}Button";
            BindButtonLabel(button, string.IsNullOrEmpty(major.displayName) ? major.id : major.displayName);
            string majorId = major.id;
            button.onClick.AddListener(() => OnMajorSelected(majorId));
            _spawnedButtons.Add(button);
        }
    }

    private void BindExistingButtons(System.Collections.Generic.List<MajorInfo> majors)
    {
        var existingButtons = buttonContainer.GetComponentsInChildren<UnityEngine.UI.Button>(true);
        int majorIndex = 0;

        foreach (var button in existingButtons)
        {
            if (button == null || button == backButton) continue;
            if (majorIndex >= majors.Count)
            {
                button.gameObject.SetActive(false);
                continue;
            }

            var major = majors[majorIndex++];
            button.gameObject.SetActive(true);
            button.name = string.IsNullOrEmpty(major.id) ? "MajorButton" : $"{major.id}Button";
            BindButtonLabel(button, string.IsNullOrEmpty(major.displayName) ? major.id : major.displayName);
            string majorId = major.id;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnMajorSelected(majorId));
        }
    }

    private void BindButtonLabel(UnityEngine.UI.Button button, string label)
    {
        var tmp = button.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (tmp != null)
        {
            tmp.text = label;
            return;
        }

        var legacyText = button.GetComponentInChildren<UnityEngine.UI.Text>(true);
        if (legacyText != null)
        {
            legacyText.text = label;
        }
    }

    private void ClearSpawnedButtons()
    {
        for (int i = 0; i < _spawnedButtons.Count; i++)
        {
            if (_spawnedButtons[i] != null)
                Destroy(_spawnedButtons[i].gameObject);
        }
        _spawnedButtons.Clear();
    }

    private void OnMajorSelected(string majorId)
    {
        AudioManager.Instance?.PlayButtonClick();
        // Logic: lưu major & gọi GameManager bắt đầu game
        PersistentData.Instance.CurrentMajor = majorId;
        PersistentData.Instance.ResetSession();
        GameManager.Instance.StartGameWithMajor(majorId);

        // UI: đóng menu hiện tại — không dùng event vì GameManager đã gọi UIManager.ChangeMenu rồi
        Close();
    }

    private void OnBackClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        UIManager.Instance.ChangeMenu(MenuType.MapSelection);
    }
}
