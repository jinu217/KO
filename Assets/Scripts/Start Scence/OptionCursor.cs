using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class OptionCursor : MonoBehaviour
{
    public TMP_Text[] texts;

    // 🔥 도형 커서
    public RectTransform cursor;

    public Slider volumeSlider;
    public TMP_Dropdown resolutionDropdown;
    public OptionManager optionManager;
    public TitleMenuController TitleMenuController;
    public GameObject optionPanel;

    private int index = 0;

    void Start()
    {
        UpdateSelection();
    }

    void Update()
    {
        // 🔥 옵션창 꺼져있으면 입력 무시
        if (!optionPanel.activeSelf) return;

        // 🔼 위 이동
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            index--;

            if (index < 0)
                index = texts.Length - 1;

            UpdateSelection();
        }

        // 🔽 아래 이동
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            index++;

            if (index >= texts.Length)
                index = 0;

            UpdateSelection();
        }

        // ◀ 값 감소
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            AdjustValue(-1);
        }

        // ▶ 값 증가
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            AdjustValue(1);
        }

        // ⏎ 선택
        if (Input.GetKeyDown(KeyCode.Return))
        {
            SelectOption();
        }

        // ESC 뒤로가기
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TitleMenuController.CloseOption();
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

            // 🔥 선택된 텍스트 강조
            texts[i].color = selected ? Color.yellow : Color.white;

            if (texts[i].fontMaterial != null)
            {
                texts[i].outlineWidth = selected ? 0.3f : 0f;
            }
        }

        // 🔥 도형 커서 이동
        if (cursor != null)
        {
            cursor.position =
                texts[index].rectTransform.position + new Vector3(-80, 0, 0);
        }
    }

    void AdjustValue(int dir)
    {
        switch (index)
        {
            case 0: // 🔊 Volume
                float v = volumeSlider.value;

                v += dir * 0.1f;
                v = Mathf.Clamp01(v);

                volumeSlider.value = v;
                optionManager.SetVolume(v);
                break;

            case 1: // 🖥 Resolution
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

                if (TitleMenuController != null)
                    TitleMenuController.CloseOption();

                break;
        }
    }

    public void ResetSelection()
    {
        index = 0; // 🔥 항상 Volume부터 시작
        UpdateSelection();
    }
}