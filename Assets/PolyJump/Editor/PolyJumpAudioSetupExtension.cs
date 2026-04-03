using PolyJump.Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PolyJump.Editor
{
    /// <summary>
    /// Công cụ Editor để thiết lập AudioManager theo hướng idempotent và gán tài nguyên âm thanh mặc định.
    /// </summary>
    public static class PolyJumpAudioSetupExtension
    {
        private const string AudioManagerObjectName = "AudioManager";
        private const string ToggleSpriteSetFolderPath = "Assets/PolyJump/Settings/Audio";
        private const string ToggleSpriteSetAssetPath = "Assets/PolyJump/Settings/Audio/AudioToggleSpriteSet.asset";

        [MenuItem("PolyJump/Setup Audio Manager (Idempotent)")]
        /// <summary>
        /// Thiết lập up Audio Manager phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        public static void SetupAudioManager()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            Scene activeScene = SceneManager.GetActiveScene();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (!activeScene.IsValid())
            {
                Debug.LogWarning("[PolyJump] Khong tim thay scene active de setup AudioManager.");
                return;
            }

            bool changed = false;

            GameObject audioManagerObject = GameObject.Find(AudioManagerObjectName);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (audioManagerObject == null)
            {
                audioManagerObject = new GameObject(AudioManagerObjectName);
                Undo.RegisterCreatedObjectUndo(audioManagerObject, "Create AudioManager");
                changed = true;
            }

            AudioManager manager = audioManagerObject.GetComponent<AudioManager>();
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (manager == null)
            {
                manager = Undo.AddComponent<AudioManager>(audioManagerObject);
                changed = true;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (manager != null)
            {
                changed |= EnsureAudioSources(manager, audioManagerObject);
                changed |= AssignDefaultClipsIfMissing(manager);
                changed |= EnsureToggleSpriteSet(manager);
                EditorUtility.SetDirty(manager);
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (changed)
            {
                EditorUtility.SetDirty(audioManagerObject);
                EditorSceneManager.MarkSceneDirty(activeScene);
            }

            Debug.Log(changed
                ? "[PolyJump] AudioManager setup completed (idempotent)."
                : "[PolyJump] AudioManager already configured. No changes applied.");
        }

        [MenuItem("PolyJump/Build Stage 1 Prototype + Audio (Idempotent)")]
        /// <summary>
        /// Xây dựng Prototype With Audio phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        public static void BuildPrototypeWithAudio()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            PolyJumpPrototypeBuilder.BuildStage1Prototype();
            SetupAudioManager();
        }

        /// <summary>
        /// Đảm bảo Audio Sources phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static bool EnsureAudioSources(AudioManager manager, GameObject owner)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            bool changed = false;

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (manager.musicSource == null)
            {
                AudioSource existing = owner.GetComponent<AudioSource>();
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (existing == null)
                {
                    existing = Undo.AddComponent<AudioSource>(owner);
                    ConfigureMusicDefaults(existing);
                }

                manager.musicSource = existing;
                changed = true;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (manager.sfxSource == null || manager.sfxSource == manager.musicSource)
            {
                AudioSource second = FindSecondaryAudioSource(owner, manager.musicSource);
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (second == null)
                {
                    second = Undo.AddComponent<AudioSource>(owner);
                    ConfigureSfxDefaults(second);
                }

                manager.sfxSource = second;
                changed = true;
            }

            return changed;
        }

        /// <summary>
        /// Tìm Secondary Audio Source phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static AudioSource FindSecondaryAudioSource(GameObject owner, AudioSource exclude)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            AudioSource[] all = owner.GetComponents<AudioSource>();
            // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
            for (int i = 0; i < all.Length; i++)
            {
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (all[i] != null && all[i] != exclude)
                {
                    return all[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Thực hiện nghiệp vụ Assign Default Clips If Missing theo ngữ cảnh sử dụng của script.
        /// </summary>
        private static bool AssignDefaultClipsIfMissing(AudioManager manager)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            bool changed = false;

            changed |= TryAssignClipIfMissing(manager, clip => manager.backgroundMusic = clip, manager.backgroundMusic,
                "Assets/PolyJump/Sounds/background music.mp3");

            changed |= TryAssignClipIfMissing(manager, clip => manager.buttonClickClip = clip, manager.buttonClickClip,
                "Assets/PolyJump/Sounds/button.wav");

            changed |= TryAssignClipIfMissing(manager, clip => manager.correctAnswerClip = clip, manager.correctAnswerClip,
                "Assets/PolyJump/Sounds/correct.mp3");

            changed |= TryAssignClipIfMissing(manager, clip => manager.wrongAnswerClip = clip, manager.wrongAnswerClip,
                "Assets/PolyJump/Sounds/wrong.mp3");

            changed |= TryAssignClipIfMissing(manager, clip => manager.gameOverClip = clip, manager.gameOverClip,
                "Assets/PolyJump/Sounds/GameOver.mp3",
                "Assets/PolyJump/Sounds/end game.wav",
                "Assets/PolyJump/Sounds/die.mp3");

            changed |= TryAssignClipIfMissing(manager, clip => manager.jumpClip = clip, manager.jumpClip,
                "Assets/PolyJump/Sounds/jump.wav",
                "Assets/PolyJump/Sounds/jump 2.wav");

            return changed;
        }

        /// <summary>
        /// Đảm bảo Toggle Sprite Set phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static bool EnsureToggleSpriteSet(AudioManager manager)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (manager == null || manager.toggleSpriteSet != null)
            {
                return false;
            }

            EnsureFolderPath(ToggleSpriteSetFolderPath);

            AudioToggleSpriteSet set = AssetDatabase.LoadAssetAtPath<AudioToggleSpriteSet>(ToggleSpriteSetAssetPath);
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<AudioToggleSpriteSet>();
                AssetDatabase.CreateAsset(set, ToggleSpriteSetAssetPath);
                AssetDatabase.SaveAssets();
            }

            manager.toggleSpriteSet = set;
            return true;
        }

        /// <summary>
        /// Đảm bảo Folder Path phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void EnsureFolderPath(string folderPath)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (string.IsNullOrWhiteSpace(folderPath) || AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string normalized = folderPath.Replace('\\', '/');
            string[] parts = normalized.Split('/');
            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
            if (parts.Length == 0)
            {
                return;
            }

            string current = parts[0];
            // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        /// <summary>
        /// Thử xử lý Assign Clip If Missing phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static bool TryAssignClipIfMissing(AudioManager manager, System.Action<AudioClip> assign, AudioClip currentValue, params string[] candidatePaths)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (manager == null || assign == null || currentValue != null || candidatePaths == null)
            {
                return false;
            }

            // Khối lặp: duyệt tuần tự các phần tử cần xử lý.
            for (int i = 0; i < candidatePaths.Length; i++)
            {
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (string.IsNullOrWhiteSpace(candidatePaths[i]))
                {
                    continue;
                }

                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(candidatePaths[i]);
                // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
                if (clip == null)
                {
                    continue;
                }

                assign(clip);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Cấu hình Music Defaults phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void ConfigureMusicDefaults(AudioSource source)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (source == null)
            {
                return;
            }

            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0.7f;
        }

        /// <summary>
        /// Cấu hình Sfx Defaults phục vụ luồng xử lý hiện tại của hệ thống.
        /// </summary>
        private static void ConfigureSfxDefaults(AudioSource source)
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (source == null)
            {
                return;
            }

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.volume = 1f;
        }
    }
}
