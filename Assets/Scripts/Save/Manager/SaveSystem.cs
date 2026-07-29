using System.IO;
using UnityEngine;

// JSON 파일 저장/불러오기 담당
public static class SaveSystem
{
    // 저장 경로
    private static string GetPath(int slot)
    {
        return Path.Combine(Application.persistentDataPath, $"SaveSlot{slot}.json");
    }

    // 저장
    public static void Save(int slot, SaveFile saveFile)
    {
        string json = JsonUtility.ToJson(saveFile, true);
        File.WriteAllText(GetPath(slot), json);
    }

    // 불러오기
    public static SaveFile Load(int slot)
    {
        string path = GetPath(slot);

        if (!File.Exists(path))
            return null;

        string json = File.ReadAllText(path);

        return JsonUtility.FromJson<SaveFile>(json);
    }

    // 삭제
    public static void Delete(int slot)
    {
        string path = GetPath(slot);

        if (File.Exists(path))
            File.Delete(path);
    }

    // 저장 여부
    public static bool HasSave(int slot)
    {
        return File.Exists(GetPath(slot));
    }
}