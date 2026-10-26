#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using S = BossClipEffects.Style;
using A = BossClipEffects.Anchor;

// Authored stroke windows, in clip seconds. Recovery and wind-up are deliberately excluded.
public static class BossMotionEffects
{
    static BossClipEffects.Cue Stroke(List<BossClipEffects.Cue> cues, S style, float start, float end)
    {
        var c = new BossClipEffects.Cue { style=style, time=start, duration=end-start,
            anchor=A.Weapon, fitDuration=true, loop=style==S.Spin, followRotation=style!=S.Spin, alignBlade=style==S.Thrust };
        cues.Add(c); return c;
    }
    public static void Apply(BossClipEffects driver, BossEffects source, int stage, bool main)
    {
        var samplingObject = UnityEngine.Object.Instantiate(driver.gameObject);
        samplingObject.hideFlags = HideFlags.HideAndDontSave;
        var sampler = samplingObject.GetComponent<BossClipEffects>();
        try { foreach (var track in driver.tracks)
        {
            string n=track.clip.name.ToLowerInvariant();
            var cues=track.cues.ToList();
            var cuts=cues.Where(c=>c.style<=S.Punch).ToArray();
            void Clear() { cues.RemoveAll(c=>c.style<=S.Punch); }
            void Cut(S s,float a,float b) { Stroke(cues,s,a,b); }
            void F(S s,float a,float b) { Cut(s,a/12f,b/12f); }
            if(stage==4 && main && (n.Contains("attack") || n.Contains("skill")))
            { Clear(); Boxer(track,cues,source); }
            else if (stage==1 && main && n.Contains("attack"))
            { Clear(); F(S.Thrust,7,11); F(S.Down,18,20.5f); track.damageTimes=new[]{9f/12,19f/12}; }
            else if (stage==1 && main && n.Contains("skill1"))
            { Clear(); F(S.Down,2,3.5f); F(S.Down,11,12.5f); F(S.Thrust,23,25.5f); track.damageTimes=new[]{3f/12,12f/12,24f/12}; }
            else if (stage==1 && main && n.Contains("skill5"))
            { Clear(); cues.Add(new BossClipEffects.Cue{style=S.Buff,anchor=A.Body,time=13f/12,duration=5f/12,fitDuration=true}); }
            else if(stage==1 && main && n.Contains("skill4"))
            { cues.RemoveAll(c=>c.style==S.Dash); cues.Add(new BossClipEffects.Cue{style=S.Dash,anchor=A.Feet,time=12f/12,duration=7f/12,loop=true}); }
            else if (stage==1 && !main && n.Contains("combo"))
            {
                Clear(); var hits=MidBoss1_AnimGenerators.GetComboImpactTimes(track.clip);
                // Vertical source .6s is the landed pose, not the start of the downswing.
                Cut(S.Down,hits[0]-.15f,hits[0]+.025f);
                if(hits.Length==3) Cut(S.Down,hits[1]-.13f,hits[1]+.025f);
                Cut(S.Thrust,hits.Last()-.10f,hits.Last()+.07f);
                track.damageTimes=hits;
            }
            else if(stage==1 && !main && n.Contains("세로공격")) { Clear(); Cut(S.Down,.42f,.63f); }
            else if(stage==1 && !main && n.Contains("양손찍기")) { Clear(); Cut(S.Down,.54f,.70f); }
            else if(stage==1 && !main && n.Contains("horizontalattack")) { Clear(); Cut(S.Thrust,.55f,.72f); }
            else if (stage==2 && !main && n.Contains("attack"))
            { Clear(); F(S.Thrust,4,7); F(S.Up,10,13.5f); track.damageTimes=new[]{6f/12,12f/12}; }
            else if (stage==2 && !main && n.Contains("skill2"))
            {
                Clear(); // Read the actual extension peaks, including the shorter alternate clip.
                var binding=AnimationUtility.GetCurveBindings(track.clip).FirstOrDefault(b=>b.path.EndsWith("Shoulder_B") && b.propertyName.Contains("Euler") && b.propertyName.EndsWith(".z"));
                var curve=AnimationUtility.GetEditorCurve(track.clip,binding);
                if(curve!=null) { var k=curve.keys; for(int i=1;i<k.Length;i++) if(k[i].value>30 && k[i-1].value<0) Cut(S.Thrust,k[i-1].time,k[i].time+.07f); }
                track.damageTimes=cues.Where(c=>c.style==S.Thrust).Select(c=>c.time+c.duration-.07f).ToArray();
            }
            else if (stage==2 && !main && n.Contains("skill3"))
            { Clear(); F(S.Down,13,14.5f); F(S.Thrust,18,20); }
            else if (stage==2 && main && n.Contains("attack"))
            { Clear(); bool alt=n.Contains("2"); F(S.Up,alt?13:19,alt?18:24); track.damageTimes=new[]{(alt?15f:21f)/12}; }
            else if (stage==2 && main && n.Contains("skill1"))
            { Clear(); F(S.Down,6,9.5f); F(S.Down,20,23.5f); }
            else if(stage==2 && main && n.Contains("skill2"))
            { Clear(); F(S.Down,4,6); F(S.Up,13,17.5f); F(S.Down,21,24.5f); track.damageTimes=new[]{5f/12,17f/12,24f/12}; }
            else if(stage==2 && main && n.Contains("skill3"))
            { Clear(); F(S.Down,17,19.5f); }
            else if (stage==3 && !main && n.Contains("attack"))
            { Clear(); Cut(S.Down,.52f,.73f); Cut(S.Up,1.30f,1.48f); track.damageTimes=new[]{.70f,1.42f}; }
            else if (stage==3 && !main && n.Contains("skill3"))
            { Clear(); Cut(S.Down,1.40f,1.63f); }
            else if(stage==3 && !main && n.Contains("skill2"))
            { Clear(); Cut(S.Down,.84f,.93f); }
            else if (stage==3 && main && n.Contains("attack"))
            { Clear(); Cut(S.Down,.98f,1.14f); track.damageTimes=new[]{1.10f}; }
            else if (stage==3 && main && n.Contains("skill1"))
            { Clear(); Cut(S.Down,.46f,.64f); Cut(S.Spin,.80f,2.36f); }
            else if (stage==3 && main && n.Contains("skill2"))
            { Clear(); foreach(float t in new[]{.34f,.59f,.84f,1.09f}) Cut(S.Thrust,t-.10f,t+.025f); }
            else if (stage==3 && main && n.Contains("skill3"))
            { Clear(); Cut(S.Thrust,.30f,.46f); Cut(S.Spin,MainBoss3_AnimGenerators.RetreatStart,MainBoss3_AnimGenerators.RetreatEnd); }
            else if (stage==4 && !main && n.Contains("attack"))
            { Clear(); Cut(S.Down,.48f,.64f); Cut(S.Down,1.18f,1.34f); Cut(S.Thrust,2.07f,2.29f); track.damageTimes=new[]{.60f,1.30f,2.25f}; }
            else if (stage==4 && !main && n.Contains("skill1"))
            { Clear(); Cut(S.Spin,.60f,1.05f); Cut(S.Down,2.20f,2.53f); }
            else if(stage==4 && !main && n.Contains("skill2"))
            { Clear(); Cut(S.Thrust,.40f,.57f); Cut(S.Spin,1.35f,2.85f); }
            else if(stage==5 && !main && n.Contains("skill1"))
            { Clear(); for(int hit=0;hit<5;hit++) { float impact=12+hit*7.2f; bool slash=hit==1||hit==3; F(hit==1?S.Up:hit==3?S.Down:S.Thrust,impact-(slash?1.8f:.6f),impact+(slash?1.8f:.6f)); } }
            else
            {
                foreach(var c in cuts)
                {
                    if(c.anchor==A.Feet && c.style==S.Punch) continue; // Ground contact puff stays at contact, not on the moving sword.
                    // Impact-driven cues begin at contact. Move them back into the actual stroke.
                    float lead=c.style==S.Punch?.055f:Mathf.Min(.14f,2f/Mathf.Max(12,track.clip.frameRate));
                    if(stage==5 && main && (n.Contains("skill1") || n.Contains("skill5"))) lead=0;
                    if(c.style==S.Spin) { c.fitDuration=true; continue; }
                    c.time=Mathf.Max(0,c.time-lead); c.duration=lead+.06f;
                    if(stage==5 && main && (n.Contains("skill1") || n.Contains("skill5"))) c.duration=1.8f/12;
                    c.anchor=A.Weapon; c.offset=Vector2.zero; c.fitDuration=true;
                    c.followRotation=c.style!=S.Punch; c.alignBlade=c.style==S.Thrust;
                }
            }
            track.cues=cues.Where(c=>c.time<track.clip.length && c.duration>0).OrderBy(c=>c.time).ToArray();
            bool attack=track.cues.Any(c=>c.style<=S.Punch || c.style==S.Dash);
            track.runtime=attack || cues.Any(c=>c.style==S.Buff) || (stage==5 && main && track.runtime);
            foreach(var cue in track.cues.Where(c=>c.style<=S.Punch)) BakeProgress(sampler,track.clip,cue);
        }
        } finally { UnityEngine.Object.DestroyImmediate(samplingObject); }
        driver.runtimeEnabled=true;
    }
    static void Boxer(BossClipEffects.Track track,List<BossClipEffects.Cue> cues,BossEffects source)
    {
        string n=track.clip.name.ToLowerInvariant();
        int index=n.Contains("attack")?-1:n.Contains("skill1")?0:n.Contains("skill2")?1:n.Contains("skill4")?3:-2;
        if(index==-2) return;
        var boss=source.GetComponent<MidBossController>();
        var original=index<0?boss.data.basicAttack:boss.data.skills[index];
        var skill=JsonUtility.FromJson<BossSkillData>(JsonUtility.ToJson(original));
        var steps=skill.active.attackSteps;
        bool desire=n.Contains("desire"), upperSegment=n.Contains("uppercut");
        if(desire)
        {
            if(index==-1) steps.Add(new BossAttackStepData{actionType=BossActionType.DashAttack,preDelay=.16f,duration=.12f,postDelay=.07f,attackCount=1,damage=20});
            if(index==1) { steps.Add(new BossAttackStepData{actionType=BossActionType.Jump,duration=.18f}); steps.Add(new BossAttackStepData{actionType=BossActionType.MeleeAttack,preDelay=.04f,postDelay=.40f,attackCount=1,damage=22}); }
            if(index==3) { steps[4].postDelay=.10f; steps.Add(new BossAttackStepData{actionType=BossActionType.DashAttack,preDelay=.08f,duration=.12f,postDelay=.28f,attackCount=1,damage=25}); }
        }
        int start=upperSegment?2:0, end=index==1&&!upperSegment?Mathf.Min(2,steps.Count):steps.Count;
        float time=upperSegment?0:skill.preDelay;
        cues.RemoveAll(c=>c.style==S.Dash); // Rebuild segmented movement on the same clock as its pose.
        for(int i=start;i<end;i++)
        {
            var step=steps[i];
            float length=Mathf.Max(.08f,step.preDelay+(step.actionType==BossActionType.MeleeAttack?0:step.duration)+step.postDelay);
            if(step.damage>0 && (step.actionType==BossActionType.MeleeAttack || step.actionType==BossActionType.DashAttack))
            {
                bool upper=index==1&&i==2, hook=(index==0&&i==2)||(index==3&&i>=1&&i<=4), body=(index==0&&i==3)||(index==1&&i==1);
                bool front=index<0?i>=2:index==3?(i==5||i%2==0):index==1?i!=1:i==2||i==4;
                float impact=Mathf.Max(.04f,step.preDelay), lead=(hook||body||(index==3&&i==5))?.055f:impact*.35f;
                var c=Stroke(cues,S.Punch,time+Mathf.Max(0,impact-lead),time+impact+.06f);
                c.anchor=front?A.FrontHand:A.BackHand; c.followRotation=false; c.angle=upper?65:0; c.scale=.75f;
            }
            time+=length;
        }
    }
    static void BakeProgress(BossClipEffects driver,AnimationClip clip,BossClipEffects.Cue cue)
    {
        Transform anchor=cue.anchor==A.FrontHand?driver.frontHand:cue.anchor==A.BackHand?driver.backHand:driver.weapon;
        if(anchor==null) { cue.anchor=A.Body; return; }
        int count=Mathf.Max(8,Mathf.CeilToInt(cue.duration*120));
        var lengths=new float[count+1]; Vector3 last=Vector3.zero; Vector3 previousAxis=Vector3.zero;
        for(int i=0;i<=count;i++)
        {
            clip.SampleAnimation(driver.gameObject,Mathf.Min(clip.length,cue.time+cue.duration*i/count));
            var sr=anchor.GetComponent<SpriteRenderer>();
            Vector3 point=sr!=null && sr.sprite!=null?sr.bounds.center:anchor.position;
            Vector3 axis=anchor.TransformVector(Vector3.up).normalized;
            if(i>0) lengths[i]=lengths[i-1]+Vector3.Distance(last,point)+Vector3.Distance(previousAxis,axis)*.5f;
            last=point; previousAxis=axis;
            if(i==count/2) cue.referenceAngle=Mathf.Atan2(axis.y,axis.x)*Mathf.Rad2Deg;
        }
        var keys=new Keyframe[count+1];
        for(int i=0;i<=count;i++) keys[i]=new Keyframe((float)i/count,lengths[count]>.001f?lengths[i]/lengths[count]:(float)i/count);
        cue.progress=new AnimationCurve(keys);
        for(int i=0;i<keys.Length;i++) { AnimationUtility.SetKeyLeftTangentMode(cue.progress,i,AnimationUtility.TangentMode.Linear); AnimationUtility.SetKeyRightTangentMode(cue.progress,i,AnimationUtility.TangentMode.Linear); }
    }
}
#endif
