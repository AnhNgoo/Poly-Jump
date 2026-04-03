using System.IO;
using PolyJump.Scripts;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace PolyJump.Editor
{
    /// <summary>
    /// Công cụ Editor để dựng nhanh prototype scene, prefab và giao diện PolyJump theo cấu hình chuẩn.
    /// </summary>
    public static class PolyJumpPrototypeBuilder
    {
        private const string RootFolder = "Assets/PolyJump";
        private const string ScriptsFolder = RootFolder + "/Scripts";
        private const string ResourcesFolder = RootFolder + "/Resources";
        private const string PrefabsFolder = RootFolder + "/Prefabs";
        private const string AnimationsFolder = RootFolder + "/Animations";
        private const string UiFolder = RootFolder + "/UI";
        private const string EditorFolder = RootFolder + "/Editor";

        private static readonly Color32 FptOrange = new Color32(0xF3, 0x70, 0x21, 0xFF);
        private static readonly Color32 FptNavy = new Color32(0x12, 0x1A, 0x2F, 0xFF);

        private struct UiRefs
        {
            public GameObject panelStart;
            public GameObject panelHud;
            public GameObject panelQuiz;
            public GameObject panelGameOver;
            public GameObject panelLeaderboard;

            public Text hudScore;
            public Text hudTime;
            public Text gameOverScore;
            public Text gameOverHighscore;

            public Button playButton;
            public Button leaderboardButton;
            public Button toggleMusicButton;
            public Button toggleSfxButton;
            public Button replayButton;
            public Button[] answerButtons;
            public Text quizQuestion;
        }

        private struct AuthUiRefs
        {
            public GameObject authRoot;
            public GameObject panelRegister;
            public GameObject panelLogin;
            public TMP_InputField regName;
            public TMP_InputField regEmail;
            public TMP_InputField regPhone;
            public TMP_InputField regPass;
            public Button btnConfirmRegister;
            public TMP_InputField loginEmail;
            public TMP_InputField loginPass;
            public Button btnConfirmLogin;
            public TMP_Text statusText;
        }

        [MenuItem("PolyJump/Build Stage 1 Prototype (Local)")]
        /// <summary>
        /// Xây dựng Stage1 Prototype phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        public static void BuildStage1Prototype()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            EnsureFolders();
            EnsureTag("Platform");
            EnsureTag("QuizPlatform");

            GameObject playerPrefab = CreateOrUpdatePlayerPrefab();
            GameObject platformPrefab = CreateOrUpdatePlatformPrefab(false);
            GameObject quizPlatformPrefab = CreateOrUpdatePlatformPrefab(true);

            SetupScene(playerPrefab, platformPrefab, quizPlatformPrefab);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[PolyJump] Stage 1 prototype build completed.");
        }

        /// <summary>
        /// Đảm bảo Folders phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void EnsureFolders()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            Directory.CreateDirectory(ScriptsFolder);
            Directory.CreateDirectory(ResourcesFolder);
            Directory.CreateDirectory(PrefabsFolder);
            Directory.CreateDirectory(AnimationsFolder);
            Directory.CreateDirectory(UiFolder);
            Directory.CreateDirectory(EditorFolder);
        }

        /// <summary>
        /// Đảm bảo Tag phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void EnsureTag(string tag)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (string.IsNullOrWhiteSpace(tag))
            {
                return;
            }

            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty prop = tagsProp.GetArrayElementAtIndex(i);
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (prop.stringValue == tag)
                {
                    return;
                }
            }

            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
            newTag.stringValue = tag;
            tagManager.ApplyModifiedProperties();
        }

        /// <summary>
        /// Tạo Or Update Player Prefab phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static GameObject CreateOrUpdatePlayerPrefab()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            string path = PrefabsFolder + "/Player.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (existing != null)
            {
                EnsurePlayerPrefabEnhancements(path);
                return existing;
            }

            GameObject temp = new GameObject("Player");
            temp.transform.position = Vector3.zero;
            temp.transform.localScale = new Vector3(1.1f, 1.1f, 1f);

            SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
            sr.sprite = GetSquareSprite();
            sr.color = FptOrange;
            sr.sortingOrder = 20;

            Rigidbody2D rb = temp.AddComponent<Rigidbody2D>();
            rb.gravityScale = 2.5f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            BoxCollider2D collider = temp.AddComponent<BoxCollider2D>();
            collider.isTrigger = false;

            PlayerController player = temp.AddComponent<PlayerController>();
            player.rb = rb;
            player.moveSpeed = 6f;
            player.jumpVelocity = 12f;

            GameObject prefab = SaveTempAsPrefab(temp, path);
            Object.DestroyImmediate(temp);
            EnsurePlayerPrefabEnhancements(path);
            return prefab;
        }

        /// <summary>
        /// Tạo Or Update Platform Prefab phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static GameObject CreateOrUpdatePlatformPrefab(bool isQuiz)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            string prefabName = isQuiz ? "QuizPlatform" : "Platform";
            string path = PrefabsFolder + "/" + prefabName + ".prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (existing != null)
            {
                return existing;
            }

            GameObject temp = new GameObject(prefabName);
            temp.transform.position = Vector3.zero;
            temp.transform.localScale = new Vector3(5f, 1f, 1f);
            temp.tag = isQuiz ? "QuizPlatform" : "Platform";

            SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
            sr.sprite = GetSquareSprite();
            sr.color = isQuiz ? FptNavy : FptOrange;
            sr.sortingOrder = 10;

            BoxCollider2D collider = temp.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            GameObject prefab = SaveTempAsPrefab(temp, path);
            Object.DestroyImmediate(temp);
            return prefab;
        }

        /// <summary>
        /// Lưu Temp As Prefab phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static GameObject SaveTempAsPrefab(GameObject temp, string path)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
            return prefab != null ? prefab : AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        /// <summary>
        /// Đảm bảo Player Prefab Enhancements phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void EnsurePlayerPrefabEnhancements(string prefabPath)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (string.IsNullOrEmpty(prefabPath))
            {
                return;
            }

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (prefabAsset == null)
            {
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            bool changed = false;

            Animator animator = root.GetComponent<Animator>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (animator == null)
            {
                animator = root.AddComponent<Animator>();
                changed = true;
            }

            AnimatorController controller = EnsurePlayerAnimatorController();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (animator.runtimeAnimatorController == null && controller != null)
            {
                animator.runtimeAnimatorController = controller;
                changed = true;
            }

            PlayerController playerController = root.GetComponent<PlayerController>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (playerController != null)
            {
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (playerController.animator == null)
                {
                    playerController.animator = animator;
                    changed = true;
                }

                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (string.IsNullOrWhiteSpace(playerController.normalStateName))
                {
                    playerController.normalStateName = "Normal";
                    changed = true;
                }

                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (string.IsNullOrWhiteSpace(playerController.crouchStateName))
                {
                    playerController.crouchStateName = "Squash";
                    changed = true;
                }

                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (playerController.bounceParticle == null)
                {
                    ParticleSystem existingFx = root.GetComponentInChildren<ParticleSystem>(true);
                    if (existingFx == null)
                    {
                        existingFx = CreateDefaultPlayerBounceParticle(root.transform);
                        changed = true;
                    }

                    if (existingFx != null)
                    {
                        playerController.bounceParticle = existingFx;
                        changed = true;
                    }
                }
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        /// <summary>
        /// Đảm bảo Player Animator Controller phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static AnimatorController EnsurePlayerAnimatorController()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            string controllerPath = AnimationsFolder + "/Player.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (controller == null)
            {
                return null;
            }

            AnimationClip normalClip = EnsureScaleAnimationClip(AnimationsFolder + "/Player_Normal.anim", new Vector3(1f, 1f, 1f));
            AnimationClip squashClip = EnsureScaleAnimationClip(AnimationsFolder + "/Player_Squash.anim", new Vector3(1.15f, 0.78f, 1f));

            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState normalState = FindOrCreateState(sm, "Normal", normalClip, new Vector3(220f, 120f, 0f));
            FindOrCreateState(sm, "Squash", squashClip, new Vector3(520f, 120f, 0f));

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (sm.defaultState == null)
            {
                sm.defaultState = normalState;
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        /// <summary>
        /// Đảm bảo Scale Animation Clip phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static AnimationClip EnsureScaleAnimationClip(string clipPath, Vector3 scale)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (clip != null)
            {
                return clip;
            }

            clip = new AnimationClip();
            clip.frameRate = 60f;

            Keyframe key = new Keyframe(0f, scale.x);
            AnimationCurve curveX = new AnimationCurve(key);
            AnimationCurve curveY = new AnimationCurve(new Keyframe(0f, scale.y));
            AnimationCurve curveZ = new AnimationCurve(new Keyframe(0f, scale.z));

            clip.SetCurve("", typeof(Transform), "m_LocalScale.x", curveX);
            clip.SetCurve("", typeof(Transform), "m_LocalScale.y", curveY);
            clip.SetCurve("", typeof(Transform), "m_LocalScale.z", curveZ);

            AssetDatabase.CreateAsset(clip, clipPath);
            return clip;
        }

        /// <summary>
        /// Tìm Or Create State phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static AnimatorState FindOrCreateState(AnimatorStateMachine sm, string name, Motion motion, Vector3 position)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            foreach (ChildAnimatorState child in sm.states)
            {
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (child.state != null && child.state.name == name)
                {
                    if (child.state.motion == null && motion != null)
                    {
                        child.state.motion = motion;
                    }

                    return child.state;
                }
            }

            AnimatorState state = sm.AddState(name, position);
            state.motion = motion;
            return state;
        }

        /// <summary>
        /// Tạo Default Player Bounce Particle phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static ParticleSystem CreateDefaultPlayerBounceParticle(Transform parent)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (parent == null)
            {
                return null;
            }

            GameObject fxObj = new GameObject("BounceFx");
            fxObj.transform.SetParent(parent, false);
            fxObj.transform.localPosition = Vector3.zero;

            ParticleSystem ps = fxObj.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.25f;
            main.startLifetime = 0.18f;
            main.startSpeed = 1.8f;
            main.startSize = 0.15f;
            main.maxParticles = 24;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.08f;

            return ps;
        }

        /// <summary>
        /// Lấy Square Sprite phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static Sprite GetSquareSprite()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (sprite == null)
            {
                sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            }

            return sprite;
        }

        /// <summary>
        /// Lấy Builtin Font phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static Font GetBuiltinFont()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return font;
        }

        /// <summary>
        /// Thiết lập up Scene phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void SetupScene(GameObject playerPrefab, GameObject platformPrefab, GameObject quizPlatformPrefab)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            Camera cam = SetupMainCamera();
            EnsureDirectionalLight();
            EnsureEventSystem();
            CreateOrUpdateSideBoundaries(cam);

            Canvas canvas = CreateOrGetCanvas();
            UiRefs ui = BuildUi(canvas.transform);
            AuthUiRefs authUi = BuildAuthUi(canvas.transform);

            GameObject spawnPointObj = CreateOrGetPlayerSpawnPoint();
            PrepareExistingScenePlayer();

            GameObject platformRootObj = FindOrCreate("Platforms");
            platformRootObj.transform.position = Vector3.zero;
            GameObject startGroundObj = CreateOrUpdateStartGround(platformRootObj.transform, platformPrefab);

            GameObject managersObj = FindOrCreate("GameManagers");
            GameManager gm = GetOrAddComponent<GameManager>(managersObj);
            LevelSpawner spawner = GetOrAddComponent<LevelSpawner>(managersObj);
            QuizManager quiz = GetOrAddComponent<QuizManager>(managersObj);
            PlayFabAuthManager authManager = GetOrAddComponent<PlayFabAuthManager>(managersObj);
            LeaderboardUiManager leaderboardUi = GetOrAddComponent<LeaderboardUiManager>(managersObj);

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (spawner.platformPrefab == null)
            {
                spawner.platformPrefab = platformPrefab;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (spawner.quizPlatformPrefab == null)
            {
                spawner.quizPlatformPrefab = quizPlatformPrefab;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (spawner.platformRoot == null)
            {
                spawner.platformRoot = platformRootObj.transform;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (spawner.quizPlatformChance <= 0f)
            {
                spawner.quizPlatformChance = 0.15f;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (spawner.initialPlatformCount <= 0)
            {
                spawner.initialPlatformCount = 16;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (spawner.horizontalRange <= 0f)
            {
                spawner.horizontalRange = 2.7f;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (spawner.spawnEdgePadding < 0f)
            {
                spawner.spawnEdgePadding = 0.45f;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (spawner.minGapY <= 0f)
            {
                spawner.minGapY = 0.95f;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (spawner.maxGapY <= 0f)
            {
                spawner.maxGapY = 1.5f;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (spawner.initialSpawnBelowPlayer <= 0f)
            {
                spawner.initialSpawnBelowPlayer = 3.2f;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (spawner.guaranteedFirstPlatformOffsetY <= 0f)
            {
                spawner.guaranteedFirstPlatformOffsetY = 1.7f;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (spawner.groundOffsetBelowPlayer <= 0f)
            {
                spawner.groundOffsetBelowPlayer = 1.35f;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (spawner.groundWidth <= 0f)
            {
                spawner.groundWidth = 7.4f;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (spawner.groundHeight <= 0f)
            {
                spawner.groundHeight = 0.9f;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (spawner.sceneStartGround == null && startGroundObj != null)
            {
                spawner.sceneStartGround = startGroundObj.transform;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (spawner.spawnAheadDistance <= 0f)
            {
                spawner.spawnAheadDistance = 14f;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (spawner.cleanupBelowDistance <= 0f)
            {
                spawner.cleanupBelowDistance = 12f;
            }

            quiz.panelQuiz = ui.panelQuiz;
            quiz.questionText = ui.quizQuestion;
            quiz.answerButtons = ui.answerButtons;
            quiz.resourceFileName = "QuizData";

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (gm.playerPrefab == null)
            {
                gm.playerPrefab = playerPrefab;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (gm.playerSpawnPoint == null && spawnPointObj != null)
            {
                gm.playerSpawnPoint = spawnPointObj.transform;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (gm.levelSpawner == null)
            {
                gm.levelSpawner = spawner;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (gm.quizManager == null)
            {
                gm.quizManager = quiz;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (gm.mainCamera == null)
            {
                gm.mainCamera = cam;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (gm.playFabAuthManager == null)
            {
                gm.playFabAuthManager = authManager;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (gm.panelStart == null)
            {
                gm.panelStart = ui.panelStart;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (gm.panelHud == null)
            {
                gm.panelHud = ui.panelHud;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (gm.panelQuiz == null)
            {
                gm.panelQuiz = ui.panelQuiz;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (gm.panelGameOver == null)
            {
                gm.panelGameOver = ui.panelGameOver;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (gm.hudScoreText == null)
            {
                gm.hudScoreText = ui.hudScore;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (gm.hudTimeText == null)
            {
                gm.hudTimeText = ui.hudTime;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (gm.gameOverScoreText == null)
            {
                gm.gameOverScoreText = ui.gameOverScore;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (gm.gameOverHighscoreText == null)
            {
                gm.gameOverHighscoreText = ui.gameOverHighscore;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (gm.playButton == null)
            {
                gm.playButton = ui.playButton;
            }

            EnsureStartPanelAudioToggles(ui);

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (gm.replayButton == null)
            {
                gm.replayButton = ui.replayButton;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (gm.startTimeSeconds <= 0f)
            {
                gm.startTimeSeconds = 180f;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (gm.fallOutOffset <= 0f)
            {
                gm.fallOutOffset = 1.5f;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (gm.cameraFollowLerp <= 0f)
            {
                gm.cameraFollowLerp = 10f;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (authManager.reg_Name == null)
            {
                authManager.reg_Name = authUi.regName;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (authManager.reg_Email == null)
            {
                authManager.reg_Email = authUi.regEmail;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (authManager.reg_Phone == null)
            {
                authManager.reg_Phone = authUi.regPhone;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (authManager.reg_Pass == null)
            {
                authManager.reg_Pass = authUi.regPass;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (authManager.Btn_ConfirmRegister == null)
            {
                authManager.Btn_ConfirmRegister = authUi.btnConfirmRegister;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (authManager.login_Email == null)
            {
                authManager.login_Email = authUi.loginEmail;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (authManager.login_Pass == null)
            {
                authManager.login_Pass = authUi.loginPass;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (authManager.Btn_ConfirmLogin == null)
            {
                authManager.Btn_ConfirmLogin = authUi.btnConfirmLogin;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (authManager.auth_Status == null)
            {
                authManager.auth_Status = authUi.statusText;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (leaderboardUi.playFabAuthManager == null)
            {
                leaderboardUi.playFabAuthManager = authManager;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (leaderboardUi.targetCanvas == null)
            {
                leaderboardUi.targetCanvas = canvas;
            }

        }

        /// <summary>
        /// Đảm bảo Start Panel Audio Toggles phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void EnsureStartPanelAudioToggles(UiRefs ui)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (ui.toggleMusicButton != null)
            {
                AudioStartToggleRelay musicRelay = ui.toggleMusicButton.GetComponent<AudioStartToggleRelay>();
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (musicRelay != null)
                {
                    Object.DestroyImmediate(musicRelay);
                }
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (ui.toggleSfxButton != null)
            {
                AudioStartToggleRelay sfxRelay = ui.toggleSfxButton.GetComponent<AudioStartToggleRelay>();
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (sfxRelay != null)
                {
                    Object.DestroyImmediate(sfxRelay);
                }
            }
        }

        /// <summary>
        /// Tạo Or Update Side Boundaries phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void CreateOrUpdateSideBoundaries(Camera cam)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (cam == null)
            {
                return;
            }

            float halfWidth = cam.orthographicSize * cam.aspect;
            float yCenter = cam.transform.position.y;
            float boundaryXOffset = halfWidth + 0.15f;

            GameObject boundsRoot = FindOrCreate("WorldBounds");
            boundsRoot.transform.position = Vector3.zero;

            CreateOrUpdateBoundary(boundsRoot.transform, "Boundary_Left", cam.transform.position.x - boundaryXOffset, yCenter);
            CreateOrUpdateBoundary(boundsRoot.transform, "Boundary_Right", cam.transform.position.x + boundaryXOffset, yCenter);
        }

        /// <summary>
        /// Tạo Or Update Boundary phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void CreateOrUpdateBoundary(Transform parent, string name, float x, float y)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            GameObject boundary = FindChildByName(parent, name);
            bool created = false;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (boundary == null)
            {
                boundary = new GameObject(name, typeof(BoxCollider2D));
                boundary.transform.SetParent(parent, false);
                created = true;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (created)
            {
                boundary.transform.position = new Vector3(x, y, 0f);
            }

            BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(boundary);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (created)
            {
                collider.isTrigger = false;
                collider.size = new Vector2(0.4f, 400f);
                collider.offset = Vector2.zero;
            }
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Remove Existing Scene Player theo ngữ cảnh sử dụng của script.
        /// </summary>
        private static void RemoveExistingScenePlayer()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            GameObject playerObj = GameObject.Find("Player");
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (playerObj != null)
            {
                Object.DestroyImmediate(playerObj);
            }
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Prepare Existing Scene Player theo ngữ cảnh sử dụng của script.
        /// </summary>
        private static void PrepareExistingScenePlayer()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            GameObject playerObj = GameObject.Find("Player");
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (playerObj == null)
            {
                return;
            }

            playerObj.SetActive(false);
        }

        /// <summary>
        /// Tạo Or Get Player Spawn Point phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static GameObject CreateOrGetPlayerSpawnPoint()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            GameObject spawn = GameObject.Find("PlayerSpawnPoint");
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (spawn == null)
            {
                spawn = new GameObject("PlayerSpawnPoint");
                spawn.transform.position = new Vector3(0f, -1.2f, 0f);
            }

            return spawn;
        }

        /// <summary>
        /// Tạo Or Update Start Ground phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static GameObject CreateOrUpdateStartGround(Transform parent, GameObject platformPrefab)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (parent == null)
            {
                return null;
            }

            GameObject startGround = FindChildByName(parent, "StartGround");
            bool isAutoCreated = false;
            bool convertedFromLegacyPlatform = false;

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (startGround == null)
            {
                GameObject existingPlatformChild = FindChildByName(parent, "Platform");
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (existingPlatformChild != null)
                {
                    startGround = existingPlatformChild;
                    convertedFromLegacyPlatform = true;
                }
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (startGround == null)
            {
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (platformPrefab != null)
                {
                    startGround = PrefabUtility.InstantiatePrefab(platformPrefab, parent) as GameObject;
                }

                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (startGround == null)
                {
                    startGround = new GameObject("StartGround", typeof(SpriteRenderer), typeof(BoxCollider2D));
                    startGround.transform.SetParent(parent, false);

                    SpriteRenderer sr = startGround.GetComponent<SpriteRenderer>();
                    sr.sprite = GetSquareSprite();
                    sr.color = FptOrange;
                    sr.sortingOrder = 9;
                }

                isAutoCreated = true;
            }

            startGround.name = "StartGround";

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (isAutoCreated || convertedFromLegacyPlatform)
            {
                startGround.transform.position = new Vector3(0f, -2.55f, 0f);
                startGround.transform.localScale = new Vector3(7.4f, 0.9f, 1f);
            }
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (string.IsNullOrEmpty(startGround.tag) || startGround.tag == "Untagged")
            {
                startGround.tag = "Platform";
            }

            BoxCollider2D col = GetOrAddComponent<BoxCollider2D>(startGround);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (isAutoCreated || convertedFromLegacyPlatform)
            {
                col.isTrigger = true;
            }

            return startGround;
        }

        /// <summary>
        /// Thiết lập up Main Camera phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static Camera SetupMainCamera()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            Camera cam = Camera.main;
            bool created = false;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (cam == null)
            {
                GameObject cameraObj = FindOrCreate("Main Camera");
                cameraObj.tag = "MainCamera";
                cam = GetOrAddComponent<Camera>(cameraObj);
                GetOrAddComponent<AudioListener>(cameraObj);
                created = true;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (created)
            {
                cam.orthographic = true;
                cam.orthographicSize = 5f;
                cam.backgroundColor = FptNavy;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.transform.position = new Vector3(0f, 0f, -10f);
            }

            return cam;
        }

        /// <summary>
        /// Đảm bảo Directional Light phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void EnsureDirectionalLight()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            Light[] lights = Object.FindObjectsOfType<Light>();
            // Khối lặp: duyệt từng phần tử trong tập dữ liệu để xử lý.
            foreach (Light light in lights)
            {
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (light.type == LightType.Directional)
                {
                    light.gameObject.name = "Directional Light";
                    light.intensity = 0.8f;
                    return;
                }
            }

            GameObject lightObj = new GameObject("Directional Light");
            Light dirLight = lightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.intensity = 0.8f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        /// <summary>
        /// Đảm bảo Event System phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void EnsureEventSystem()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (Object.FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
        }

        /// <summary>
        /// Tạo Or Get Canvas phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static Canvas CreateOrGetCanvas()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            GameObject canvasObj = GameObject.Find("Canvas_PolyJump");
            bool created = false;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (canvasObj == null)
            {
                canvasObj = new GameObject("Canvas_PolyJump");
                created = true;
            }

            Canvas canvas = GetOrAddComponent<Canvas>(canvasObj);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (created)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            CanvasScaler scaler = GetOrAddComponent<CanvasScaler>(canvasObj);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (created)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            GetOrAddComponent<GraphicRaycaster>(canvasObj);
            return canvas;
        }

        /// <summary>
        /// Xây dựng Ui phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static UiRefs BuildUi(Transform canvas)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            bool panelStartCreated = FindChildByName(canvas, "Panel_Start") == null;
            bool panelHudCreated = FindChildByName(canvas, "Panel_HUD") == null;
            bool panelQuizCreated = FindChildByName(canvas, "Panel_Quiz") == null;
            bool panelGameOverCreated = FindChildByName(canvas, "Panel_GameOver") == null;

            UiRefs ui = new UiRefs
            {
                panelStart = CreatePanel("Panel_Start", canvas, new Color(0f, 0f, 0f, 0f)),
                panelHud = CreatePanel("Panel_HUD", canvas, new Color(0f, 0f, 0f, 0f)),
                panelQuiz = CreatePanel("Panel_Quiz", canvas, new Color(FptNavy.r / 255f, FptNavy.g / 255f, FptNavy.b / 255f, 0.86f)),
                panelGameOver = CreatePanel("Panel_GameOver", canvas, new Color(FptNavy.r / 255f, FptNavy.g / 255f, FptNavy.b / 255f, 0.9f)),
                answerButtons = new Button[4]
            };

            Font font = GetBuiltinFont();

            CreateText("Title", ui.panelStart.transform, "PolyJump", 120, FptOrange, font, new Vector2(0.5f, 0.72f), new Vector2(700f, 180f));
            ui.playButton = CreateButton("Btn_Play", ui.panelStart.transform, "Chơi thôi", new Vector2(0.5f, 0.45f), new Vector2(360f, 120f), FptOrange, Color.white, font, 56);
            ui.leaderboardButton = CreateButton("Btn_Leaderboard", ui.panelStart.transform, "Bảng xếp hạng", new Vector2(0.5f, 0.33f), new Vector2(360f, 100f), FptNavy, Color.white, font, 42);
            ui.toggleMusicButton = CreateButton("Btn_ToggleMusic", ui.panelStart.transform, string.Empty, new Vector2(0.84f, 0.93f), new Vector2(92f, 92f), FptNavy, Color.white, font, 22);
            ui.toggleSfxButton = CreateButton("Btn_ToggleSfx", ui.panelStart.transform, string.Empty, new Vector2(0.94f, 0.93f), new Vector2(92f, 92f), FptNavy, Color.white, font, 22);

            ui.hudScore = CreateText("Txt_Score", ui.panelHud.transform, "Diem: 0", 56, FptOrange, font, new Vector2(0.18f, 0.95f), new Vector2(400f, 100f));
            ui.hudTime = CreateText("Txt_Time", ui.panelHud.transform, "03:00", 56, FptOrange, font, new Vector2(0.82f, 0.95f), new Vector2(280f, 100f));

            ui.quizQuestion = CreateText("Txt_Question", ui.panelQuiz.transform, "Cau hoi", 52, FptOrange, font, new Vector2(0.5f, 0.74f), new Vector2(920f, 360f));
            ui.quizQuestion.alignment = TextAnchor.MiddleCenter;
            ui.quizQuestion.horizontalOverflow = HorizontalWrapMode.Wrap;
            ui.quizQuestion.verticalOverflow = VerticalWrapMode.Overflow;

            // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
            for (int i = 0; i < 4; i++)
            {
                float y = 0.56f - (i * 0.11f);
                ui.answerButtons[i] = CreateButton(
                    "Btn_Answer_" + (i + 1),
                    ui.panelQuiz.transform,
                    "Dap an " + (i + 1),
                    new Vector2(0.5f, y),
                    new Vector2(860f, 110f),
                    FptOrange,
                    Color.white,
                    font,
                    40);
            }

            CreateText("Txt_GameOver", ui.panelGameOver.transform, "GAME OVER", 110, FptOrange, font, new Vector2(0.5f, 0.7f), new Vector2(800f, 170f));
            ui.gameOverScore = CreateText("Txt_FinalScore", ui.panelGameOver.transform, "Diem: 0", 58, Color.white, font, new Vector2(0.5f, 0.55f), new Vector2(700f, 100f));
            ui.gameOverHighscore = CreateText("Txt_Highscore", ui.panelGameOver.transform, "Highscore: 0", 52, Color.white, font, new Vector2(0.5f, 0.48f), new Vector2(700f, 100f));
            ui.replayButton = CreateButton("Btn_Replay", ui.panelGameOver.transform, "REPLAY", new Vector2(0.5f, 0.34f), new Vector2(380f, 120f), FptOrange, Color.white, font, 52);

            bool panelLeaderboardCreated;
            ui.panelLeaderboard = BuildLeaderboardPanel(canvas, font, out panelLeaderboardCreated);

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (panelStartCreated)
            {
                ui.panelStart.SetActive(true);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (panelHudCreated)
            {
                ui.panelHud.SetActive(false);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (panelQuizCreated)
            {
                ui.panelQuiz.SetActive(false);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (panelGameOverCreated)
            {
                ui.panelGameOver.SetActive(false);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (panelLeaderboardCreated && ui.panelLeaderboard != null)
            {
                ui.panelLeaderboard.SetActive(false);
            }

            return ui;
        }

        /// <summary>
        /// Xây dựng Leaderboard Panel phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static GameObject BuildLeaderboardPanel(Transform canvas, Font font, out bool panelCreated)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            panelCreated = FindChildByName(canvas, "Panel_Leaderboard") == null;
            GameObject panel = CreatePanel("Panel_Leaderboard", canvas, new Color(FptNavy.r / 255f, FptNavy.g / 255f, FptNavy.b / 255f, 0.93f));

            GameObject card = FindChildByName(panel.transform, "Card");
            bool cardCreated = false;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (card == null)
            {
                card = new GameObject("Card", typeof(RectTransform), typeof(Image));
                card.transform.SetParent(panel.transform, false);
                cardCreated = true;
            }

            RectTransform cardRt = card.GetComponent<RectTransform>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (cardCreated)
            {
                cardRt.anchorMin = new Vector2(0.5f, 0.5f);
                cardRt.anchorMax = new Vector2(0.5f, 0.5f);
                cardRt.pivot = new Vector2(0.5f, 0.5f);
                cardRt.sizeDelta = new Vector2(1020f, 1740f);
                cardRt.anchoredPosition = Vector2.zero;
            }

            Image cardImage = GetOrAddComponent<Image>(card);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (cardCreated)
            {
                cardImage.color = new Color32(0xF6, 0xF8, 0xFC, 0xFF);
            }

            CreateButton("Btn_Back", card.transform, "Quay lại", new Vector2(0.12f, 0.93f), new Vector2(220f, 74f), FptNavy, Color.white, font, 30);
            CreateText("Txt_LeaderboardTitle", card.transform, "Bảng Xếp Hạng", 58, FptOrange, font, new Vector2(0.5f, 0.93f), new Vector2(620f, 96f));
            CreateText("Txt_TabTitle", card.transform, "Bảng xếp hạng thường", 34, FptNavy, font, new Vector2(0.5f, 0.865f), new Vector2(620f, 68f));

            CreateText("Txt_CurrentUser", card.transform, "Người chơi: --", 30, FptNavy, font, new Vector2(0.5f, 0.815f), new Vector2(900f, 58f));
            CreateText("Txt_CurrentScore", card.transform, "Điểm: --", 30, FptNavy, font, new Vector2(0.36f, 0.775f), new Vector2(360f, 58f));
            CreateText("Txt_CurrentRank", card.transform, "Hạng: --", 30, FptNavy, font, new Vector2(0.64f, 0.775f), new Vector2(360f, 58f));

            GameObject headerRow = FindChildByName(card.transform, "Header_Row");
            bool headerCreated = false;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (headerRow == null)
            {
                headerRow = new GameObject("Header_Row", typeof(RectTransform), typeof(Image));
                headerRow.transform.SetParent(card.transform, false);
                headerCreated = true;
            }

            RectTransform headerRt = headerRow.GetComponent<RectTransform>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (headerCreated)
            {
                headerRt.anchorMin = new Vector2(0.08f, 0.73f);
                headerRt.anchorMax = new Vector2(0.92f, 0.775f);
                headerRt.offsetMin = Vector2.zero;
                headerRt.offsetMax = Vector2.zero;
            }

            Image headerImage = GetOrAddComponent<Image>(headerRow);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (headerCreated)
            {
                headerImage.color = new Color32(0xD9, 0xE4, 0xFA, 0xFF);
            }

            bool headerRankCreated = FindChildByName(headerRow.transform, "Txt_HeaderRank") == null;
            Text headerRank = CreateText("Txt_HeaderRank", headerRow.transform, "Thứ hạng", 26, FptNavy, font, new Vector2(0f, 0.5f), new Vector2(220f, 52f));
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (headerRankCreated)
            {
                RectTransform headerRankRt = headerRank.GetComponent<RectTransform>();
                headerRankRt.pivot = new Vector2(0f, 0.5f);
                headerRankRt.anchoredPosition = new Vector2(18f, 0f);
                headerRank.alignment = TextAnchor.MiddleLeft;
                headerRank.fontStyle = FontStyle.Bold;
            }

            bool headerNameCreated = FindChildByName(headerRow.transform, "Txt_HeaderName") == null;
            Text headerName = CreateText("Txt_HeaderName", headerRow.transform, "Người chơi", 26, FptNavy, font, new Vector2(0.5f, 0.5f), new Vector2(420f, 52f));
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (headerNameCreated)
            {
                headerName.alignment = TextAnchor.MiddleCenter;
                headerName.fontStyle = FontStyle.Bold;
            }

            bool headerScoreCreated = FindChildByName(headerRow.transform, "Txt_HeaderScore") == null;
            Text headerScore = CreateText("Txt_HeaderScore", headerRow.transform, "Điểm", 26, FptNavy, font, new Vector2(1f, 0.5f), new Vector2(180f, 52f));
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (headerScoreCreated)
            {
                RectTransform headerScoreRt = headerScore.GetComponent<RectTransform>();
                headerScoreRt.pivot = new Vector2(1f, 0.5f);
                headerScoreRt.anchoredPosition = new Vector2(-18f, 0f);
                headerScore.alignment = TextAnchor.MiddleRight;
                headerScore.fontStyle = FontStyle.Bold;
            }

            bool eventTimeCreated = FindChildByName(card.transform, "Txt_EventTime") == null;
            Text eventTime = CreateText("Txt_EventTime", card.transform, "Thời gian sự kiện: --", 24, FptNavy, font, new Vector2(0.5f, 0.695f), new Vector2(860f, 42f));
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (eventTimeCreated)
            {
                eventTime.alignment = TextAnchor.MiddleLeft;
                eventTime.horizontalOverflow = HorizontalWrapMode.Wrap;
                eventTime.verticalOverflow = VerticalWrapMode.Truncate;
            }

            bool eventRewardsCreated = FindChildByName(card.transform, "Txt_EventRewards") == null;
            Text eventRewards = CreateText("Txt_EventRewards", card.transform, "Quà sự kiện: --", 24, FptNavy, font, new Vector2(0.5f, 0.665f), new Vector2(860f, 42f));
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (eventRewardsCreated)
            {
                eventRewards.alignment = TextAnchor.MiddleLeft;
                eventRewards.horizontalOverflow = HorizontalWrapMode.Wrap;
                eventRewards.verticalOverflow = VerticalWrapMode.Truncate;
            }

            GameObject scrollRoot = FindChildByName(card.transform, "ScrollRoot");
            bool scrollRootCreated = false;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (scrollRoot == null)
            {
                scrollRoot = new GameObject("ScrollRoot", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
                scrollRoot.transform.SetParent(card.transform, false);
                scrollRootCreated = true;
            }

            RectTransform scrollRootRt = scrollRoot.GetComponent<RectTransform>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (scrollRootCreated)
            {
                scrollRootRt.anchorMin = new Vector2(0.08f, 0.17f);
                scrollRootRt.anchorMax = new Vector2(0.92f, 0.62f);
                scrollRootRt.offsetMin = Vector2.zero;
                scrollRootRt.offsetMax = Vector2.zero;
            }

            Image scrollBg = GetOrAddComponent<Image>(scrollRoot);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (scrollRootCreated)
            {
                scrollBg.color = new Color32(0xE9, 0xEE, 0xF7, 0xFF);
            }

            GameObject viewport = FindChildByName(scrollRoot.transform, "Viewport");
            bool viewportCreated = false;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (viewport == null)
            {
                viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
                viewport.transform.SetParent(scrollRoot.transform, false);
                viewportCreated = true;
            }

            RectTransform viewportRt = viewport.GetComponent<RectTransform>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (viewportCreated)
            {
                viewportRt.anchorMin = Vector2.zero;
                viewportRt.anchorMax = Vector2.one;
                viewportRt.offsetMin = new Vector2(14f, 14f);
                viewportRt.offsetMax = new Vector2(-14f, -14f);
            }

            Image viewportImage = GetOrAddComponent<Image>(viewport);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (viewportCreated)
            {
                viewportImage.color = new Color32(0xF9, 0xFB, 0xFF, 0xFF);
            }

            Mask viewportMask = GetOrAddComponent<Mask>(viewport);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (viewportCreated)
            {
                viewportMask.showMaskGraphic = true;
            }

            GameObject content = FindChildByName(viewport.transform, "Content");
            bool contentCreated = false;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (content == null)
            {
                content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                content.transform.SetParent(viewport.transform, false);
                contentCreated = true;
            }

            RectTransform contentRt = content.GetComponent<RectTransform>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (contentCreated)
            {
                contentRt.anchorMin = new Vector2(0f, 1f);
                contentRt.anchorMax = new Vector2(1f, 1f);
                contentRt.pivot = new Vector2(0.5f, 1f);
                contentRt.anchoredPosition = Vector2.zero;
                contentRt.sizeDelta = Vector2.zero;
            }

            VerticalLayoutGroup layout = GetOrAddComponent<VerticalLayoutGroup>(content);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (contentCreated)
            {
                layout.spacing = 8f;
                layout.padding = new RectOffset(8, 8, 8, 8);
                layout.childControlHeight = true;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;
            }

            ContentSizeFitter fitter = GetOrAddComponent<ContentSizeFitter>(content);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (contentCreated)
            {
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            GameObject rowTemplate = FindChildByName(content.transform, "Row_1");
            bool rowTemplateCreated = false;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (rowTemplate == null)
            {
                rowTemplate = new GameObject("Row_1", typeof(RectTransform), typeof(Image));
                rowTemplate.transform.SetParent(content.transform, false);
                rowTemplateCreated = true;
            }

            RectTransform rowRt = rowTemplate.GetComponent<RectTransform>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (rowTemplateCreated)
            {
                rowRt.anchorMin = new Vector2(0f, 1f);
                rowRt.anchorMax = new Vector2(1f, 1f);
                rowRt.pivot = new Vector2(0.5f, 1f);
                rowRt.sizeDelta = new Vector2(0f, 74f);
            }

            Image rowImage = GetOrAddComponent<Image>(rowTemplate);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (rowTemplateCreated)
            {
                rowImage.color = new Color32(0xF2, 0xF6, 0xFF, 0xFF);
            }

            bool rowRankCreated = FindChildByName(rowTemplate.transform, "Txt_Rank") == null;
            Text rowRank = CreateText("Txt_Rank", rowTemplate.transform, "#1", 28, FptNavy, font, new Vector2(0f, 0.5f), new Vector2(220f, 56f));
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (rowRankCreated)
            {
                RectTransform rowRankRt = rowRank.GetComponent<RectTransform>();
                rowRankRt.pivot = new Vector2(0f, 0.5f);
                rowRankRt.anchoredPosition = new Vector2(18f, 0f);
                rowRank.alignment = TextAnchor.MiddleLeft;
            }

            bool rowNameCreated = FindChildByName(rowTemplate.transform, "Txt_Name") == null;
            Text rowName = CreateText("Txt_Name", rowTemplate.transform, "Người chơi", 28, FptNavy, font, new Vector2(0.5f, 0.5f), new Vector2(440f, 56f));
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (rowNameCreated)
            {
                rowName.alignment = TextAnchor.MiddleCenter;
            }

            bool rowScoreCreated = FindChildByName(rowTemplate.transform, "Txt_Score") == null;
            Text rowScore = CreateText("Txt_Score", rowTemplate.transform, "0", 28, FptOrange, font, new Vector2(1f, 0.5f), new Vector2(180f, 56f));
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (rowScoreCreated)
            {
                RectTransform rowScoreRt = rowScore.GetComponent<RectTransform>();
                rowScoreRt.pivot = new Vector2(1f, 0.5f);
                rowScoreRt.anchoredPosition = new Vector2(-18f, 0f);
                rowScore.alignment = TextAnchor.MiddleRight;
                rowScore.fontStyle = FontStyle.Bold;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (rowTemplateCreated)
            {
                rowTemplate.SetActive(false);
            }

            ScrollRect scrollRect = scrollRoot.GetComponent<ScrollRect>();
            bool scrollRectCreated = false;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (scrollRect == null)
            {
                scrollRect = scrollRoot.AddComponent<ScrollRect>();
                scrollRectCreated = true;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (scrollRect.viewport == null)
            {
                scrollRect.viewport = viewportRt;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (scrollRect.content == null)
            {
                scrollRect.content = contentRt;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (scrollRectCreated)
            {
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.scrollSensitivity = 20f;
            }

            bool emptyTextCreated = FindChildByName(viewport.transform, "Txt_Empty") == null;
            Text emptyText = CreateText("Txt_Empty", viewport.transform, "Chưa có xếp hạng", 34, new Color32(0x5D, 0x6B, 0x85, 0xFF), font, new Vector2(0.5f, 0.5f), new Vector2(640f, 110f));
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (emptyTextCreated)
            {
                emptyText.alignment = TextAnchor.MiddleCenter;
            }

            CreateButton("Btn_TabNormal", card.transform, "Thường", new Vector2(0.33f, 0.09f), new Vector2(230f, 78f), FptNavy, Color.white, font, 30);
            CreateButton("Btn_TabRaceTop", card.transform, "Sự kiện", new Vector2(0.56f, 0.09f), new Vector2(230f, 78f), FptNavy, Color.white, font, 30);
            CreateButton("Btn_RefreshLeaderboard", card.transform, "Làm mới", new Vector2(0.79f, 0.09f), new Vector2(230f, 78f), FptOrange, Color.white, font, 30);

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (panelCreated)
            {
                panel.SetActive(false);
            }

            return panel;
        }

        /// <summary>
        /// Xây dựng Auth Ui phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static AuthUiRefs BuildAuthUi(Transform canvas)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            AuthUiRefs auth = new AuthUiRefs();

            auth.authRoot = FindChildByName(canvas, "Panel_Auth");
            bool rootCreated = false;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (auth.authRoot == null)
            {
                auth.authRoot = new GameObject("Panel_Auth", typeof(RectTransform));
                auth.authRoot.transform.SetParent(canvas, false);
                rootCreated = true;
            }

            RectTransform rootRect = auth.authRoot.GetComponent<RectTransform>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (rootCreated)
            {
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;
            }

            auth.panelRegister = CreateAuthPanel(
                "Panel_Register",
                auth.authRoot.transform,
                new Vector2(0.26f, 0.38f),
                new Vector2(460f, 800f),
                new Color(FptNavy.r / 255f, FptNavy.g / 255f, FptNavy.b / 255f, 0.72f));

            auth.panelLogin = CreateAuthPanel(
                "Panel_Login",
                auth.authRoot.transform,
                new Vector2(0.74f, 0.38f),
                new Vector2(460f, 620f),
                new Color(FptNavy.r / 255f, FptNavy.g / 255f, FptNavy.b / 255f, 0.72f));

            CreateTMPLabel("reg_Title", auth.panelRegister.transform, "Dang Ky", 56, FptOrange, new Vector2(0.5f, 0.92f), new Vector2(360f, 90f));
            auth.regName = CreateTMPInputField("reg_Name", auth.panelRegister.transform, "Ho ten", new Vector2(0.5f, 0.78f), new Vector2(380f, 84f), TMP_InputField.ContentType.Standard);
            auth.regEmail = CreateTMPInputField("reg_Email", auth.panelRegister.transform, "Email", new Vector2(0.5f, 0.66f), new Vector2(380f, 84f), TMP_InputField.ContentType.EmailAddress);
            auth.regPhone = CreateTMPInputField("reg_Phone", auth.panelRegister.transform, "So dien thoai", new Vector2(0.5f, 0.54f), new Vector2(380f, 84f), TMP_InputField.ContentType.Standard);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (auth.regPhone != null && auth.regPhone.characterValidation == TMP_InputField.CharacterValidation.None)
            {
                auth.regPhone.characterValidation = TMP_InputField.CharacterValidation.Integer;
            }
            auth.regPass = CreateTMPInputField("reg_Pass", auth.panelRegister.transform, "Mat khau", new Vector2(0.5f, 0.42f), new Vector2(380f, 84f), TMP_InputField.ContentType.Password);
            auth.btnConfirmRegister = CreateTMPButton("Btn_ConfirmRegister", auth.panelRegister.transform, "Dang Ky", new Vector2(0.5f, 0.26f), new Vector2(280f, 90f), FptOrange, Color.white, 42);

            CreateTMPLabel("login_Title", auth.panelLogin.transform, "Dang Nhap", 56, FptOrange, new Vector2(0.5f, 0.88f), new Vector2(360f, 90f));
            auth.loginEmail = CreateTMPInputField("login_Email", auth.panelLogin.transform, "Email", new Vector2(0.5f, 0.66f), new Vector2(380f, 84f), TMP_InputField.ContentType.EmailAddress);
            auth.loginPass = CreateTMPInputField("login_Pass", auth.panelLogin.transform, "Mat khau", new Vector2(0.5f, 0.5f), new Vector2(380f, 84f), TMP_InputField.ContentType.Password);
            auth.btnConfirmLogin = CreateTMPButton("Btn_ConfirmLogin", auth.panelLogin.transform, "Dang Nhap", new Vector2(0.5f, 0.32f), new Vector2(280f, 90f), FptOrange, Color.white, 42);

            auth.statusText = CreateTMPLabel("auth_Status", auth.authRoot.transform, string.Empty, 34, Color.white, new Vector2(0.5f, 0.08f), new Vector2(920f, 80f));
            auth.statusText.alignment = TextAlignmentOptions.Center;

            return auth;
        }

        /// <summary>
        /// Tạo Auth Panel phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static GameObject CreateAuthPanel(string name, Transform parent, Vector2 anchor, Vector2 size, Color color)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            GameObject panel = FindChildByName(parent, name);
            bool created = false;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (panel == null)
            {
                panel = new GameObject(name, typeof(RectTransform), typeof(Image));
                panel.transform.SetParent(parent, false);
                created = true;
            }

            RectTransform rect = panel.GetComponent<RectTransform>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (created)
            {
                rect.anchorMin = anchor;
                rect.anchorMax = anchor;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = size;
                rect.anchoredPosition = Vector2.zero;
            }

            Image image = panel.GetComponent<Image>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (created)
            {
                image.color = color;
            }

            return panel;
        }

        /// <summary>
        /// Tạo TMP Label phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static TMP_Text CreateTMPLabel(string name, Transform parent, string content, int fontSize, Color color, Vector2 anchor, Vector2 size)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            GameObject obj = FindChildByName(parent, name);
            bool created = false;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (obj == null)
            {
                obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
                obj.transform.SetParent(parent, false);
                created = true;
            }

            RectTransform rect = obj.GetComponent<RectTransform>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (created)
            {
                rect.anchorMin = anchor;
                rect.anchorMax = anchor;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = size;
                rect.anchoredPosition = Vector2.zero;
            }

            TMP_Text text = obj.GetComponent<TextMeshProUGUI>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (created)
            {
                text.text = content;
                text.fontSize = fontSize;
                text.color = color;
                text.alignment = TextAlignmentOptions.Midline;
            }

            return text;
        }

        /// <summary>
        /// Tạo TMP Input Field phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static TMP_InputField CreateTMPInputField(string name, Transform parent, string placeholder, Vector2 anchor, Vector2 size, TMP_InputField.ContentType contentType)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            GameObject obj = FindChildByName(parent, name);
            bool created = false;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (obj == null)
            {
                obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
                obj.transform.SetParent(parent, false);
                created = true;
            }

            RectTransform rect = obj.GetComponent<RectTransform>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (created)
            {
                rect.anchorMin = anchor;
                rect.anchorMax = anchor;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = size;
                rect.anchoredPosition = Vector2.zero;
            }

            Image image = obj.GetComponent<Image>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (created)
            {
                image.color = new Color(1f, 1f, 1f, 0.95f);
            }

            TMP_InputField input = obj.GetComponent<TMP_InputField>();
            EnsureTmpInputTextObjects(input, placeholder);

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (created)
            {
                input.contentType = contentType;
                input.lineType = TMP_InputField.LineType.SingleLine;
            }

            return input;
        }

        /// <summary>
        /// Đảm bảo Tmp Input Text Objects phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void EnsureTmpInputTextObjects(TMP_InputField input, string placeholderText)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (input == null)
            {
                return;
            }

            Transform inputTransform = input.transform;
            GameObject textArea = FindChildByName(inputTransform, "Text Area");
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (textArea == null)
            {
                textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
                textArea.transform.SetParent(inputTransform, false);
            }

            RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(18f, 10f);
            textAreaRect.offsetMax = new Vector2(-18f, -10f);

            GameObject placeholderObj = FindChildByName(textArea.transform, "Placeholder");
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (placeholderObj == null)
            {
                placeholderObj = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
                placeholderObj.transform.SetParent(textArea.transform, false);
            }

            RectTransform placeholderRect = placeholderObj.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;

            TextMeshProUGUI placeholderTextComp = placeholderObj.GetComponent<TextMeshProUGUI>();
            placeholderTextComp.text = placeholderText;
            placeholderTextComp.fontSize = 32;
            placeholderTextComp.color = new Color(0.26f, 0.26f, 0.26f, 0.6f);
            placeholderTextComp.alignment = TextAlignmentOptions.MidlineLeft;

            GameObject textObj = FindChildByName(textArea.transform, "Text");
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (textObj == null)
            {
                textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(textArea.transform, false);
            }

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI inputTextComp = textObj.GetComponent<TextMeshProUGUI>();
            inputTextComp.fontSize = 34;
            inputTextComp.color = new Color(0.13f, 0.13f, 0.13f, 1f);
            inputTextComp.alignment = TextAlignmentOptions.MidlineLeft;

            input.textViewport = textAreaRect;
            input.textComponent = inputTextComp;
            input.placeholder = placeholderTextComp;
        }

        /// <summary>
        /// Tạo TMP Button phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static Button CreateTMPButton(string name, Transform parent, string label, Vector2 anchor, Vector2 size, Color bgColor, Color textColor, int fontSize)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            GameObject obj = FindChildByName(parent, name);
            bool created = false;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (obj == null)
            {
                obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                obj.transform.SetParent(parent, false);
                created = true;
            }

            RectTransform rect = obj.GetComponent<RectTransform>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (created)
            {
                rect.anchorMin = anchor;
                rect.anchorMax = anchor;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = size;
                rect.anchoredPosition = Vector2.zero;
            }

            Image image = obj.GetComponent<Image>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (created)
            {
                image.color = bgColor;
            }

            Button button = obj.GetComponent<Button>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (created)
            {
                ColorBlock colors = button.colors;
                colors.normalColor = bgColor;
                colors.highlightedColor = new Color(bgColor.r * 1.05f, bgColor.g * 1.05f, bgColor.b * 1.05f, 1f);
                colors.pressedColor = new Color(bgColor.r * 0.9f, bgColor.g * 0.9f, bgColor.b * 0.9f, 1f);
                colors.selectedColor = colors.highlightedColor;
                button.colors = colors;
            }

            GameObject labelObj = FindChildByName(obj.transform, "Label");
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (labelObj == null)
            {
                labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelObj.transform.SetParent(obj.transform, false);
            }

            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI labelText = labelObj.GetComponent<TextMeshProUGUI>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (created)
            {
                labelText.text = label;
                labelText.fontSize = fontSize;
                labelText.color = textColor;
                labelText.alignment = TextAlignmentOptions.Center;
            }

            return button;
        }

        /// <summary>
        /// Tạo Panel phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            GameObject panel = FindChildByName(parent, name);
            bool created = false;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (panel == null)
            {
                panel = new GameObject(name, typeof(RectTransform), typeof(Image));
                panel.transform.SetParent(parent, false);
                created = true;
            }

            RectTransform rect = panel.GetComponent<RectTransform>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (created)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            Image image = panel.GetComponent<Image>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (created)
            {
                image.color = color;
            }

            return panel;
        }

        /// <summary>
        /// Tạo Text phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static Text CreateText(string name, Transform parent, string content, int fontSize, Color color, Font font, Vector2 anchor, Vector2 size)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            GameObject obj = FindChildByName(parent, name);
            bool created = false;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (obj == null)
            {
                obj = new GameObject(name, typeof(RectTransform), typeof(Text));
                obj.transform.SetParent(parent, false);
                created = true;
            }

            RectTransform rect = obj.GetComponent<RectTransform>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (created)
            {
                rect.anchorMin = anchor;
                rect.anchorMax = anchor;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = size;
                rect.anchoredPosition = Vector2.zero;
            }

            Text text = obj.GetComponent<Text>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (created)
            {
                text.text = content;
                text.font = font;
                text.fontSize = fontSize;
                text.color = color;
                text.alignment = TextAnchor.MiddleCenter;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;
            }

            return text;
        }

        /// <summary>
        /// Tạo Button phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static Button CreateButton(
            string name,
            Transform parent,
            string label,
            Vector2 anchor,
            Vector2 size,
            Color buttonColor,
            Color textColor,
            Font font,
            int fontSize)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            GameObject obj = FindChildByName(parent, name);
            bool created = false;
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (obj == null)
            {
                obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                obj.transform.SetParent(parent, false);
                created = true;
            }

            RectTransform rect = obj.GetComponent<RectTransform>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (created)
            {
                rect.anchorMin = anchor;
                rect.anchorMax = anchor;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = size;
                rect.anchoredPosition = Vector2.zero;
            }

            Image image = obj.GetComponent<Image>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (created)
            {
                image.color = buttonColor;
            }

            Button button = obj.GetComponent<Button>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (created)
            {
                ColorBlock colors = button.colors;
                colors.normalColor = buttonColor;
                colors.highlightedColor = new Color(buttonColor.r * 1.05f, buttonColor.g * 1.05f, buttonColor.b * 1.05f, 1f);
                colors.pressedColor = new Color(buttonColor.r * 0.9f, buttonColor.g * 0.9f, buttonColor.b * 0.9f, 1f);
                colors.selectedColor = colors.highlightedColor;
                colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
                button.colors = colors;
            }

            GameObject textObj = FindChildByName(obj.transform, "Text");
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (textObj == null)
            {
                textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
                textObj.transform.SetParent(obj.transform, false);
            }

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (created)
            {
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
            }

            Text text = textObj.GetComponent<Text>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (created)
            {
                text.text = label;
                text.font = font;
                text.fontSize = fontSize;
                text.color = textColor;
                text.alignment = TextAnchor.MiddleCenter;
            }

            return button;
        }

        /// <summary>
        /// Tìm Or Create phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static GameObject FindOrCreate(string name)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            GameObject obj = GameObject.Find(name);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (obj == null)
            {
                obj = new GameObject(name);
            }

            return obj;
        }

        /// <summary>
        /// Tìm Child By Name phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static GameObject FindChildByName(Transform parent, string childName)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            Transform child = parent.Find(childName);
            return child != null ? child.gameObject : null;
        }

        private static T GetOrAddComponent<T>(GameObject obj) where T : Component
        {
            T comp = obj.GetComponent<T>();
            if (comp == null)
            {
                comp = obj.AddComponent<T>();
            }

            return comp;
        }
    }
}
