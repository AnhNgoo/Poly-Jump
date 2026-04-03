using PolyJump.Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PolyJump.Editor
{
    public static class PolyJumpAudioSetupExtension
    {
        private const string AudioManagerObjectName = "AudioManager";
        private const string ToggleSpriteSetFolderPath = "Assets/PolyJump/Settings/Audio";
        private const string ToggleSpriteSetAssetPath = "Assets/PolyJump/Settings/Audio/AudioToggleSpriteSet.asset";

        [MenuItem("PolyJump/Setup Audio Manager (Idempotent)")]
        public static void SetupAudioManager()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                Debug.LogWarning("[PolyJump] Khong tim thay scene active de setup AudioManager.");
                return;
            }

            bool changed = false;

            GameObject audioManagerObject = GameObject.Find(AudioManagerObjectName);
            if (audioManagerObject == null)
            {
                audioManagerObject = new GameObject(AudioManagerObjectName);
                Undo.RegisterCreatedObjectUndo(audioManagerObject, "Create AudioManager");
                changed = true;
            }

            AudioManager manager = audioManagerObject.GetComponent<AudioManager>();
            if (manager == null)
            {
                manager = Undo.AddComponent<AudioManager>(audioManagerObject);
                changed = true;
            }

            if (manager != null)
            {
                changed |= EnsureAudioSources(manager, audioManagerObject);
                changed |= AssignDefaultClipsIfMissing(manager);
                changed |= EnsureToggleSpriteSet(manager);
                EditorUtility.SetDirty(manager);
            }

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
        public static void BuildPrototypeWithAudio()
        {
            PolyJumpPrototypeBuilder.BuildStage1Prototype();
            SetupAudioManager();
        }

        private static bool EnsureAudioSources(AudioManager manager, GameObject owner)
        {
            bool changed = false;

            if (manager.musicSource == null)
            {
                AudioSource existing = owner.GetComponent<AudioSource>();
                if (existing == null)
                {
                    existing = Undo.AddComponent<AudioSource>(owner);
                    ConfigureMusicDefaults(existing);
                }

                manager.musicSource = existing;
                changed = true;
            }

            if (manager.sfxSource == null || manager.sfxSource == manager.musicSource)
            {
                AudioSource second = FindSecondaryAudioSource(owner, manager.musicSource);
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

        private static AudioSource FindSecondaryAudioSource(GameObject owner, AudioSource exclude)
        {
            AudioSource[] all = owner.GetComponents<AudioSource>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i] != exclude)
                {
                    return all[i];
                }
            }

            return null;
        }

        private static bool AssignDefaultClipsIfMissing(AudioManager manager)
        {
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

        private static bool EnsureToggleSpriteSet(AudioManager manager)
        {
            if (manager == null || manager.toggleSpriteSet != null)
            {
                return false;
            }

            EnsureFolderPath(ToggleSpriteSetFolderPath);

            AudioToggleSpriteSet set = AssetDatabase.LoadAssetAtPath<AudioToggleSpriteSet>(ToggleSpriteSetAssetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<AudioToggleSpriteSet>();
                AssetDatabase.CreateAsset(set, ToggleSpriteSetAssetPath);
                AssetDatabase.SaveAssets();
            }

            manager.toggleSpriteSet = set;
            return true;
        }

        private static void EnsureFolderPath(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string normalized = folderPath.Replace('\\', '/');
            string[] parts = normalized.Split('/');
            if (parts.Length == 0)
            {
                return;
            }

            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static bool TryAssignClipIfMissing(AudioManager manager, System.Action<AudioClip> assign, AudioClip currentValue, params string[] candidatePaths)
        {
            if (manager == null || assign == null || currentValue != null || candidatePaths == null)
            {
                return false;
            }

            for (int i = 0; i < candidatePaths.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(candidatePaths[i]))
                {
                    continue;
                }

                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(candidatePaths[i]);
                if (clip == null)
                {
                    continue;
                }

                assign(clip);
                return true;
            }

            return false;
        }

        private static void ConfigureMusicDefaults(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0.7f;
        }

        private static void ConfigureSfxDefaults(AudioSource source)
        {
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