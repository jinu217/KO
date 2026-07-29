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
        return SaveSystem.HasSave(slot);
    }

    //============== 저장 ==================
    public void Save(int slot)
    {
        SaveData.Instance.currentSlot = slot;

        SaveFile file = CreateSaveFile();

        SaveSystem.Save(slot, file);

        Debug.Log($"슬롯 {slot} 저장 완료");
    }

    private SaveFile CreateSaveFile()
    {
        SaveFile file = new SaveFile();

        // Persistent
        file.persistent.playTime = SaveData.Instance.persistent.playTime;
        file.persistent.deathCount = SaveData.Instance.persistent.deathCount;
        file.persistent.nextStage = SaveData.Instance.persistent.nextStage;
        file.persistent.soul = SaveData.Instance.persistent.soul;

        // Run
        file.run.hp = SaveData.Instance.run.hp;
        file.run.sp = SaveData.Instance.run.sp;
        file.run.bpm = SaveData.Instance.run.bpm;

        return file;
    }

    //============== 불러오기 ==================
    public void Load(int slot)
    {
        SaveFile file = SaveSystem.Load(slot);

        if (file == null)
        {
            Debug.Log("저장 데이터가 없습니다.");
            return;
        }

        ApplySaveFile(file);

        SaveData.Instance.currentSlot = slot;
        SaveData.Instance.isLoaded = true;
    }

    private void ApplySaveFile(SaveFile file)
    {
        // Persistent
        SaveData.Instance.persistent.playTime = file.persistent.playTime;
        SaveData.Instance.persistent.deathCount = file.persistent.deathCount;
        SaveData.Instance.persistent.nextStage = file.persistent.nextStage;
        SaveData.Instance.persistent.soul = file.persistent.soul;

        // Run
        SaveData.Instance.run.hp = file.run.hp;
        SaveData.Instance.run.sp = file.run.sp;
        SaveData.Instance.run.bpm = file.run.bpm;
    }

    //============== 저장 삭제 ==================
    public void Delete(int slot)
    {
        SaveSystem.Delete(slot);

        Debug.Log($"슬롯 {slot} 삭제 완료");
    }
}