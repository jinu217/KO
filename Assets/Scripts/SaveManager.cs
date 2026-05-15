using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static void SaveGame(int slot, int level, float hp, Vector3 pos)
    {
        PlayerPrefs.SetInt($"SaveSlot{slot}_Level", level);
        PlayerPrefs.SetFloat($"SaveSlot{slot}_HP", hp);

        PlayerPrefs.SetFloat($"SaveSlot{slot}_PosX", pos.x);
        PlayerPrefs.SetFloat($"SaveSlot{slot}_PosY", pos.y);
        PlayerPrefs.SetFloat($"SaveSlot{slot}_PosZ", pos.z);

        PlayerPrefs.Save();
    }

    public static void LoadGame(int slot)
    {
        GameData.Instance.level =
            PlayerPrefs.GetInt($"SaveSlot{slot}_Level", 1);

        GameData.Instance.hp =
            PlayerPrefs.GetFloat($"SaveSlot{slot}_HP", 100f);

        float x = PlayerPrefs.GetFloat($"SaveSlot{slot}_PosX", 0f);
        float y = PlayerPrefs.GetFloat($"SaveSlot{slot}_PosY", 0f);
        float z = PlayerPrefs.GetFloat($"SaveSlot{slot}_PosZ", 0f);

        GameData.Instance.spawnPosition = new Vector3(x, y, z);
    }
}