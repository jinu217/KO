#if UNITY_EDITOR
using UnityEditor;
public static class MainBoss5_AnimGenerators
{
    [MenuItem("Tools/5/MainBoss 5/Generate Animations")]
    public static void GenerateAll() => Stage5BossAnimationBuilder.Generate(true);
    [MenuItem("Tools/5/MainBoss 5/Setup Animator")]
    public static void SetupAnimator() => Stage5BossAnimationBuilder.Generate(true, null, true);
    [MenuItem("Tools/5/MainBoss 5/Apply Existing Pattern Data")]
    public static void ApplyPatternData() => Stage5BossAnimationBuilder.ApplyExistingData(true);
    [MenuItem("Tools/5/MainBoss 5/Clips/Idle")]
    public static void GenerateIdle() => Stage5BossAnimationBuilder.Generate(true, "Idle");
    [MenuItem("Tools/5/MainBoss 5/Clips/Walk")]
    public static void GenerateWalk() => Stage5BossAnimationBuilder.Generate(true, "Walk");
    [MenuItem("Tools/5/MainBoss 5/Clips/Run")]
    public static void GenerateRun() => Stage5BossAnimationBuilder.Generate(true, "Run");
    [MenuItem("Tools/5/MainBoss 5/Clips/Attack")]
    public static void GenerateAttack() => Stage5BossAnimationBuilder.Generate(true, "Attack");
    [MenuItem("Tools/5/MainBoss 5/Clips/Skill1")]
    public static void GenerateSkill1() => Stage5BossAnimationBuilder.Generate(true, "skill1");
    [MenuItem("Tools/5/MainBoss 5/Clips/Skill2")]
    public static void GenerateSkill2() => Stage5BossAnimationBuilder.Generate(true, "skill2");
    [MenuItem("Tools/5/MainBoss 5/Clips/Skill3")]
    public static void GenerateSkill3() => Stage5BossAnimationBuilder.Generate(true, "skill3");
    [MenuItem("Tools/5/MainBoss 5/Clips/Skill4")]
    public static void GenerateSkill4() => Stage5BossAnimationBuilder.Generate(true, "skill4");
    [MenuItem("Tools/5/MainBoss 5/Clips/Skill5")]
    public static void GenerateSkill5() => Stage5BossAnimationBuilder.Generate(true, "skill5");
    [MenuItem("Tools/5/MainBoss 5/Clips/Dskill")]
    public static void GenerateDskill() => Stage5BossAnimationBuilder.Generate(true, "Dskill");
    [MenuItem("Tools/5/MainBoss 5/Clips/DesireSuccess")]
    public static void GenerateDesireSuccess() => Stage5BossAnimationBuilder.Generate(true, "DesireSuccess");
    [MenuItem("Tools/5/MainBoss 5/Clips/Groggy")]
    public static void GenerateGroggy() => Stage5BossAnimationBuilder.Generate(true, "Groggy");
    [MenuItem("Tools/5/MainBoss 5/Clips/Death")]
    public static void GenerateDeath() => Stage5BossAnimationBuilder.Generate(true, "Death");
}
#endif
