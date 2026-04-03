using UnityEngine;

namespace PolyJump.Scripts
{
    [CreateAssetMenu(fileName = "AudioToggleSpriteSet", menuName = "PolyJump/Audio Toggle Sprite Set")]
    /// <summary>
    /// Lưu tập sprite bật/tắt âm thanh để các nút toggle hiển thị đúng trạng thái.
    /// </summary>
    public class AudioToggleSpriteSet : ScriptableObject
    {
        [Header("Music Toggle")]
        public Sprite musicOnSprite;
        public Sprite musicOffSprite;

        [Header("SFX Toggle")]
        public Sprite sfxOnSprite;
        public Sprite sfxOffSprite;
    }
}
