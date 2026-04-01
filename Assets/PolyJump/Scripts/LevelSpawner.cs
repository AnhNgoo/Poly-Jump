using System.Collections.Generic;
using UnityEngine;

namespace PolyJump.Scripts
{
    public class LevelSpawner : MonoBehaviour
    {
        [Header("Prefab References")]
        public GameObject platformPrefab;
        public GameObject quizPlatformPrefab;
        public Transform playerTransform;
        public Transform platformRoot;

        [Header("Spawn Config")]
        public float horizontalRange = 2.5f;
        public float spawnEdgePadding = 0.45f;
        public float minGapY = 1.2f;
        public float maxGapY = 2.0f;
        public int initialPlatformCount = 12;
        public float initialSpawnBelowPlayer = 2.8f;
        public float guaranteedFirstPlatformOffsetY = 1.7f;
        public float spawnAheadDistance = 14f;
        public float cleanupBelowDistance = 12f;
        public float cleanupBelowScreenMargin = 0.1f;

        [Header("Start Ground")]
        public bool createStartGround = true;
        public float groundOffsetBelowPlayer = 1.35f;
        public float groundWidth = 7.4f;
        public float groundHeight = 0.9f;
        public Transform sceneStartGround;

        [Range(0f, 1f)]
        public float quizPlatformChance = 0.15f;

        private readonly List<GameObject> _spawnedPlatforms = new List<GameObject>();
        private float _nextSpawnY;
        private bool _paused;
        private GameObject _startGround;
        private bool _ownsRuntimeGround;
        private Camera _mainCamera;

        private void Start()
        {
            if (platformRoot == null)
            {
                GameObject holder = new GameObject("Platforms");
                platformRoot = holder.transform;
            }

            if (playerTransform == null && GameManager.Instance != null && GameManager.Instance.player != null)
            {
                playerTransform = GameManager.Instance.player.transform;
            }

            _mainCamera = Camera.main;

            if (playerTransform != null)
            {
                ResetLevelAroundPlayer();
            }
        }

        private void Update()
        {
            if (_paused || playerTransform == null)
            {
                return;
            }

            SpawnAhead();
            CleanupOldPlatforms();
        }

        public void SetPaused(bool paused)
        {
            _paused = paused;
        }

        public void ResetLevelAroundPlayer()
        {
            for (int i = _spawnedPlatforms.Count - 1; i >= 0; i--)
            {
                if (_spawnedPlatforms[i] != null)
                {
                    Destroy(_spawnedPlatforms[i]);
                }
            }

            _spawnedPlatforms.Clear();

            if (_startGround != null && _ownsRuntimeGround)
            {
                Destroy(_startGround);
                _startGround = null;
                _ownsRuntimeGround = false;
            }

            if (playerTransform == null)
            {
                return;
            }

            _startGround = ResolveSceneGround();

            if (_startGround != null)
            {
                EnsureGroundTagIfMissing(_startGround);
            }
            else if (createStartGround)
            {
                SpawnStartGround();
            }

            SpawnGuaranteedFirstPlatform();

            _nextSpawnY = playerTransform.position.y - Mathf.Abs(initialSpawnBelowPlayer);

            for (int i = 0; i < initialPlatformCount; i++)
            {
                SpawnSinglePlatform();
            }
        }

        private void SpawnAhead()
        {
            if (playerTransform == null)
            {
                return;
            }

            float topLimit = playerTransform.position.y + spawnAheadDistance;
            while (_nextSpawnY < topLimit)
            {
                SpawnSinglePlatform();
            }
        }

        private void SpawnSinglePlatform()
        {
            GameObject selectedPrefab = platformPrefab;
            bool canUseQuiz = quizPlatformPrefab != null;

            if (canUseQuiz && _spawnedPlatforms.Count > 3 && Random.value <= quizPlatformChance)
            {
                selectedPrefab = quizPlatformPrefab;
            }

            if (selectedPrefab == null)
            {
                return;
            }

            float gap = Random.Range(minGapY, maxGapY);
            _nextSpawnY += gap;

            float x = GetSpawnX();
            Vector3 spawnPos = new Vector3(x, _nextSpawnY, 0f);

            GameObject platform = Instantiate(selectedPrefab, spawnPos, Quaternion.identity, platformRoot);
            _spawnedPlatforms.Add(platform);
        }

        private void SpawnGuaranteedFirstPlatform()
        {
            if (platformPrefab == null || playerTransform == null)
            {
                return;
            }

            Vector3 spawnPos = new Vector3(
                Mathf.Clamp(playerTransform.position.x, -horizontalRange, horizontalRange),
                playerTransform.position.y + Mathf.Abs(guaranteedFirstPlatformOffsetY),
                0f);

            GameObject platform = Instantiate(platformPrefab, spawnPos, Quaternion.identity, platformRoot);
            _spawnedPlatforms.Add(platform);
        }

        private void SpawnStartGround()
        {
            if (platformPrefab == null || playerTransform == null)
            {
                return;
            }

            float y = playerTransform.position.y - Mathf.Abs(groundOffsetBelowPlayer);
            Vector3 pos = new Vector3(0f, y, 0f);

            _startGround = Instantiate(platformPrefab, pos, Quaternion.identity, platformRoot);
            _startGround.name = "StartGround";
            _startGround.tag = "Platform";
            _startGround.transform.localScale = new Vector3(groundWidth, groundHeight, 1f);
            _ownsRuntimeGround = true;

            ConfigureGroundForBounce(_startGround);
        }

        private GameObject ResolveSceneGround()
        {
            if (sceneStartGround != null)
            {
                return sceneStartGround.gameObject;
            }

            Transform rootGround = platformRoot != null ? platformRoot.Find("StartGround") : null;
            if (rootGround != null)
            {
                sceneStartGround = rootGround;
                return rootGround.gameObject;
            }

            GameObject byName = GameObject.Find("StartGround");
            if (byName != null)
            {
                sceneStartGround = byName.transform;
                return byName;
            }

            return null;
        }

        private static void EnsureGroundTagIfMissing(GameObject ground)
        {
            if (ground == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(ground.tag) || ground.tag == "Untagged")
            {
                ground.tag = "Platform";
            }
        }

        private static void ConfigureGroundForBounce(GameObject ground)
        {
            if (ground == null)
            {
                return;
            }

            ground.tag = "Platform";
            BoxCollider2D col = ground.GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private float GetSpawnX()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            if (_mainCamera != null)
            {
                float halfWidth = _mainCamera.orthographicSize * _mainCamera.aspect;
                float minX = _mainCamera.transform.position.x - halfWidth + Mathf.Abs(spawnEdgePadding);
                float maxX = _mainCamera.transform.position.x + halfWidth - Mathf.Abs(spawnEdgePadding);
                if (maxX - minX > 0.1f)
                {
                    return Random.Range(minX, maxX);
                }
            }

            return Random.Range(-horizontalRange, horizontalRange);
        }

        private void CleanupOldPlatforms()
        {
            if (playerTransform == null)
            {
                return;
            }

            float removeBelowY = GetScreenBottomY() - Mathf.Abs(cleanupBelowScreenMargin);

            for (int i = _spawnedPlatforms.Count - 1; i >= 0; i--)
            {
                GameObject platform = _spawnedPlatforms[i];
                if (platform == null)
                {
                    _spawnedPlatforms.RemoveAt(i);
                    continue;
                }

                if (IsBelowScreen(platform, removeBelowY))
                {
                    Destroy(platform);
                    _spawnedPlatforms.RemoveAt(i);
                }
            }

            if (_startGround != null && IsBelowScreen(_startGround, removeBelowY))
            {
                Destroy(_startGround);
                _startGround = null;
                _ownsRuntimeGround = false;

                if (sceneStartGround != null && !sceneStartGround)
                {
                    sceneStartGround = null;
                }
            }
        }

        private float GetScreenBottomY()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            if (_mainCamera == null)
            {
                return playerTransform.position.y - cleanupBelowDistance;
            }

            return _mainCamera.transform.position.y - _mainCamera.orthographicSize;
        }

        private static bool IsBelowScreen(GameObject obj, float screenBottomY)
        {
            if (obj == null)
            {
                return false;
            }

            Collider2D col = obj.GetComponent<Collider2D>();
            if (col != null)
            {
                return col.bounds.max.y < screenBottomY;
            }

            return obj.transform.position.y < screenBottomY;
        }
    }
}
