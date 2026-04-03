using System.Collections.Generic;
using UnityEngine;

namespace PolyJump.Scripts
{
    /// <summary>
    /// Sinh nền tảng theo tiến trình lên cao, dọn đối tượng cũ và duy trì không gian chơi liên tục.
    /// </summary>
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

        /// <summary>
        /// Thiết lập dữ liệu và liên kết cần dùng ngay trước khi vòng lặp gameplay bắt đầu.
        /// </summary>
        private void Start()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (platformRoot == null)
            {
                GameObject holder = new GameObject("Platforms");
                platformRoot = holder.transform;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (playerTransform == null && GameManager.Instance != null && GameManager.Instance.player != null)
            {
                playerTransform = GameManager.Instance.player.transform;
            }

            _mainCamera = Camera.main;

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (playerTransform != null)
            {
                ResetLevelAroundPlayer();
            }
        }

        /// <summary>
        /// Cập nhật logic theo từng khung hình để phản hồi trạng thái hiện tại của game.
        /// </summary>
        private void Update()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_paused || playerTransform == null)
            {
                return;
            }

            SpawnAhead();
            CleanupOldPlatforms();
        }

        /// <summary>
        /// Thiết lập Paused phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        public void SetPaused(bool paused)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            _paused = paused;
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Reset Level Around Player theo ngữ cảnh sử dụng của script.
        /// </summary>
        public void ResetLevelAroundPlayer()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            for (int i = _spawnedPlatforms.Count - 1; i >= 0; i--)
            {
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (_spawnedPlatforms[i] != null)
                {
                    Destroy(_spawnedPlatforms[i]);
                }
            }

            _spawnedPlatforms.Clear();

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_startGround != null && _ownsRuntimeGround)
            {
                Destroy(_startGround);
                _startGround = null;
                _ownsRuntimeGround = false;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (playerTransform == null)
            {
                return;
            }

            _startGround = ResolveSceneGround();

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
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

            // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
            for (int i = 0; i < initialPlatformCount; i++)
            {
                SpawnSinglePlatform();
            }
        }

        /// <summary>
        /// Sinh Ahead phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void SpawnAhead()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (playerTransform == null)
            {
                return;
            }

            float topLimit = playerTransform.position.y + spawnAheadDistance;
            // Khối lặp điều kiện: tiếp tục xử lý cho đến khi đạt điều kiện dừng.
            while (_nextSpawnY < topLimit)
            {
                SpawnSinglePlatform();
            }
        }

        /// <summary>
        /// Sinh Single Platform phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void SpawnSinglePlatform()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            GameObject selectedPrefab = platformPrefab;
            bool canUseQuiz = quizPlatformPrefab != null;

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (canUseQuiz && _spawnedPlatforms.Count > 3 && Random.value <= quizPlatformChance)
            {
                selectedPrefab = quizPlatformPrefab;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
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

        /// <summary>
        /// Sinh Guaranteed First Platform phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void SpawnGuaranteedFirstPlatform()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
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

        /// <summary>
        /// Sinh Start Ground phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void SpawnStartGround()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
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

        /// <summary>
        /// Xác định Scene Ground phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private GameObject ResolveSceneGround()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (sceneStartGround != null)
            {
                return sceneStartGround.gameObject;
            }

            Transform rootGround = platformRoot != null ? platformRoot.Find("StartGround") : null;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (rootGround != null)
            {
                sceneStartGround = rootGround;
                return rootGround.gameObject;
            }

            GameObject byName = GameObject.Find("StartGround");
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (byName != null)
            {
                sceneStartGround = byName.transform;
                return byName;
            }

            return null;
        }

        /// <summary>
        /// Đảm bảo Ground Tag If Missing phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void EnsureGroundTagIfMissing(GameObject ground)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (ground == null)
            {
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (string.IsNullOrEmpty(ground.tag) || ground.tag == "Untagged")
            {
                ground.tag = "Platform";
            }
        }

        /// <summary>
        /// Cấu hình Ground For Bounce phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void ConfigureGroundForBounce(GameObject ground)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (ground == null)
            {
                return;
            }

            ground.tag = "Platform";
            BoxCollider2D col = ground.GetComponent<BoxCollider2D>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        /// <summary>
        /// Lấy Spawn X phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private float GetSpawnX()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_mainCamera != null)
            {
                float halfWidth = _mainCamera.orthographicSize * _mainCamera.aspect;
                float minX = _mainCamera.transform.position.x - halfWidth + Mathf.Abs(spawnEdgePadding);
                float maxX = _mainCamera.transform.position.x + halfWidth - Mathf.Abs(spawnEdgePadding);
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (maxX - minX > 0.1f)
                {
                    return Random.Range(minX, maxX);
                }
            }

            return Random.Range(-horizontalRange, horizontalRange);
        }

        /// <summary>
        /// Dọn dẹp Old Platforms phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private void CleanupOldPlatforms()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (playerTransform == null)
            {
                return;
            }

            float removeBelowY = GetScreenBottomY() - Mathf.Abs(cleanupBelowScreenMargin);

            // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
            for (int i = _spawnedPlatforms.Count - 1; i >= 0; i--)
            {
                GameObject platform = _spawnedPlatforms[i];
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (platform == null)
                {
                    _spawnedPlatforms.RemoveAt(i);
                    continue;
                }

                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (IsBelowScreen(platform, removeBelowY))
                {
                    Destroy(platform);
                    _spawnedPlatforms.RemoveAt(i);
                }
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_startGround != null && IsBelowScreen(_startGround, removeBelowY))
            {
                Destroy(_startGround);
                _startGround = null;
                _ownsRuntimeGround = false;

                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (sceneStartGround != null && !sceneStartGround)
                {
                    sceneStartGround = null;
                }
            }
        }

        /// <summary>
        /// Lấy Screen Bottom Y phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private float GetScreenBottomY()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (_mainCamera == null)
            {
                return playerTransform.position.y - cleanupBelowDistance;
            }

            return _mainCamera.transform.position.y - _mainCamera.orthographicSize;
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Is Below Screen theo ngữ cảnh sử dụng của script.
        /// </summary>
        private static bool IsBelowScreen(GameObject obj, float screenBottomY)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (obj == null)
            {
                return false;
            }

            Collider2D col = obj.GetComponent<Collider2D>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (col != null)
            {
                return col.bounds.max.y < screenBottomY;
            }

            return obj.transform.position.y < screenBottomY;
        }
    }
}
