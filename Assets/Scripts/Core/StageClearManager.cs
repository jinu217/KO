using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Stage 씬에 배치된 일반 몬스터와 보스의 처치 상태를 관리합니다.</summary>
public class StageClearManager : MonoBehaviour
{
    private static StageClearManager instance;
    private readonly HashSet<EnemyController> enemies = new HashSet<EnemyController>();
    private readonly HashSet<MidBossController> bosses = new HashSet<MidBossController>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneCallbacks()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeActiveScene()
    {
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsStageScene(scene))
            return;

        GetOrCreate(scene).RegisterActiveMonsters();
    }

    public static bool CanLeave(Scene scene)
    {
        if (!IsStageScene(scene))
            return true;

        StageClearManager manager = GetOrCreate(scene);
        manager.RegisterActiveMonsters(); // 플레이 중 생성된 몬스터도 포함
        foreach (EnemyController enemy in manager.enemies)
            if (enemy != null && !enemy.IsDead)
                return false;
        foreach (MidBossController boss in manager.bosses)
            if (boss != null && !boss.IsDead)
                return false;
        return true;
    }

    private static bool IsStageScene(Scene scene)
    {
        string name = scene.name;
        return scene.IsValid() && name.StartsWith("Stage", StringComparison.OrdinalIgnoreCase) &&
            name.Length > 5 && char.IsDigit(name[5]);
    }

    private static StageClearManager GetOrCreate(Scene scene)
    {
        if (instance != null && instance.gameObject.scene == scene)
            return instance;

        foreach (StageClearManager manager in FindObjectsByType<StageClearManager>(FindObjectsSortMode.None))
            if (manager.gameObject.scene == scene)
                return instance = manager;

        GameObject managerObject = new GameObject(nameof(StageClearManager));
        SceneManager.MoveGameObjectToScene(managerObject, scene);
        return instance = managerObject.AddComponent<StageClearManager>();
    }

    private void RegisterActiveMonsters()
    {
        Scene scene = gameObject.scene;
        foreach (EnemyController enemy in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
            if (enemy.gameObject.scene == scene)
                enemies.Add(enemy);
        foreach (MidBossController boss in FindObjectsByType<MidBossController>(FindObjectsSortMode.None))
            if (boss.gameObject.scene == scene)
                bosses.Add(boss);
    }
}
