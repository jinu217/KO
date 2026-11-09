using UnityEngine;

// MainBoss1's 3/4-view basic swings: one clip clock for preview and gameplay.
[DefaultExecutionOrder(1250)]
public sealed class MainBoss1AttackSlash : MonoBehaviour
{
    MobEffectInstance effect;
    SpriteRenderer sprite, blade;
    int hit;
    float facing;

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
            current.effect=instance; current.sprite=instance.GetComponent<SpriteRenderer>(); current.blade=weapon; current.hit=ordinal;
        }
        current.facing=direction;
        // f6/f18 are the windup boundary; the leading arc reaches its full shape
        // at f8/f20, then fades before f9/f21. Never stretch a sheet into recovery.
        float age = frame - (ordinal == 0 ? 6f : 18f);
        int cell = age <= 2f ? Mathf.FloorToInt(age*2.5f+.0001f) : 5+Mathf.FloorToInt((age-2f)*5f);
        current.effect.SetManualFrame(Mathf.Clamp(cell,0,9));
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
        sprite.flipX=facing<0;
        BossEffects.FitWeaponEffect(sprite,blade.transform,.9f);
        BossWeaponSlashAttachment.Apply(sprite,blade,0f);
    }
    public static Vector3 WorldTip(SpriteRenderer renderer) => BossWeaponSlashAttachment.WorldTip(renderer);
}
