using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;

public class MapSelectionUI : MenuBase
{
    public override MenuType menuType => MenuType.MapSelection;

    [Header("Map Buttons")]
    [SerializeField] private UnityEngine.UI.Button itButton;
    [SerializeField] private UnityEngine.UI.Button marketingButton;
    [SerializeField] private UnityEngine.UI.Button backButton;

    [Header("Animation")]
    [SerializeField] private float animDuration = 0.3f;

    protected override void LoadComponent()
    {
        if (itButton == null) itButton = transform.Find("ButtonPanel/ITButton")?.GetComponent<UnityEngine.UI.Button>();
        if (marketingButton == null) marketingButton = transform.Find("ButtonPanel/MarketingButton")?.GetComponent<UnityEngine.UI.Button>();
        if (backButton == null) backButton = transform.Find("ButtonPanel/BackButton")?.GetComponent<UnityEngine.UI.Button>();
    }

    protected override void LoadComponentRuntime()
    {
        itButton?.onClick.AddListener(() => OnFacultySelected("IT"));
        marketingButton?.onClick.AddListener(() => OnFacultySelected("Marketing"));
        backButton?.onClick.AddListener(OnBackClicked);
    }

    private void OnFacultySelected(string facultyId)
    {
        PersistentData.Instance.CurrentFaculty = facultyId;
        UIManager.Instance.ChangeMenu(MenuType.MajorSelection, facultyId);
    }

    private void OnBackClicked()
    {
        UIManager.Instance.ChangeMenu(MenuType.MainMenu);
    }
}
