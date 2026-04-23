using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class MenuCursor : MonoBehaviour
{
    public GameObject[] cursors;
    public TMP_Text[] texts;
    public FadeManager fadeManager;
    public GameObject mainMenu;
    public GameObject optionPanel;
    public OptionCursor optionCursor;

    private int index = 0;
    private bool isSelecting = false;

    void Start()
    {
        UpdateSelection();
        optionPanel.SetActive(false);
        mainMenu.SetActive(true);
    }

    void Update()
    {
        // 옵션이 열려 있으면 메인메뉴 입력 막기
        if (optionPanel.activeSelf) return;

        if (isSelecting) return;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            index--;
            if (index < 0) index = cursors.Length - 1;
            UpdateSelection();
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            index++;
            if (index >= cursors.Length) index = 0;
            UpdateSelection();
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            StartCoroutine(SelectMenu());
        }
    }

    void UpdateSelection()
    {
        for (int i = 0; i < cursors.Length; i++)
        {
            bool isSelected = (i == index);
            cursors[i].SetActive(isSelected);
            texts[i].color = isSelected ? Color.yellow : Color.white;
            texts[i].transform.localScale = isSelected ? Vector3.one * 1.2f : Vector3.one;
        }
    }

    IEnumerator SelectMenu()
    {
        isSelecting = true;
        switch (index)
        {
            case 0: // START
                yield return StartCoroutine(fadeManager.FadeOut());
                SceneManager.LoadScene("SampleScene");
                break;

            case 1: // OPTION
                OpenOption();
                break;

            case 2: // QUIT
                Application.Quit();
                break;
        }
    }

    void OpenOption()
    {
        optionPanel.SetActive(true);
        if (optionCursor != null)
            optionCursor.ResetSelection();
        isSelecting = false;
    }

    // 🔥 옵션에서 돌아오기용
    public void CloseOption()
    {
        optionPanel.SetActive(false);
        isSelecting = false;
        UpdateSelection();
    }
}