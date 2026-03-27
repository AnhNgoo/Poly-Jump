using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;

public class PlatformComponent : MonoBehaviour
{
    public int PlatformId { get; private set; }
    private static int _idCounter = 0;

    private void Awake()
    {
        PlatformId = _idCounter++;
    }

    public void ResetId()
    {
        PlatformId = _idCounter++;
    }
}

public class PlatformSpawner : LoadComponents
{
    [Header("Prefabs")]
    [SerializeField] private GameObject platformPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnYStart = 0f;
    [SerializeField] private float spawnYStep = 2f;
    [SerializeField] private float minX = -3.5f;
    [SerializeField] private float maxX = 3.5f;
    [SerializeField] private float platformWidth = 3f;
    [SerializeField] private int initialPlatformCount = 10;

    [Header("Player Alignment")]
    [SerializeField] private bool alignToPlayerOnStart = true;
    [SerializeField] private float playerStartYOffset = 1f;

    [Header("Bounds")]
    [SerializeField] private bool clampToCameraBounds = true;
    [SerializeField] private float cameraHorizontalPadding = 0.5f;

    [Header("Pooling")]
    [SerializeField] private int poolSize = 20;

    [Header("Spawn Window")]
    [SerializeField, Tooltip("Spawn ahead of camera top so player never sees pop-in.")]
    private float spawnAheadOfCamera = 6f;

    private Transform _cameraTransform;
    private List<GameObject> _activePlatforms = new List<GameObject>();
    private Queue<GameObject> _platformPool = new Queue<GameObject>();
    private float _highestSpawnY;
    private int _currentPlatformId;
    private bool _isPaused;

    protected override void LoadComponent()
    {
        _cameraTransform = Camera.main?.transform;
    }

    protected override void LoadComponentRuntime()
    {
        AlignSpawnToPlayer();
        _currentPlatformId = 0;
        _highestSpawnY = spawnYStart;
        BuildPool();
        SpawnInitialPlatforms();

        EventManager.Instance.Subscribe(GameEvent.GameOver, OnGameOver);
        EventManager.Instance.Subscribe(GameEvent.QuizTriggered, OnQuizPaused);
        EventManager.Instance.Subscribe(GameEvent.QuizClosed, OnQuizResumed);
    }

    private void Update()
    {
        if (_isPaused) return;
        if (_cameraTransform == null) return;

        float cameraBottom = _cameraTransform.position.y - Camera.main.orthographicSize;
        float cameraTop = _cameraTransform.position.y + Camera.main.orthographicSize;

        CleanupPlatformsBelow(cameraBottom - 2f);

        // Always keep a spawn buffer above the camera
        float spawnAheadY = cameraTop + spawnAheadOfCamera;
        if (_highestSpawnY < spawnAheadY)
            SpawnPlatformsAbove(spawnAheadY);
    }

    private void BuildPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            var obj = Instantiate(platformPrefab, Vector3.one * 1000f, Quaternion.identity);
            obj.SetActive(false);
            obj.AddComponent<PlatformComponent>();
            _platformPool.Enqueue(obj);
        }
    }

    private void SpawnInitialPlatforms()
    {
        if (_cameraTransform == null || Camera.main == null)
        {
            for (int i = 0; i < initialPlatformCount; i++)
                SpawnPlatform(spawnYStart + i * spawnYStep);

            _highestSpawnY = spawnYStart + (initialPlatformCount - 1) * spawnYStep;
            return;
        }

        float cameraTop = _cameraTransform.position.y + Camera.main.orthographicSize;
        float spawnTargetY = cameraTop + spawnAheadOfCamera;
        SpawnPlatformsAbove(spawnTargetY);
    }

    private void SpawnPlatform(float y)
    {
        float localMinX = minX;
        float localMaxX = maxX;

        if (clampToCameraBounds && Camera.main != null)
        {
            float halfWidth = Camera.main.orthographicSize * Camera.main.aspect;
            float camX = Camera.main.transform.position.x;
            localMinX = camX - halfWidth + cameraHorizontalPadding;
            localMaxX = camX + halfWidth - cameraHorizontalPadding;
        }

        if (localMaxX < localMinX)
        {
            float mid = (localMinX + localMaxX) * 0.5f;
            localMinX = mid;
            localMaxX = mid;
        }

        float x = Random.Range(localMinX, localMaxX);
        Vector3 pos = new Vector3(x, y, 0f);

        GameObject platform;
        if (_platformPool.Count > 0)
        {
            platform = _platformPool.Dequeue();
            platform.transform.position = pos;
            platform.SetActive(true);

            var pc = platform.GetComponent<PlatformComponent>();
            if (pc != null) pc.ResetId();
        }
        else
        {
            platform = Instantiate(platformPrefab, pos, Quaternion.identity);
            platform.AddComponent<PlatformComponent>();
        }

        _activePlatforms.Add(platform);
    }

    private void SpawnPlatformsAbove(float threshold)
    {
        while (_highestSpawnY < threshold)
        {
            _highestSpawnY += spawnYStep;
            SpawnPlatform(_highestSpawnY);
        }
    }

    private void CleanupPlatformsBelow(float threshold)
    {
        for (int i = _activePlatforms.Count - 1; i >= 0; i--)
        {
            var p = _activePlatforms[i];
            if (p == null)
            {
                _activePlatforms.RemoveAt(i);
                continue;
            }

            if (p.transform.position.y < threshold)
            {
                p.SetActive(false);
                _platformPool.Enqueue(p);
                _activePlatforms.RemoveAt(i);
            }
        }
    }

    private void OnQuizPaused(object data)
    {
        _isPaused = true;
    }

    private void OnQuizResumed(object data)
    {
        _isPaused = false;
    }


    private void OnGameOver(object data)
    {
        enabled = false;
    }

    private void OnDestroy()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.Unsubscribe(GameEvent.GameOver, OnGameOver);
            EventManager.Instance.Unsubscribe(GameEvent.QuizTriggered, OnQuizPaused);
            EventManager.Instance.Unsubscribe(GameEvent.QuizClosed, OnQuizResumed);
        }
    }

    public void ResetSpawner()
    {
        AlignSpawnToPlayer();
        foreach (var p in _activePlatforms)
        {
            if (p != null)
            {
                p.SetActive(false);
                _platformPool.Enqueue(p);
            }
        }
        _activePlatforms.Clear();
        _currentPlatformId = 0;
        _isPaused = false;
        enabled = true;

        // Spawn rồi mới tính _highestSpawnY — không đặt trước
        SpawnInitialPlatforms();
        _highestSpawnY = spawnYStart;
    }

    /// <summary>
    /// Gọi sau khi player đã được Instantiate — đảm bảo tìm đúng player.
    /// </summary>
    public void AlignSpawnToPlayer(Transform playerTransform)
    {
        if (!alignToPlayerOnStart) return;
        if (playerTransform == null) return;

        spawnYStart = playerTransform.position.y - Mathf.Abs(playerStartYOffset);
    }

    private void AlignSpawnToPlayer()
    {
        if (!alignToPlayerOnStart) return;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        spawnYStart = player.transform.position.y - Mathf.Abs(playerStartYOffset);
    }

}
