using UnityEngine;

// MainBoss1's 3/4-view basic swings: one clip clock for preview and gameplay.
[DefaultExecutionOrder(1250)]
public sealed class MainBoss1AttackSlash : MonoBehaviour
{
    MobEffectInstance effect;
    SpriteRenderer sprite, blade;
    int hit;
    float facing, authoredScale, swingAge;

    public static bool Applies(BossEffects source, AnimationClip clip)
    {
        if(source == null || !source.mainBoss || source.stage != 1 || clip == null) return false;
        if(MainBoss1SlashComparison.Settings == null || MainBoss1SlashComparison.Settings.mode == MainBoss1SlashComparison.DisplayMode.WeaponTrail) return false;
        return clip.name == "MainBoss1_Attack" || clip.name == "MainBoss1_Prepared_Attack" || clip.name == "MainBoss1_Prepared_Attack_Phase3";
    }
    public static void Clear(ref MainBoss1AttackSlash current)
    {
        if(current != null)
        {
            current.gameObject.SetActive(false);
            if(Application.isPlaying) Destroy(current.gameObject); else DestroyImmediate(current.gameObject);
        }
        current = null;
    }
    public static MobEffectInstance Sample(BossEffects source, AnimationClip clip, float time, float direction, ref MainBoss1AttackSlash current)
    {
        float frame = time * 12f;
        int ordinal = frame > 6f + .0001f && frame < 9f - .0001f ? 0 : frame > 18f + .0001f && frame < 21f - .0001f ? 1 : -1;
        if(!Applies(source,clip) || ordinal < 0 || source.weaponAnchor == null) { Clear(ref current); return null; }
        var weapon = source.weaponAnchor.GetComponent<SpriteRenderer>();
        if(weapon == null || weapon.sprite == null) { Clear(ref current); return null; }
        if(current != null && current.hit != ordinal) Clear(ref current);
        if(current == null)
        {
            var kind = ordinal == 0 ? BossEffects.Kind.Horizontal : BossEffects.Kind.Down;
            var sheet = ordinal == 0 ? source.spin : source.attack;
            if(MainBoss1SlashComparison.UseOriginal(source)) sheet = MainBoss1SlashComparison.Settings.SelectEffect(kind,sheet);
            else if(BossMotionFXSettings.Settings != null) sheet = BossMotionFXSettings.Settings.Effect(kind,sheet);
            var instance = MobEffectInstance.Spawn(sheet,weapon.transform.position,direction,source.pixelsPerUnit,
                weapon.sortingLayerID,weapon.sortingOrder-1,source.gameObject.scene);
            if(instance == null) return null;
            current = instance.gameObject.AddComponent<MainBoss1AttackSlash>();
            current.authoredScale=instance.transform.localScale.x;
            current.effect=instance; current.sprite=instance.GetComponent<SpriteRenderer>(); current.blade=weapon; current.hit=ordinal;
        }
        current.facing=direction;
        // f6/f18 are the windup boundary; the leading arc reaches its full shape
        // at f8/f20, then fades before f9/f21. Never stretch a sheet into recovery.
        float age = frame - (ordinal == 0 ? 6f : 18f);
        int cell = age <= 2f ? Mathf.FloorToInt(age*2.5f+.0001f) : 5+Mathf.FloorToInt((age-2f)*5f);
        current.swingAge=age;
        current.effect.SetManualFrame(ordinal==1 ? 4 : Mathf.Clamp(cell,0,9));
        current.Align();
        return current.effect;
    }
    public static void UpdateRuntime(BossEffects source, ref MainBoss1AttackSlash current)
    {
        if(!source.mainBoss || source.stage != 1) { Clear(ref current); return; }
        var animator=source.motionAnimator;
        if(!Application.isPlaying || animator==null || !animator.isActiveAndEnabled) { Clear(ref current); return; }
        var clips=animator.GetCurrentAnimatorClipInfo(0);
        AnimationClip clip=null; float weight=0;
        foreach(var item in clips) if(item.weight>weight) { weight=item.weight;clip=item.clip; }
        if(!Applies(source,clip)) { Clear(ref current); return; }
        float time=animator.GetCurrentAnimatorStateInfo(0).normalizedTime*clip.length;
        Sample(source,clip,time,Mathf.Sign(animator.transform.lossyScale.x),ref current);
    }
    void LateUpdate() { Align(); }
    public void Align()
    {
        if(sprite==null || sprite.sprite==null || blade==null || blade.sprite==null) return;
        if(hit==1) { AlignDownwardSweep();return; }
        sprite.flipX=facing<0;
        sprite.transform.localScale=Vector3.one*authoredScale;
        BossEffects.FitWeaponEffect(sprite,blade.transform,.9f);
        BossWeaponSlashAttachment.Apply(sprite,blade,0f);
    }
    // The full crescent surrounds the implied swing around the grip. Its lower
    // leading point joins the blade tip; the arc is not centered on that tip.
    void AlignDownwardSweep()
    {
        sprite.flipX=false;sprite.flipY=false;
        sprite.sortingLayerID=blade.sortingLayerID;sprite.sortingOrder=blade.sortingOrder-1;
        var s=sprite.sprite;
        Vector2 center=new Vector2(39.25f,33.5f), leading=new Vector2(33f,4.5f);
        Vector3 grip=blade.transform.position, tip=BossWeaponSlashAttachment.WorldTip(blade);
        float radius=Vector3.Distance(grip,tip);
        float sx=radius/(54.5f-center.x),sy=radius/(center.y-leading.y);
        Vector2 localLead=Vector2.Scale(leading-center,new Vector2(sx,sy));
        float normalizer=radius/localLead.magnitude;sx*=normalizer;sy*=normalizer;
        float sign=facing<0?-1:1;sx*=sign;
        localLead=Vector2.Scale(leading-center,new Vector2(sx,sy));
        Vector3 axis=tip-grip;
        float rotation=Mathf.Atan2(axis.y,axis.x)*Mathf.Rad2Deg-Mathf.Atan2(localLead.y,localLead.x)*Mathf.Rad2Deg;
        sprite.transform.localScale=new Vector3(sx*s.pixelsPerUnit,sy*s.pixelsPerUnit,1);
        sprite.transform.rotation=Quaternion.Euler(0,0,rotation);
        sprite.transform.position=grip-sprite.transform.TransformVector((Vector3)((center-s.pivot)/s.pixelsPerUnit));
        Color color=Color.white;color.a=Mathf.SmoothStep(0,1,Mathf.Clamp01(swingAge/.8f))*Mathf.Clamp01(3-swingAge);sprite.color=color;
    }
    public static Vector3 WorldTip(SpriteRenderer renderer)
    {
        var slash=renderer.GetComponent<MainBoss1AttackSlash>();
        if(slash!=null && slash.hit==1)
            return renderer.transform.TransformPoint((Vector3)((new Vector2(33f,4.5f)-renderer.sprite.pivot)/renderer.sprite.pixelsPerUnit));
        return BossWeaponSlashAttachment.WorldTip(renderer);
    }
}
