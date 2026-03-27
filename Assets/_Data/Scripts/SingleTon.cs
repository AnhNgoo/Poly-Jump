using UnityEngine;

public class Singleton<T> : LoadComponents where T : LoadComponents
{
    public bool isDontDestroyOnLoad = false;
    public static T Instance;
    protected override void Awake()
    {
        if (Instance == null)
        {
            Instance = this as T;
            LoadComponentRuntime();
            if (isDontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    protected override void LoadComponent()
    {
    }

    protected override void LoadComponentRuntime()
    {
    }
}
