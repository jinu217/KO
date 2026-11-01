using UnityEngine;
using UnityEngine.Audio;

// AudioManager
// - 게임 전체 오디오 시스템 관리
// - BGM / SFX 재생 처리
// - 볼륨 적용 (AudioMixer 연동)
// - 설정값 저장 / 로드

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    //============== Mixer & Sources ==============
    [Header("Audio Mixer")]
    public AudioMixer audioMixer;

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;
    private UIAudioLibrary uiLibrary;
    private int lastUISoundFrame = -1;
    private UISoundId lastUISound;

    //============== PlayerPrefs Key ==============
    private const string MASTER_KEY = "MasterVolume";
    private const string BGM_KEY = "BGMVolume";
    private const string SFX_KEY = "SFXVolume";

    //============== 초기화 ==============
    void Awake()
    {
        // 싱글톤 유지
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject);
        uiLibrary = Resources.Load<UIAudioLibrary>("Audio/UIAudioLibrary");
        if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        if (uiLibrary != null)
        {
            audioMixer = uiLibrary.mixer;
            bgmSource.outputAudioMixerGroup = uiLibrary.bgmGroup;
            sfxSource.outputAudioMixerGroup = uiLibrary.sfxGroup;
        }
        bgmSource.playOnAwake = sfxSource.playOnAwake = false;
        bgmSource.spatialBlend = sfxSource.spatialBlend = 0f;
        sfxSource.ignoreListenerPause = true;
    }

    private void Start() => LoadVolume();

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void PlayUI(UISoundId id)
    {
        UIAudioLibrary.Entry entry = uiLibrary != null ? uiLibrary.Find(id) : null;
        if (entry == null || entry.clip == null || sfxSource == null) return;
        // Dropdown callbacks and keyboard navigation can report the same change in one frame.
        if (lastUISoundFrame == Time.frameCount && lastUISound == id) return;
        lastUISoundFrame = Time.frameCount;
        lastUISound = id;
        sfxSource.PlayOneShot(entry.clip, entry.volume);
    }

    // BGM CONTROL
    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (clip == null) return;

        // 동일 BGM 재생 중이면 무시
        if (bgmSource.clip == clip && bgmSource.isPlaying)
            return;

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.Play();
    }

    public void StopBGM() => bgmSource.Stop();
    public void PauseBGM() => bgmSource.Pause();
    public void ResumeBGM() => bgmSource.UnPause();

    // SFX CONTROL
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;

        sfxSource.PlayOneShot(clip);
    }

    // VOLUME CONTROL
    public void SetMasterVolume(float value) => SetVolume(MASTER_KEY, "MasterVolume", value);
    public void SetBGMVolume(float value) => SetVolume(BGM_KEY, "BGMVolume", value);
    public void SetSFXVolume(float value) => SetVolume(SFX_KEY, "SFXVolume", value);

    private void SetVolume(string prefKey, string mixerParam, float value)
    {
        value = Mathf.Clamp01(value);

        float db = value <= 0f ? -80f : Mathf.Log10(value) * 20f;

        if (audioMixer != null) audioMixer.SetFloat(mixerParam, db);
        PlayerPrefs.SetFloat(prefKey, value);
    }

    // VOLUME GET
    public float GetMasterVolume() => PlayerPrefs.GetFloat(MASTER_KEY, 1f);
    public float GetBGMVolume() => PlayerPrefs.GetFloat(BGM_KEY, 1f);
    public float GetSFXVolume() => PlayerPrefs.GetFloat(SFX_KEY, 1f);

    // LOAD
    private void LoadVolume()
    {
        SetMasterVolume(GetMasterVolume());
        SetBGMVolume(GetBGMVolume());
        SetSFXVolume(GetSFXVolume());
    }
}
