#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// R9 2026-10-18: rear fist, front-hand sword transfer, clock buff, unified pull/dash and rear-knee drive.
// Rest transforms, Sprite pivots and scale are read from the target prefab on each generation.
public static class Stage5BossAnimationBuilder
{
    const int Body = 0, Head = 1, SF = 2, AF = 3, HF = 4, SB = 5, AB = 6, HB = 7, Weapon = 8,
        Bottom = 9, TF = 10, CF = 11, FF = 12, TB = 13, CB = 14, FB = 15, Cape = 16;
    static readonly int[] Parent = { -1, 0, 0, 2, 3, 0, 5, 6, 7, 0, 9, 10, 11, 9, 13, 14, 0 };
    const string Tag = "Stage5Generated_";
    const float FPS = 12f;

    sealed class Pose
    {
        public Vector3[] pos, scale;
        public float[] rot;
        public Pose Clone() => new Pose { pos = (Vector3[])pos.Clone(), scale = (Vector3[])scale.Clone(), rot = (float[])rot.Clone() };
    }
    sealed class Key
    {
        public float frame;
        public Pose pose;
        public Key(float frame, Pose pose) { this.frame = frame; this.pose = pose; }
    }
    sealed class Signal
    {
        public float frame;
        public string name;
        public Signal(float frame, string name) { this.frame = frame; this.name = name; }
    }
    sealed class Motion
    {
        public string name;
        public int frames;
        public bool loop;
        public Func<float, Pose> sample;
        public List<Signal> signals = new List<Signal>();
        public Motion(string name, int frames, bool loop, Func<float, Pose> sample)
        { this.name = name; this.frames = frames; this.loop = loop; this.sample = sample; }
        public Motion Cue(float frame, string cue) { signals.Add(new Signal(frame, cue)); return this; }
    }
    sealed class Rig : IDisposable
    {
        public bool main;
        public string name, prefix, folder, prefabPath;
        public GameObject contents;
        public Transform visual;
        public string[] paths;
        public Pose rest;
        public Vector2[][] vertices;
        public float floor;

        public Rig(bool main)
        {
            this.main = main;
            name = main ? "MainBoss 5" : "MidBoss 5";
            prefix = main ? "MainBoss5_" : "MidBoss5_";
            folder = "Assets/Animations/5/" + name;
            prefabPath = FindPrefab(name);
            contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var roots = contents.GetComponentsInChildren<Transform>(true)
                    .Where(t => t.name == name && t.Find("Body") != null).ToArray();
                if (roots.Length != 1) throw new InvalidOperationException(name + ": Body를 가진 시각 루트가 정확히 하나 필요합니다.");
                visual = roots[0];
                paths = new[] {
                    "Body","Body/Head","Body/Shoulder_F","Body/Shoulder_F/Forearm_F",
                    "Body/Shoulder_F/Forearm_F/Hand_F","Body/Shoulder_B",
                    "Body/Shoulder_B/Forearm_B","Body/Shoulder_B/Forearm_B/Hand_B",
                    "Body/Shoulder_B/Forearm_B/Hand_B/"+(main?"Sword":"Weapon"),
                    "Body/Bottom","Body/Bottom/Thigh_F","Body/Bottom/Thigh_F/Calf_F",
                    "Body/Bottom/Thigh_F/Calf_F/Foot_F","Body/Bottom/Thigh_B",
                    "Body/Bottom/Thigh_B/Calf_B","Body/Bottom/Thigh_B/Calf_B/Foot_B" };
                if (main) paths = paths.Concat(new[] { "Body/Cape" }).ToArray();
                int partCount = paths.Length;
                rest = new Pose { pos = new Vector3[partCount], scale = new Vector3[partCount], rot = new float[partCount] };
                vertices = new Vector2[partCount][];
                for (int i = 0; i < partCount; i++)
                {
                    Transform t = visual.Find(paths[i]);
                    if (t == null) throw new InvalidOperationException("파츠가 없습니다: " + paths[i]);
                    rest.pos[i] = t.localPosition;
                    rest.scale[i] = t.localScale;
                    rest.rot[i] = Mathf.DeltaAngle(0, t.localEulerAngles.z);
                    var renderer = t.GetComponent<SpriteRenderer>();
                    if (renderer != null && renderer.sprite != null) vertices[i] = renderer.sprite.vertices;
                    if (renderer != null && (renderer.flipX || renderer.flipY))
                        throw new InvalidOperationException("파츠 Flip 대신 루트 반전을 사용하세요: " + paths[i]);
                }
                if (vertices[Body] == null || vertices[FF] == null || vertices[FB] == null)
                    throw new InvalidOperationException(name + ": Body와 양발의 Sprite 참조가 필요합니다.");
                if (main)
                {
                    rest.pos[Cape] = new Vector3(-.125f, 1.313f, 0);
                    rest.rot[Cape] = 0;
                    Transform cape = visual.Find(paths[Cape]);
                    cape.localPosition = rest.pos[Cape]; cape.localRotation = Quaternion.identity;
                    // User rest revision: 2026-10-17. Keep Sprite pivots and existing scale.
                    Vector3[] lower ={new Vector3(-.084f,-.465f,0),new Vector3(-.100f,.414f,0),
                        new Vector3(-.259f,-1.146f,0),new Vector3(-.233f,-1.112f,0),
                        new Vector3(.172f,.433f,0),new Vector3(.234f,-1.148f,0),new Vector3(-.249f,-1.128f,0)};
                    for (int i = Bottom; i <= FB; i++)
                    {
                        rest.pos[i] = lower[i - Bottom]; rest.rot[i] = 0;
                        Transform t = visual.Find(paths[i]); t.localPosition = rest.pos[i]; t.localRotation = Quaternion.identity;
                    }
                }
                floor = Lowest(rest);
            }
            catch { PrefabUtility.UnloadPrefabContents(contents); contents = null; throw; }
        }
        public void Dispose() { if (contents != null) PrefabUtility.UnloadPrefabContents(contents); }
        public Pose Rest() => rest.Clone();
        public Matrix4x4 Matrix(Pose p, int i)
        {
            Matrix4x4 m = Matrix4x4.TRS(p.pos[i], Quaternion.Euler(0, 0, p.rot[i]), p.scale[i]);
            return Parent[i] < 0 ? m : Matrix(p, Parent[i]) * m;
        }
        public Vector2 World(Pose p, int i) => Matrix(p, i).MultiplyPoint3x4(Vector3.zero);
        public float Angle(Pose p, int i) => p.rot[i] + (Parent[i] < 0 ? 0 : Angle(p, Parent[i]));
        public float Lowest(Pose p)
        {
            float low = float.PositiveInfinity;
            // Cape is cloth, not a contact point: do not lift the body to clear its hem.
            for (int i = 0; i <= FB; i++)
            {
                if (i == Weapon || vertices[i] == null) continue;
                Matrix4x4 m = Matrix(p, i);
                foreach (Vector2 v in vertices[i])
                    low = Mathf.Min(low, m.MultiplyPoint3x4(new Vector3(v.x, v.y, 0)).y);
            }
            return low;
        }
        public void Ground(Pose p) { p.pos[Body].y += floor - Lowest(p); }
        // Standing upper-body actions leave the legs at rest, with pelvis compensation.
        public void Stand(Pose p)
        {
            Vector2 target = World(rest, Bottom);
            Vector3 local = Matrix(p, Body).inverse.MultiplyPoint3x4(new Vector3(target.x, target.y, 0));
            p.pos[Bottom] = new Vector3(local.x, local.y, rest.pos[Bottom].z);
            p.rot[Bottom] = rest.rot[Bottom] + rest.rot[Body] - p.rot[Body];
            for (int i = TF; i <= FB; i++) { p.pos[i] = rest.pos[i]; p.rot[i] = rest.rot[i]; }
        }
        // Two joints rotate toward a foot target; bone offsets and scales remain intact.
        public void Leg(Pose p, bool front, Vector2 target, float worldFootAngle = 0)
        {
            int thigh = front ? TF : TB, calf = front ? CF : CB, foot = front ? FF : FB;
            p.rot[thigh] = rest.rot[thigh] + 22;
            p.rot[calf] = rest.rot[calf] - 38;
            for (int iteration = 0; iteration < 18; iteration++)
            {
                foreach (int joint in new[] { calf, thigh })
                {
                    Vector2 origin = World(p, joint), tip = World(p, foot);
                    float delta = Vector2.SignedAngle(tip - origin, target - origin);
                    p.rot[joint] += Mathf.Clamp(delta, -20, 20);
                }
                if ((World(p, foot) - target).sqrMagnitude < 0.000001f) break;
            }
            p.rot[foot] = worldFootAngle - Angle(p, calf);
        }
        // CCD keeps authored bone positions/scales; only shoulder and forearm rotate.
        public void HandAt(Pose p, bool front, Vector2 target)
        {
            int shoulder = front ? SF : SB, forearm = front ? AF : AB, hand = front ? HF : HB;
            for (int iteration = 0; iteration < 28; iteration++)
            {
                foreach (int joint in new[] { forearm, shoulder })
                {
                    Vector2 origin = World(p, joint), tip = World(p, hand);
                    float handed = Matrix(p, Parent[joint]).determinant < 0 ? -1 : 1;
                    p.rot[joint] += handed * Mathf.Clamp(Vector2.SignedAngle(tip - origin, target - origin), -12, 12);
                }
                if ((World(p, hand) - target).sqrMagnitude < .00001f) break;
            }
        }
        public void AxisAt(Pose p, int part, Vector2 worldDirection, Vector2 localAxis)
        {
            Vector3 direction = Matrix(p, Parent[part]).inverse.MultiplyVector(worldDirection);
            Vector2 scaled = Vector2.Scale(localAxis, new Vector2(p.scale[part].x, p.scale[part].y));
            float target = Vector2.SignedAngle(scaled, new Vector2(direction.x, direction.y));
            p.rot[part] += Mathf.DeltaAngle(p.rot[part], target);
        }
        public void BladeAt(Pose p, Vector2 direction)
        { AxisAt(p, Weapon, direction, Vector2.up); }
        public void BladeToFloor(Pose p)
        {
            if (vertices[Weapon] == null) throw new InvalidOperationException("칼 Sprite가 필요합니다.");
            // Blade extends toward sprite +Y. Solve hand pitch first, then the small blade correction.
            float gripY = World(p, Weapon).y;
            float reach = vertices[Weapon].Max(v => v.y) * Mathf.Abs(p.scale[Weapon].y);
            if (reach < .01f) return;
            float down = Mathf.Clamp((gripY - floor) / reach, .05f, .98f);
            Vector2 dir = new Vector2(Mathf.Sqrt(1 - down * down), -down);
            float before = p.rot[Weapon]; BladeAt(p, dir);
            float correction = Mathf.DeltaAngle(before, p.rot[Weapon]);
            p.rot[Weapon] = before; p.rot[HB] += Mathf.Clamp(correction * .55f, -40, 40);
            // Search the forward/downward blade angle against actual transformed sprite vertices.
            float low = 0, high = 89.9f;
            for (int k = 0; k < 24; k++)
            {
                float a = (low + high) * .5f * Mathf.Deg2Rad;
                BladeAt(p, new Vector2(Mathf.Cos(a), -Mathf.Sin(a)));
                Matrix4x4 m = Matrix(p, Weapon);
                float y = vertices[Weapon].Min(v => m.MultiplyPoint3x4(v).y);
                if (y > floor) low = a * Mathf.Rad2Deg; else high = a * Mathf.Rad2Deg;
            }
            float angle = low * Mathf.Deg2Rad; BladeAt(p, new Vector2(Mathf.Cos(angle), -Mathf.Sin(angle)));
        }
        public void WeaponAt(Pose p, Vector2 rootTarget, float worldAngle)
        {
            Vector3 local = Matrix(p, HB).inverse.MultiplyPoint3x4(new Vector3(rootTarget.x, rootTarget.y, 0));
            p.pos[Weapon] = new Vector3(local.x, local.y, rest.pos[Weapon].z);
            p.rot[Weapon] = worldAngle - Angle(p, HB);
        }
    }

    static float S(float x) { x = Mathf.Clamp01(x); return x * x * (3 - 2 * x); }
    static Pose Mix(Pose a, Pose b, float u)
    {
        Pose p = a.Clone(); u = S(u);
        for (int i = 0; i < a.pos.Length; i++)
        {
            p.pos[i] = Vector3.LerpUnclamped(a.pos[i], b.pos[i], u);
            p.scale[i] = Vector3.LerpUnclamped(a.scale[i], b.scale[i], u);
            p.rot[i] = Mathf.LerpUnclamped(a.rot[i], b.rot[i], u);
        }
        return p;
    }
    static Pose Track(float frame, params Key[] keys)
    {
        for (int i = 1; i < keys.Length; i++)
            if (frame <= keys[i].frame)
                return Mix(keys[i - 1].pose, keys[i].pose, (frame - keys[i - 1].frame) / (keys[i].frame - keys[i - 1].frame));
        return keys[keys.Length - 1].pose.Clone();
    }
    static Pose Arms(Rig r, float body, float sb, float ab, float hb, float sf, float af, float hf)
    {
        Pose p = r.Rest();
        p.rot[Body] += body; p.rot[Head] -= body * .65f;
        p.rot[SB] += sb; p.rot[AB] += ab; p.rot[HB] += hb;
        p.rot[SF] += sf; p.rot[AF] += af; p.rot[HF] += hf;
        r.Stand(p);
        return p;
    }
    static Pose Charge(Rig r) => Arms(r, 5, 135, -22, 12, -18, 32, 10);
    static Pose Thrust(Rig r)
    {
        Pose p = Arms(r, -14, 88, -7, -10, -22, 28, 8);
        ForwardStance(r, p, false);
        r.BladeAt(p, Vector2.right);
        return p;
    }
    static Pose Slam(Rig r)
    {
        Pose p = Arms(r, -22, 55, 12, -12, -28, 38, 8);
        p.pos[Body].x += .10f; r.Stand(p); return p;
    }
    static Pose Command(Rig r) => Arms(r, -3, 65, 25, -8, 72, -12, 8);
    static Pose Idle(Rig r, float f)
    {
        Pose p = r.Rest(); float breath = (1 - Mathf.Cos(f / 36 * Mathf.PI * 2)) * .5f;
        p.pos[Body].y -= .012f * breath; p.rot[Head] += .5f * breath;
        p.rot[AF] += 1.2f * breath; p.rot[AB] -= .8f * breath;
        r.Stand(p); return p;
    }
    static Pose Move(Rig r, float f, int frames, bool run)
    {
        Pose p = r.Rest(); float phase = f / frames, w = Mathf.Sin(phase * Mathf.PI * 2);
        p.rot[Body] += (run ? -10 : -3) + (run ? 1.8f : .7f) * w;
        p.rot[Head] -= p.rot[Body] * .65f;
        p.pos[Body].y -= (run ? .09f : .045f) * (1 - Mathf.Cos(phase * Mathf.PI * 4)) * .5f;
        p.rot[Bottom] = -p.rot[Body] * .85f;
        p.rot[SF] += (run ? -8 : 0) + (run ? 18 : 9) * w; p.rot[AF] += run ? 22 : 8;
        p.rot[SB] += (run ? 12 : 4) - (run ? 9 : 5) * w; p.rot[AB] += run ? 20 : 8;
        p.rot[HB] -= 5;
        // Keep the authored weapon grip; do not spin the blade during locomotion.
        for (int side = 0; side < 2; side++)
        {
            bool front = side == 0; int foot = front ? FF : FB;
            float u = Mathf.Repeat(phase + side * .5f, 1);
            float stride = run ? .32f : .18f;
            float x = u < .5f ? Mathf.Lerp(stride, -stride, S(u * 2)) : Mathf.Lerp(-stride, stride, S((u - .5f) * 2));
            float lift = u < .5f ? 0 : (run ? .20f : .10f) * Mathf.Pow(Mathf.Sin((u - .5f) * 2 * Mathf.PI), 2);
            Vector2 target = r.World(r.rest, foot) + new Vector2(x, lift);
            r.Leg(p, front, target, u > .68f && u < .94f ? 10 * Mathf.Sin((u - .68f) / .26f * Mathf.PI) : 0);
        }
        return p;
    }
    static Pose Death(Rig r, float f)
    {
        Pose rest = r.Rest(), kneel = Arms(r, -7, 12, 55, 10, 8, 50, 8);
        kneel.pos[Body].y -= r.main ? .85f : .60f;
        kneel.rot[Bottom] = 0; kneel.rot[TF] = 68; kneel.rot[CF] = -115; kneel.rot[FF] = 47;
        kneel.rot[TB] = 64; kneel.rot[CB] = -108; kneel.rot[FB] = 44;
        Pose fall = kneel.Clone(); fall.rot[Body] = -88; fall.rot[Head] = 8;
        fall.rot[SF] = 55; fall.rot[AF] = 35; fall.rot[SB] = 25; fall.rot[AB] = 65;
        fall.rot[Bottom] = 0; fall.rot[TF] = 0; fall.rot[CF] = 0; fall.rot[FF] = 0;
        fall.rot[TB] = 0; fall.rot[CB] = 0; fall.rot[FB] = 0;
        fall.pos[Body].x += .38f;
        Pose p = Track(f, new Key(0, rest), new Key(10, kneel), new Key(14, kneel), new Key(24, fall), new Key(30, fall));
        r.Ground(p); return p;
    }

    static List<Motion> Motions(Rig r)
    {
        var list = new List<Motion> {
            new Motion("Idle",36,true,f=>Idle(r,f)),
            new Motion("Walk",16,true,f=>Move(r,f,16,false)),
            new Motion("Run",r.main?10:8,true,f=>Move(r,f,r.main?10:8,true)),
            new Motion("Death",30,false,f=>Death(r,f))
        };
        if (r.main) MainMotions(r, list); else MidMotions(r, list);
        return list;
    }

    // Upper body floats relative to the pelvis: preserve the pelvis world pose exactly.
    static void FloatTorso(Rig r, Motion m, float f, Pose p)
    {
        if (m.name == "Death") return;
        Vector2 pelvis = r.World(p, Bottom); float angle = r.Angle(p, Bottom);
        float envelope = m.loop ? 1 : S(f / 3) * S((m.frames - f) / 3);
        float wave = Mathf.Sin(f / (m.loop ? m.frames : 24f) * Mathf.PI * 2);
        // Contact frames keep the sword tip stable.
        if (m.name == "Attack") envelope *= 1 - S((f - 28) / 3);
        if (m.name == "skill2") envelope *= 1 - S((f - 20) / 4);
        p.pos[Body].y += .035f * wave * envelope;
        p.pos[Body].x += .012f * (1 - Mathf.Cos(f / (m.loop ? m.frames : 24f) * Mathf.PI * 2)) * envelope;
        p.rot[Body] += .65f * wave * envelope;
        Vector3 local = r.Matrix(p, Body).inverse.MultiplyPoint3x4(pelvis);
        p.pos[Bottom] = new Vector3(local.x, local.y, p.pos[Bottom].z);
        p.rot[Bottom] = angle - r.Angle(p, Body);
    }
    static Pose LowStance(Rig r, Pose p, float lower)
    {
        p.pos[Body].y -= lower; p.rot[Bottom] = -p.rot[Body] * .8f;
        r.Leg(p, true, r.World(r.rest, FF) + new Vector2(.10f, 0));
        r.Leg(p, false, r.World(r.rest, FB) + new Vector2(-.10f, 0));
        r.Ground(p); return p;
    }
    // Front leg trails slightly; rear knee carries the forward weight.
    static void ForwardStance(Rig r, Pose p, bool airborne)
    {
        p.pos[Body].x += .06f;
        p.pos[Body].y -= airborne ? .08f : .18f;
        p.pos[Bottom] = r.rest.pos[Bottom];
        p.rot[Bottom] = -p.rot[Body] * .65f;
        if (airborne)
        {
            p.rot[TF] = -16; p.rot[CF] = -12; p.rot[FF] = 10;
            p.rot[TB] = 42; p.rot[CB] = -68; p.rot[FB] = 18;
        }
        else
        {
            r.Leg(p, true, r.World(r.rest, FF) + new Vector2(-.32f, 0));
            r.Leg(p, false, r.World(r.rest, FB) + new Vector2(.34f, 0));
            r.Ground(p);
        }
    }
    static void MatchFrontGrip(Rig r, Pose p)
    {
        for (int i = 0; i < 5; i++)
        {
            Vector2 offset = r.Matrix(p, HF).MultiplyVector(new Vector3(.04f, -.14f, 0));
            r.HandAt(p, true, r.World(p, Weapon) - offset);
        }
    }
    static void FrontHandSword(Rig r, Pose p, float f)
    {
        // Keep the actual hierarchy under Hand_B. Bake the front-hand attachment into its local curve.
        float hold = S((f - 9) / 1f) * (1 - S((f - 21) / 1f));
        if (hold <= 0) return;
        Vector2 original = r.World(p, Weapon);
        Vector2 frontGrip = r.Matrix(p, HF).MultiplyPoint3x4(new Vector3(.04f, -.14f, 0));
        float originalAngle = r.Angle(p, Weapon);
        r.WeaponAt(p, Vector2.Lerp(original, frontGrip, hold), Mathf.Lerp(originalAngle, -90, hold));
    }
    static Pose RaisedSword(Rig r)
    {
        if (!r.main)
        {
            Pose raised = Arms(r, 0, 0, 0, 0, -12, 20, 5);
            raised.pos[SB] = new Vector3(.442f, .713f, 0); raised.rot[SB] = 150;
            raised.pos[AB] = new Vector3(.063f, -.575f, 0); raised.rot[AB] = 30;
            raised.pos[HB] = new Vector3(.069f, -.601f, 0); raised.rot[HB] = 0;
            raised.pos[Weapon] = new Vector3(.100f, -.213f, 0); raised.rot[Weapon] = -90;
            return raised;
        }
        Pose p = Arms(r, 4, 140, -25, 0, -20, 25, 8);
        r.HandAt(p, false, r.World(r.rest, Head) + new Vector2(.05f, .30f));
        r.BladeAt(p, Vector2.left); return p;
    }
    static Pose GroundSlam(Rig r)
    {
        Pose p = Arms(r, -18, 60, 12, -10, -30, 25, 5);
        ForwardStance(r, p, false);
        if (!r.main) p.scale[Weapon].y = 1.5f;
        r.HandAt(p, false, new Vector2(.95f, r.floor + (r.main ? 1.50f : 1.15f)));
        r.BladeToFloor(p); return p;
    }
    static Pose Summon(Rig r)
    {
        if (!r.main) return RaisedSword(r);
        Pose p = Arms(r, 0, 150, -10, 0, -8, 12, 4);
        r.HandAt(p, false, r.World(r.rest, Head) + new Vector2(.10f, .40f));
        r.BladeAt(p, Vector2.right); return p;
    }
    // One blade, alternating thrusts and cuts. All variants use identical damage timing.
    static Pose FlyingSword(Rig r, float f, bool main, int variant)
    {
        Pose summon = Summon(r), rest = r.Rest(); float end = main ? 60 : 48, total = main ? 66 : 54;
        float ready = main ? 12 : 9;
        if (f < ready) return Mix(rest, summon, f / ready);
        Pose p;
        if (f >= end) p = Mix(summon, rest, (f - end) / (total - end));
        else if (main)
        {
            // One physical footfall every six frames, matching the eight runtime steps.
            p = Move(r, f - 12, 12, false);
            p = Mix(summon, p, S((f - 12) / 3) * (1 - S((f - (end - 3)) / 3)));
        }
        else p = summon.Clone();
        float release = S((f - (main ? 12 : 9)) / (main ? 1.5f : 3f)) * (1 - S((f - (end - 3)) / 3));
        if (release <= 0) return p;
        float period = main ? 6 : 7.2f, phase = Mathf.Repeat(Mathf.Max(0, f - 12) / period, 1);
        float beat = Mathf.Sin(phase * Mathf.PI * 2), height = variant == 0 ? .55f : variant == 1 ? 1.05f : .25f;
        // Main blade orbits its owner, mid blade stays in the forward square attack area.
        Vector2 center = main ? new Vector2(.35f, .65f) : new Vector2(1.85f, .55f);
        Vector2 target; float angle;
        if (variant == 0)
        { target = center + new Vector2((main ? 1.55f : .65f) * Mathf.Cos(phase * Mathf.PI * 2), .45f * beat); angle = -90 + 70 * beat; }
        else if (variant == 1)
        { target = center + new Vector2((main ? 1.3f : .60f) * beat, .70f * Mathf.Cos(phase * Mathf.PI * 2)); angle = -45 + 110 * beat; }
        else
        { target = center + new Vector2((main ? 1.6f : .7f) * beat, .35f * Mathf.Sin(phase * Mathf.PI * 4)); angle = -120 + 90 * Mathf.Sin(phase * Mathf.PI * 4); }
        target.y += height - .55f;
        Vector2 grip = r.World(p, Weapon);
        r.WeaponAt(p, Vector2.Lerp(grip, target, release), Mathf.Lerp(r.Angle(p, Weapon), angle, release));
        return p;
    }
    static Pose MidFlyingSword(Rig r, float f, int variant)
    {
        Pose rest = r.Rest(), raised = RaisedSword(r);
        if (f < 5) return Mix(rest, raised, f / 5);
        Pose p = f < 48 ? raised.Clone() : Mix(raised, rest, (f - 48) / 6);
        Vector2 grip = r.World(p, Weapon), hover = grip + Vector2.up * 1.10f;
        float hoverAngle = r.Angle(raised, Weapon);
        if (f < 10)
        {
            r.WeaponAt(p, Vector2.Lerp(grip, hover, S((f - 5) / 5)), hoverAngle);
            return p;
        }
        // The target remains in the authored forward square; variants vary breadth, not order.
        float width = 1f + variant * .06f;
        Vector2 center = new Vector2(3.20f, .05f + variant * .04f);
        Vector2[] before ={center+new Vector2(.65f,.65f)*width,
            center+new Vector2(.20f,-.55f)*width,
            center+new Vector2(-.65f,-.55f)*width,
            center+new Vector2(-.15f,.70f)*width,
            center+new Vector2(-.65f,0)};
        Vector2[] after ={center+new Vector2(-.45f,-.45f)*width,
            center+new Vector2(-.05f,.60f)*width,
            center+new Vector2(.55f,.65f)*width,
            center+new Vector2(.20f,-.55f)*width,
            center+new Vector2(.65f,0)};
        // Sprite +Y is the blade tip. Angles describe upper-right -> lower-left etc.
        for (int i = 0; i < 5; i++)
        {
            Vector2 middle = (before[i] + after[i]) * .5f;
            Vector2 half = (after[i] - before[i]) * .5f * (i == 0 || i == 2 || i == 4 ? 3f : 1.6f);
            before[i] = middle - half; after[i] = middle + half;
        }
        // Keep the third thrust's preparation point forward and above the lower-left edge.
        before[2] += new Vector2(1.20f, .65f); after[2] += new Vector2(1.20f, .65f);
        float[] readyAngle = { 135, -165, -45, -15, -90 };
        float[] hitAngle = { 135, -15, -45, -165, -90 };
        // Use the weapon sprite center at frame 12 as the enemy-position anchor.
        Vector2 bladeTip = r.vertices[Weapon].OrderByDescending(v => v.y).First();
        Vector2 min = new Vector2(r.vertices[Weapon].Min(v => v.x), r.vertices[Weapon].Min(v => v.y));
        Vector2 max = new Vector2(r.vertices[Weapon].Max(v => v.x), r.vertices[Weapon].Max(v => v.y));
        Vector2 spriteCenter = (min + max) * .5f;
        for (int i = 0; i < 5; i++)
        {
            Pose reference = p.Clone(); r.WeaponAt(reference, Vector2.zero, readyAngle[i]);
            Vector2 tipOffset = r.Matrix(reference, Weapon).MultiplyVector(bladeTip);
            Vector2 pathCenterOffset = r.Matrix(reference, Weapon).MultiplyVector(spriteCenter);
            before[i] += pathCenterOffset - tipOffset;
            r.WeaponAt(reference, Vector2.zero, hitAngle[i]);
            tipOffset = r.Matrix(reference, Weapon).MultiplyVector(bladeTip);
            pathCenterOffset = r.Matrix(reference, Weapon).MultiplyVector(spriteCenter);
            after[i] += pathCenterOffset - tipOffset;
        }
        // The first impact's sprite center is the enemy point. Tighten the full
        // sequence around it, and bring the high frame-11 windup down noticeably.
        Vector2 enemyPoint = after[0];
        for (int i = 0; i < 5; i++)
        {
            float reach = i == 0 ? .65f : .90f;
            before[i] = Vector2.Lerp(enemyPoint, before[i], reach);
            if (i > 0) after[i] = Vector2.Lerp(enemyPoint, after[i], .90f);
        }
        // Place a virtual pivot below the grip along the weapon's local -Y axis.
        // The grip now travels too, producing a wider arc without scaling the blade.
        // At -90 degrees the sprite center still crosses the frame-12 enemy point.
        float slashPivotExtension = (max.y - min.y) * .35f;
        Vector2 virtualPivotLocal = new Vector2(0, -slashPivotExtension);
        Vector2 slashCenterLever = spriteCenter - virtualPivotLocal;
        Pose slashReference = p.Clone(); r.WeaponAt(slashReference, Vector2.zero, -90f);
        Vector2 slashPivot = enemyPoint - (Vector2)r.Matrix(slashReference, Weapon).MultiplyVector(slashCenterLever);
        for (int slashIndex = 1; slashIndex <= 3; slashIndex += 2)
        {
            r.WeaponAt(slashReference, Vector2.zero, readyAngle[slashIndex]);
            before[slashIndex] = slashPivot + (Vector2)r.Matrix(slashReference, Weapon).MultiplyVector(slashCenterLever);
            r.WeaponAt(slashReference, Vector2.zero, hitAngle[slashIndex]);
            after[slashIndex] = slashPivot + (Vector2)r.Matrix(slashReference, Weapon).MultiplyVector(slashCenterLever);
        }
        Pose hoverPose = p.Clone(); r.WeaponAt(hoverPose, hover, hoverAngle);
        Vector2 hoverCenter = hover + (Vector2)r.Matrix(hoverPose, Weapon).MultiplyVector(spriteCenter);
        Vector2 target = hoverCenter; float angle = hoverAngle;
        for (int hit = 0; hit < 5; hit++)
        {
            bool isSlash = hit == 1 || hit == 3;
            float impact = 12 + 7.2f * hit, start = impact - (isSlash ? 1.8f : .6f);
            float sweepEnd = impact + (isSlash ? 1.8f : 0f);
            float holdEnd = isSlash ? sweepEnd + .8f : impact + .6f;
            bool previousWasSlash = hit == 2 || hit == 4;
            float previous = hit == 0 ? 10 : 12 + 7.2f * (hit - 1) + (previousWasSlash ? 2.6f : .6f);
            Vector2 previousPoint = hit == 0 ? hoverCenter : after[hit - 1];
            float previousAngle = hit == 0 ? hoverAngle : hitAngle[hit - 1];
            if (f < start)
            {
                // Set the low/high windup first, then show it briefly before cutting.
                float readyFrame = isSlash ? start - .8f : start;
                float u = S((f - previous) / (readyFrame - previous));
                target = Vector2.Lerp(previousPoint, before[hit], u);
                angle = previousAngle + Mathf.DeltaAngle(previousAngle, readyAngle[hit]) * u; break;
            }
            if (f <= holdEnd)
            {
                float u = S((f - start) / (sweepEnd - start));
                angle = Mathf.Lerp(readyAngle[hit], hitAngle[hit], u);
                target = Vector2.Lerp(before[hit], after[hit], u);
                if (isSlash)
                {
                    Pose arcPose = p.Clone(); r.WeaponAt(arcPose, Vector2.zero, angle);
                    target = slashPivot + (Vector2)r.Matrix(arcPose, Weapon).MultiplyVector(slashCenterLever);
                }
                break;
            }
            target = after[hit]; angle = hitAngle[hit];
        }
        // Return to the still-raised hand before lowering the arm.
        if (f > 41.4f)
        {
            float u = S((f - 41.4f) / 6.6f);
            Vector2 gripCenter = grip + (Vector2)r.Matrix(p, Weapon).MultiplyVector(spriteCenter);
            target = Vector2.Lerp(after[4], gripCenter, u); angle = Mathf.Lerp(hitAngle[4], r.Angle(p, Weapon), u);
        }
        Pose centerPose = p.Clone(); r.WeaponAt(centerPose, Vector2.zero, angle);
        Vector2 finalCenterOffset = r.Matrix(centerPose, Weapon).MultiplyVector(spriteCenter);
        r.WeaponAt(p, target - finalCenterOffset, angle); return p;
    }
    static void AddSwordVariants(Rig r, List<Motion> list)
    {
        for (int v = 0; v < 3; v++)
        {
            int variant = v; string name = v == 0 ? "skill1" : "skill1_V" + (v + 1);
            Motion motion = new Motion(name, r.main ? 66 : 54, false, f => r.main ? FlyingSword(r, f, true, variant) : MidFlyingSword(r, f, variant));
            if (r.main)
            {
                motion.Cue(12, "Skill1.MoveStart").Cue(60, "Skill1.MoveEnd").Cue(66, "Action.End");
                for (int step = 0; step < 8; step++)
                {
                    motion.Cue(12 + step * 6, "Skill1.Step" + (step + 1));
                    for (int hit = 0; hit < 4; hit++) motion.Cue(13.5f + step * 6 + hit * 1.5f, "Skill1.Hit." + step + "." + hit);
                }
            }
            else
            {
                motion.Cue(12, "Skill1.AreaStart").Cue(48, "Skill1.AreaEnd").Cue(54, "Action.End");
                for (int hit = 0; hit < 5; hit++) motion.Cue(12 + hit * 7.2f, "Skill1.Pulse" + (hit + 1));
            }
            list.Add(motion);
        }
    }
    static void MidMotions(Rig r, List<Motion> list)
    {
        Pose rest = r.Rest(), charge = RaisedSword(r), dash = Thrust(r), slam = GroundSlam(r);
        // Attack charging: spread planted feet and bring the raised grip within both arms' reach.
        charge.pos[Body].y -= .32f; charge.pos[Bottom] = r.rest.pos[Bottom];
        r.Leg(charge, true, r.World(rest, FF) + new Vector2(-.38f, 0));
        r.Leg(charge, false, r.World(rest, FB) + new Vector2(.38f, 0)); r.Ground(charge);
        r.HandAt(charge, false, new Vector2(0, 1.30f)); r.BladeAt(charge, Vector2.left);
        charge.rot[SF] = 60; charge.rot[AF] = 45; MatchFrontGrip(r, charge);
        // attack-only deep, two-handed slam; other clips keep their existing poses.
        slam = Arms(r, -32, 60, 12, -10, 35, 35, 5);
        slam.pos[Body] += new Vector3(.12f, -.42f, 0);
        slam.pos[Bottom] = r.rest.pos[Bottom]; slam.rot[Bottom] = 24;
        r.Leg(slam, true, r.World(rest, FF) + new Vector2(-.55f, 0));
        r.Leg(slam, false, r.World(rest, FB) + new Vector2(.60f, 0)); r.Ground(slam);
        slam.scale[Weapon].y = 1.8f;
        r.HandAt(slam, false, new Vector2(.30f, r.floor + 1.60f));
        r.BladeToFloor(slam); MatchFrontGrip(r, slam);
        // Front knee on the ground, rear knee forward. This is deliberately asymmetric.
        Pose kneel = Arms(r, -5, 8, 15, 0, 12, 35, 8);
        kneel.rot[Bottom] = -kneel.rot[Body];
        kneel.rot[TF] = -12; kneel.rot[CF] = -78; kneel.rot[FF] = 0;
        kneel.pos[Body].y += r.floor + .10f - r.World(kneel, CF).y;
        r.Leg(kneel, false, r.World(rest, FB) + new Vector2(.45f, 0)); r.Ground(kneel);
        r.BladeAt(kneel, Vector2.right);
        Pose handoff = kneel.Clone();
        handoff.rot[SB] = 75; handoff.rot[AB] = -90; handoff.rot[HB] = 0;
        r.HandAt(handoff, false, new Vector2(.40f, handoff.pos[Body].y + .15f));
        r.BladeAt(handoff, Vector2.right); MatchFrontGrip(r, handoff);
        Pose punch = Arms(r, -30, 0, 0, 0, -15, 35, 5);
        punch.pos[Body] += new Vector3(.14f, -.40f, 0);
        punch.pos[Bottom] = r.rest.pos[Bottom]; punch.rot[Bottom] = 22;
        r.Leg(punch, true, r.World(rest, FF) + new Vector2(-.48f, 0));
        r.Leg(punch, false, r.World(rest, FB) + new Vector2(.52f, 0)); r.Ground(punch);
        // Exact supplied extended rear arm. Do not run HandAt on this arm.
        punch.pos[SB] = new Vector3(.442f, .713f, 0); punch.rot[SB] = 111.9f;
        punch.pos[AB] = new Vector3(.063f, -.575f, 0); punch.rot[AB] = -10.1f;
        punch.pos[HB] = new Vector3(.069f, -.601f, 0); punch.rot[HB] = 5;
        Vector2 punchTarget = r.World(punch, HB);
        punch.pos[Body].y -= .20f;
        r.Leg(punch, true, r.World(rest, FF) + new Vector2(-.52f, 0));
        r.Leg(punch, false, r.World(rest, FB) + new Vector2(.56f, 0)); r.Ground(punch);
        r.HandAt(punch, false, punchTarget); // Keep fist contact height while lowering the torso.
        Vector2 fistCorrection = punchTarget - r.World(punch, HB);
        punch.pos[SB] += r.Matrix(punch, Body).inverse.MultiplyVector(fistCorrection);
        Pose takeBack = Arms(r, -4, 75, -90, 0, 25, 55, 5);
        r.HandAt(takeBack, false, new Vector2(.40f, .15f));
        r.BladeAt(takeBack, Vector2.right); MatchFrontGrip(r, takeBack);
        list.Add(new Motion("Attack", 39, false, f =>
        {
            Pose p = Track(f, new Key(0, rest), new Key(5, kneel), new Key(7, kneel), new Key(10, handoff),
                new Key(12, punch), new Key(18, punch), new Key(21, takeBack), new Key(25, charge),
                new Key(30, charge), new Key(32, slam), new Key(34, slam), new Key(39, rest));
            FrontHandSword(r, p, f);
            // Blend into the second grip during the downswing, then release on recovery.
            float twoHand = S((f - 22) / 3f) * (1 - S((f - 34) / 3f));
            if (twoHand > 0)
            {
                Pose gripped = p.Clone(); MatchFrontGrip(r, gripped);
                p.rot[SF] = Mathf.Lerp(p.rot[SF], gripped.rot[SF], twoHand);
                p.rot[AF] = Mathf.Lerp(p.rot[AF], gripped.rot[AF], twoHand);
            }
            return p;
        }).Cue(12, "Attack.DashStart").Cue(18, "Attack.DashEnd").Cue(32, "Attack.Slam").Cue(39, "Action.End"));
        AddSwordVariants(r, list);
        Pose reach = Arms(r, -18, 8, 12, 0, 70, 0, 0);
        reach.pos[Body].y -= .45f; reach.pos[Bottom] = r.rest.pos[Bottom]; reach.rot[Bottom] = 14;
        r.Leg(reach, true, r.World(rest, FF) + new Vector2(-.40f, 0));
        r.Leg(reach, false, r.World(rest, FB) + new Vector2(.48f, .12f)); r.Ground(reach);
        reach.pos[SF] = new Vector3(-.856f, .727f, 0); reach.rot[SF] = 70;
        reach.pos[AF] = new Vector3(-.312f, -.590f, 0); reach.rot[AF] = 0;
        reach.pos[HF] = new Vector3(.054f, -.633f, 0); reach.rot[HF] = 0;
        Pose beckon = reach.Clone(); beckon.rot[SF] = 100; beckon.rot[AF] = 65; beckon.rot[HF] = 45;
        // Lower skill2 dash with rear knee forward and front leg trailing.
        dash.pos[Body].y -= .25f; dash.rot[Body] = -28; dash.rot[Bottom] = 20;
        r.Leg(dash, true, r.World(rest, FF) + new Vector2(-.64f, 0));
        r.Leg(dash, false, r.World(rest, FB) + new Vector2(.66f, 0)); r.Ground(dash);
        // One clip. Runtime seeks to frame 33 when a hit/wall ends the variable dash early.
        list.Add(new Motion("skill2", 39, false, f =>
            Track(f, new Key(0, rest), new Key(5, reach), new Key(8, reach), new Key(10, beckon), new Key(20, beckon), new Key(24, dash),
                new Key(33, dash), new Key(39, rest)))
            .Cue(0, "Skill2.PullStart").Cue(24, "Skill2.PullEnd").Cue(24, "Skill2.DashStart")
            .Cue(33, "Skill2.DashEnd").Cue(39, "Action.End"));
        // User-authored skill3 arm pose: absolute local transforms, no IK overwrite.
        Pose buff = rest.Clone();
        buff.pos[SF] = new Vector3(-.856f, .727f, 0); buff.rot[SF] = 15.307f;
        buff.pos[AF] = new Vector3(-.312f, -.590f, 0); buff.rot[AF] = 67.600f;
        buff.pos[HF] = new Vector3(.054f, -.633f, 0); buff.rot[HF] = 1.250f;
        buff.pos[SB] = new Vector3(.442f, .713f, 0); buff.rot[SB] = 35f;
        buff.pos[AB] = new Vector3(.063f, -.575f, 0); buff.rot[AB] = -125f;
        buff.pos[HB] = new Vector3(.069f, -.601f, 0); buff.rot[HB] = 0;
        r.BladeAt(buff, Vector2.up); buff.rot[Head] = -12;
        // Use the previously authored Mirrored rotation as the single skill3.
        {
            float sign = r.Matrix(buff, HB).determinant < 0 ? -1 : 1;
            Pose three = buff.Clone(), six = buff.Clone(), nine = buff.Clone(), twelve = buff.Clone(), end = rest.Clone();
            three.rot[Weapon] += 90 * sign; six.rot[Weapon] += 180 * sign; nine.rot[Weapon] += 270 * sign;
            twelve.rot[Weapon] += 360 * sign; end.rot[Weapon] += 360 * sign;
            list.Add(new Motion("skill3", 24, false, f =>
                Track(f, new Key(0, rest), new Key(4, buff), new Key(7, buff), new Key(8, three), new Key(10, three),
                    new Key(11, six), new Key(13, six), new Key(14, nine), new Key(16, nine), new Key(17, twelve),
                    new Key(20, twelve), new Key(24, end)))
                .Cue(12, "Skill3.BuffApply").Cue(24, "Action.End"));
        }
    }
    static Pose CapeSpread(Rig r, bool back)
    {
        Pose p = Arms(r, back ? 3 : -3, 8, 12, 0, 8, 12, 5);
        int shoulder = back ? SB : SF;
        Vector2 origin = r.World(p, shoulder);
        r.HandAt(p, !back, origin + new Vector2(back ? 1.20f : -1.20f, .08f));
        if (back) r.BladeAt(p, new Vector2(.4f, -1));
        return p;
    }
    static void MainMotions(Rig r, List<Motion> list)
    {
        Pose rest = r.Rest(), charge = RaisedSword(r), slam = GroundSlam(r), dash = Thrust(r);
        Pose spin = Arms(r, -2, 75, 25, 0, -15, 25, 8), spinEnd = spin.Clone();
        spinEnd.rot[Weapon] += 720;
        Pose dashEnd = dash.Clone(); dashEnd.rot[Weapon] += 720;
        Pose final = rest.Clone(); final.rot[Weapon] += 720;
        list.Add(new Motion("Attack", 39, false, f =>
        {
            Pose p = Track(f, new Key(0, rest), new Key(6, charge), new Key(9, slam), new Key(12, slam),
                new Key(15, spin), new Key(24, spinEnd), new Key(28, spinEnd), new Key(30, dashEnd), new Key(34, dashEnd), new Key(39, final));
            if (f >= 12 && f <= 24) p.rot[Weapon] = Mathf.Lerp(slam.rot[Weapon], spin.rot[Weapon], S((f - 12) / 3)) + 720 * (f - 12) / 12;
            return p;
        })
            .Cue(9, "Attack.Slam").Cue(12, "Attack.SpinStart").Cue(12, "Attack.Pulse1").Cue(15, "Attack.Pulse2")
            .Cue(18, "Attack.Pulse3").Cue(21, "Attack.Pulse4").Cue(24, "Attack.SpinEnd")
            .Cue(30, "Attack.DashStart").Cue(34, "Attack.DashEnd").Cue(39, "Action.End"));
        AddSwordVariants(r, list);
        Pose frontBuff = CapeSpread(r, false), backBuff = CapeSpread(r, true);
        list.Add(new Motion("skill2", 24, false, f => Track(f, new Key(0, rest), new Key(5, rest), new Key(9, frontBuff),
            new Key(13, frontBuff), new Key(24, rest))).Cue(12, "Skill2.BuffApply").Cue(24, "Action.End"));
        list.Add(new Motion("skill4", 30, false, f => Track(f, new Key(0, rest), new Key(6, rest), new Key(11, backBuff),
            new Key(16, backBuff), new Key(30, rest))).Cue(15, "Skill4.BuffApply").Cue(30, "Action.End"));
        Pose hover = Arms(r, 0, 12, 15, 0, 40, 65, 8);
        r.HandAt(hover, true, new Vector2(.05f, .70f));
        Pose lift = Arms(r, -22, 115, -15, 0, -35, 20, 8);
        ForwardStance(r, lift, true);
        r.HandAt(lift, false, new Vector2(1.30f, .35f)); r.BladeAt(lift, Vector2.up);
        r.HandAt(lift, true, r.World(lift, Weapon) + new Vector2(0, .30f));
        list.Add(new Motion("skill3", 15, false, f => Track(f, new Key(0, rest), new Key(3, hover), new Key(4, lift), new Key(15, lift)))
            .Cue(4, "Skill3.DashStart").Cue(15, "Skill3.DashTimeout"));
        Pose over = lift.Clone(); over.rot[Body] -= 165; over.rot[TB] = 55; over.rot[CB] = -75;
        Pose landing = slam.Clone(); landing.rot[Body] -= 360;
        Pose followEnd = rest.Clone(); followEnd.rot[Body] -= 360;
        list.Add(new Motion("skill3_Follow", 18, false, f =>
        {
            Pose p = Track(f, new Key(0, lift), new Key(6, over), new Key(12, landing), new Key(14, landing), new Key(18, followEnd));
            return p;
        }).Cue(0, "Skill3.DashEnd").Cue(0, "Skill3.VaultStart").Cue(12, "Skill3.FollowImpact")
            .Cue(12, "Skill3.VaultEnd").Cue(18, "Action.End"));
        list.Add(new Motion("skill3_Miss", 6, false, f => Mix(lift, rest, f / 6)).Cue(0, "Skill3.DashEnd").Cue(6, "Action.End"));
        Pose crossUp = dash.Clone(), crossDown = dash.Clone();
        crossUp.rot[Body] = -12; crossDown.rot[Body] = -26;
        foreach (Pose pose in new[] { crossUp, crossDown, dash })
        { ForwardStance(r, pose, true); }
        Motion cross = new Motion("skill5", 48, false, f =>
        {
            if (f < 6) return Mix(rest, crossUp, f / 6);
            int pass = Mathf.Min(3, Mathf.FloorToInt((f - 6) / 8)); float local = f - (6 + pass * 8);
            Pose cut = pass == 0 ? crossUp : pass == 1 ? crossDown : pass == 2 ? crossUp : dash;
            if (f >= 38) return Mix(dash, rest, (f - 38) / 10);
            Pose previous = pass == 0 ? crossUp : pass == 1 ? crossUp : pass == 2 ? crossDown : crossUp;
            return Track(local, new Key(0, previous), new Key(2, cut), new Key(5, cut), new Key(8, cut));
        }).Cue(48, "Action.End");
        for (int i = 0; i < 4; i++) cross.Cue(8 + i * 8, "Skill5.PassStart." + i).Cue(9 + i * 8, "Skill5.Impact." + i).Cue(11 + i * 8, "Skill5.PassEnd." + i);
        list.Add(cross);
        Pose timePose = Arms(r, 0, 55, 55, 0, 55, 55, 8);
        r.HandAt(timePose, false, new Vector2(.10f, .50f));
        r.HandAt(timePose, true, r.World(timePose, Weapon) + new Vector2(-.08f, -.08f));
        // Reflected weapon parents reverse local rotation: choose the sign for root-local CCW.
        float spinSign = r.Matrix(timePose, HB).determinant < 0 ? -1 : 1;
        // Preserve mirrored desire variants: these belong to MainBoss, not MidBoss skill3.
        for (int mirror = 0; mirror < 2; mirror++)
        {
            Pose spun = timePose.Clone(), returned = rest.Clone();
            float direction = mirror == 0 ? 1 : -1;
            spun.rot[Weapon] += 360 * spinSign * direction;
            returned.rot[Weapon] += 360 * spinSign * direction;
            list.Add(new Motion(mirror == 0 ? "Dskill" : "Dskill_Mirrored", 30, false, f => Track(f,
                new Key(0, rest), new Key(7, timePose), new Key(22, spun), new Key(24, spun), new Key(30, returned)))
                .Cue(0, "Desire.Begin").Cue(30, "Action.End"));
        }
        list.Add(new Motion("DesireSuccess", 12, false, f => Track(f, new Key(0, rest), new Key(4, timePose), new Key(12, rest)))
            .Cue(0, "Desire.RestoreHP").Cue(12, "Action.End"));
        Pose groggy = Arms(r, -20, 5, 4, 0, 8, 5, 0); groggy.pos[Body].y -= .24f; groggy.rot[Head] = -18;
        r.Leg(groggy, true, r.World(rest, FF)); r.Leg(groggy, false, r.World(rest, FB));
        list.Add(new Motion("Groggy", 12, false, f => Mix(rest, groggy, f / 12)).Cue(0, "Groggy.Begin"));
        list.Add(new Motion("GroggyLoop", 24, true, f => { Pose p = groggy.Clone(); p.rot[Head] += .6f * Mathf.Sin(f / 24 * Mathf.PI * 2); return p; }));
        list.Add(new Motion("GroggyEnd", 8, false, f => Mix(groggy, rest, f / 8)).Cue(8, "Action.End"));
    }

    public static void Generate(bool main, string only = null, bool setupOnly = false)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || AnimationMode.InAnimationMode())
            throw new InvalidOperationException("Play와 Animation Preview를 끄고 실행하세요.");
        using (var r = new Rig(main))
        {
            EnsureFolder(r.folder);
            List<Motion> motions = Motions(r);
            // Group conditional sections under their owning skill menu.
            foreach (Motion m in motions)
            {
                string path = r.folder + "/" + r.prefix + m.name + ".anim";
                bool selected = only == null || m.name == only || m.name.StartsWith(only + "_", StringComparison.Ordinal)
                    || (only == "Groggy" && m.name.StartsWith("Groggy", StringComparison.Ordinal));
                if ((!setupOnly && selected) || AssetDatabase.LoadAssetAtPath<AnimationClip>(path) == null) BuildClip(r, m, path);
            }
            Setup(r, motions);
            ApplyExistingData(r.main);
            AssetDatabase.SaveAssets();
            Debug.Log("[Stage5 R13] " + r.name + " 애니메이션/컨트롤러 완료: " + r.folder +
                ". 조건부 연계와 실제 타격/이동은 Stage5 전투 코드에서 Relay 신호를 연결하세요.");
        }
    }

    // Baked cloth follow-through: visible in Animation preview as well as in Play mode.
    // Position stays on the shoulder attachment; only the Cape's local Z rotation changes.
    static void ApplyCape(Rig r, Motion m, float frame, Pose pose)
    {
        float turn = 0;
        if (m.loop)
        {
            float phase = frame / m.frames * Mathf.PI * 2;
            if (m.name == "Idle") turn = .65f * Mathf.Sin(phase);
            else if (m.name == "Walk") turn = 1f + 3f * Mathf.Sin(phase - .6f);
            else if (m.name == "Run") turn = 4f + 6f * Mathf.Sin(phase - .8f);
            else turn = .45f * Mathf.Sin(phase); // GroggyLoop
        }
        else
        {
            float previousFrame = Mathf.Max(0, frame - 1.2f);
            Pose previous = m.sample(previousFrame);
            float bodyLag = Mathf.DeltaAngle(pose.rot[Body], previous.rot[Body]);
            float armLag = Mathf.DeltaAngle(pose.rot[SB], previous.rot[SB]);
            float envelope = S(frame / 2f) * S((m.frames - frame) / 4f);
            turn = Mathf.Clamp(bodyLag * .65f + armLag * .12f, -14f, 14f) * envelope;
            if (m.name.StartsWith("skill1", StringComparison.Ordinal)) turn += 2f * Mathf.Sin(frame * Mathf.PI / 6f) * envelope;
            if (m.name == "skill2_Dash" || m.name == "skill3") turn += 6f * S(frame / 3f) * envelope;
        }
        if (m.name == "skill2" || m.name == "skill4")
        {
            float start = m.name == "skill2" ? 7 : 9, peak = m.name == "skill2" ? 12 : 15;
            float spread = S((frame - start) / (peak - start)) * (1 - S((frame - peak) / (m.frames - peak)));
            turn += (m.name == "skill2" ? -22 : 22) * spread;
        }
        pose.pos[Cape] = r.rest.pos[Cape]; pose.scale[Cape] = r.rest.scale[Cape];
        pose.rot[Cape] = r.rest.rot[Cape] + turn;
    }

    static void BuildClip(Rig r, Motion m, string path)
    {
        var clip = new AnimationClip { name = r.prefix + m.name, frameRate = FPS };
        int count = m.frames * 5;
        var poses = new Pose[count + 1];
        for (int i = 0; i <= count; i++)
        {
            float frame = m.loop && i == count ? 0 : i / 5f;
            poses[i] = m.sample(frame);
            if (r.main) ApplyCape(r, m, frame, poses[i]);
            else FloatTorso(r, m, frame, poses[i]);
            for (int part = 0; part < r.paths.Length; part++)
                if (float.IsNaN(poses[i].rot[part]) || float.IsInfinity(poses[i].rot[part]))
                    throw new InvalidOperationException("Invalid pose: " + clip.name + " frame " + frame);
        }
        // Unwrap all angles by adjacent samples. Spin increments are well below 180 degrees/sample.
        for (int part = 0; part < r.paths.Length; part++)
            for (int i = 1; i <= count; i++)
                poses[i].rot[part] = poses[i - 1].rot[part] + Mathf.DeltaAngle(poses[i - 1].rot[part], poses[i].rot[part]);
        for (int part = 0; part < r.paths.Length; part++)
            for (int axis = 0; axis < 3; axis++)
            {
                var position = new Keyframe[count + 1]; var rotation = new Keyframe[count + 1]; var scale = new Keyframe[count + 1];
                for (int i = 0; i <= count; i++)
                {
                    float time = i / (5 * FPS);
                    position[i] = new Keyframe(time, poses[i].pos[part][axis]);
                    rotation[i] = new Keyframe(time, axis == 2 ? poses[i].rot[part] : 0);
                    scale[i] = new Keyframe(time, poses[i].scale[part][axis]);
                }
                Curve(clip, r.paths[part], "m_LocalPosition." + "xyz"[axis], position);
                Curve(clip, r.paths[part], "localEulerAnglesRaw." + "xyz"[axis], rotation);
                Curve(clip, r.paths[part], "m_LocalScale." + "xyz"[axis], scale);
            }
        // Animate small flags; the relay applies exact integer SortingLayer IDs even in preview.
        FlagCurve(clip, "frontWeapon", new Keyframe(0, ((r.main && m.name.StartsWith("Dskill", StringComparison.Ordinal)) || (!r.main && m.name == "Attack")) ? 1 : 0),
            new Keyframe(m.frames / FPS, 0));
        FlagCurve(clip, "frontRearArm", new Keyframe(0, !r.main && m.name == "skill3" ? 1 : 0),
            new Keyframe(m.frames / FPS, 0));
        FlagCurve(clip, "npcBlade", new Keyframe(0, 0),
            new Keyframe(4 / FPS, !r.main && m.name == "skill3" ? 1 : 0), new Keyframe(m.frames / FPS, 0));
        if (!r.main && m.name == "Attack")
            FlagCurve(clip, "frontBlade", new Keyframe(0, 0), new Keyframe(9 / FPS, 1), new Keyframe(22 / FPS, 0), new Keyframe(39 / FPS, 0));
        else FlagCurve(clip, "frontBlade", new Keyframe(0, 0), new Keyframe(m.frames / FPS, 0));
        if (r.main && m.name == "skill5")
            FlagCurve(clip, "hideVisual", new Keyframe(0, 0), new Keyframe(22 / FPS, 1), new Keyframe(24 / FPS, 0),
                new Keyframe(30 / FPS, 1), new Keyframe(32 / FPS, 0), new Keyframe(48 / FPS, 0));
        else FlagCurve(clip, "hideVisual", new Keyframe(0, 0), new Keyframe(m.frames / FPS, 0));
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = m.loop; settings.loopBlend = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        var events = m.signals.Select(c => new AnimationEvent
        {
            time = c.frame / FPS,
            functionName = "OnStage5AnimationCue",
            stringParameter = c.name,
            messageOptions = SendMessageOptions.DontRequireReceiver
        }).ToList();
        AnimationClip old = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (old != null)
            events.AddRange(AnimationUtility.GetAnimationEvents(old).Where(e => e.functionName != "OnStage5AnimationCue"));
        AnimationUtility.SetAnimationEvents(clip, events.OrderBy(e => e.time).ToArray());
        if (old == null) AssetDatabase.CreateAsset(clip, path);
        else
        {
            Undo.RecordObject(old, "Regenerate " + clip.name); EditorUtility.CopySerialized(clip, old);
            EditorUtility.SetDirty(old); UnityEngine.Object.DestroyImmediate(clip);
        }
    }
    static void FlagCurve(AnimationClip clip, string field, params Keyframe[] keys)
    {
        AnimationCurve curve = new AnimationCurve(keys);
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
        }
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Stage5BossAnimationRelay), field), curve);
    }
    static void Curve(AnimationClip clip, string path, string property, Keyframe[] keys)
    {
        var curve = new AnimationCurve(keys);
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
        }
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);
    }

    static void Parameter(AnimatorController c, string name, AnimatorControllerParameterType type, float value = 0)
    {
        foreach (var p in c.parameters)
            if (p.name == name)
            {
                if (p.type != type) throw new InvalidOperationException(name + " 파라미터 타입이 다릅니다.");
                return;
            }
        c.AddParameter(new AnimatorControllerParameter { name = name, type = type, defaultFloat = value });
    }
    static AnimatorStateTransition Transition(AnimatorState from, AnimatorState to, bool exit, string trigger = null)
    {
        var t = from.AddTransition(to); t.name = Tag + from.name + "_" + to.name;
        t.hasExitTime = exit; t.exitTime = 1; t.hasFixedDuration = true; t.duration = .05f; t.canTransitionToSelf = false;
        t.interruptionSource = TransitionInterruptionSource.None;
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
        if (trigger != null) t.AddCondition(AnimatorConditionMode.If, 0, trigger);
        return t;
    }
    static void Moving(AnimatorStateTransition t, int index)
    {
        t.AddCondition(index == 0 ? AnimatorConditionMode.Less : AnimatorConditionMode.Greater, .01f, "MoveSpeed");
        if (index != 0) t.AddCondition(index == 2 ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "IsChasing");
    }
    static void Setup(Rig r, List<Motion> motions)
    {
        string controllerPath = r.folder + "/" + r.prefix.TrimEnd('_') + "_AC.controller";
        var c = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (c == null) c = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        foreach (string b in new[] { "IsChasing", "IsAttacking", "IsDead" }) Parameter(c, b, AnimatorControllerParameterType.Bool);
        Parameter(c, "MoveSpeed", AnimatorControllerParameterType.Float);
        Parameter(c, "AttackSpeed", AnimatorControllerParameterType.Float, 1);
        string[] triggers = r.main ? new[] { "Attack", "skill1", "skill2", "skill3", "skill4", "skill5", "Dskill", "Skill3Hit", "DesireSuccess", "Groggy", "GroggyEnd" }
            : new[] { "Attack", "skill1", "skill2", "skill3", "DashEnd" };
        foreach (string trigger in triggers) Parameter(c, trigger, AnimatorControllerParameterType.Trigger);
        AnimatorStateMachine sm = c.layers[0].stateMachine;
        if (!r.main)
        {
            foreach (string obsolete in new[] { "skill2_Dash", "skill2_End", "skill3_Mirrored" })
            {
                var oldState = sm.states.FirstOrDefault(x => x.state.name == r.prefix + obsolete).state;
                if (oldState != null)
                {
                    foreach (var child in sm.states)
                        foreach (var transition in child.state.transitions)
                            if (transition.destinationState == oldState) child.state.RemoveTransition(transition);
                    foreach (var transition in sm.anyStateTransitions)
                        if (transition.destinationState == oldState) sm.RemoveAnyStateTransition(transition);
                    sm.RemoveState(oldState);
                }
                AssetDatabase.DeleteAsset(r.folder + "/" + r.prefix + obsolete + ".anim");
            }
        }
        var states = new Dictionary<string, AnimatorState>();
        int action = 0;
        foreach (Motion m in motions)
        {
            string name = r.prefix + m.name;
            var matches = sm.states.Where(x => x.state.name == name).ToArray();
            if (matches.Length > 1) throw new InvalidOperationException("중복 Animator state: " + name);
            AnimatorState state = matches.Length == 0 ? sm.AddState(name, Vector3.zero) : matches[0].state;
            state.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(r.folder + "/" + name + ".anim");
            state.writeDefaultValues = false; state.speed = 1;
            state.speedParameter = "AttackSpeed";
            state.speedParameterActive = !(m.loop || m.name == "Death" || m.name.StartsWith("Groggy") || m.name.StartsWith("Dskill", StringComparison.Ordinal) || m.name == "DesireSuccess");
            states[m.name] = state;
            foreach (var t in state.transitions) if (t.name.StartsWith(Tag, StringComparison.Ordinal)) state.RemoveTransition(t);
            Vector3 location;
            if (m.name == "Idle") location = new Vector3(340, 80, 0);
            else if (m.name == "Walk") location = new Vector3(200, 180, 0);
            else if (m.name == "Run") location = new Vector3(480, 180, 0);
            else if (m.name == "Death") location = new Vector3(0, 80, 0);
            else { location = new Vector3(200 + (action % 3) * 280, 320 + (action / 3) * 140, 0); action++; }
            var children = sm.states;
            for (int i = 0; i < children.Length; i++) if (children[i].state == state) children[i].position = location;
            sm.states = children;
        }
        sm.defaultState = states["Idle"]; sm.entryPosition = new Vector3(340, -30, 0);
        sm.anyStatePosition = new Vector3(0, 180, 0); sm.exitPosition = new Vector3(1100, 180, 0);
        foreach (var t in sm.anyStateTransitions) if (t.name.StartsWith(Tag, StringComparison.Ordinal)) sm.RemoveAnyStateTransition(t);
        var death = sm.AddAnyStateTransition(states["Death"]); death.name = Tag + "Death";
        death.hasExitTime = false; death.hasFixedDuration = true; death.duration = .04f; death.canTransitionToSelf = false;
        death.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
        string[] moving = { "Idle", "Walk", "Run" };
        string[] starters = r.main ? new[] { "Attack", "skill1", "skill2", "skill3", "skill4", "skill5", "Dskill" }
            : new[] { "Attack", "skill1", "skill2", "skill3" };
        foreach (string a in moving)
        {
            for (int to = 0; to < 3; to++) if (a != moving[to])
                {
                    var t = Transition(states[a], states[moving[to]], false); Moving(t, to);
                    t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
                }
            foreach (string starter in starters) Transition(states[a], states[starter], false, starter).duration = .03f;
        }
        foreach (Motion m in motions)
        {
            if (moving.Contains(m.name) || m.name == "Death" || m.name == "Groggy" || m.name == "GroggyLoop") continue;
            if (r.main && m.name == "skill3") continue;
            for (int to = 0; to < 3; to++)
            {
                var back = Transition(states[m.name], states[moving[to]], true); Moving(back, to);
                back.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
            }
        }
        if (r.main)
        {
            // A hit has higher priority than timeout; never play the follow-up automatically.
            Transition(states["skill3"], states["skill3_Follow"], false, "Skill3Hit").duration = 0;
            // Runtime selects Follow or Miss after the actual dash stops.
            foreach (string special in new[] { "Groggy", "DesireSuccess" })
            {
                var t = sm.AddAnyStateTransition(states[special]); t.name = Tag + special;
                t.hasExitTime = false; t.hasFixedDuration = true; t.duration = .08f; t.canTransitionToSelf = false;
                t.AddCondition(AnimatorConditionMode.If, 0, special); t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            }
            Transition(states["Groggy"], states["GroggyEnd"], false, "GroggyEnd");
            Transition(states["Groggy"], states["GroggyLoop"], true).duration = 0;
            Transition(states["GroggyLoop"], states["GroggyEnd"], false, "GroggyEnd");
        }
        Animator animator = r.visual.GetComponent<Animator>();
        if (animator == null) animator = r.visual.gameObject.AddComponent<Animator>();
        animator.runtimeAnimatorController = c; animator.applyRootMotion = false;
        if (r.visual.GetComponent<Stage5BossAnimationRelay>() == null) r.visual.gameObject.AddComponent<Stage5BossAnimationRelay>();
        var relay = r.visual.GetComponent<Stage5BossAnimationRelay>();
        relay.ConfigureVisuals(r.visual.Find(r.paths[HB]).GetComponent<SpriteRenderer>(),
            r.visual.Find(r.paths[Weapon]).GetComponent<SpriteRenderer>(), r.visual.GetComponentsInChildren<SpriteRenderer>(true));
        EditorUtility.SetDirty(relay);
        foreach (var component in r.contents.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (component == null) continue;
            var so = new SerializedObject(component);
            SetObject(so, "deathAnimation", states["Death"].motion);
            SetObject(so, "deathAnimator", animator);
            SetObject(so, "bossAnimator", animator);
            SetObject(so, "visualRoot", r.visual);
            var prop = so.FindProperty("deathStateName") ?? so.FindProperty("deathAnimationState");
            if (prop != null && prop.propertyType == SerializedPropertyType.String) prop.stringValue = r.prefix + "Death";
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        PrefabUtility.SaveAsPrefabAsset(r.contents, r.prefabPath, out bool saved);
        if (!saved) throw new InvalidOperationException("프리팹 저장 실패: " + r.prefabPath);
        EditorUtility.SetDirty(sm); EditorUtility.SetDirty(c);
    }
    static void SetObject(SerializedObject so, string name, UnityEngine.Object value)
    {
        var p = so.FindProperty(name);
        if (p != null && p.propertyType == SerializedPropertyType.ObjectReference) p.objectReferenceValue = value;
    }
    static string FindPrefab(string name)
    {
        var candidates = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith("Assets/", StringComparison.Ordinal)) continue;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            // Support uploaded "(1)" filename, while checking the actual prefab root's name.
            if (prefab != null && prefab.name == name) candidates.Add(path);
        }
        var preferred = candidates.Where(p => p.StartsWith("Assets/Prefab/5/", StringComparison.OrdinalIgnoreCase)).ToList();
        if (preferred.Count == 1) return preferred[0];
        if (candidates.Count != 1) throw new InvalidOperationException(name + " 프리팹 검색 결과 " + candidates.Count + "개. Assets/Prefab/5/에서 대상을 하나로 정리하세요.");
        return candidates[0];
    }
    static void EnsureFolder(string folder)
    {
        string path = "Assets";
        foreach (string part in folder.Split('/').Skip(1))
        {
            if (!AssetDatabase.IsValidFolder(path + "/" + part)) AssetDatabase.CreateFolder(path, part);
            path += "/" + part;
        }
    }

    // Supplied Stage5 schema. Only pattern fields are written; HP, detection and asset identity stay intact.
    public static void ApplyExistingData(bool main)
    {
        string stem = main ? "Stage5_MainBoss" : "Stage5_MidBoss";
        var data = FindData(stem);
        if (data == null) { Debug.LogWarning(stem + ": 데이터 에셋이 없습니다. 기존 Stage5 에셋을 프로젝트에 넣어주세요."); return; }
        var so = new SerializedObject(data);
        var skills = so.FindProperty("skills");
        int[] counts = main ? new[] { 16, 0, 3, 0, 4 } : new[] { 1, 2, 0 };
        var basic = so.FindProperty("basicAttack.active.attackSteps");
        if (skills == null || !skills.isArray || skills.arraySize != counts.Length || basic == null || basic.arraySize != (main ? 3 : 2))
            throw new InvalidOperationException(stem + ": 전달된 Stage5 데이터 구조와 다릅니다. 원본 에셋을 확인하세요.");
        for (int i = 0; i < counts.Length; i++)
        {
            var a = skills.GetArrayElementAtIndex(i).FindPropertyRelative("active.attackSteps");
            if (a == null || a.arraySize != counts[i]) throw new InvalidOperationException(stem + ": 스킬 단계 수 불일치 " + (i + 1));
        }
        Undo.RecordObject(data, "Stage5 pattern data");
        if (main)
        {
            Number(so, "basicAttack.skillType", 0f);
            Number(so, "basicAttack.executionOrder", 0f);
            Number(so, "basicAttack.cooldown", 3.1f);
            Number(so, "basicAttack.minimumPhase", 1f);
            Number(so, "basicAttack.castRange", 3.2f);
            Number(so, "basicAttack.forceCastMinDistance", 0f);
            Number(so, "basicAttack.forceCastRemainingCooldown", 0f);
            Number(so, "basicAttack.preDelay", 0f);
            Number(so, "basicAttack.postDelay", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].actionType", 2f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].preDelay", 0.75f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].postDelay", 0.25f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].duration", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].interval", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].attackCount", 1f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].damage", 11f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].hitboxSize.x", 2.8f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].hitboxSize.y", 2.2f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].hitboxOffset.x", 1.45f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].hitboxOffset.y", -0.1f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].knockbackForce.x", 4f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].knockbackForce.y", 1.5f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].knockbackAwayFromCenter", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].moveSpeed", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].maxMoveDistance", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].stopAtWall", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].stopOnPlayerHit", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].pullTargetOffset.x", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].pullTargetOffset.y", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].persistentFixedCadence", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].actionType", 8f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].preDelay", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].postDelay", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].duration", 1f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].interval", 0.25f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].attackCount", 4f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].damage", 3f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].hitboxSize.x", 2.8f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].hitboxSize.y", 2.2f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].hitboxOffset.x", 1.45f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].hitboxOffset.y", -0.1f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].knockbackForce.x", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].knockbackForce.y", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].knockbackAwayFromCenter", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].moveSpeed", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].maxMoveDistance", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].stopAtWall", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].stopOnPlayerHit", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].pullTargetOffset.x", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].pullTargetOffset.y", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].persistentFixedCadence", 1f);
            Number(so, "basicAttack.active.attackSteps.Array.data[2].actionType", 4f);
            Number(so, "basicAttack.active.attackSteps.Array.data[2].preDelay", 0.5f);
            Number(so, "basicAttack.active.attackSteps.Array.data[2].postDelay", 0.416666667f);
            Number(so, "basicAttack.active.attackSteps.Array.data[2].duration", 0.333333333f);
            Number(so, "basicAttack.active.attackSteps.Array.data[2].interval", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[2].attackCount", 1f);
            Number(so, "basicAttack.active.attackSteps.Array.data[2].damage", 16f);
            Number(so, "basicAttack.active.attackSteps.Array.data[2].hitboxSize.x", 2.5f);
            Number(so, "basicAttack.active.attackSteps.Array.data[2].hitboxSize.y", 1.5f);
            Number(so, "basicAttack.active.attackSteps.Array.data[2].hitboxOffset.x", 1.5f);
            Number(so, "basicAttack.active.attackSteps.Array.data[2].hitboxOffset.y", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[2].knockbackForce.x", 7f);
            Number(so, "basicAttack.active.attackSteps.Array.data[2].knockbackForce.y", 2f);
            Number(so, "basicAttack.active.attackSteps.Array.data[2].knockbackAwayFromCenter", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[2].moveSpeed", 8.4f);
            Number(so, "basicAttack.active.attackSteps.Array.data[2].maxMoveDistance", 2.8f);
            Number(so, "basicAttack.active.attackSteps.Array.data[2].stopAtWall", 1f);
            Number(so, "basicAttack.active.attackSteps.Array.data[2].stopOnPlayerHit", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[2].pullTargetOffset.x", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[2].pullTargetOffset.y", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[2].persistentFixedCadence", 0f);
            Number(so, "basicAttack.buff.duration", 0f);
            Number(so, "skills.Array.data[0].skillType", 0f);
            Number(so, "skills.Array.data[0].executionOrder", 0f);
            Number(so, "skills.Array.data[0].cooldown", 13f);
            Number(so, "skills.Array.data[0].minimumPhase", 1f);
            Number(so, "skills.Array.data[0].castRange", 5.2f);
            Number(so, "skills.Array.data[0].forceCastMinDistance", 0f);
            Number(so, "skills.Array.data[0].forceCastRemainingCooldown", 0f);
            Number(so, "skills.Array.data[0].preDelay", 1f);
            Number(so, "skills.Array.data[0].postDelay", 0.5f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].actionType", 3f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].preDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].postDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].duration", 0.125f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].interval", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].attackCount", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].damage", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].hitboxSize.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].hitboxSize.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].hitboxOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].hitboxOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].knockbackForce.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].knockbackForce.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].knockbackAwayFromCenter", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].moveSpeed", 1.25f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].maxMoveDistance", 0.5f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].stopAtWall", 1f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].stopOnPlayerHit", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].pullTargetOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].pullTargetOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].persistentFixedCadence", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[1].actionType", 2f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[1].preDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[1].postDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[1].duration", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[1].interval", 0.125f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[1].attackCount", 4f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[1].damage", 2.5f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[1].hitboxSize.x", 3.4f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[1].hitboxSize.y", 2.4f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[1].hitboxOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[1].hitboxOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[1].knockbackForce.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[1].knockbackForce.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[1].knockbackAwayFromCenter", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[1].moveSpeed", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[1].maxMoveDistance", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[1].stopAtWall", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[1].stopOnPlayerHit", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[1].pullTargetOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[1].pullTargetOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[1].persistentFixedCadence", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[2].actionType", 3f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[2].preDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[2].postDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[2].duration", 0.125f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[2].interval", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[2].attackCount", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[2].damage", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[2].hitboxSize.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[2].hitboxSize.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[2].hitboxOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[2].hitboxOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[2].knockbackForce.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[2].knockbackForce.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[2].knockbackAwayFromCenter", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[2].moveSpeed", 1.25f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[2].maxMoveDistance", 0.5f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[2].stopAtWall", 1f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[2].stopOnPlayerHit", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[2].pullTargetOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[2].pullTargetOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[2].persistentFixedCadence", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[3].actionType", 2f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[3].preDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[3].postDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[3].duration", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[3].interval", 0.125f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[3].attackCount", 4f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[3].damage", 2.5f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[3].hitboxSize.x", 3.4f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[3].hitboxSize.y", 2.4f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[3].hitboxOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[3].hitboxOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[3].knockbackForce.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[3].knockbackForce.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[3].knockbackAwayFromCenter", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[3].moveSpeed", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[3].maxMoveDistance", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[3].stopAtWall", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[3].stopOnPlayerHit", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[3].pullTargetOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[3].pullTargetOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[3].persistentFixedCadence", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[4].actionType", 3f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[4].preDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[4].postDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[4].duration", 0.125f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[4].interval", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[4].attackCount", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[4].damage", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[4].hitboxSize.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[4].hitboxSize.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[4].hitboxOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[4].hitboxOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[4].knockbackForce.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[4].knockbackForce.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[4].knockbackAwayFromCenter", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[4].moveSpeed", 1.25f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[4].maxMoveDistance", 0.5f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[4].stopAtWall", 1f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[4].stopOnPlayerHit", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[4].pullTargetOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[4].pullTargetOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[4].persistentFixedCadence", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[5].actionType", 2f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[5].preDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[5].postDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[5].duration", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[5].interval", 0.125f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[5].attackCount", 4f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[5].damage", 2.5f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[5].hitboxSize.x", 3.4f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[5].hitboxSize.y", 2.4f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[5].hitboxOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[5].hitboxOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[5].knockbackForce.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[5].knockbackForce.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[5].knockbackAwayFromCenter", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[5].moveSpeed", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[5].maxMoveDistance", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[5].stopAtWall", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[5].stopOnPlayerHit", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[5].pullTargetOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[5].pullTargetOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[5].persistentFixedCadence", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[6].actionType", 3f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[6].preDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[6].postDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[6].duration", 0.125f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[6].interval", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[6].attackCount", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[6].damage", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[6].hitboxSize.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[6].hitboxSize.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[6].hitboxOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[6].hitboxOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[6].knockbackForce.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[6].knockbackForce.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[6].knockbackAwayFromCenter", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[6].moveSpeed", 1.25f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[6].maxMoveDistance", 0.5f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[6].stopAtWall", 1f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[6].stopOnPlayerHit", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[6].pullTargetOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[6].pullTargetOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[6].persistentFixedCadence", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[7].actionType", 2f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[7].preDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[7].postDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[7].duration", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[7].interval", 0.125f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[7].attackCount", 4f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[7].damage", 2.5f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[7].hitboxSize.x", 3.4f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[7].hitboxSize.y", 2.4f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[7].hitboxOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[7].hitboxOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[7].knockbackForce.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[7].knockbackForce.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[7].knockbackAwayFromCenter", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[7].moveSpeed", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[7].maxMoveDistance", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[7].stopAtWall", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[7].stopOnPlayerHit", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[7].pullTargetOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[7].pullTargetOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[7].persistentFixedCadence", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[8].actionType", 3f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[8].preDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[8].postDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[8].duration", 0.125f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[8].interval", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[8].attackCount", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[8].damage", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[8].hitboxSize.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[8].hitboxSize.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[8].hitboxOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[8].hitboxOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[8].knockbackForce.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[8].knockbackForce.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[8].knockbackAwayFromCenter", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[8].moveSpeed", 1.25f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[8].maxMoveDistance", 0.5f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[8].stopAtWall", 1f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[8].stopOnPlayerHit", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[8].pullTargetOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[8].pullTargetOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[8].persistentFixedCadence", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[9].actionType", 2f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[9].preDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[9].postDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[9].duration", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[9].interval", 0.125f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[9].attackCount", 4f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[9].damage", 2.5f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[9].hitboxSize.x", 3.4f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[9].hitboxSize.y", 2.4f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[9].hitboxOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[9].hitboxOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[9].knockbackForce.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[9].knockbackForce.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[9].knockbackAwayFromCenter", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[9].moveSpeed", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[9].maxMoveDistance", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[9].stopAtWall", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[9].stopOnPlayerHit", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[9].pullTargetOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[9].pullTargetOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[9].persistentFixedCadence", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[10].actionType", 3f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[10].preDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[10].postDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[10].duration", 0.125f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[10].interval", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[10].attackCount", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[10].damage", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[10].hitboxSize.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[10].hitboxSize.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[10].hitboxOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[10].hitboxOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[10].knockbackForce.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[10].knockbackForce.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[10].knockbackAwayFromCenter", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[10].moveSpeed", 1.25f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[10].maxMoveDistance", 0.5f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[10].stopAtWall", 1f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[10].stopOnPlayerHit", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[10].pullTargetOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[10].pullTargetOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[10].persistentFixedCadence", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[11].actionType", 2f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[11].preDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[11].postDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[11].duration", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[11].interval", 0.125f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[11].attackCount", 4f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[11].damage", 2.5f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[11].hitboxSize.x", 3.4f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[11].hitboxSize.y", 2.4f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[11].hitboxOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[11].hitboxOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[11].knockbackForce.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[11].knockbackForce.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[11].knockbackAwayFromCenter", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[11].moveSpeed", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[11].maxMoveDistance", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[11].stopAtWall", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[11].stopOnPlayerHit", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[11].pullTargetOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[11].pullTargetOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[11].persistentFixedCadence", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[12].actionType", 3f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[12].preDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[12].postDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[12].duration", 0.125f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[12].interval", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[12].attackCount", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[12].damage", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[12].hitboxSize.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[12].hitboxSize.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[12].hitboxOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[12].hitboxOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[12].knockbackForce.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[12].knockbackForce.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[12].knockbackAwayFromCenter", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[12].moveSpeed", 1.25f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[12].maxMoveDistance", 0.5f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[12].stopAtWall", 1f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[12].stopOnPlayerHit", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[12].pullTargetOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[12].pullTargetOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[12].persistentFixedCadence", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[13].actionType", 2f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[13].preDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[13].postDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[13].duration", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[13].interval", 0.125f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[13].attackCount", 4f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[13].damage", 2.5f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[13].hitboxSize.x", 3.4f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[13].hitboxSize.y", 2.4f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[13].hitboxOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[13].hitboxOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[13].knockbackForce.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[13].knockbackForce.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[13].knockbackAwayFromCenter", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[13].moveSpeed", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[13].maxMoveDistance", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[13].stopAtWall", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[13].stopOnPlayerHit", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[13].pullTargetOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[13].pullTargetOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[13].persistentFixedCadence", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[14].actionType", 3f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[14].preDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[14].postDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[14].duration", 0.125f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[14].interval", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[14].attackCount", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[14].damage", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[14].hitboxSize.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[14].hitboxSize.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[14].hitboxOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[14].hitboxOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[14].knockbackForce.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[14].knockbackForce.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[14].knockbackAwayFromCenter", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[14].moveSpeed", 1.25f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[14].maxMoveDistance", 0.5f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[14].stopAtWall", 1f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[14].stopOnPlayerHit", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[14].pullTargetOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[14].pullTargetOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[14].persistentFixedCadence", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[15].actionType", 2f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[15].preDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[15].postDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[15].duration", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[15].interval", 0.125f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[15].attackCount", 4f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[15].damage", 2.5f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[15].hitboxSize.x", 3.4f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[15].hitboxSize.y", 2.4f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[15].hitboxOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[15].hitboxOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[15].knockbackForce.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[15].knockbackForce.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[15].knockbackAwayFromCenter", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[15].moveSpeed", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[15].maxMoveDistance", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[15].stopAtWall", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[15].stopOnPlayerHit", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[15].pullTargetOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[15].pullTargetOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[15].persistentFixedCadence", 0f);
            Number(so, "skills.Array.data[0].buff.duration", 0f);
            Number(so, "skills.Array.data[1].skillType", 1f);
            Number(so, "skills.Array.data[1].executionOrder", 0f);
            Number(so, "skills.Array.data[1].cooldown", 20f);
            Number(so, "skills.Array.data[1].minimumPhase", 1f);
            Number(so, "skills.Array.data[1].castRange", 0f);
            Number(so, "skills.Array.data[1].forceCastMinDistance", 0f);
            Number(so, "skills.Array.data[1].forceCastRemainingCooldown", 0f);
            Number(so, "skills.Array.data[1].preDelay", 1f);
            Number(so, "skills.Array.data[1].postDelay", 1f);
            Number(so, "skills.Array.data[1].buff.duration", 10f);
            Number(so, "skills.Array.data[1].buff.effects.Array.data[0].stat", 0f);
            Number(so, "skills.Array.data[1].buff.effects.Array.data[0].valueMode", 1f);
            Number(so, "skills.Array.data[1].buff.effects.Array.data[0].value", 1.3f);
            Number(so, "skills.Array.data[1].buff.effects.Array.data[1].stat", 1f);
            Number(so, "skills.Array.data[1].buff.effects.Array.data[1].valueMode", 1f);
            Number(so, "skills.Array.data[1].buff.effects.Array.data[1].value", 1.15f);
            Number(so, "skills.Array.data[2].skillType", 0f);
            Number(so, "skills.Array.data[2].executionOrder", 0f);
            Number(so, "skills.Array.data[2].cooldown", 11f);
            Number(so, "skills.Array.data[2].minimumPhase", 1f);
            Number(so, "skills.Array.data[2].castRange", 7f);
            Number(so, "skills.Array.data[2].forceCastMinDistance", 0f);
            Number(so, "skills.Array.data[2].forceCastRemainingCooldown", 0f);
            Number(so, "skills.Array.data[2].preDelay", 0f);
            Number(so, "skills.Array.data[2].postDelay", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[0].actionType", 5f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[0].preDelay", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[0].postDelay", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[0].duration", 0.333333333f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[0].interval", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[0].attackCount", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[0].damage", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[0].hitboxSize.x", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[0].hitboxSize.y", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[0].hitboxOffset.x", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[0].hitboxOffset.y", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[0].knockbackForce.x", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[0].knockbackForce.y", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[0].knockbackAwayFromCenter", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[0].moveSpeed", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[0].maxMoveDistance", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[0].stopAtWall", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[0].stopOnPlayerHit", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[0].pullTargetOffset.x", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[0].pullTargetOffset.y", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[0].persistentFixedCadence", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[1].actionType", 4f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[1].preDelay", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[1].postDelay", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[1].duration", 0.916666667f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[1].interval", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[1].attackCount", 1f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[1].damage", 15f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[1].hitboxSize.x", 2.5f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[1].hitboxSize.y", 2.4f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[1].hitboxOffset.x", 1.25f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[1].hitboxOffset.y", -0.5f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[1].knockbackForce.x", 1.5f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[1].knockbackForce.y", 1f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[1].knockbackAwayFromCenter", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[1].moveSpeed", 15f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[1].maxMoveDistance", 6f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[1].stopAtWall", 1f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[1].stopOnPlayerHit", 1f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[1].pullTargetOffset.x", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[1].pullTargetOffset.y", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[1].persistentFixedCadence", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[2].actionType", 4f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[2].preDelay", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[2].postDelay", 0.5f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[2].duration", 1f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[2].interval", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[2].attackCount", 1f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[2].damage", 18f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[2].hitboxSize.x", 2.2f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[2].hitboxSize.y", 2.4f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[2].hitboxOffset.x", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[2].hitboxOffset.y", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[2].knockbackForce.x", 4f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[2].knockbackForce.y", 2f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[2].knockbackAwayFromCenter", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[2].moveSpeed", 10f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[2].maxMoveDistance", 3.5f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[2].stopAtWall", 1f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[2].stopOnPlayerHit", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[2].pullTargetOffset.x", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[2].pullTargetOffset.y", 0f);
            Number(so, "skills.Array.data[2].active.attackSteps.Array.data[2].persistentFixedCadence", 0f);
            Number(so, "skills.Array.data[2].buff.duration", 0f);
            Number(so, "skills.Array.data[3].skillType", 1f);
            Number(so, "skills.Array.data[3].executionOrder", 0f);
            Number(so, "skills.Array.data[3].cooldown", 24f);
            Number(so, "skills.Array.data[3].minimumPhase", 2f);
            Number(so, "skills.Array.data[3].castRange", 0f);
            Number(so, "skills.Array.data[3].forceCastMinDistance", 0f);
            Number(so, "skills.Array.data[3].forceCastRemainingCooldown", 0f);
            Number(so, "skills.Array.data[3].preDelay", 1.25f);
            Number(so, "skills.Array.data[3].postDelay", 1.25f);
            Number(so, "skills.Array.data[3].buff.duration", 10f);
            Number(so, "skills.Array.data[3].buff.effects.Array.data[0].stat", 4f);
            Number(so, "skills.Array.data[3].buff.effects.Array.data[0].valueMode", 1f);
            Number(so, "skills.Array.data[3].buff.effects.Array.data[0].value", 0.8f);
            Number(so, "skills.Array.data[3].buff.effects.Array.data[1].stat", 1f);
            Number(so, "skills.Array.data[3].buff.effects.Array.data[1].valueMode", 1f);
            Number(so, "skills.Array.data[3].buff.effects.Array.data[1].value", 1.2f);
            Number(so, "skills.Array.data[4].skillType", 0f);
            Number(so, "skills.Array.data[4].executionOrder", 0f);
            Number(so, "skills.Array.data[4].cooldown", 15f);
            Number(so, "skills.Array.data[4].excludeDistanceCooldownReduction", 1f);
            Number(so, "skills.Array.data[4].minimumPhase", 2f);
            Number(so, "skills.Array.data[4].castRange", 9f);
            Number(so, "skills.Array.data[4].forceCastMinDistance", 0f);
            Number(so, "skills.Array.data[4].forceCastRemainingCooldown", 0f);
            Number(so, "skills.Array.data[4].preDelay", 0.5f);
            Number(so, "skills.Array.data[4].postDelay", 0.833333333f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[0].actionType", 4f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[0].preDelay", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[0].postDelay", 0.25f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[0].duration", 0.25f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[0].interval", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[0].attackCount", 1f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[0].damage", 10f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[0].hitboxSize.x", 2f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[0].hitboxSize.y", 2.1f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[0].hitboxOffset.x", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[0].hitboxOffset.y", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[0].knockbackForce.x", 1.5f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[0].knockbackForce.y", 0.5f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[0].knockbackAwayFromCenter", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[0].moveSpeed", 18f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[0].maxMoveDistance", 6f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[0].stopAtWall", 1f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[0].stopOnPlayerHit", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[0].pullTargetOffset.x", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[0].pullTargetOffset.y", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[0].persistentFixedCadence", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[1].actionType", 4f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[1].preDelay", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[1].postDelay", 0.25f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[1].duration", 0.25f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[1].interval", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[1].attackCount", 1f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[1].damage", 10f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[1].hitboxSize.x", 2f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[1].hitboxSize.y", 2.1f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[1].hitboxOffset.x", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[1].hitboxOffset.y", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[1].knockbackForce.x", 1.5f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[1].knockbackForce.y", 0.5f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[1].knockbackAwayFromCenter", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[1].moveSpeed", 18f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[1].maxMoveDistance", 6f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[1].stopAtWall", 1f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[1].stopOnPlayerHit", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[1].pullTargetOffset.x", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[1].pullTargetOffset.y", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[1].persistentFixedCadence", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[2].actionType", 4f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[2].preDelay", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[2].postDelay", 0.25f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[2].duration", 0.25f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[2].interval", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[2].attackCount", 1f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[2].damage", 10f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[2].hitboxSize.x", 2f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[2].hitboxSize.y", 2.1f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[2].hitboxOffset.x", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[2].hitboxOffset.y", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[2].knockbackForce.x", 1.5f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[2].knockbackForce.y", 0.5f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[2].knockbackAwayFromCenter", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[2].moveSpeed", 18f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[2].maxMoveDistance", 6f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[2].stopAtWall", 1f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[2].stopOnPlayerHit", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[2].pullTargetOffset.x", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[2].pullTargetOffset.y", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[2].persistentFixedCadence", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[3].actionType", 4f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[3].preDelay", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[3].postDelay", 0.25f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[3].duration", 0.25f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[3].interval", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[3].attackCount", 1f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[3].damage", 10f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[3].hitboxSize.x", 2f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[3].hitboxSize.y", 2.1f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[3].hitboxOffset.x", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[3].hitboxOffset.y", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[3].knockbackForce.x", 1.5f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[3].knockbackForce.y", 0.5f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[3].knockbackAwayFromCenter", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[3].moveSpeed", 18f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[3].maxMoveDistance", 6f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[3].stopAtWall", 1f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[3].stopOnPlayerHit", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[3].pullTargetOffset.x", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[3].pullTargetOffset.y", 0f);
            Number(so, "skills.Array.data[4].active.attackSteps.Array.data[3].persistentFixedCadence", 0f);
            Number(so, "skills.Array.data[4].buff.duration", 0f);
        }
        else
        {
            Number(so, "basicAttack.skillType", 0f);
            Number(so, "basicAttack.executionOrder", 0f);
            Number(so, "basicAttack.cooldown", 3.4f);
            Number(so, "basicAttack.minimumPhase", 1f);
            Number(so, "basicAttack.castRange", 3.3f);
            Number(so, "basicAttack.forceCastMinDistance", 0f);
            Number(so, "basicAttack.forceCastRemainingCooldown", 0f);
            Number(so, "basicAttack.preDelay", 0f);
            Number(so, "basicAttack.postDelay", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].actionType", 4f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].preDelay", 1f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].postDelay", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].duration", 0.5f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].interval", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].attackCount", 1f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].damage", 10f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].hitboxSize.x", 2.1f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].hitboxSize.y", 1.25f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].hitboxOffset.x", 1.25f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].hitboxOffset.y", -0.1f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].knockbackForce.x", 3f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].knockbackForce.y", 1f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].knockbackAwayFromCenter", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].moveSpeed", 4.4f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].maxMoveDistance", 2.2f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].stopAtWall", 1f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].stopOnPlayerHit", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].pullTargetOffset.x", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].pullTargetOffset.y", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[0].persistentFixedCadence", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].actionType", 2f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].preDelay", 1.16666667f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].postDelay", 0.583333333f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].duration", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].interval", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].attackCount", 1f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].damage", 18f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].hitboxSize.x", 2.6f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].hitboxSize.y", 2.3f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].hitboxOffset.x", 1.45f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].hitboxOffset.y", -0.25f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].knockbackForce.x", 8f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].knockbackForce.y", 3f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].knockbackAwayFromCenter", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].moveSpeed", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].maxMoveDistance", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].stopAtWall", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].stopOnPlayerHit", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].pullTargetOffset.x", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].pullTargetOffset.y", 0f);
            Number(so, "basicAttack.active.attackSteps.Array.data[1].persistentFixedCadence", 0f);
            Number(so, "basicAttack.buff.duration", 0f);
            Number(so, "skills.Array.data[0].skillType", 0f);
            Number(so, "skills.Array.data[0].executionOrder", 0f);
            Number(so, "skills.Array.data[0].cooldown", 9f);
            Number(so, "skills.Array.data[0].minimumPhase", 1f);
            Number(so, "skills.Array.data[0].castRange", 4.2f);
            Number(so, "skills.Array.data[0].forceCastMinDistance", 0f);
            Number(so, "skills.Array.data[0].forceCastRemainingCooldown", 0f);
            Number(so, "skills.Array.data[0].preDelay", 1f);
            Number(so, "skills.Array.data[0].postDelay", 0.5f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].actionType", 8f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].preDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].postDelay", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].duration", 3f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].interval", 0.6f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].attackCount", 5f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].damage", 5f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].hitboxSize.x", 3.2f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].hitboxSize.y", 3.2f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].hitboxOffset.x", 2.5f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].hitboxOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].knockbackForce.x", 0.8f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].knockbackForce.y", 0.3f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].knockbackAwayFromCenter", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].moveSpeed", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].maxMoveDistance", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].stopAtWall", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].stopOnPlayerHit", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].pullTargetOffset.x", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].pullTargetOffset.y", 0f);
            Number(so, "skills.Array.data[0].active.attackSteps.Array.data[0].persistentFixedCadence", 1f);
            Number(so, "skills.Array.data[0].buff.duration", 0f);
            Number(so, "skills.Array.data[1].skillType", 0f);
            Number(so, "skills.Array.data[1].executionOrder", 0f);
            Number(so, "skills.Array.data[1].cooldown", 11f);
            Number(so, "skills.Array.data[1].minimumPhase", 1f);
            Number(so, "skills.Array.data[1].castRange", 6f);
            Number(so, "skills.Array.data[1].forceCastMinDistance", 0f);
            Number(so, "skills.Array.data[1].forceCastRemainingCooldown", 0f);
            Number(so, "skills.Array.data[1].preDelay", 0f);
            Number(so, "skills.Array.data[1].postDelay", 0f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[0].actionType", 9f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[0].preDelay", 0f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[0].postDelay", 0f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[0].duration", 2f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[0].interval", 0f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[0].attackCount", 0f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[0].damage", 0f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[0].hitboxSize.x", 6f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[0].hitboxSize.y", 2.5f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[0].hitboxOffset.x", 3f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[0].hitboxOffset.y", 0f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[0].knockbackForce.x", 0f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[0].knockbackForce.y", 0f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[0].knockbackAwayFromCenter", 0f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[0].moveSpeed", 1.3f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[0].maxMoveDistance", 0f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[0].stopAtWall", 0f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[0].stopOnPlayerHit", 0f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[0].pullTargetOffset.x", 1.6f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[0].pullTargetOffset.y", 0f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[0].persistentFixedCadence", 0f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[1].actionType", 4f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[1].preDelay", 0f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[1].postDelay", 0.5f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[1].duration", 0.75f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[1].interval", 0f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[1].attackCount", 1f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[1].damage", 16f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[1].hitboxSize.x", 2.2f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[1].hitboxSize.y", 1.4f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[1].hitboxOffset.x", 1.3f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[1].hitboxOffset.y", -0.1f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[1].knockbackForce.x", 10f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[1].knockbackForce.y", 3f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[1].knockbackAwayFromCenter", 0f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[1].moveSpeed", 13f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[1].maxMoveDistance", 7f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[1].stopAtWall", 1f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[1].stopOnPlayerHit", 1f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[1].pullTargetOffset.x", 0f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[1].pullTargetOffset.y", 0f);
            Number(so, "skills.Array.data[1].active.attackSteps.Array.data[1].persistentFixedCadence", 0f);
            Number(so, "skills.Array.data[1].buff.duration", 0f);
            Number(so, "skills.Array.data[2].skillType", 1f);
            Number(so, "skills.Array.data[2].executionOrder", 0f);
            Number(so, "skills.Array.data[2].cooldown", 18f);
            Number(so, "skills.Array.data[2].minimumPhase", 1f);
            Number(so, "skills.Array.data[2].castRange", 0f);
            Number(so, "skills.Array.data[2].forceCastMinDistance", 0f);
            Number(so, "skills.Array.data[2].forceCastRemainingCooldown", 0f);
            Number(so, "skills.Array.data[2].preDelay", 1f);
            Number(so, "skills.Array.data[2].postDelay", 1f);
            Number(so, "skills.Array.data[2].buff.duration", 8f);
            Number(so, "skills.Array.data[2].buff.effects.Array.data[0].stat", 0f);
            Number(so, "skills.Array.data[2].buff.effects.Array.data[0].valueMode", 1f);
            Number(so, "skills.Array.data[2].buff.effects.Array.data[0].value", 1.15f);
            Number(so, "skills.Array.data[2].buff.effects.Array.data[1].stat", 4f);
            Number(so, "skills.Array.data[2].buff.effects.Array.data[1].valueMode", 1f);
            Number(so, "skills.Array.data[2].buff.effects.Array.data[1].value", 0.92f);
        }
        so.ApplyModifiedProperties(); EditorUtility.SetDirty(data); AssetDatabase.SaveAssets();
        Debug.Log(stem + ": 패턴 수치/버프/타격 타이밍 반영 완료. [Stage5 R2 Cape 2026-10-17]");
    }
    static void Number(SerializedObject so, string path, float value)
    {
        var p = so.FindProperty(path);
        if (p == null) throw new InvalidOperationException("Stage5 데이터 필드 없음: " + path);
        if (p.propertyType == SerializedPropertyType.Float) p.floatValue = value;
        else if (p.propertyType == SerializedPropertyType.Boolean) p.boolValue = value != 0;
        else if (p.propertyType == SerializedPropertyType.Enum || p.propertyType == SerializedPropertyType.Integer) p.intValue = Mathf.RoundToInt(value);
        else throw new InvalidOperationException("Stage5 데이터 타입 불일치: " + path);
    }
    static UnityEngine.Object FindData(string stem)
    {
        string preferred = "Assets/Data/Enemys/Stage5/" + stem + ".asset";
        UnityEngine.Object data = AssetDatabase.LoadMainAssetAtPath(preferred);
        if (data != null) return data;
        var paths = AssetDatabase.FindAssets(stem).Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.StartsWith("Assets/", StringComparison.Ordinal) && p.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)
                && Path.GetFileNameWithoutExtension(p) == stem).ToArray();
        if (paths.Length > 1) throw new InvalidOperationException("동명 데이터 중복: " + string.Join(", ", paths));
        return paths.Length == 1 ? AssetDatabase.LoadMainAssetAtPath(paths[0]) : null;
    }
}
#endif
