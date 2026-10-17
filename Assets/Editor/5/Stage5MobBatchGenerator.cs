#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
public static class Stage5MobBatchGenerator
{
    [MenuItem("Tools/5/Generate Melee Mob Run Animations")]
    public static void GenerateMeleeRuns()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || AnimationMode.InAnimationMode())
        {
            Debug.LogWarning("Play와 Animation 미리보기를 끄고 실행해주세요.");
            return;
        }
        Action[] jobs = {
            BasicMob5_AnimGenerators.GenerateRun, SpeedMob5_AnimGenerators.GenerateRun,
            TankMob5_AnimGenerators.GenerateRun, EBasicMob5_AnimGenerators.GenerateRun
        };
        int completed = 0;
        foreach (Action job in jobs)
        {
            try { job(); completed++; }
            catch (Exception e) { Debug.LogException(e); }
        }
        Debug.Log("Stage5 근접몹 Run R4 생성: " + completed + " / 4. HB 75도, Tank Run 0.6초.");
    }

    [MenuItem("Tools/5/Generate All Mob Death Animations")]
    public static void GenerateAllDeaths()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || AnimationMode.InAnimationMode())
        {
            Debug.LogWarning("Play와 Animation 미리보기를 끄고 실행해주세요.");
            return;
        }
        Action[] jobs = {
            BasicMob5_AnimGenerators.GenerateDeath, SpeedMob5_AnimGenerators.GenerateDeath,
            TankMob5_AnimGenerators.GenerateDeath, EBasicMob5_AnimGenerators.GenerateDeath,
            EArcher5_AnimGenerators.GenerateDeath
        };
        int completed = 0;
        foreach (Action job in jobs)
        {
            try { job(); completed++; }
            catch (Exception e) { Debug.LogException(e); }
        }
        Debug.Log("Stage5 일반몹 Death R3 생성: " + completed + " / 5 (Stage4 모션 기준)");
    }

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
