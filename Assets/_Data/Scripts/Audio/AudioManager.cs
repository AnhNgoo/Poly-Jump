using UnityEngine;

public class AudioManager : Singleton<AudioManager>
{
    public const string MusicVolumeKey = "Audio_MusicVolume";
    public const string VfxVolumeKey = "Audio_VfxVolume";

    [Header("Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource vfxSource;

    [Header("Clips")]
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip correctClip;
    [SerializeField] private AudioClip wrongClip;
    [SerializeField] private AudioClip gameOverClip;

    [Header("Defaults")]
    [SerializeField, Range(0f, 1f)] private float defaultMusicVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float defaultVfxVolume = 1f;

    public float MusicVolume { get; private set; }
    public float VfxVolume { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        LoadVolumes();
        ApplyVolumes();
        PlayMusic();
    }

    protected override void LoadComponent()
    {
        if (musicSource == null)
            musicSource = transform.Find("MusicSource")?.GetComponent<AudioSource>();
        if (vfxSource == null)
            vfxSource = transform.Find("VfxSource")?.GetComponent<AudioSource>();
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        if (musicSource != null)
            musicSource.volume = MusicVolume;
        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        PlayerPrefs.Save();
    }

    public void SetVfxVolume(float value)
    {
        VfxVolume = Mathf.Clamp01(value);
        if (vfxSource != null)
            vfxSource.volume = VfxVolume;
        PlayerPrefs.SetFloat(VfxVolumeKey, VfxVolume);
        PlayerPrefs.Save();
    }

    public void PlayVfx(AudioClip clip)
    {
        if (clip == null || vfxSource == null) return;
        vfxSource.PlayOneShot(clip, VfxVolume);
    }

    public void PlayMusic()
    {
        if (musicSource == null || musicClip == null) return;
        musicSource.clip = musicClip;
        musicSource.loop = true;
        if (!musicSource.isPlaying)
            musicSource.Play();
    }

    public void PlayButtonClick() => PlayVfx(buttonClickClip);
    public void PlayJump() => PlayVfx(jumpClip);
    public void PlayCorrect() => PlayVfx(correctClip);
    public void PlayWrong() => PlayVfx(wrongClip);
    public void PlayGameOver() => PlayVfx(gameOverClip);

    private void LoadVolumes()
    {
        MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, defaultMusicVolume);
        VfxVolume = PlayerPrefs.GetFloat(VfxVolumeKey, defaultVfxVolume);
    }

    private void ApplyVolumes()
    {
        if (musicSource != null)
            musicSource.volume = MusicVolume;
        if (vfxSource != null)
            vfxSource.volume = VfxVolume;
    }
}
