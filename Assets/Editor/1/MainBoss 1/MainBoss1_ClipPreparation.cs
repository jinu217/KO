#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;

public static class MainBoss1_ClipPreparation
{
    struct Joint
    {
        public string path; public float x, y, angle;
        public Joint(string p, float px, float py, float a) { path=p; x=px; y=py; angle=a; }
    }
    static readonly Joint[] Rig = {
        new Joint("Body",0,-.001f,20), new Joint("Body/Head",0,.7f,0),
        new Joint("Body/Shoulder_F",-.5f,.5f,.76f),
        new Joint("Body/Shoulder_F/Forearm_F",-.29f,-.57f,19.28f),
        new Joint("Body/Shoulder_F/Forearm_F/Hand_F",.07f,-.585f,0),
        new Joint("Body/Shoulder_B",.26f,.55f,-20),
        new Joint("Body/Shoulder_B/Forearm_B",.15f,-.65f,0),
        new Joint("Body/Shoulder_B/Forearm_B/Hand_B",.05f,-.54f,0),
        new Joint("Body/Shoulder_B/Forearm_B/Hand_B/Weapon",.13f,-.28f,-90),
        new Joint("Body/Shoulder_B/Forearm_B/Hand_B/Weapon/GripPoint",0,-.45f,0),
        new Joint("Body/Bottom",-.2f,-.299f,-20),
        new Joint("Body/Bottom/Thigh_F",-.35f,-.15f,0),
        new Joint("Body/Bottom/Thigh_F/Calf_F",-.17f,-.88f,0),
        new Joint("Body/Bottom/Thigh_F/Calf_F/Foot_F",-.53f,-.845f,0),
        new Joint("Body/Bottom/Thigh_B",.07f,-.215f,0),
        new Joint("Body/Bottom/Thigh_B/Calf_B",.57f,-.75f,0),
        new Joint("Body/Bottom/Thigh_B/Calf_B/Foot_B",-.14f,-.91f,0)
    };


    public static Dictionary<string, AnimationClip> Prepare(Dictionary<string, AnimationClip> source, string controllerPath)
    {
        string parent = Path.GetDirectoryName(controllerPath).Replace('\\', '/');
        string folder = parent + "/MainBoss1_Prepared";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(parent, "MainBoss1_Prepared");
        var output = new Dictionary<string, AnimationClip>();
        foreach (var item in source)
        {
            bool loop = item.Key == "Idle" || item.Key == "Idle2" || item.Key == "Walk" || item.Key == "Chase";
            bool phaseEntry = item.Key == "P2E" || item.Key == "P3E";
            AnimationClip idle = source[item.Key == "Idle2" || item.Key == "P3E" ? "Idle2" : "Idle"];
            output.Add(item.Key, Bake(item.Value, idle, item.Key, loop, folder));
            if (!loop && !phaseEntry)
                output.Add(item.Key + "_Phase3", Bake(item.Value, source["Idle2"], item.Key + "_Phase3", false, folder));
        }
        return output;
    }

    static AnimationClip Bake(AnimationClip source, AnimationClip idle, string key, bool loop, string folder)
    {
        var clip = UnityEngine.Object.Instantiate(source);
        clip.name = "MainBoss1_Prepared_" + key;
        float originalEnd = source.length;
        float recovery = loop ? 0f : 0.35f;
        float end = originalEnd + recovery;
        // 이동·방향 전환용 시각 루트는 보정 클립에서 제어하지 않습니다.
        foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            if (binding.type == typeof(Transform) && binding.path.Length == 0)
                AnimationUtility.SetEditorCurve(clip, binding, null);
        foreach (var joint in Rig)
        {
            string family = RotationFamily(source, joint.path);
            Vector3 targetPosition = new Vector3(joint.x, joint.y, 0f);
            Vector3 targetEuler = new Vector3(0f, 0f, joint.angle);
            Vector3 targetScale = Vector3.one;
            for (int axis = 0; axis < 3; axis++)
            {
                string suffix = "." + "xyz"[axis];
                targetPosition[axis] = Value(idle, joint.path, "m_LocalPosition" + suffix, targetPosition[axis]);
                targetScale[axis] = Value(idle, joint.path, "m_LocalScale" + suffix, 1f);
            }
            string idleFamily = RotationFamily(idle, joint.path);
            if (idleFamily == "m_LocalRotation")
            {
                Quaternion fallback = Quaternion.Euler(targetEuler);
                Quaternion q = new Quaternion(Value(idle,joint.path,idleFamily+".x",fallback.x),
                    Value(idle,joint.path,idleFamily+".y",fallback.y), Value(idle,joint.path,idleFamily+".z",fallback.z),
                    Value(idle,joint.path,idleFamily+".w",fallback.w));
                targetEuler = q.eulerAngles;
            }
            else for (int axis=0; axis<3; axis++) targetEuler[axis] = Value(idle,joint.path,idleFamily+"."+"xyz"[axis],targetEuler[axis]);
            for (int axis=0; axis<3; axis++)
            {
                string suffix="."+"xyz"[axis];
                Fill(clip,joint.path,"m_LocalPosition"+suffix,targetPosition[axis],originalEnd,end,false);
                Fill(clip,joint.path,"m_LocalScale"+suffix,targetScale[axis],originalEnd,end,false);
            }
            if (family == "m_LocalRotation")
            {
                Quaternion q = Quaternion.Euler(targetEuler);
                Quaternion last = new Quaternion(ValueAt(clip,joint.path,family+".x",originalEnd,q.x),
                    ValueAt(clip,joint.path,family+".y",originalEnd,q.y),ValueAt(clip,joint.path,family+".z",originalEnd,q.z),
                    ValueAt(clip,joint.path,family+".w",originalEnd,q.w));
                float sign = Quaternion.Dot(last,q)<0 ? -1f : 1f;
                for(int axis=0; axis<4; axis++) Fill(clip,joint.path,family+"."+"xyzw"[axis],q[axis]*sign,originalEnd,end,false);
            }
            else for(int axis=0; axis<3; axis++) Fill(clip,joint.path,family+"."+"xyz"[axis],targetEuler[axis],originalEnd,end,true);
        }
        var settings=AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime=loop; settings.loopBlend=false; settings.startTime=0; settings.stopTime=end;
        AnimationUtility.SetAnimationClipSettings(clip,settings);
        string path=folder+"/"+clip.name+".anim";
        var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if(existing==null) { AssetDatabase.CreateAsset(clip,path); return clip; }
        EditorUtility.CopySerialized(clip,existing); EditorUtility.SetDirty(existing);
        UnityEngine.Object.DestroyImmediate(clip); return existing;
    }

    static string RotationFamily(AnimationClip clip, string path)
    {
        string family=null;
        foreach(var b in AnimationUtility.GetCurveBindings(clip))
        {
            if(b.type!=typeof(Transform)||b.path!=path) continue;
            if(!b.propertyName.StartsWith("localEulerAngles")&&!b.propertyName.StartsWith("m_LocalRotation.")) continue;
            string next=b.propertyName.Substring(0,b.propertyName.LastIndexOf('.'));
            if(family!=null&&family!=next) throw new InvalidOperationException(clip.name+": 회전 커브 형식 중복: "+path);
            family=next;
        }
        return family??"localEulerAnglesRaw";
    }
    static float Value(AnimationClip clip,string path,string property,float fallback)
        => ValueAt(clip,path,property,0f,fallback);
    static float ValueAt(AnimationClip clip,string path,string property,float time,float fallback)
    {
        var curve=AnimationUtility.GetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),property));
        return curve==null||curve.length==0?fallback:curve.Evaluate(time);
    }
    static void Fill(AnimationClip clip,string path,string property,float target,float originalEnd,float end,bool angle)
    {
        var binding=EditorCurveBinding.FloatCurve(path,typeof(Transform),property);
        var curve=AnimationUtility.GetEditorCurve(clip,binding);
        if(curve==null||curve.length==0) curve=AnimationCurve.Constant(0,end,target);
        else if(end>originalEnd)
        {
            float last=curve.Evaluate(originalEnd);
            if(angle) target=last+Mathf.DeltaAngle(last,target);
            // 기존 동작과 타격/이벤트 시간은 유지하고 마지막 뒤에만 복귀를 추가합니다.
            var keys=new List<Keyframe>(curve.keys);
            if(keys[keys.Count-1].time < originalEnd-0.00001f) keys.Add(new Keyframe(originalEnd,last,0,0));
            else { var k=keys[keys.Count-1]; k.outTangent=0; k.weightedMode=WeightedMode.None; keys[keys.Count-1]=k; }
            keys.Add(new Keyframe(end,target,0,0));
            curve=new AnimationCurve(keys.ToArray());
        }
        AnimationUtility.SetEditorCurve(clip,binding,curve);
    }
}
#endif
