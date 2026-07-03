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

    //====================================================
    // INIT
    //====================================================

    void Start()
    {
        InitializeResolution();
        InitializeDisplayMode();
        LoadSettings();
    }

    //====================================================
    // RESOLUTION
    //====================================================

    void InitializeResolution()
    {
        resolutions = Screen.resolutions;

        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentIndex = i;
            }
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

        int resolution = PlayerPrefs.GetInt("Resolution", resolutionDropdown.value);
        int displayMode = PlayerPrefs.GetInt("DisplayMode", 2);

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

    public void SetMaster(float value) => AudioManager.Instance.SetMasterVolume(value);
    public void SetBGM(float value) => AudioManager.Instance.SetBGMVolume(value);
    public void SetSFX(float value) => AudioManager.Instance.SetSFXVolume(value);

    //====================================================
    // DISPLAY APPLY
    //====================================================

    public void SetResolution(int index)
    {
        Resolution res = resolutions[index];

        Screen.SetResolution(res.width, res.height, Screen.fullScreenMode);

        PlayerPrefs.SetInt("Resolution", index);
    }

    public void SetDisplayMode(int mode)
    {
        switch (mode)
        {
            case 0:
                Screen.fullScreenMode = FullScreenMode.Windowed;
                break;

            case 1:
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                break;

            case 2:
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
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

        resolutionDropdown.value = resolutionDropdown.options.Count - 1;
        displayModeDropdown.value = 2;

        SetMaster(1f);
        SetBGM(1f);
        SetSFX(1f);

        SetResolution(resolutionDropdown.value);
        SetDisplayMode(2);
    }
}