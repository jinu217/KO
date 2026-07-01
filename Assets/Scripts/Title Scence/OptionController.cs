using UnityEngine;
using TMPro;
using UnityEngine.UI;

// - 옵션 메뉴 입력 처리, 옵션 항목 선택
// - 볼륨 조절, 해상도 변경
// - 설정 초기화, 타이틀 메뉴로 돌아가기

/// [조작키]
/// ↑ / ↓      : 항목 이동
/// ← / →      : 값 변경 (볼륨, 해상도)
/// Enter / Z  : 선택
/// Esc / X    : 뒤로가기

public class OptionController : MonoBehaviour
{
    [Header("Option Text")]
    public TMP_Text[] texts;

    [Header("Option UI")]
    public Slider volumeSlider;
    public TMP_Dropdown resolutionDropdown;
    public GameObject optionPanel;

    public OptionManager optionManager;
    public TitleMenuController titleMenuController;

    private int index = 0;

    void Start()
    {
        UpdateSelection();
    }

    void Update()
    {
        // 옵션창이 닫혀있으면 입력 무시
        if (!optionPanel.activeSelf)
            return;

        HandleInput();
    }

    //============== 입력 처리 ==================
    void HandleInput()
    {
        // 위 이동
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            index--;

            if (index < 0)
                index = texts.Length - 1;

            UpdateSelection();
        }

        // 아래 이동
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            index++;

            if (index >= texts.Length)
                index = 0;

            UpdateSelection();
        }

        // 값 감소
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            AdjustValue(-1);
        }

        // 값 증가
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            AdjustValue(1);
        }

        // 결정 (Enter 또는 Z)
        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.Z))
        {
            SelectOption();
        }

        // 취소 (Esc 또는 X)
        if (Input.GetKeyDown(KeyCode.Escape) ||
            Input.GetKeyDown(KeyCode.X))
        {
            titleMenuController.CloseOption();
        }
    }

    //============== 선택 표시 =================
    void UpdateSelection()
    {
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null)
                continue;

            bool selected = (i == index);

            // 선택된 텍스트 색상
            texts[i].color = selected ? Color.yellow : Color.white;

            // 선택된 텍스트 외곽선
            texts[i].outlineWidth = selected ? 0.3f : 0f;
        }
    }

    //============= 옵션 값 변경 ==================
    void AdjustValue(int dir)
    {
        switch (index)
        {
            // Volume
            case 0:

                float volume = volumeSlider.value;

                volume += dir * 0.1f;
                volume = Mathf.Clamp01(volume);

                volumeSlider.value = volume;
                optionManager.SetVolume(volume);

                break;

            // Resolution
            case 1:

                int resolution = resolutionDropdown.value + dir;

                resolution = Mathf.Clamp(
                    resolution,
                    0,
                    resolutionDropdown.options.Count - 1);

                resolutionDropdown.value = resolution;
                optionManager.SetResolution(resolution);

                break;
        }
    }

    // 선택 실행
    void SelectOption()
    {
        switch (index)
        {
            // Reset
            case 2:

                optionManager.ResetSettings();

                break;

            // Back
            case 3:

                titleMenuController.CloseOption();

                break;
        }
    }

    // 옵션창이 열릴 때 선택 초기화
    public void ResetSelection()
    {
        index = 0;
        UpdateSelection();
    }
}