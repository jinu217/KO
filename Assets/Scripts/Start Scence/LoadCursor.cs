using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadCursor : MonoBehaviour
{
    public GameObject[] loadSlots;
    private int index = 0;

    public FadeManager fadeManager;

    void OnEnable()
    {
        index = 0;
        UpdateSelection();
    }

    public void Init()
    {
        index = 0;
        UpdateSelection();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            index--;
            if (index < 0) index = loadSlots.Length - 1;

            UpdateSelection();
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            index++;
            if (index >= loadSlots.Length) index = 0;

            UpdateSelection();
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            LoadGame(index + 1);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            gameObject.SetActive(false);
        }
    }

    void UpdateSelection()
    {
        for (int i = 0; i < loadSlots.Length; i++)
        {
            Outline outline = loadSlots[i].GetComponent<Outline>();

            if (outline != null)
                outline.enabled = (i == index);
        }
    }

    void LoadGame(int slot)
    {
        int level = PlayerPrefs.GetInt($"SaveSlot{slot}_Level", 1);
        float hp = PlayerPrefs.GetFloat($"SaveSlot{slot}_HP", 100f);

        float x = PlayerPrefs.GetFloat($"SaveSlot{slot}_PosX", 0);
        float y = PlayerPrefs.GetFloat($"SaveSlot{slot}_PosY", 0);
        float z = PlayerPrefs.GetFloat($"SaveSlot{slot}_PosZ", 0);

        GameData.Instance.level = level;
        GameData.Instance.hp = hp;
        GameData.Instance.spawnPosition = new Vector3(x, y, z);

        StartCoroutine(LoadScene());
    }

    IEnumerator LoadScene()
    {
        yield return fadeManager.FadeOut();
        SceneManager.LoadScene("SampleScene");
    }
}