using UnityEngine;

// Third comparison mode: authored motion cues, one full-proportion sheet per action.
public sealed class BossMotionFXSettings : ScriptableObject
{
    public MobEffects.Effect down = new MobEffects.Effect { flipY = true };
    public MobEffects.Effect up = new MobEffects.Effect();
    public MobEffects.Effect horizontal = new MobEffects.Effect();
    public MobEffects.Effect spin = new MobEffects.Effect();
    public MobEffects.Effect thrust = new MobEffects.Effect();
    static BossMotionFXSettings instance;
    public static BossMotionFXSettings Settings
    {
        get { if(instance == null) instance = Resources.Load<BossMotionFXSettings>("BossMotionFXSettings"); return instance; }
    }
    public static bool Enabled => MainBoss1SlashComparison.Settings != null
        && MainBoss1SlashComparison.Settings.mode == MainBoss1SlashComparison.DisplayMode.MotionMatched;
    public static bool Applies(BossEffects source) => Enabled && source != null && source.mainBoss && source.stage >= 1 && source.stage <= 5;
    public static bool Applies(BossEffects source, AnimationClip clip)
        => Applies(source) || (Enabled && source == null && clip != null && clip.name.StartsWith("MainBoss"));

    public MobEffects.Effect Effect(BossEffects.Kind kind, MobEffects.Effect fallback)
    {
        var selected = kind == BossEffects.Kind.Down ? down : kind == BossEffects.Kind.Up ? up
            : kind == BossEffects.Kind.Horizontal ? horizontal : kind == BossEffects.Kind.Spin ? spin
            : kind == BossEffects.Kind.Thrust ? thrust : fallback;
        return selected != null && (selected.sheet != null || selected.frames != null && selected.frames.Length > 0) ? selected : fallback;
    }
    public static bool SpinWindow(AnimationClip clip, out Vector2 window)
    {
        window = default;
        if(clip == null) return false;
        switch(clip.name.ToLowerInvariant())
        {
            case "mainboss3_attack": window = new Vector2(.15f, .70f); return true;
            case "mainboss3_skill1": window = new Vector2(.80f, 2.36f); return true;
            case "mainboss3_skill3": window = new Vector2(1.12f, 1.78f); return true;
            case "mainboss5_attack": window = new Vector2(8f/12, 20f/12); return true;
            case "mainboss5_skill3_follow": window = new Vector2(0, 11f/12); return true;
        }
        return false;
    }
    public static BossEffects.Kind Resolve(BossEffects source, BossAttackStepData step, int skill, int ordinal, BossEffects.Kind fallback)
    {
        if(step.actionType == BossActionType.SpawnPrefab) return BossEffects.Kind.None;
        if(step.actionType == BossActionType.Slam && source.stage != 4) return BossEffects.Kind.Down;
        if(source.stage == 1)
        {
            if(step.actionType == BossActionType.DashAttack) return BossEffects.Kind.Horizontal;
            // MainBoss1's actual base clip is horizontal then vertical, unlike the written plan.
            if(skill < 0) return ordinal >= 2 ? BossEffects.Kind.None : ordinal == 0 ? BossEffects.Kind.Horizontal : BossEffects.Kind.Down;
            if(skill == 0) return ordinal >= 3 ? BossEffects.Kind.None : ordinal == 2 ? BossEffects.Kind.Horizontal : BossEffects.Kind.Down;
        }
        if(source.stage == 2)
        {
            if(step.actionType == BossActionType.DashAttack) return BossEffects.Kind.Punch; // shoulder/body charge
            if(skill < 0 || skill == 1 && ordinal == 0) return BossEffects.Kind.Up;
            return BossEffects.Kind.Down;
        }
        if(source.stage == 3)
        {
            if(step.actionType == BossActionType.DashAttack || skill == 1) return BossEffects.Kind.Thrust;
            if(step.actionType == BossActionType.PersistentHitbox) return BossEffects.Kind.Spin;
            if(skill < 0 && ordinal == 0) return BossEffects.Kind.Spin;
            return BossEffects.Kind.Down;
        }
        return fallback; // boxing keeps its authored fist/uppercut mapping
    }
}
