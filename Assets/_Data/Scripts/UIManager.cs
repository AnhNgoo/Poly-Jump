using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using Sirenix.OdinInspector;
using UnityEngine.SceneManagement;
using System.Linq;

public enum MenuType
{
    None = -1,
    LoadingMenu = 0,
    MainMenu = 1,
    CreateRoomMenu = 2,
    SettingsMenu = 14,
    PauseMenu = 3,
    LobbyMenu = 6,
    HUDMenu = 7,
    RoomMenu = 8,
    MapSelection = 9,
    MajorSelection = 10,
    GameplayHUD = 11,
    QuizPanel = 12,
    GameOverPanel = 13,
    AchievementMenu = 15,
    AchievementMajor = 16,
}

public class UIManager : Singleton<UIManager>
{
    [SerializeField] GameObject canvas;
    [ShowInInspector] public MenuType CurrentMenuType { get; private set; }
    [ShowInInspector] public MenuType PreviousMenuType { get; private set; }
    [SerializeField] List<MenuData> menus = new List<MenuData>();

    [ShowInInspector] public MenuBase CurrentMenu { get; private set; }

    [Serializable]
    public class MenuData
    {
        public MenuType menuType;
        public MenuBase menuBase;
    }


    void Start()
    {
        LoadMenus();
        CloseAllMenus();
        Invoke(nameof(TryOpenInitialMenu), 0.05f);
    }

    private void TryOpenInitialMenu()
    {
        if (GameManager.PendingRestart || PlayerPrefs.GetInt("PendingRestart", 0) == 1)
            return;

        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
        {
            ChangeMenu(MenuType.GameplayHUD);
            return;
        }

        ChangeMenu(MenuType.MainMenu);
    }
    protected override void LoadComponent()
    {
        base.LoadComponent();
        LoadMenus();
    }
    private void LoadMenus()
    {
        if (canvas == null)
            canvas = GameObject.FindGameObjectWithTag("Canvas");

        if (canvas == null)
            return;

        List<MenuBase> menuList = new List<MenuBase>(canvas.GetComponentsInChildren<MenuBase>(true));

        if (menuList == null || menuList.Count == 0)
            return;

        menus.Clear();
        foreach (MenuBase menu in menuList)
        {
            menus.Add(new MenuData { menuType = menu.menuType, menuBase = menu });
        }
    }


    //Chuyển đổi menu
    public void ChangeMenu(MenuType menuType, object data = null)
    {
        var menuData = menus.FirstOrDefault(m => m.menuType == menuType);
        if (menuData == null) return;

        PreviousMenuType = CurrentMenu != null ? CurrentMenu.menuType : MenuType.None;

        CurrentMenu?.Close();
        CurrentMenu = menuData.menuBase;
        CurrentMenu.Open(data);

        CurrentMenuType = CurrentMenu.menuType;
    }

    //Đóng tất cả menu
    public void CloseAllMenus()
    {
        foreach (var menu in menus)
        {
            menu.menuBase.Close();
        }
    }
}
