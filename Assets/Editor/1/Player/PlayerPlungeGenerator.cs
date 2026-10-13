#if UNITY_EDITOR
using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class PlayerPlungeGenerator
{
    const string Folder = "Assets/Animations/Player";
    const string Flag = "IsEmpoweredDownJump", Land = "EmpoweredLand";
    [MenuItem("Tools/Player/Empowered Down Jump/Generate and Setup")]
    public static void Generate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var prefab = Selection.activeObject as GameObject;
        if (prefab == null || !PrefabUtility.IsPartOfPrefabAsset(prefab))
        {
            var paths = AssetDatabase.FindAssets("t:Prefab").Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetFileNameWithoutExtension(p) == "Player").ToArray();
            if (paths.Length != 1) throw new InvalidOperationException("Project 창에서 사용할 Player 프리팹을 선택하세요.");
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[0]);
        }
        GameObject preview = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(prefab));
        try
        {
            Animator animator = preview.GetComponentInChildren<Animator>(true);
            var ac = animator != null ? animator.runtimeAnimatorController as AnimatorController : null;
            if (ac == null) throw new InvalidOperationException("Player 프리팹에 AnimatorController가 필요합니다.");
            var sm = ac.layers[0].stateMachine;
            var idle = sm.states.Select(x => x.state).FirstOrDefault(x => x.name.EndsWith("Idle", StringComparison.OrdinalIgnoreCase));
            if (idle == null) throw new InvalidOperationException("Base Layer의 Idle 상태를 찾지 못했습니다.");
            var body = animator.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "Body");
            if (body == null) throw new InvalidOperationException("Body 없음");
            EnsureFolder(Folder);
            var nodes = body.GetComponentsInChildren<Transform>(true);
            var pos = nodes.Select(x => x.localPosition).ToArray();
            var rot = nodes.Select(x => Mathf.DeltaAngle(0, x.localEulerAngles.z)).ToArray();
            var scale = nodes.Select(x => x.localScale).ToArray();
            var dive = Make("Player_EmpoweredDownJump", false, animator.transform, nodes, pos, rot, scale);
            var land = Make("Player_EmpoweredLand", true, animator.transform, nodes, pos, rot, scale);
            Parameter(ac, Flag, AnimatorControllerParameterType.Bool);
            Parameter(ac, Land, AnimatorControllerParameterType.Trigger);
            var ds = State(sm, dive, new Vector3(550,280,0));
            var ls = State(sm, land, new Vector3(800,280,0));
            foreach (var tr in sm.anyStateTransitions.Where(x => x.destinationState == ds || x.destinationState == ls).ToArray()) sm.RemoveAnyStateTransition(tr);
            foreach (var tr in sm.anyStateTransitions.Concat(sm.states.Where(x=>x.state!=ds && x.state!=ls).SelectMany(x=>x.state.transitions)))
                if (!tr.conditions.Any(x=>x.parameter==Flag)) tr.AddCondition(AnimatorConditionMode.IfNot,0,Flag);
            var enter = sm.AddAnyStateTransition(ds);
            Configure(enter); enter.AddCondition(AnimatorConditionMode.If,0,Flag);
            enter.AddCondition(AnimatorConditionMode.If,0,"IsDownJumping");
            var impact = sm.AddAnyStateTransition(ls);
            Configure(impact); impact.AddCondition(AnimatorConditionMode.If,0,Land);
            sm.anyStateTransitions = new[]{impact,enter}.Concat(sm.anyStateTransitions.Where(x=>x!=impact && x!=enter)).ToArray();
            var end = ls.AddTransition(idle); Configure(end); end.hasExitTime=true; end.exitTime=1; end.duration=.04f;
            EditorUtility.SetDirty(ac); AssetDatabase.SaveAssets();
            Debug.Log("강화 하향 점프/착지 생성 및 Animator 연결 완료. PlayerController도 제공된 버전으로 교체하세요.");
        }
        finally { PrefabUtility.UnloadPrefabContents(preview); }
    }
    static void Parameter(AnimatorController ac,string name,AnimatorControllerParameterType type)
    {
        var p=ac.parameters.FirstOrDefault(x=>x.name==name);
        if(p!=null && p.type!=type) throw new InvalidOperationException(name+" 파라미터 타입 불일치");
        if(p==null) ac.AddParameter(name,type);
    }
    static AnimatorState State(AnimatorStateMachine sm,AnimationClip clip,Vector3 position)
    {
        var s=sm.states.Select(x=>x.state).FirstOrDefault(x=>x.name==clip.name)??sm.AddState(clip.name,position);
        s.motion=clip;s.speed=1;s.speedParameterActive=false;s.writeDefaultValues=true;
        foreach(var t in s.transitions.ToArray())s.RemoveTransition(t);
        return s;
    }
    static void Configure(AnimatorStateTransition t)
    { t.hasExitTime=false;t.hasFixedDuration=true;t.duration=.025f;t.canTransitionToSelf=false;t.interruptionSource=TransitionInterruptionSource.None; }
    static void EnsureFolder(string path)
    { if(AssetDatabase.IsValidFolder(path))return;string parent=Path.GetDirectoryName(path).Replace('\\','/');EnsureFolder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path)); }
    static float Ease(float t,float a,float b) { float x=Mathf.InverseLerp(a,b,t);return x*x*(3-2*x); }
    static AnimationClip Make(string name,bool landing,Transform root,Transform[] nodes,Vector3[] pos,float[] rest,Vector3[] scale)
    {
        float duration=landing?.35f:.28f;
        var clip=new AnimationClip{name=name,frameRate=60};
        var curves=new AnimationCurve[nodes.Length,9];
        for(int i=0;i<nodes.Length;i++)for(int j=0;j<9;j++)curves[i,j]=new AnimationCurve();
        int count=Mathf.RoundToInt(duration*60);
        for(int frame=0;frame<=count;frame++)
        {
            float t=duration*frame/count;
            float prepare=landing?0:Ease(t,0,.10f)*(1-Ease(t,.10f,.24f));
            float plunge=landing?1-Ease(t,.09f,.35f):Ease(t,.10f,.24f);
            float press=landing?Ease(t,0,.055f)*(1-Ease(t,.10f,.35f)):0;
            float[] angles=(float[])rest.Clone();
            for(int i=0;i<nodes.Length;i++)
            {
                Vector3 p=pos[i];string n=nodes[i].name;
                // This player's unflipped rig faces left. Do not animate the Player root.
                switch(n)
                {
                    case "Body": angles[i]+= -7*prepare+12*plunge+6*press;p.y+=.035f*prepare-.07f*press;break;
                    case "Head":angles[i]+=-5*plunge;break;
                    case "ShoulderJoint_B":angles[i]+=-38*prepare-22*plunge;break;
                    case "ArmJoint_B":angles[i]+=65*prepare+25*plunge;break;
                    case "Hand_B":angles[i]+=-10*plunge;break;
                    case "ShoulderJoint_F":angles[i]+=25*prepare-30*plunge;break;
                    case "ArmJoint_F":angles[i]+=-25*prepare-20*plunge;break;
                    case "LegJoint_F":p.y+=.10f*prepare+.06f*plunge+.07f*press;angles[i]+=12*prepare+6*plunge;break;
                    case "LegJoint_B":p.y+=.08f*prepare+.04f*plunge+.07f*press;angles[i]+=-12*prepare-8*plunge;break;
                }
                nodes[i].localPosition=p;nodes[i].localRotation=Quaternion.Euler(0,0,angles[i]);
            }
            int sword=Array.FindIndex(nodes,x=>x.name=="Sword");
            if(sword<0)throw new InvalidOperationException("Sword 없음");
            float parents=0;
            for(Transform p=nodes[sword].parent;p!=root && p!=null;p=p.parent)
            { int i=Array.IndexOf(nodes,p); if(i>=0)parents+=angles[i]; }
            // Blade points down at full plunge. Rotation is compensated for parent motion.
            angles[sword]=Mathf.Lerp(rest[sword],180f-parents,plunge);
            for(int i=0;i<nodes.Length;i++)
            {
                Vector3 p=nodes[i].localPosition;float[] v={p.x,p.y,p.z,0,0,angles[i],scale[i].x,scale[i].y,scale[i].z};
                for(int j=0;j<9;j++)curves[i,j].AddKey(new Keyframe(t,v[j]));
            }
        }
        string[] props={"m_LocalPosition.x","m_LocalPosition.y","m_LocalPosition.z","localEulerAnglesRaw.x","localEulerAnglesRaw.y","localEulerAnglesRaw.z","m_LocalScale.x","m_LocalScale.y","m_LocalScale.z"};
        for(int i=0;i<nodes.Length;i++)for(int j=0;j<9;j++)
        {
            for(int k=0;k<curves[i,j].length;k++)
            {AnimationUtility.SetKeyLeftTangentMode(curves[i,j],k,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curves[i,j],k,AnimationUtility.TangentMode.Linear);}
            AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(AnimationUtility.CalculateTransformPath(nodes[i],root),typeof(Transform),props[j]),curves[i,j]);
        }
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;AnimationUtility.SetAnimationClipSettings(clip,settings);
        string path=Folder+"/"+name+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if(existing!=null){EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);EditorUtility.SetDirty(existing);return existing;}
        AssetDatabase.CreateAsset(clip,path);return clip;
    }
}
#endif
