using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;

public class MapSelectionUI : MenuBase
{
    public override MenuType menuType => MenuType.MapSelection;

    [Header("Map Buttons")]
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private UnityEngine.UI.Button facultyButtonPrefab;
    [SerializeField] private UnityEngine.UI.Button backButton;

    private readonly System.Collections.Generic.List<UnityEngine.UI.Button> _spawnedButtons = new System.Collections.Generic.List<UnityEngine.UI.Button>();

    [Header("Animation")]
    [SerializeField] private float animDuration = 0.3f;

    protected override void LoadComponent()
    {
        if (buttonContainer == null)
            buttonContainer = transform.Find("ButtonPanel/FacultyButtons") ?? transform.Find("ButtonPanel");
        if (backButton == null) backButton = transform.Find("ButtonPanel/BackButton")?.GetComponent<UnityEngine.UI.Button>();
    }

    protected override void LoadComponentRuntime()
    {
        backButton?.onClick.AddListener(OnBackClicked);
    }

    public override void Open(object data = null)
    {
        base.Open(data);
        RebuildFacultyButtons();
    }

    private void RebuildFacultyButtons()
    {
        ClearSpawnedButtons();
        if (buttonContainer == null) return;

        var faculties = DataManager.Instance.GetFaculties();
        if (faculties == null || faculties.Count == 0) return;

        if (facultyButtonPrefab == null)
        {
            BindExistingButtons(faculties);
            return;
        }

        foreach (var faculty in faculties)
        {
            var button = Instantiate(facultyButtonPrefab, buttonContainer);
            button.name = string.IsNullOrEmpty(faculty.id) ? "FacultyButton" : $"{faculty.id}Button";
            BindButtonLabel(button, string.IsNullOrEmpty(faculty.displayName) ? faculty.id : faculty.displayName);
            string facultyId = faculty.id;
            button.onClick.AddListener(() => OnFacultySelected(facultyId));
            _spawnedButtons.Add(button);
        }
    }

    private void BindExistingButtons(System.Collections.Generic.List<FacultyInfo> faculties)
    {
        var existingButtons = buttonContainer.GetComponentsInChildren<UnityEngine.UI.Button>(true);
        int facultyIndex = 0;

        foreach (var button in existingButtons)
        {
            if (button == null || button == backButton) continue;
            if (facultyIndex >= faculties.Count)
            {
                button.gameObject.SetActive(false);
                continue;
            }

            var faculty = faculties[facultyIndex++];
            button.gameObject.SetActive(true);
            button.name = string.IsNullOrEmpty(faculty.id) ? "FacultyButton" : $"{faculty.id}Button";
            BindButtonLabel(button, string.IsNullOrEmpty(faculty.displayName) ? faculty.id : faculty.displayName);
            string facultyId = faculty.id;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnFacultySelected(facultyId));
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

    private void OnFacultySelected(string facultyId)
    {
        AudioManager.Instance?.PlayButtonClick();
        PersistentData.Instance.CurrentFaculty = facultyId;
        UIManager.Instance.ChangeMenu(MenuType.MajorSelection, facultyId);
    }

    private void OnBackClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        UIManager.Instance.ChangeMenu(MenuType.MainMenu);
    }
}
