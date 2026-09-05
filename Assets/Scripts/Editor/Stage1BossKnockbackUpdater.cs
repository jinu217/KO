#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class Stage1BossKnockbackUpdater
{
    private const string MidPath = "Assets/Data/Enemys/Stage1/Stage1_MidBoss.asset";
    private const string MainPath = "Assets/Data/Enemys/Stage1/Stage1_MainBoss.asset";

    [MenuItem("Tools/Boss/Apply Stage1 Knockback Settings")]
    public static void Apply()
    {
        MidBossData mid = AssetDatabase.LoadAssetAtPath<MidBossData>(MidPath);
        if (mid != null)
        {
            SetKnockback(mid.basicAttack, 0, new Vector2(2f, 0.8f));
            SetKnockback(mid.basicAttack, 1, new Vector2(2.5f, 1f));
            SetKnockback(mid.skills[1], 0, new Vector2(2f, 0.8f));
            SetKnockback(mid.skills[1], 1, new Vector2(2.5f, 1f));
            SetKnockback(mid.skills[2], 0, new Vector2(13f, 4f));
            EditorUtility.SetDirty(mid);
        }

        MainBossData main = AssetDatabase.LoadAssetAtPath<MainBossData>(MainPath);
        if (main != null)
        {
            SetKnockback(main.basicAttack, 0, new Vector2(1.5f, 0.6f));
            SetKnockback(main.basicAttack, 1, new Vector2(1.5f, 0.6f));
            SetKnockback(main.skills[3], 0, new Vector2(14f, 4f));
            EditorUtility.SetDirty(main);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Stage1 보스 넉백 설정 적용 완료");
    }

    private static void SetKnockback(BossSkillData skill, int hitboxStepIndex, Vector2 force)
    {
        if (skill?.active?.attackSteps == null) return;
        int currentHitboxIndex = 0;
        foreach (BossAttackStepData step in skill.active.attackSteps)
        {
            if (step == null || (step.actionType != BossActionType.MeleeAttack
                && step.actionType != BossActionType.DashAttack && step.actionType != BossActionType.Slam))
                continue;
            if (currentHitboxIndex++ == hitboxStepIndex)
            {
                step.knockbackForce = force;
                return;
            }
        }
    }
}
#endif
