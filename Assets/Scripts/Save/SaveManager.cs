using UnityEngine;

// - 게임 저장
// - 게임 불러오기
// - 저장 데이터 삭제

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //============== 저장 데이터 존재 여부 확인 ==================
    public bool HasSave(int slot)
    {
        return PlayerPrefs.HasKey($"SaveSlot{slot}_PlayTime");
    }

    //============== 저장 ==================
    public void Save(int slot)
    {
        PlayerPrefs.SetFloat($"SaveSlot{slot}_PlayTime", SaveData.Instance.playTime);

        PlayerPrefs.SetFloat($"SaveSlot{slot}_HP", SaveData.Instance.hp);
        PlayerPrefs.SetFloat($"SaveSlot{slot}_SP", SaveData.Instance.sp);
        PlayerPrefs.SetFloat($"SaveSlot{slot}_BPM", SaveData.Instance.bpm);

        PlayerPrefs.SetString($"SaveSlot{slot}_Stage", SaveData.Instance.currentStage);

        PlayerPrefs.SetFloat($"SaveSlot{slot}_PosX", SaveData.Instance.spawnPosition.x);
        PlayerPrefs.SetFloat($"SaveSlot{slot}_PosY", SaveData.Instance.spawnPosition.y);
        PlayerPrefs.SetFloat($"SaveSlot{slot}_PosZ", SaveData.Instance.spawnPosition.z);

        PlayerPrefs.SetInt($"SaveSlot{slot}_Potion", SaveData.Instance.potionCount);

        PlayerPrefs.Save();

        Debug.Log($"슬롯 {slot} 저장 완료");
    }

    //============== 불러오기 ==================
    public void Load(int slot)
    {
        if (!HasSave(slot))
        {
            Debug.Log("저장 데이터가 없습니다.");
            return;
        }

        SaveData.Instance.playTime =
            PlayerPrefs.GetFloat($"SaveSlot{slot}_PlayTime");

        SaveData.Instance.hp =
            PlayerPrefs.GetFloat($"SaveSlot{slot}_HP");

        SaveData.Instance.sp =
            PlayerPrefs.GetFloat($"SaveSlot{slot}_SP");

        SaveData.Instance.bpm =
            PlayerPrefs.GetFloat($"SaveSlot{slot}_BPM");

        SaveData.Instance.currentStage =
            PlayerPrefs.GetString($"SaveSlot{slot}_Stage");

        float x = PlayerPrefs.GetFloat($"SaveSlot{slot}_PosX");
        float y = PlayerPrefs.GetFloat($"SaveSlot{slot}_PosY");
        float z = PlayerPrefs.GetFloat($"SaveSlot{slot}_PosZ");

        SaveData.Instance.spawnPosition = new Vector3(x, y, z);

        SaveData.Instance.potionCount =
            PlayerPrefs.GetInt($"SaveSlot{slot}_Potion");

        Debug.Log($"슬롯 {slot} 불러오기 완료");
    }

    //============== 저장 삭제 ==================
    public void Delete(int slot)
    {
        PlayerPrefs.DeleteKey($"SaveSlot{slot}_PlayTime");
        PlayerPrefs.DeleteKey($"SaveSlot{slot}_HP");
        PlayerPrefs.DeleteKey($"SaveSlot{slot}_SP");
        PlayerPrefs.DeleteKey($"SaveSlot{slot}_BPM");
        PlayerPrefs.DeleteKey($"SaveSlot{slot}_Stage");

        PlayerPrefs.DeleteKey($"SaveSlot{slot}_PosX");
        PlayerPrefs.DeleteKey($"SaveSlot{slot}_PosY");
        PlayerPrefs.DeleteKey($"SaveSlot{slot}_PosZ");

        PlayerPrefs.DeleteKey($"SaveSlot{slot}_Potion");

        PlayerPrefs.Save();

        Debug.Log($"슬롯 {slot} 삭제 완료");
    }
}