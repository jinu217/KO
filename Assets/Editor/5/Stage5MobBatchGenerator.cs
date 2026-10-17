#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
public static class Stage5MobBatchGenerator
{
    [MenuItem("Tools/5/Generate All Mob Animations")]
    public static void GenerateAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Action[] jobs = {
            BasicMob5_AnimGenerators.GenerateAll, SpeedMob5_AnimGenerators.GenerateAll,
            TankMob5_AnimGenerators.GenerateAll, EBasicMob5_AnimGenerators.GenerateAll,
            EArcher5_AnimGenerators.GenerateAll
        };
        int completed = 0;
        foreach (Action job in jobs)
        {
            try { job(); completed++; }
            catch (Exception e) { Debug.LogException(e); }
        }
        Debug.Log("Stage5 몹 생성 완료: " + completed + " / 5. 실패한 대상은 앞선 Console 오류를 확인하세요.");
    }
}
#endif
