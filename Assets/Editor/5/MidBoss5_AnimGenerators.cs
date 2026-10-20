#if UNITY_EDITOR
using UnityEditor;
public static class MidBoss5_AnimGenerators
{
    [MenuItem("Tools/5/MidBoss 5/Generate Animations")]
    public static void GenerateAll() => Stage5BossAnimationBuilder.Generate(false);
    [MenuItem("Tools/5/MidBoss 5/Setup Animator")]
    public static void SetupAnimator() => Stage5BossAnimationBuilder.Generate(false, null, true);
    [MenuItem("Tools/5/MidBoss 5/Apply Existing Pattern Data")]
    public static void ApplyPatternData() => Stage5BossAnimationBuilder.ApplyExistingData(false);
    [MenuItem("Tools/5/MidBoss 5/Clips/Idle")]
    public static void GenerateIdle() => Stage5BossAnimationBuilder.Generate(false, "Idle");
    [MenuItem("Tools/5/MidBoss 5/Clips/Walk")]
    public static void GenerateWalk() => Stage5BossAnimationBuilder.Generate(false, "Walk");
    [MenuItem("Tools/5/MidBoss 5/Clips/Run")]
    public static void GenerateRun() => Stage5BossAnimationBuilder.Generate(false, "Run");
    [MenuItem("Tools/5/MidBoss 5/Clips/Attack")]
    public static void GenerateAttack() => Stage5BossAnimationBuilder.Generate(false, "Attack");
    [MenuItem("Tools/5/MidBoss 5/Clips/Skill1")]
    public static void GenerateSkill1() => Stage5BossAnimationBuilder.Generate(false, "skill1");
    [MenuItem("Tools/5/MidBoss 5/Clips/Skill2")]
    public static void GenerateSkill2() => Stage5BossAnimationBuilder.Generate(false, "skill2");
    [MenuItem("Tools/5/MidBoss 5/Clips/Skill3")]
    public static void GenerateSkill3() => Stage5BossAnimationBuilder.Generate(false, "skill3");
    [MenuItem("Tools/5/MidBoss 5/Clips/Death")]
    public static void GenerateDeath() => Stage5BossAnimationBuilder.Generate(false, "Death");
}
#endif
