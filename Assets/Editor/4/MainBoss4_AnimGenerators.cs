#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Place in Assets/Editor/4. All curves are relative to the MainBoss 4 visual root.
public static class MainBoss4_AnimGenerators
{
    const string Folder="Assets/Animations/4/MainBoss 4";
    const string DataPath="Assets/Data/Enemys/Stage4/Stage4_MainBoss.asset";
    const string Prefix="MainBoss4_";
    const int Body=0,Head=1,SF=2,AF=3,HF=4,SB=5,AB=6,HB=7,Bottom=8,TF=9,CF=10,FF=11,TB=12,CB=13,FB=14;
    static readonly int[] Parent={-1,0,0,2,3,0,5,6,0,8,9,10,8,12,13};
    static readonly string[] Paths={"Body","Body/Head","Body/Shoulder_F","Body/Shoulder_F/Forearm_F","Body/Shoulder_F/Forearm_F/Hand_F",
        "Body/Shoulder_B","Body/Shoulder_B/Forearm_B","Body/Shoulder_B/Forearm_B/Hand_B","Body/Bottom",
        "Body/Bottom/Thigh_F","Body/Bottom/Thigh_F/Calf_F","Body/Bottom/Thigh_F/Calf_F/Foot_F",
        "Body/Bottom/Thigh_B","Body/Bottom/Thigh_B/Calf_B","Body/Bottom/Thigh_B/Calf_B/Foot_B"};
    static readonly Vector3[] RestPositions={new Vector3(0,0,0),new Vector3(-.237f,1.291f,0),
        new Vector3(-.995f,.923f,0),new Vector3(-.253f,-.929f,0),new Vector3(.069f,-.618f,0),
        new Vector3(.414f,.924f,0),new Vector3(.365f,-.850f,0),new Vector3(.054f,-.662f,0),
        new Vector3(.154f,-.049f,0),new Vector3(-.884f,-.345f,0),new Vector3(-.018f,-1.084f,0),new Vector3(-.237f,-1.123f,0),
        new Vector3(.039f,-.345f,0),new Vector3(.154f,-1.084f,0),new Vector3(-.237f,-1.123f,0)};
    sealed class Pose
    {
        public Vector3[] position=(Vector3[])RestPositions.Clone();
        public float[] angle=new float[15];
        public float frontStep,backStep,frontLift,backLift;
    }
    sealed class ClipSpec
    {
        public string name;public float length;public bool loop;public Func<float,Pose> pose;
        public ClipSpec(string n,float l,bool repeat,Func<float,Pose> fn){name=n;length=l;loop=repeat;pose=fn;}
    }
    static List<ClipSpec> specs;
    [MenuItem("Tools/4/MainBoss 4/Generate Animations")]
    public static void GenerateAll(){BuildSpecs();EnsureFolder(Folder);foreach(var spec in specs)Save(spec);SetupAnimator();}
    [MenuItem("Tools/4/MainBoss 4/Clips/Idle")]public static void IdleMenu()=>One("Idle");
    [MenuItem("Tools/4/MainBoss 4/Clips/Walk")]public static void WalkMenu()=>One("Walk");
    [MenuItem("Tools/4/MainBoss 4/Clips/Run")]public static void RunMenu()=>One("Run");
    [MenuItem("Tools/4/MainBoss 4/Clips/Attack")]public static void AttackMenu()=>Group(-1);
    [MenuItem("Tools/4/MainBoss 4/Clips/Skill1")]public static void Skill1Menu()=>Group(0);
    [MenuItem("Tools/4/MainBoss 4/Clips/Skill2")]public static void Skill2Menu()=>Group(1);
    [MenuItem("Tools/4/MainBoss 4/Clips/Skill3 Buff")]public static void Skill3Menu()=>One("skill3");
    [MenuItem("Tools/4/MainBoss 4/Clips/Skill4")]public static void Skill4Menu()=>Group(3);
    [MenuItem("Tools/4/MainBoss 4/Clips/Skill5 Buff")]public static void Skill5Menu()=>One("skill5");
    [MenuItem("Tools/4/MainBoss 4/Clips/Desire")]public static void DesireMenu(){One("Dskill");One("Groggy");}
    [MenuItem("Tools/4/MainBoss 4/Clips/Death")]public static void DeathMenu()=>One("Death");
    static void One(string name){BuildSpecs();EnsureFolder(Folder);foreach(var s in specs)if(s.name==name)Save(s);SetupAnimator();}
    static void Group(int index)
    {
        BuildSpecs();EnsureFolder(Folder);string key=index<0?"Attack":"skill"+(index+1);
        foreach(var s in specs)if(s.name==key||s.name==key+"_Desire"||s.name.StartsWith("Step_"+(index+1)+"_"))Save(s);
        SetupAnimator();
    }
    static void BuildSpecs()
    {
        var data=AssetDatabase.LoadAssetAtPath<MainBossData>(DataPath);
        if(data==null)throw new InvalidOperationException("데이터를 찾지 못했습니다: "+DataPath);
        specs=new List<ClipSpec>{new ClipSpec("Idle",2.6f,true,Idle),new ClipSpec("Walk",1.1f,true,t=>Walk(t,1.1f,false)),
            new ClipSpec("Run",.72f,true,t=>Walk(t,.72f,true)),new ClipSpec("Death",2f,false,Death),
            new ClipSpec("Recover",.32f,false,t=>Mix(Guard(),new Pose(),t/.32f)),
            new ClipSpec("Dskill",1.1f,false,t=>Buff(t,1.1f)),new ClipSpec("Groggy",3f,false,Groggy)};
        AddPattern(data.basicAttack,-1,false);
        for(int i=0;i<data.skills.Count;i++)AddPattern(data.skills[i],i,false);
        // Match Stage4_MainBossDesireSkill's appended steps exactly.
        var basic=JsonUtility.FromJson<BossSkillData>(JsonUtility.ToJson(data.basicAttack));
        basic.active.attackSteps.Add(PunchStep("4타 전진 스트레이트",20,.16f,true));AddPattern(basic,-1,true);
        var second=JsonUtility.FromJson<BossSkillData>(JsonUtility.ToJson(data.skills[1]));
        second.active.attackSteps.Add(new BossAttackStepData{actionType=BossActionType.Jump,jumpForce=new Vector2(2,7),duration=.18f});
        second.active.attackSteps.Add(PunchStep("공중 스트레이트",22,.04f));AddPattern(second,1,true);
        var fourth=JsonUtility.FromJson<BossSkillData>(JsonUtility.ToJson(data.skills[3]));
        fourth.active.attackSteps.Add(PunchStep("최종 스트레이트",25,.4f,true));AddPattern(fourth,3,true);
    }
    static BossAttackStepData PunchStep(string name,float damage,float delay,bool advance=false)
    {
        return new BossAttackStepData{stepName=name,actionType=advance?BossActionType.DashAttack:BossActionType.MeleeAttack,
            damage=damage,preDelay=delay,postDelay=.07f,duration=advance?.12f:0,attackCount=1};
    }
    static void AddPattern(BossSkillData skill,int index,bool enhanced)
    {
        string name=index<0?"Attack":"skill"+(index+1);if(enhanced)name+="_Desire";
        if(skill.skillType==BossSkillType.Buff)
        {float duration=Mathf.Max(.5f,skill.preDelay+skill.postDelay);specs.Add(new ClipSpec(name,duration,false,t=>Buff(t,duration)));return;}
        var stages=new List<ClipSpec>();float total=skill.preDelay;
        for(int n=0;n<skill.active.attackSteps.Count;n++)
        {
            int stepIndex=n;var step=skill.active.attackSteps[n];
            float length=Mathf.Max(.08f,step.preDelay+step.duration+step.postDelay);
            if(step.actionType==BossActionType.MeleeAttack)length=Mathf.Max(.08f,step.preDelay+step.postDelay);
            string stepName="Step_"+(index+1)+"_"+n+(enhanced?"_D":"");
            var spec=new ClipSpec(stepName,length,false,t=>StepPose(index,stepIndex,step,t,length));
            specs.Add(spec);stages.Add(spec);total+=length;
        }
        total+=skill.postDelay+.32f;
        specs.Add(new ClipSpec(name,total,false,t=>{
            float time=t-skill.preDelay;if(time<0)return Mix(new Pose(),Guard(),t/Mathf.Max(.01f,skill.preDelay));
            foreach(var stage in stages){if(time<=stage.length)return stage.pose(time);time-=stage.length;}
            return Mix(Guard(),new Pose(),Mathf.Max(0,time-skill.postDelay)/.32f);
        }));
    }
    static Pose Mix(Pose a,Pose b,float u)
    {
        u=Smooth(Mathf.Clamp01(u));var p=new Pose();
        for(int i=0;i<15;i++){p.position[i]=Vector3.Lerp(a.position[i],b.position[i],u);p.angle[i]=Mathf.Lerp(a.angle[i],b.angle[i],u);}
        return p;
    }
    private static Vector2 Rotate(Vector2 v,float degrees)
    {float a=degrees*Mathf.Deg2Rad;return new Vector2(v.x*Mathf.Cos(a)-v.y*Mathf.Sin(a),v.x*Mathf.Sin(a)+v.y*Mathf.Cos(a));}
    private static float Smooth(float t)=>t*t*(3-2*t);
    private static float WorldAngle(Pose p,int part)
    {float a=0;for(int i=part;i>=0;i=Parent[i])a+=p.angle[i];return a;}
    private static Vector2 WorldPosition(Pose p,int part)
    {
        int parent=Parent[part];
        return parent<0?(Vector2)p.position[part]:
            WorldPosition(p,parent)+Rotate(p.position[part],WorldAngle(p,parent));
    }
    // Solve the two leg links with their original lengths. No stretching of calf/foot offsets.
    private static void PlantLeg(Pose p,bool front,float step,float lift)
    {
        int thigh=front?TF:TB,calf=front?CF:CB,foot=front?FF:FB;
        var rest=new Pose();
        Vector2 target=WorldPosition(rest,foot)+new Vector2(step,lift);
        Vector2 hip=WorldPosition(p,thigh),d=target-hip;
        Vector2 upper=RestPositions[calf],lower=RestPositions[foot];
        float a=upper.magnitude,b=lower.magnitude;
        float length=Mathf.Clamp(d.magnitude,Mathf.Abs(a-b)+.00001f,a+b);
        float bend=-Mathf.Acos(Mathf.Clamp((length*length-a*a-b*b)/(2*a*b),-1,1));
        float heading=Mathf.Atan2(d.y,d.x)-Mathf.Atan2(b*Mathf.Sin(bend),a+b*Mathf.Cos(bend));
        float thighWorld=heading*Mathf.Rad2Deg-Mathf.Atan2(upper.y,upper.x)*Mathf.Rad2Deg;
        float calfWorld=(heading+bend)*Mathf.Rad2Deg-Mathf.Atan2(lower.y,lower.x)*Mathf.Rad2Deg;
        p.angle[thigh]=Mathf.DeltaAngle(0,thighWorld-WorldAngle(p,Bottom));
        p.angle[calf]=Mathf.DeltaAngle(0,calfWorld-thighWorld);
        p.angle[foot]=-WorldAngle(p,calf);
    }
    private static Pose Ground(Pose p)
    {PlantLeg(p,true,p.frontStep,p.frontLift);PlantLeg(p,false,p.backStep,p.backLift);return p;}
    static Pose Idle(float t)
    {
        var p=new Pose();float u=.5f-.5f*Mathf.Cos(t/2.6f*Mathf.PI*2);
        p.position[Body].y=.009f*u;p.position[Bottom].y-=.009f*u;p.angle[Head]=-.3f*u;
        p.angle[SF]=.5f*u;p.angle[SB]=-.4f*u;return p;
    }
    // Analytic wrist targets keep the original arm lengths and attachment positions.
    static void Arm(Pose p,bool front,Vector2 wrist,float handAngle=0)
    {
        int shoulder=front?SF:SB,forearm=front?AF:AB,hand=front?HF:HB;
        Vector2 upper=RestPositions[forearm],lower=RestPositions[hand];
        Vector2 d=wrist-WorldPosition(p,shoulder);float a=upper.magnitude,b=lower.magnitude;
        float length=Mathf.Clamp(d.magnitude,Mathf.Abs(a-b)+.001f,a+b-.001f);
        float bend=Mathf.Acos(Mathf.Clamp((length*length-a*a-b*b)/(2*a*b),-1,1));
        float heading=Mathf.Atan2(d.y,d.x)-Mathf.Atan2(b*Mathf.Sin(bend),a+b*Mathf.Cos(bend));
        float upperWorld=heading*Mathf.Rad2Deg-Mathf.Atan2(upper.y,upper.x)*Mathf.Rad2Deg;
        float lowerWorld=(heading+bend)*Mathf.Rad2Deg-Mathf.Atan2(lower.y,lower.x)*Mathf.Rad2Deg;
        p.angle[shoulder]=Mathf.DeltaAngle(0,upperWorld-p.angle[Body]);
        p.angle[forearm]=Mathf.DeltaAngle(0,lowerWorld-upperWorld);
        p.angle[hand]=Mathf.DeltaAngle(0,handAngle-lowerWorld);
    }
    static Pose Guard(float crouch=0,float lean=-3)
    {
        var p=new Pose();p.position[Body]=new Vector3(.03f,-crouch,0);p.angle[Body]=lean;p.angle[Bottom]=-lean*.65f;p.angle[Head]=-lean*.7f;
        Arm(p,true,new Vector2(-.2f,.95f),5);Arm(p,false,new Vector2(.85f,1.05f),-5);
        if(crouch>0){p.frontStep=-.06f;p.backStep=.13f;Ground(p);}
        else{p.angle[Bottom]=-lean;p.position[Bottom]=Quaternion.Euler(0,0,-lean)*(RestPositions[Bottom]-p.position[Body]);}
        return p;
    }
    static Pose Punch(bool front,bool hook,bool upper,bool body,bool air=false)
    {
        var p=Guard(body?.32f:.12f,upper?4:-12);
        p.position[Body].x=front?.16f:.08f;p.frontStep=-.14f;p.backStep=.22f;Ground(p);
        Vector2 shoulder=WorldPosition(p,front?SF:SB);
        Vector2 target=shoulder+new Vector2(upper?.65f:hook?1.15f:1.50f,upper?1.25f:body?-.65f:.05f);
        Arm(p,front,target,upper?70:hook?12:0);
        if(air){p.angle[TF]=-18;p.angle[CF]=-8;p.angle[TB]=40;p.angle[CB]=-65;p.angle[Body]=-20;}
        return p;
    }
    static Pose StepPose(int skill,int n,BossAttackStepData step,float t,float duration)
    {
        var guard=Guard();bool movement=step.actionType==BossActionType.MoveForward;
        if(movement)
        {
            var low=Guard(skill==3?.45f:.18f,-16);
            float u=Mathf.Clamp01((t-step.preDelay)/Mathf.Max(.01f,step.duration));
            var moving=Walk(u*.72f,.72f,true);moving.position[Body].y-=.18f;moving.angle[Body]=-22;
            Arm(moving,true,new Vector2(-.12f,.6f));Arm(moving,false,new Vector2(.75f,.7f));
            return t<step.preDelay?Mix(guard,low,t/Mathf.Max(.01f,step.preDelay)):Mix(low,moving,Mathf.Min(u*5,1));
        }
        if(step.actionType==BossActionType.Jump)
        {var p=Guard(.14f,-8);p.angle[TF]=-22;p.angle[CF]=-6;p.angle[TB]=42;p.angle[CB]=-72;return p;}
        bool upper=skill==1&&n==2;
        bool hook=(skill==0&&n==2)||(skill==3&&n>=1&&n<=4);
        bool body=(skill==0&&n==3)||(skill==1&&n==1);
        bool front=skill<0?n==2:skill==3?n%2==1:skill==1?n!=1:n%2==1;
        var hit=Punch(front,hook,upper,body,skill==1&&n>=4);
        var wind=Guard(body?.28f:.08f,hook?7:3);
        Vector2 shoulder=WorldPosition(wind,front?SF:SB);
        Arm(wind,front,shoulder+new Vector2(.15f,upper?-.95f:.2f),upper?-20:5);
        float impact=Mathf.Max(.04f,step.preDelay);
        if(t<impact*.65f)return Mix(guard,wind,t/Mathf.Max(.01f,impact*.65f));
        if(t<impact)return Mix(wind,hit,(t-impact*.65f)/Mathf.Max(.01f,impact*.35f));
        // Brief impact, then controlled recoil; next step can interrupt early on a wall.
        return Mix(hit,guard,Mathf.Clamp01((t-impact-.025f)/Mathf.Max(.06f,duration-impact-.025f)));
    }
    static Pose Walk(float t,float duration,bool run)
    {
        float phase=t/duration,w=Mathf.Sin(phase*Mathf.PI*2);var p=Guard(run?.12f:.035f,run?-13:-4);
        for(int side=0;side<2;side++)
        {
            float u=Mathf.Repeat(phase+side*.5f,1),stride=run?.42f:.24f;
            float x=u<.5f?Mathf.Lerp(stride,-stride,u*2):Mathf.Lerp(-stride,stride,Smooth((u-.5f)*2));
            float lift=u<.5f?0:(run?.22f:.10f)*Mathf.Pow(Mathf.Sin((u-.5f)*2*Mathf.PI),2);
            PlantLeg(p,side==0,x,lift);
        }
        p.angle[SF]+=3*w;p.angle[SB]-=3*w;return p;
    }
    static Pose Buff(float t,float duration)
    {
        float u=t/duration;var raised=new Pose();
        Arm(raised,true,new Vector2(-1.45f,1.75f),80);Arm(raised,false,new Vector2(1.15f,1.85f),100);
        raised.angle[Head]=5;
        var p=u<.4f?Mix(new Pose(),raised,u/.4f):u<.7f?raised:Mix(raised,new Pose(),(u-.7f)/.3f);
        if(u>.4f&&u<.7f){float shake=Mathf.Sin((u-.4f)/.3f*Mathf.PI*4)*.9f;p.angle[AF]+=shake;p.angle[AB]-=shake;}
        return p;
    }
    static Pose Groggy(float t)
    {var p=Guard(.06f,-12);p.angle[Head]=-12+Mathf.Sin(t*4)*1.5f;p.angle[SF]=12;p.angle[AF]=15;p.angle[SB]=8;p.angle[AB]=18;return p;}
    static float SpriteBottom(Pose p,int part)
    {
        Vector2 size=part==Body?new Vector2(146f/64,160f/64):new Vector2(83f/64,82f/64);
        Vector2 pivot=part==Body?new Vector2(.59f,.24f):new Vector2(.45f,.37f);
        float y=float.PositiveInfinity;
        for(int a=0;a<2;a++)for(int b=0;b<2;b++)y=Mathf.Min(y,(WorldPosition(p,part)+Rotate(Vector2.Scale(new Vector2(a,b)-pivot,size),WorldAngle(p,part))).y);
        return y;
    }
    static Pose Death(float t)
    {
        float kneel=Smooth(Mathf.Clamp01(t/.65f)),fall=Smooth(Mathf.Clamp01((t-.7f)/.75f)),extend=Smooth(Mathf.Clamp01((t-1)/.6f));
        var p=new Pose();p.position[Body]=new Vector3(.65f*fall,-kneel-fall,0);p.angle[Body]=-88*fall;p.angle[Bottom]=10*fall;
        p.angle[TF]=Mathf.Lerp(65*kneel,-10,extend);p.angle[TB]=Mathf.Lerp(60*kneel,-10,extend);
        p.angle[CF]=-110*kneel*(1-extend);p.angle[CB]=-110*kneel*(1-extend);
        p.angle[SF]=15*fall;p.angle[AF]=70*fall;p.angle[SB]=10*fall;p.angle[AB]=65*fall;
        var rest=new Pose();float floor=Mathf.Min(SpriteBottom(rest,FF),SpriteBottom(rest,FB));
        p.position[Body].y+=floor-Mathf.Min(SpriteBottom(p,Body),Mathf.Min(SpriteBottom(p,FF),SpriteBottom(p,FB)));
        return p;
    }
    static AnimationClip Save(ClipSpec spec)
    {
        var clip=new AnimationClip{name=Prefix+spec.name,frameRate=60};int count=Mathf.CeilToInt(spec.length*60);
        var poses=new Pose[count+1];for(int i=0;i<=count;i++)poses[i]=spec.pose(spec.loop&&i==count?0:spec.length*i/count);
        for(int bone=0;bone<15;bone++)for(int axis=0;axis<3;axis++)
        {
            var pos=new Keyframe[count+1];var rot=new Keyframe[count+1];
            for(int i=0;i<=count;i++){float t=spec.length*i/count;pos[i]=new Keyframe(t,poses[i].position[bone][axis]);rot[i]=new Keyframe(t,axis==2?poses[i].angle[bone]:0);}
            SetCurve(clip,Paths[bone],"m_LocalPosition."+"xyz"[axis],pos);
            SetCurve(clip,Paths[bone],"localEulerAnglesRaw."+"xyz"[axis],rot);
            SetCurve(clip,Paths[bone],"m_LocalScale."+"xyz"[axis],new[]{new Keyframe(0,1),new Keyframe(spec.length,1)});
        }
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=spec.loop;AnimationUtility.SetAnimationClipSettings(clip,settings);
        string path=Folder+"/"+clip.name+".anim";var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if(old!=null){var events=AnimationUtility.GetAnimationEvents(old);EditorUtility.CopySerialized(clip,old);AnimationUtility.SetAnimationEvents(old,events);UnityEngine.Object.DestroyImmediate(clip);EditorUtility.SetDirty(old);return old;}
        AssetDatabase.CreateAsset(clip,path);return clip;
    }
    private static void SetCurve(AnimationClip clip, string path, string property, Keyframe[] keys)
    {
        var curve = new AnimationCurve(keys);
        // 촘촘한 샘플 사이 선형 보간: 자동 탄젠트에 의한 회전/발 높이 오버슈트 방지.
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
        }
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);
    }

    private static void EnsureFolder(string folder)
    {
        string[] parts = folder.Split('/');
        string path = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = path + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(path, parts[i]);
            path = next;
        }
    }
    [MenuItem("Tools/4/MainBoss 4/Setup Animator")]
    public static void SetupAnimator()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        BuildSpecs();EnsureFolder(Folder);
        AnimatorController controller=null;
        foreach(string guid in AssetDatabase.FindAssets("MainBoss4_AC t:AnimatorController"))
        {string path=AssetDatabase.GUIDToAssetPath(guid);if(System.IO.Path.GetFileNameWithoutExtension(path)!="MainBoss4_AC")continue;
            if(controller!=null)throw new InvalidOperationException("MainBoss4_AC 이름이 중복됩니다.");controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);}
        if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(Folder+"/MainBoss4_AC.controller");
        Param(controller,"MoveSpeed",AnimatorControllerParameterType.Float);Param(controller,"AttackSpeed",AnimatorControllerParameterType.Float);
        Param(controller,"IsChasing",AnimatorControllerParameterType.Bool);Param(controller,"IsAttacking",AnimatorControllerParameterType.Bool);
        Param(controller,"IsDead",AnimatorControllerParameterType.Bool);
        var sm=controller.layers[0].stateMachine;var states=new Dictionary<string,AnimatorState>();
        int order=0;
        foreach(var spec in specs)
        {
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"/"+Prefix+spec.name+".anim");if(clip==null)clip=Save(spec);
            AnimatorState state=null;foreach(var c in sm.states)if(c.state.name==Prefix+spec.name)state=c.state;
            if(state==null)state=sm.AddState(Prefix+spec.name,new Vector3(280+(order%4)*260,(order/4)*90));order++;
            state.motion=clip;state.writeDefaultValues=false;state.speed=1;
            state.speedParameter="AttackSpeed";state.speedParameterActive=!spec.loop&&spec.name!="Death"&&spec.name!="Groggy"&&spec.name!="Dskill";
            foreach(var tr in state.transitions)if(tr.name.StartsWith("MB4_"))state.RemoveTransition(tr);
            states[spec.name]=state;
            if(!spec.loop&&spec.name!="Death")Param(controller,spec.name,AnimatorControllerParameterType.Trigger);
        }
        foreach(var tr in sm.anyStateTransitions)if(tr.name.StartsWith("MB4_"))sm.RemoveAnyStateTransition(tr);
        sm.defaultState=states["Idle"];
        string[] locomotion={"Idle","Walk","Run"};
        foreach(var pair in states)
        {
            string name=pair.Key;var state=pair.Value;
            if(name=="Death")continue;
            if(name!="Idle"&&name!="Walk"&&name!="Run")
            {
                var enter=sm.AddAnyStateTransition(state);enter.name="MB4_"+name;enter.hasExitTime=false;enter.duration=.025f;enter.hasFixedDuration=true;enter.canTransitionToSelf=true;
                enter.AddCondition(AnimatorConditionMode.If,0,name);enter.AddCondition(AnimatorConditionMode.IfNot,0,"IsDead");
            }
            foreach(string dest in locomotion)
            {
                if(dest==name)continue;var tr=state.AddTransition(states[dest]);tr.name="MB4_Return";
                tr.hasExitTime=false;tr.duration=.08f;tr.hasFixedDuration=true;
                tr.AddCondition(AnimatorConditionMode.IfNot,0,"IsAttacking");tr.AddCondition(AnimatorConditionMode.IfNot,0,"IsDead");
                tr.AddCondition(dest=="Idle"?AnimatorConditionMode.Less:AnimatorConditionMode.Greater,.01f,"MoveSpeed");
                if(dest!="Idle")tr.AddCondition(dest=="Run"?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"IsChasing");
            }
        }
        var death=sm.AddAnyStateTransition(states["Death"]);death.name="MB4_Death";death.hasExitTime=false;death.duration=.04f;death.canTransitionToSelf=false;death.AddCondition(AnimatorConditionMode.If,0,"IsDead");
        EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();Assign(controller);
    }
    static void Param(AnimatorController c,string name,AnimatorControllerParameterType type)
    {foreach(var p in c.parameters)if(p.name==name){if(p.type!=type)throw new InvalidOperationException(name+" 타입 충돌");return;}c.AddParameter(name,type);}
    static void Assign(AnimatorController controller)
    {
        const string folder="Assets/Prefab/4";if(!AssetDatabase.IsValidFolder(folder))return;
        foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{folder}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);if(System.IO.Path.GetFileNameWithoutExtension(path)!="MainBoss 4")continue;
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(Transform visual in root.GetComponentsInChildren<Transform>(true))
                {
                    if(visual.name!="MainBoss 4"||visual.Find("Body")==null)continue;
                    var animator=visual.GetComponent<Animator>();if(animator==null)animator=visual.gameObject.AddComponent<Animator>();
                    animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
                    foreach(var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if(component==null)continue;var so=new SerializedObject(component);
                        foreach(string field in new[]{"bossAnimator"}){var prop=so.FindProperty(field);if(prop!=null)prop.objectReferenceValue=animator;}
                        var vr=so.FindProperty("visualRoot");if(vr!=null)vr.objectReferenceValue=visual;
                        var dc=so.FindProperty("deathAnimation");if(dc!=null)dc.objectReferenceValue=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"/MainBoss4_Death.anim");
                        var ds=so.FindProperty("deathAnimationState");if(ds!=null)ds.stringValue="MainBoss4_Death";
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }

}
#endif
