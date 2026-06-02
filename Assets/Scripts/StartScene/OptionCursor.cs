using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class OptionCursor : MonoBehaviour
{
    public TMP_Text[] texts;
    public Slider volumeSlider;
    public TMP_Dropdown resolutionDropdown;
    public OptionManager optionManager;
    public MenuCursor menuCursor;
    public GameObject optionPanel;

    private int index = 0;

    void Start()
    {
        UpdateSelection();
    }

    void Update()
    {
        // 🔥 옵션이 꺼져 있으면 입력 무시
        if (!optionPanel.activeSelf) return;
        // 🔼🔽 이동
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            index--;
            if (index < 0) index = texts.Length - 1;
            UpdateSelection();
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            index++;
            if (index >= texts.Length) index = 0;
            UpdateSelection();
        }

        // 🔥 값 조절 (슬라이더 / 드롭다운만)
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            AdjustValue(-1);
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            AdjustValue(1);
        }

        // 🔥 실행 (텍스트만 의미 있음)
        if (Input.GetKeyDown(KeyCode.Return))
        {
            SelectOption();
        }

        // 🔥 ESC 뒤로가기
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            menuCursor.CloseOption();
        }
    }

    void UpdateSelection()
    {
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null)
            {
                Debug.LogError("texts[" + i + "] 비어있음!");
                continue;
            }

            bool selected = (i == index);

            texts[i].color = selected ? Color.yellow : Color.white;
            texts[i].transform.localScale = selected ? Vector3.one * 1.1f : Vector3.one;
            
            if (texts[i] != null && texts[i].fontMaterial != null)
            {
                texts[i].outlineWidth = selected ? 0.3f : 0f;
            }
        }
    }

    void AdjustValue(int dir)
    {
        switch (index)
        {
            case 0: // 🔊 Volume (Slider)
                float v = volumeSlider.value;
                v += dir * 0.1f;
                v = Mathf.Clamp01(v);
                volumeSlider.value = v;
                optionManager.SetVolume(v);
                break;

            case 1: // 🖥 Resolution (Dropdown)
                int r = resolutionDropdown.value + dir;
                r = Mathf.Clamp(r, 0, resolutionDropdown.options.Count - 1);
                resolutionDropdown.value = r;
                optionManager.SetResolution(r);
                break;
        }
    }
    void SelectOption()
    {
        switch (index)
        {
            case 2: // ♻ Reset
                optionManager.ResetSettings();
                break;

            case 3: // 🔙 Back
                if (optionPanel != null)
                    optionPanel.SetActive(false);
                if (menuCursor != null)
                    menuCursor.CloseOption();
                break;
        }
    }

    public void ResetSelection()
    {
        index = 0;          // 🔥 항상 Volume부터 시작
        UpdateSelection();
    }
}