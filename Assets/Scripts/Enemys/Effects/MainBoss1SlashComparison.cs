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
    [Tooltip("베기의 생략된 중간 궤적을 손잡이 중심의 연속된 호로 표현합니다.")]
    [HideInInspector] public DisplayMode mode = DisplayMode.OriginalSlash;

    // Old serialized third-mode values fall back to the original sprite effect.
    void OnEnable() { mode = DisplayMode.OriginalSlash; }

    [Header("이전 방식 전용 — 원래 시트 비율 유지")]
    public MobEffects.Effect verticalSlash = new MobEffects.Effect { columns = 10, fps = 24, flipY = true };
    public MobEffects.Effect horizontalSlash = new MobEffects.Effect { columns = 10, fps = 24 };
    public float verticalAngle = 0;
    public float horizontalAngle = -90;

    [System.Serializable] public struct AttackPose
    {
        public float frame;
        public Vector2 grip,tip;
        public AttackPose(float f,Vector2 g,Vector2 t){frame=f;grip=g;tip=t;}
    }
    [Header("기본 Attack 궤도 기준 — 원본 모션의 6/8/18/19/20 프레임")]
    [Tooltip("세 Attack 클립에서 추출한 Animator 로컬 좌표. 생략된 중간 자세는 보간하고 복귀 중에는 마지막 베기 자세를 유지합니다.")]
    public AttackPose[] basicAttackPoses=new AttackPose[]
    {
        new AttackPose(6,new Vector2(-1.068094f,-0.294191f),new Vector2(-5.274414f,0.076919f)),
        new AttackPose(8,new Vector2(0.580894f,-0.892785f),new Vector2(4.757289f,-1.51723f)),
        new AttackPose(18,new Vector2(0.520128f,1.295151f),new Vector2(-2.640328f,-1.505952f)),
        new AttackPose(19,new Vector2(-0.889235f,-0.113389f),new Vector2(0.077174f,-6.373264f)),
        new AttackPose(20,new Vector2(-0.574367f,1.117492f),new Vector2(-7.465631f,-2.089128f)),
    };
    public bool TryBasicSweep(bool horizontal,float age,Transform space,out BossSlashSweepVisual.SweepPose pose)
    {
        pose=default;if(basicAttackPoses==null || basicAttackPoses.Length<5 || space==null)return false;
        float p=Mathf.Clamp(age,0,2),aspect=horizontal?.28f:1;
        int a=horizontal?0:p<1?2:3,b=horizontal?1:a+1;
        float t=horizontal?p/2:Mathf.Clamp01(p-(a==2?0:1));
        var from=basicAttackPoses[a];var to=basicAttackPoses[b];
        Vector2 av=from.tip-from.grip,bv=to.tip-to.grip;
        float ar=horizontal?av.magnitude:av.magnitude;
        float br=new Vector2(bv.x,bv.y/aspect).magnitude;
        float aa=horizontal?180:a==2?90:Mathf.Atan2(av.y,av.x)*Mathf.Rad2Deg;
        float ba=Mathf.Atan2(bv.y/aspect,bv.x)*Mathf.Rad2Deg;
        if(horizontal && ba<180)ba+=360;
        float end=Mathf.Lerp(aa,ba,t);
        float sx=Mathf.Max(.001f,Mathf.Abs(space.lossyScale.x)),sy=Mathf.Abs(space.lossyScale.y);
        pose=new BossSlashSweepVisual.SweepPose{
            center=space.TransformPoint(Vector2.Lerp(from.grip,to.grip,t)),
            radius=Mathf.Lerp(ar,br,t)*sx,aspect=aspect*sy/sx,
            start=horizontal?180:Mathf.Min(90,end+240),end=end};
        return true;
    }

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
        return Settings != null;
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
