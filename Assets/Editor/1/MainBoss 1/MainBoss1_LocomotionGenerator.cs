#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System;
using System.Collections.Generic;
using System.IO;

public static class MainBoss1_LocomotionGenerator
{
    struct Joint
    {
        public string path; public float x, y, angle;
        public Joint(string p, float px, float py, float a) { path = p; x = px; y = py; angle = a; }
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

    [MenuItem("Tools/1/MainBoss 1/Generate Walk and Chase")]
    public static void Generate()
    {
        string folder = null;
        foreach (string guid in AssetDatabase.FindAssets("MainBoss 1_AC t:AnimatorController"))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(p);
            if (controller == null || controller.name != "MainBoss 1_AC") continue;
            if (folder != null) { Debug.LogError("MainBoss 1_AC가 여러 개 있습니다."); return; }
            folder = Path.GetDirectoryName(p).Replace('\\', '/');
        }
        if (folder == null) { Debug.LogError("MainBoss 1_AC를 찾을 수 없습니다."); return; }
        Save(Build(false), folder + "/MainBoss1_Walk.anim");
        Save(Build(true), folder + "/MainBoss1_Chase.anim");
        AssetDatabase.SaveAssets();
        MainBoss1_AnimatorSetup.Setup();
        Debug.Log("[MainBoss1] Walk 1.2초 / Chase 0.88초 생성 및 연결 완료.");
    }

    static AnimationClip Build(bool chase)
    {
        float duration = chase ? .88f : 1.2f;
        var clip = new AnimationClip { name = chase ? "MainBoss1_Chase" : "MainBoss1_Walk", frameRate = 60 };
        const int samples = 72;
        foreach (var j in Rig)
        {
            var x = new Keyframe[samples + 1]; var y = new Keyframe[samples + 1]; var z = new Keyframe[samples + 1];
            for (int k = 0; k <= samples; k++)
            {
                // 지지발이 몸 기준 뒤로 밀리고, 든 발이 앞으로 돌아오도록 재생 방향 반전.
                float phase = k == samples ? 0f : -k / (float)samples * Mathf.PI * 2f;
                float wave = Mathf.Sin(phase), bob = (1 - Mathf.Cos(phase * 2)) * .5f;
                float body = (chase ? 15f : 20f) + (chase ? 1.2f : 1f) * Mathf.Sin(phase * 2);
                float shoulderB = -20 + (chase ? 3.5f : 3f) * Mathf.Sin(phase + .25f);
                float elbowB = (chase ? 6f : 3f) * (1 + Mathf.Sin(phase - .3f)) * .5f;
                float angle = j.angle, py = j.y;
                if (j.path == "Body") { angle = body; py += (chase ? -.035f : 0) + (chase ? .03f : .025f) * bob; }
                else if (j.path == "Body/Head") angle = 20 - body - 1.5f * Mathf.Sin(phase * 2 - .4f);
                else if (j.path == "Body/Bottom") angle = -body;
                else if (j.path == "Body/Shoulder_F") angle = j.angle + (chase ? 6f : 7f) * wave + (chase ? 3f : 0);
                else if (j.path.EndsWith("Forearm_F")) angle = j.angle + (chase ? 4f : 4f) * (1 - wave) * .5f;
                else if (j.path.EndsWith("Hand_F")) angle = -3 * Mathf.Sin(phase - .3f);
                else if (j.path == "Body/Shoulder_B") angle = shoulderB;
                else if (j.path.EndsWith("Forearm_B")) angle = elbowB;
                else if (j.path.EndsWith("Hand_B")) angle = -(body + shoulderB + elbowB) + (chase ? -7f : 0) + 2 * Mathf.Sin(phase - .35f);
                else if (j.path.Contains("/Thigh_"))
                {
                    bool front = j.path.Contains("/Thigh_F");
                    float legPhase = phase + (front ? 0 : Mathf.PI);
                    float thigh = (chase ? 20f : 15f) * Mathf.Cos(legPhase);
                    float knee = -(chase ? 27f : 22f) * Mathf.Pow(Mathf.Max(0, Mathf.Sin(legPhase)), 2);
                    if (j.path.EndsWith(front ? "Thigh_F" : "Thigh_B")) angle = thigh;
                    else if (j.path.EndsWith(front ? "Calf_F" : "Calf_B")) angle = knee;
                    else angle = -thigh - knee + (chase ? 5f : 4f) * Mathf.Max(0, Mathf.Sin(legPhase));
                }
                float t = k * duration / samples;
                x[k] = new Keyframe(t, j.x); y[k] = new Keyframe(t, py); z[k] = new Keyframe(t, angle);
            }
            Put(clip, j.path, "m_LocalPosition.x", x); Put(clip, j.path, "m_LocalPosition.y", y);
            Put(clip, j.path, "m_LocalPosition.z", new[] { new Keyframe(0, 0), new Keyframe(duration, 0) });
            Put(clip, j.path, "localEulerAnglesRaw.z", z);
            foreach (string axis in new[] { "x", "y" }) Put(clip, j.path, "localEulerAnglesRaw." + axis, new[] { new Keyframe(0, 0), new Keyframe(duration, 0) });
            foreach (string axis in new[] { "x", "y", "z" }) Put(clip, j.path, "m_LocalScale." + axis, new[] { new Keyframe(0, 1), new Keyframe(duration, 1) });
        }
        GroundFeet(clip, chase, duration);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true; settings.loopBlend = false; settings.startTime = 0; settings.stopTime = duration;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        return clip;
    }
    // 발 피벗의 높이를 기준으로 접지를 맞춥니다. 루트 이동/방향에는 키를 쓰지 않습니다.
    static void GroundFeet(AnimationClip clip, bool chase, float duration)
    {
        const int count = 144;
        const string bottom = "Body/Bottom";
        string[] thighs = { bottom + "/Thigh_F", bottom + "/Thigh_B" };
        Vector2[] upper = { new Vector2(-.17f, -.88f), new Vector2(.57f, -.75f) };
        Vector2[] lower = { new Vector2(-.53f, -.845f), new Vector2(-.14f, -.91f) };
        // 사용자가 제공한 기본 자세에서 각 발의 기준 높이를 계산합니다.
        Vector2 pelvisRest = new Vector2(0, -.001f) + Rotate(new Vector2(-.2f, -.299f), 20f);
        float[] ground = { pelvisRest.y - .15f - .88f - .845f, pelvisRest.y - .215f - .75f - .91f };
        var bx = new Keyframe[count + 1]; var by = new Keyframe[count + 1];
        var angles = new Keyframe[2, 3][];
        for (int side = 0; side < 2; side++) for (int joint = 0; joint < 3; joint++) angles[side, joint] = new Keyframe[count + 1];
        for (int k = 0; k <= count; k++)
        {
            float time = k * duration / count;
            float phase = k == count ? 0f : -k / (float)count * Mathf.PI * 2;
            Vector2[] feet = new Vector2[2]; Vector2[] hips = new Vector2[2];
            float[] targets = new float[2];
            for (int side = 0; side < 2; side++)
            {
                string calf = thighs[side] + (side == 0 ? "/Calf_F" : "/Calf_B");
                string foot = calf + (side == 0 ? "/Foot_F" : "/Foot_B");
                feet[side] = WorldPosition(clip, foot, time);
                hips[side] = WorldPosition(clip, thighs[side], time);
                float lift = Mathf.Max(0, Mathf.Sin(phase + (side == 0 ? 0 : Mathf.PI)));
                targets[side] = ground[side] + (chase ? .16f : .10f) * lift * lift;
            }
            // 지지발 전환을 연속된 가중치로 연결해 골반 위치가 갑자기 바뀌지 않게 합니다.
            float frontWeight = .5f - .5f * Mathf.Sin(phase);
            float dy = Mathf.Lerp(targets[1] - feet[1].y, targets[0] - feet[0].y, frontWeight);
            for (int side = 0; side < 2; side++)
            {
                float length = upper[side].magnitude + lower[side].magnitude - .002f;
                dy = Mathf.Min(dy, targets[side] + length - hips[side].y);
            }
            float bodyAngle = CurveValue(clip, "Body", "localEulerAnglesRaw.z", time);
            Vector2 local = Position(clip, bottom, time) + Rotate(new Vector2(0, dy), -bodyAngle);
            bx[k] = new Keyframe(time, local.x); by[k] = new Keyframe(time, local.y);
            float parentAngle = bodyAngle + CurveValue(clip, bottom, "localEulerAnglesRaw.z", time);
            for (int side = 0; side < 2; side++)
            {
                string calf = thighs[side] + (side == 0 ? "/Calf_F" : "/Calf_B");
                string foot = calf + (side == 0 ? "/Foot_F" : "/Foot_B");
                Vector2 delta = new Vector2(feet[side].x - hips[side].x, targets[side] - hips[side].y - dy);
                float l1 = upper[side].magnitude, l2 = lower[side].magnitude;
                float reach = l1 + l2 - .001f;
                float maxX = Mathf.Sqrt(Mathf.Max(0, reach * reach - delta.y * delta.y));
                delta.x = Mathf.Clamp(delta.x, -maxX, maxX);
                delta = Rotate(delta, -parentAngle);
                float bend = Mathf.Acos(Mathf.Clamp((delta.sqrMagnitude - l1 * l1 - l2 * l2) / (2 * l1 * l2), -1, 1)) * Mathf.Rad2Deg;
                float oldThigh = CurveValue(clip, thighs[side], "localEulerAnglesRaw.z", time);
                float oldCalf = CurveValue(clip, calf, "localEulerAnglesRaw.z", time);
                float best = float.PositiveInfinity, bestThigh = 0, bestCalf = 0;
                foreach (float sign in new[] { -1f, 1f })
                {
                    float b = bend * sign * Mathf.Deg2Rad;
                    float thigh = (Mathf.Atan2(delta.y, delta.x) - Mathf.Atan2(l2 * Mathf.Sin(b), l1 + l2 * Mathf.Cos(b))
                        - Mathf.Atan2(upper[side].y, upper[side].x)) * Mathf.Rad2Deg;
                    float calfAngle = bend * sign + (Mathf.Atan2(upper[side].y, upper[side].x) - Mathf.Atan2(lower[side].y, lower[side].x)) * Mathf.Rad2Deg;
                    thigh = oldThigh + Mathf.DeltaAngle(oldThigh, thigh);
                    calfAngle = oldCalf + Mathf.DeltaAngle(oldCalf, calfAngle);
                    // 기본 다리의 무릎 방향을 주기 내내 유지합니다.
                    float preferredSign = -1f;
                    float score = Mathf.Abs(thigh - oldThigh) + Mathf.Abs(calfAngle - oldCalf) + (sign == preferredSign ? 0 : 1000);
                    if (score < best) { best = score; bestThigh = thigh; bestCalf = calfAngle; }
                }
                float oldFoot = CurveValue(clip, foot, "localEulerAnglesRaw.z", time);
                angles[side, 0][k] = new Keyframe(time, bestThigh);
                angles[side, 1][k] = new Keyframe(time, bestCalf);
                angles[side, 2][k] = new Keyframe(time, oldFoot + oldThigh + oldCalf - bestThigh - bestCalf);
            }
        }
        Put(clip, bottom, "m_LocalPosition.x", bx); Put(clip, bottom, "m_LocalPosition.y", by);
        for (int side = 0; side < 2; side++)
        {
            string calf = thighs[side] + (side == 0 ? "/Calf_F" : "/Calf_B");
            string foot = calf + (side == 0 ? "/Foot_F" : "/Foot_B");
            Put(clip, thighs[side], "localEulerAnglesRaw.z", angles[side, 0]);
            Put(clip, calf, "localEulerAnglesRaw.z", angles[side, 1]);
            Put(clip, foot, "localEulerAnglesRaw.z", angles[side, 2]);
        }
    }
    static Vector2 Rotate(Vector2 p, float degrees)
    {
        float r = degrees * Mathf.Deg2Rad;
        return new Vector2(p.x * Mathf.Cos(r) - p.y * Mathf.Sin(r), p.x * Mathf.Sin(r) + p.y * Mathf.Cos(r));
    }
    static float CurveValue(AnimationClip clip, string path, string property, float time)
        => AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property)).Evaluate(time);
    static Vector2 Position(AnimationClip clip, string path, float time)
        => new Vector2(CurveValue(clip, path, "m_LocalPosition.x", time), CurveValue(clip, path, "m_LocalPosition.y", time));
    static Vector2 WorldPosition(AnimationClip clip, string path, float time)
    {
        Vector2 result = Vector2.zero; float angle = 0; string current = "";
        foreach (string part in path.Split('/'))
        {
            current = current.Length == 0 ? part : current + "/" + part;
            result += Rotate(Position(clip, current, time), angle);
            angle += CurveValue(clip, current, "localEulerAnglesRaw.z", time);
        }
        return result;
    }
    static void Put(AnimationClip clip, string path, string property, Keyframe[] keys)
    {
        var curve = new AnimationCurve(keys);
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
        }
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);
    }
    static void Save(AnimationClip clip, string path)
    {
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing == null) AssetDatabase.CreateAsset(clip, path);
        else { EditorUtility.CopySerialized(clip, existing); EditorUtility.SetDirty(existing); UnityEngine.Object.DestroyImmediate(clip); }
    }
}
#endif
