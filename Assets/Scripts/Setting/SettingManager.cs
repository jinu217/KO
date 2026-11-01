using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// =====================================================
// SettingManager
// - 설정 UI 초기화 및 상태 동기화
// - Audio / Display 설정 적용
// - PlayerPrefs 저장값 기반 로드 및 초기화
// =====================================================

public class SettingManager : MonoBehaviour
{
    //====================================================
    // AUDIO
    //====================================================

    [Header("Audio")]
    public Slider masterSlider;
    public Slider bgmSlider;
    public Slider sfxSlider;

    //====================================================
    // DISPLAY
    //====================================================

    [Header("Display")]
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown displayModeDropdown;

    private Resolution[] resolutions;
    private int appliedResolutionIndex = -1;
    private int appliedDisplayMode = -1;
    private bool settingsLoaded;

    //====================================================
    // FIXED ASPECT RATIO
    //====================================================

    [Header("Fixed Screen Ratio")]
    [SerializeField] private int targetWidthRatio = 16;
    [SerializeField] private int targetHeightRatio = 9;

    //====================================================
    // INIT
    //====================================================

    void Start()
    {
        InitializeResolution();
        InitializeDisplayMode();
        LoadSettings();
        settingsLoaded = true;
    }

    //====================================================
    // RESOLUTION
    // - 지정한 화면비율에 맞는 해상도만 표시
    //====================================================

    void InitializeResolution()
    {
        Resolution[] allResolutions = Screen.resolutions;

        List<Resolution> validResolutions = new List<Resolution>();

        foreach (Resolution resolution in allResolutions)
        {
            float ratio = (float)resolution.width / resolution.height;
            float targetRatio = (float)targetWidthRatio / targetHeightRatio;

            // 16:9 해상도만 추가
            if (resolution.width * targetHeightRatio == resolution.height * targetWidthRatio)
            {
                validResolutions.Add(resolution);
            }
        }

        resolutions = validResolutions.ToArray();

        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option =
                resolutions[i].width + " x " +
                resolutions[i].height;

            options.Add(option);

            // 현재 해상도와 동일한 항목 찾기
            if (resolutions[i].width == Screen.width &&
                resolutions[i].height == Screen.height)
            {
                currentIndex = i;
            }
        }

        // 만약 현재 해상도가 16:9 목록에 없다면
        // 가장 높은 해상도를 기본값으로 사용
        if (resolutions.Length == 0)
        {
            Debug.LogWarning(
                "[SettingManager] 지정된 화면비율의 해상도를 찾을 수 없습니다."
            );

            return;
        }

        resolutionDropdown.AddOptions(options);

        resolutionDropdown.value = currentIndex;
        resolutionDropdown.RefreshShownValue();
    }

    //====================================================
    // DISPLAY MODE
    //====================================================

    void InitializeDisplayMode()
    {
        displayModeDropdown.ClearOptions();

        displayModeDropdown.AddOptions(new List<string>
        {
            "Windowed",
            "Borderless",
            "Fullscreen"
        });
    }

    //====================================================
    // LOAD SETTINGS
    //====================================================

    void LoadSettings()
    {
        float master = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float bgm = PlayerPrefs.GetFloat("BGMVolume", 1f);
        float sfx = PlayerPrefs.GetFloat("SFXVolume", 1f);

        int resolution =
            PlayerPrefs.GetInt("Resolution", resolutionDropdown.value);

        int displayMode =
            PlayerPrefs.GetInt("DisplayMode", 2);

        // 저장된 해상도 인덱스가 현재 목록보다 큰 경우 방지
        resolution = Mathf.Clamp(
            resolution,
            0,
            resolutionDropdown.options.Count - 1
        );

        displayMode = Mathf.Clamp(displayMode, 0, 2);

        masterSlider.SetValueWithoutNotify(master);
        bgmSlider.SetValueWithoutNotify(bgm);
        sfxSlider.SetValueWithoutNotify(sfx);

        resolutionDropdown.SetValueWithoutNotify(resolution);
        displayModeDropdown.SetValueWithoutNotify(displayMode);

        ApplyAudio(master, bgm, sfx);
        ApplyResolution(resolution);
        ApplyDisplayMode(displayMode);
    }

    //====================================================
    // AUDIO APPLY
    //====================================================

    void ApplyAudio(float master, float bgm, float sfx)
    {
        AudioManager.Instance.SetMasterVolume(master);
        AudioManager.Instance.SetBGMVolume(bgm);
        AudioManager.Instance.SetSFXVolume(sfx);
    }

    public void SetMaster(float value)
    {
        AudioManager.Instance.SetMasterVolume(value);
    }

    public void SetBGM(float value)
    {
        AudioManager.Instance.SetBGMVolume(value);
    }

    public void SetSFX(float value)
    {
        AudioManager.Instance.SetSFXVolume(value);
    }

    //====================================================
    // DISPLAY APPLY
    //====================================================

    public void SetResolution(int index)
    {
        if (resolutions == null || resolutions.Length == 0)
            return;

        if (index < 0 || index >= resolutions.Length)
            return;

        Resolution res = resolutions[index];

        if (settingsLoaded && appliedResolutionIndex != index)
            UISound.Play(UISoundId.DisplayAdjust);
        appliedResolutionIndex = index;

        Screen.SetResolution(
            res.width,
            res.height,
            Screen.fullScreenMode
        );

        PlayerPrefs.SetInt("Resolution", index);
    }

    public void SetDisplayMode(int mode)
    {
        if (mode < 0 || mode > 2) return;
        if (settingsLoaded && appliedDisplayMode != mode)
            UISound.Play(UISoundId.DisplayAdjust);
        appliedDisplayMode = mode;
        switch (mode)
        {
            case 0:
                Screen.fullScreenMode =
                    FullScreenMode.Windowed;
                break;

            case 1:
                Screen.fullScreenMode =
                    FullScreenMode.FullScreenWindow;
                break;

            case 2:
                Screen.fullScreenMode =
                    FullScreenMode.ExclusiveFullScreen;
                break;
        }

        PlayerPrefs.SetInt("DisplayMode", mode);
    }

    void ApplyResolution(int index)
    {
        SetResolution(index);
    }

    void ApplyDisplayMode(int mode)
    {
        SetDisplayMode(mode);
    }

    //====================================================
    // RESET
    //====================================================

    public void ResetSettings()
    {
        masterSlider.value = 1f;
        bgmSlider.value = 1f;
        sfxSlider.value = 1f;

        // 가장 높은 해상도 선택
        resolutionDropdown.value =
            resolutionDropdown.options.Count - 1;

        displayModeDropdown.value = 2;

        SetMaster(1f);
        SetBGM(1f);
        SetSFX(1f);

        SetResolution(resolutionDropdown.value);
        SetDisplayMode(2);
    }
}
