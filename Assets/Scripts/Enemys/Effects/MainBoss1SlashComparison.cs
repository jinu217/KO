using UnityEngine;

// Shared by scene preview and gameplay; only MainBoss1 reads this setting.
public sealed class MainBoss1SlashComparison : ScriptableObject
{
    public enum DisplayMode
    {
        [InspectorName("무기 궤적 잔상")] WeaponTrail = 0,
        [InspectorName("원본 Slash + 무기 추종")] OriginalSlash = 1
    }

    [Header("메인보스1 베기 이펙트 비교")]
    [Tooltip("원본 Slash의 비율과 타격 타이밍을 살리고, 무기 뒤를 따라가도록 조율합니다. 무기 궤적은 옅은 잔상 한 장을 더합니다.")]
    public DisplayMode mode = DisplayMode.WeaponTrail;

    // Old serialized third-mode values fall back to the original sprite effect.
    void OnEnable() { if((int)mode > 1) mode = DisplayMode.OriginalSlash; }

    [Header("이전 방식 전용 — 원래 시트 비율 유지")]
    public MobEffects.Effect verticalSlash = new MobEffects.Effect { columns = 10, fps = 24, flipY = true };
    public MobEffects.Effect horizontalSlash = new MobEffects.Effect { columns = 10, fps = 24 };
    public float verticalAngle = 0;
    public float horizontalAngle = -90;

    static MainBoss1SlashComparison settings;
    public static MainBoss1SlashComparison Settings
    {
        get
        {
            if (settings == null) settings = Resources.Load<MainBoss1SlashComparison>("MainBoss1SlashComparison");
            return settings;
        }
    }
    public static bool UseOriginal(BossEffects source)
    {
        if (source == null || !source.mainBoss || source.stage != 1) return false;
        return Settings != null && Settings.mode == DisplayMode.OriginalSlash;
    }

    // Names also cover generated desire/empowered combos and their inserted tracking steps.
    public BossEffects.Kind Resolve(BossAttackStepData step, int skillIndex, int ordinal, int totalHits, out float angle)
    {
        angle = verticalAngle;
        if (step.actionType == BossActionType.SpawnPrefab) return BossEffects.Kind.None;
        bool horizontal = step.actionType == BossActionType.DashAttack || (step.stepName ?? "").Contains("가로");
        if (!horizontal && !(step.stepName ?? "").Contains("세로"))
            horizontal = skillIndex < 0 || (skillIndex == 0 && totalHits == 4)
                ? ordinal % 2 == 1 : skillIndex == 0 && (ordinal == 2 || ordinal == 4);
        // The authored basic attack is horizontal then downward, regardless of legacy step labels.
        if (skillIndex < 0 && ordinal < 2) horizontal = ordinal == 0;
        angle = horizontal ? horizontalAngle : verticalAngle;
        return horizontal ? BossEffects.Kind.Horizontal : BossEffects.Kind.Down;
    }

    public MobEffects.Effect SelectEffect(BossEffects.Kind kind, MobEffects.Effect fallback)
    {
        var effect = kind == BossEffects.Kind.Horizontal ? horizontalSlash : verticalSlash;
        return effect != null && (effect.sheet != null || effect.frames != null && effect.frames.Length > 0) ? effect : fallback;
    }
}
