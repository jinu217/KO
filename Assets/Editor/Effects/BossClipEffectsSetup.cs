#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using S = BossClipEffects.Style;
using A = BossClipEffects.Anchor;

[InitializeOnLoad]
public static class BossClipEffectsSetup
{
    static BossClipEffects preview;
    [MenuItem("Tools/Effects/Bosses/Refresh Buff Effects Only")]
    public static void RefreshBuffs()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || AnimationMode.InAnimationMode())
            throw new InvalidOperationException("Edit Mode required.");
        ClearPreview();
        int count=0;
        for(int stage=1;stage<=5;stage++) foreach(bool main in new[]{false,true})
        {
            string runtimePath=$"Assets/Prefabs/Stage{stage}_Enemy/Stage{stage}_{(main?"Main":"Mid")}Boss.prefab";
            var template=AssetDatabase.LoadAssetAtPath<GameObject>(runtimePath)?.GetComponent<BossEffects>();
            if(template==null)throw new InvalidOperationException("Boss source missing: "+runtimePath);
            foreach(string path in new[]{runtimePath,$"Assets/Prefab/{stage}/{(main?"Main":"Mid")}Boss {stage}.prefab"})
            {
                string backup="C:/Users/User/Documents/Codex/2026-10-26/new-chat-3/work/buff_motion_before/"+path;
                Directory.CreateDirectory(Path.GetDirectoryName(backup));if(!File.Exists(backup))File.Copy(path,backup);
                var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var driver=root.GetComponentInChildren<BossClipEffects>(true);
                    if(driver==null)throw new InvalidOperationException("Boss clip driver missing: "+path);
                    ConfigureBuffs(driver,root.GetComponent<BossEffects>() ?? template);
                    PrefabUtility.SaveAsPrefabAsset(root,path);count++;
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
        }
        AssetDatabase.SaveAssets();Debug.Log("[Boss Buff FX] Updated "+count+" prefabs; attack and movement tracks preserved.");
    }
    static void ConfigureBuffs(BossClipEffects driver,BossEffects fx)
    {
        MobEffects.Effect Charge(string name)=>new MobEffects.Effect {
            sheet=Sheet("공격&데미지받을시&사망&대쉬등 다있는곳/"+name+".png"),columns=20,fps=20/.7f,scale=1
        };
        driver.buffOffense=Charge("charge_red_01");driver.buffDefense=Charge("charge_blue_01");
        driver.buffSpeed=Charge("charge_yellow_01");driver.buffRecovery=Charge("charge_green_01");
        var controller=fx.GetComponent<MidBossController>();if(controller==null || controller.data==null)return;
        foreach(var track in driver.tracks)
        {
            if(track.clip==null)continue;
            string name=track.clip.name.ToLowerInvariant();
            if(name.Contains("dskill") || name.Contains("desire"))continue;
            var match=Regex.Match(name,@"skill([1-9])");
            int index=match.Success?int.Parse(match.Groups[1].Value)-1:-1;
            if(index<0 && name.Contains("buff"))index=fx.stage==1?0:2;
            if(index<0)continue;
            index=BossEffects.ClipSkillIndex(fx.stage,fx.mainBoss,track.clip.name,index);
            if(index>=controller.data.skills.Count)continue;
            var skill=controller.data.skills[index];if(skill.skillType!=BossSkillType.Buff)continue;
            // Keep the user's existing MidBoss1 typhoon, including its original size/timing.
            if(fx.stage==1 && !fx.mainBoss){track.runtime=true;driver.runtimeEnabled=true;continue;}
            var kind=BossClipEffects.BuffKind.Speed;
            if(skill.buff.effects.Any(e=>e.stat==BossBuffStat.Damage))kind=BossClipEffects.BuffKind.Offense;
            if(skill.buff.effects.Any(e=>e.stat==BossBuffStat.Defense || e.stat==BossBuffStat.DamageReduction))kind=BossClipEffects.BuffKind.Defense;
            if(skill.buff.effects.Any(e=>e.stat==BossBuffStat.HealthRegeneration || e.stat==BossBuffStat.MaxHp))kind=BossClipEffects.BuffKind.Recovery;
            float impact=fx.stage==3 && !fx.mainBoss ? .9f : skill.preDelay;
            var apply=AnimationUtility.GetAnimationEvents(track.clip).FirstOrDefault(e=>(e.stringParameter??"").Contains("BuffApply"));
            if(apply!=null)impact=apply.time;
            float start=Mathf.Clamp(impact-.22f,0,Mathf.Max(0,track.clip.length-.7f));
            var cues=track.cues.Where(c=>c.style!=S.Buff).ToList();
            cues.Add(new BossClipEffects.Cue{style=S.Buff,anchor=A.Body,buffKind=kind,time=start,duration=Mathf.Min(.7f,track.clip.length-start),scale=1});
            track.cues=cues.ToArray();track.runtime=true;driver.runtimeEnabled=true;
        }
        EditorUtility.SetDirty(driver);
    }
    public static void RefreshStageFive()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required.");
        foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefab/5","Assets/Prefabs/Stage5_Enemy"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(asset.GetComponentInChildren<BossClipEffects>(true)==null)continue;
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(var fx in root.GetComponentsInChildren<BossClipEffects>(true))
                    if(fx.animator!=null && fx.source!=null)
                        Configure(fx.animator,fx.source,5,fx.animator.runtimeAnimatorController.animationClips.Any(c=>c.name.StartsWith("MainBoss5_")));
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
    }
    const string InstallRequest = "Library/BossFX_R51.install";
    static bool installRequested = System.IO.File.Exists(InstallRequest); // R51: normalized sheet cells support imported texture resizing.
    static void InstallWhenReady()
    {
        if (!installRequested || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode || AnimationMode.InAnimationMode()) return;
        installRequested = false;
        try { ApplyAndVerify(); System.IO.File.Delete(InstallRequest); }
        catch (Exception error)
        {
            System.IO.File.WriteAllText("Library/BossFX_R51_Verification_ERROR.txt", error.ToString());
            Debug.LogException(error);
        }
    }
    static BossClipEffectsSetup()
    {
        EditorApplication.update += UpdatePreview;
        EditorApplication.update += InstallWhenReady;
        AssemblyReloadEvents.beforeAssemblyReload += ClearPreview;
        EditorApplication.playModeStateChanged += _ => ClearPreview();
        Selection.selectionChanged += ClearPreview;
    }
    [MenuItem("Tools/Effects/Bosses/Setup All Boss FX And Scene Preview (R51)")]
    public static void ApplyAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || AnimationMode.InAnimationMode())
            throw new InvalidOperationException("Play와 Animation Preview를 종료하고 실행하세요.");
        ClearPreview();
        PrepareCombatSources();
        var templates = new Dictionary<string, BossEffects>();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var fx = asset != null ? asset.GetComponentInChildren<BossEffects>(true) : null;
            if (fx != null) templates[Key(fx.stage, fx.mainBoss)] = fx;
        }
        int count = 0;
        var paths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefab", "Assets/Prefabs" })
            .Select(AssetDatabase.GUIDToAssetPath).Distinct().ToArray();
        foreach (string path in paths)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) continue;
            var animators = asset.GetComponentsInChildren<Animator>(true);
            if (!animators.Any(a => a.runtimeAnimatorController != null &&
                a.runtimeAnimatorController.animationClips.Any(c => Regex.IsMatch(c.name, @"(?i)(main|mid)boss\s*[1-5]")))) continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                bool changed = false;
                foreach (var animator in root.GetComponentsInChildren<Animator>(true))
                {
                    if (animator.runtimeAnimatorController == null) continue;
                    var clips = animator.runtimeAnimatorController.animationClips.Distinct().ToArray();
                    var match = clips.Select(c => Regex.Match(c.name, @"(?i)(main|mid)boss\s*([1-5])")).FirstOrDefault(m => m.Success);
                    if (match == null) continue;
                    int stage = int.Parse(match.Groups[2].Value); bool main = match.Groups[1].Value.Equals("main", StringComparison.OrdinalIgnoreCase);
                    if (!templates.TryGetValue(Key(stage, main), out var template))
                        throw new InvalidOperationException("Boss FX template missing: " + path);
                    var ownSource = root.GetComponentsInChildren<BossEffects>(true).FirstOrDefault(f => f.stage == stage && f.mainBoss == main);
                    Configure(animator, ownSource != null ? ownSource : template, stage, main);
                    changed = true;
                }
                if (changed) { PrefabUtility.SaveAsPrefabAsset(root, path); count++; }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[Boss FX R51] " + count + "개 보스 프리팹: 클립 이펙트와 씬 미리보기 연결 완료.");
    }
    static Texture2D Sheet(string suffix)
    {
        var paths = AssetDatabase.FindAssets(System.IO.Path.GetFileNameWithoutExtension(suffix) + " t:Texture2D")
            .Select(AssetDatabase.GUIDToAssetPath).Where(p => p.EndsWith("/사용할 이펙트/" + suffix, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (paths.Length != 1) throw new InvalidOperationException("FX sheet missing or ambiguous: " + suffix);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(paths[0]);
    }
    static void PrepareCombatSources()
    {
        // Read shared art without changing import settings used by players or normal enemies.
        var slash = Sheet("공격/slash_big.png"); var sting = Sheet("공격/sting.png");
        var cut = Sheet("공격/cut_medium.png"); var impact = Sheet("데미지받을시/impact_big.png");
        var death = Sheet("공격&데미지받을시&사망&대쉬등 다있는곳/death_01.png"); var dust = Sheet("걸을때 지속.png");
        var dash = Enumerable.Range(1, 6).Select(i => AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(
            Sheet("대쉬/SM7001_" + i.ToString("00") + ".png"))).OfType<Sprite>().Single()).ToArray();
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var stageMatch = Regex.Match(path, @"Stage([1-5])_Enemy/");
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!stageMatch.Success || asset == null || asset.GetComponentInChildren<MidBossController>(true) == null) continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var boss in root.GetComponentsInChildren<MidBossController>(true))
                {
                    var fx = boss.GetComponent<BossEffects>() ?? boss.gameObject.AddComponent<BossEffects>();
                    fx.stage = int.Parse(stageMatch.Groups[1].Value); fx.mainBoss = boss is MainBossController;
                    Fill(fx.attack, slash, true, 2); Fill(fx.upward, slash, false, 2); Fill(fx.horizontal, sting, false, 2);
                    Fill(fx.spin, cut, false, 2); Fill(fx.punch, impact, false, 1.6f); Fill(fx.hit, impact, false, 1.2f);
                    Fill(fx.death, death, false, 2, 24); Fill(fx.chase, dust, false, 1.5f, 8);
                    if (fx.dash.sheet == null && (fx.dash.frames == null || fx.dash.frames.Length == 0))
                    { fx.dash.frames = dash; fx.dash.fps = 20; fx.dash.scale = 3; fx.dash.artFacesRight = false; }
                    fx.motionAnimator = boss.GetComponentInChildren<Animator>(true);
                    if (fx.stage == 1 && !fx.mainBoss) BossEffectsSetup.ConfigureComboTimings(fx);
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
    static void Fill(MobEffects.Effect effect, Texture2D sheet, bool flip, float scale, int columns = 10)
    {
        if (effect.sheet != null || (effect.frames != null && effect.frames.Length > 0)) return;
        effect.sheet = sheet; effect.columns = columns; effect.flipY = flip; effect.scale = scale;
    }
    static string Key(int stage, bool main) => stage + (main ? "Main" : "Mid");
    static void Configure(Animator animator, BossEffects source, int stage, bool main)
    {
        var driver = animator.GetComponent<BossClipEffects>();
        if (driver == null) driver = animator.gameObject.AddComponent<BossClipEffects>();
        driver.Release(); driver.animator = animator; driver.Configure(source);
        driver.buff = new MobEffects.Effect { sheet = Sheet("공격&데미지받을시&사망&대쉬등 다있는곳/charge_spiral_01.png"), columns = 24, fps = 30, scale = 2 };
        // A visual-only prefab must not reference a component in a different prefab asset at runtime.
        driver.source = source.transform.IsChildOf(animator.transform.root) ? source : null;
        driver.runtimeEnabled = (stage == 5 && main) || (stage == 1 && !main);
        if (stage == 1 && !main)
        {
            driver.buff = MidOneSheet("typhoon_01"); driver.sand = MidOneSheet("sand_03");
        }
        var transforms = animator.GetComponentsInChildren<Transform>(true);
        driver.body = transforms.FirstOrDefault(t => t.name == "Body") ?? animator.transform;
        driver.weapon = transforms.FirstOrDefault(t => new[] { "Sword", "Weapon", "Axe", "Scythe", "Spear", "Mace" }.Contains(t.name));
        driver.feet = transforms.FirstOrDefault(t => t.name == "Foot_F") ?? transforms.FirstOrDefault(t => t.name.Contains("Foot")) ?? animator.transform;
        var clips = animator.runtimeAnimatorController.animationClips.Distinct().ToList();
        var folders = clips.Select(c => System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(c)).Replace('\\', '/')).Distinct();
        foreach (var folder in folders.ToArray())
            foreach (var guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { folder }))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guid));
                if (clip != null && Regex.IsMatch(clip.name, "(?i)" + (main ? "main" : "mid") + @"boss\s*" + stage) && !clips.Contains(clip)) clips.Add(clip);
            }
        driver.tracks = clips.Select(c => BuildTrack(c, source, stage, main)).ToArray();
        ConfigureBuffs(driver,source);
        foreach (var track in driver.tracks) Validate(track);
        EditorUtility.SetDirty(driver);
    }
    static MobEffects.Effect MidOneSheet(string name)
    {
        string path = AssetDatabase.GetAssetPath(Sheet("공격&데미지받을시&사망&대쉬등 다있는곳/" + name + ".png"));
        // Single import mode: use sheet cells instead of stale sliced sprite IDs.
        int columns = 24;
        return new MobEffects.Effect { sheet = AssetDatabase.LoadAssetAtPath<Texture2D>(path),
            columns = columns, fps = columns / .5f, scale = 2 };
    }
    public static BossClipEffects.Track BuildTrack(AnimationClip clip, BossEffects fx, int stage, bool main)
    {
        var cues = new List<BossClipEffects.Cue>();
        string name = clip.name.ToLowerInvariant();
        bool mainFive = stage == 5 && main;
        bool runtime = mainFive && (name.Contains("attack") || name.Contains("skill") || name.Contains("desiresuccess"));
        if (stage == 5 && (name.Contains("skill") || name.Contains("attack") || name.Contains("desiresuccess")))
            StageFive(clip, cues, main);
        else if (name.Contains("death")) Add(cues, S.Death, Mathf.Max(0, clip.length - .65f), .65f, A.Body);
        else if (name.Contains("hit")) Add(cues, S.Hit, 0, .3f, A.Body);
        else if (stage == 1 && !main && name.Contains("walk")) { }
        else if (Regex.IsMatch(name, "walk|run|chase")) Add(cues, S.Dust, .3f, Mathf.Max(.01f, clip.length - .3f), A.Feet, true);
        else Generic(clip, cues, fx);
        if (stage == 1 && !main)
        {
            runtime = name.Contains("buff") || name.Contains("combo3");
            if (name.Contains("combo3"))
            {
                var down = cues.Where(c => c.style == S.Down).Skip(1).FirstOrDefault();
                if (down != null) { down.time += .1f; down.style = S.Sand; down.anchor = A.WeaponTip;
                    down.offset = Vector2.zero; down.duration = .5f; }
            }
        }
        return new BossClipEffects.Track { clip = clip, runtime = runtime, cues = cues.ToArray() };
    }
    static BossClipEffects.Cue Add(List<BossClipEffects.Cue> list, S style, float time, float duration = .28f, A anchor = A.Weapon, bool loop = false)
    {
        var cue = new BossClipEffects.Cue { style = style, time = time, duration = duration, anchor = anchor, loop = loop };
        list.Add(cue); return cue;
    }
    static void StageFive(AnimationClip clip, List<BossClipEffects.Cue> cues, bool main)
    {
        var events = AnimationUtility.GetAnimationEvents(clip).Where(e => e.functionName == "OnStage5AnimationCue").ToArray();
        string name = clip.name.ToLowerInvariant();
        foreach (var e in events)
        {
            string signal = e.stringParameter ?? "";
            if (signal.EndsWith("Slam")) { Add(cues, S.Down, e.time); Add(cues, S.Punch, e.time, .22f, A.Feet).scale = .65f; }
            else if (signal.Contains("SpinStart"))
            {
                float end = events.FirstOrDefault(x => x.stringParameter == "Attack.SpinEnd")?.time ?? e.time + .6f;
                Add(cues, S.Spin, e.time, end - e.time, A.Body, true);
            }
            else if (signal.Contains("DashStart") || signal.Contains("PassStart"))
            {
                string endName = signal.Replace("Start", "End");
                float end = events.FirstOrDefault(x => x.stringParameter == endName)?.time
                    ?? events.FirstOrDefault(x => x.stringParameter == signal.Replace("Start", "Timeout"))?.time ?? clip.length;
                var cue = Add(cues, S.Dash, e.time, Mathf.Max(.01f, end - e.time), A.Feet, true);
                cue.offset = new Vector2(-.5f, .05f); cue.scale = .8f;
                if (signal.Contains("PassStart"))
                {
                    int pass = int.Parse(signal.Substring(signal.LastIndexOf('.') + 1));
                    cue.angle = pass == 3 ? 0 : (pass == 1 ? -1 : 1) * Mathf.Atan2(3.6f, 16f) * Mathf.Rad2Deg;
                    // Second pass is the authored 22..24f downward cut. Other passes follow the actual blade.
                    var slash = Add(cues, pass == 1 ? S.Down : S.Thrust, e.time + (pass == 1 ? .5f : 1f) / 12, .22f);
                    slash.alignBlade = pass != 1;
                }
            }
            else if (signal.StartsWith("Skill1.Hit."))
            {
                string[] parts = signal.Split('.'); int hit = int.Parse(parts[parts.Length - 1]);
                if (hit == 2) continue; // The middle two damage slots are one uninterrupted rotation.
                var cue = Add(cues, hit == 0 ? S.Down : hit == 1 ? S.Spin : S.Thrust,
                    e.time - 1.5f / 12, hit == 1 ? 3.8f / 12 : 2.5f / 12);
                cue.scale = .7f; cue.alignBlade = hit == 3; cue.weaponSide = true;
            }
            else if (signal.StartsWith("Skill1.Pulse"))
            {
                int hit = int.Parse(signal.Substring("Skill1.Pulse".Length)) - 1;
                var cue = Add(cues, hit == 1 ? S.Up : hit == 3 ? S.Down : S.Thrust, e.time, .24f);
                cue.alignBlade = cue.style == S.Thrust;
            }
            else if (signal.Contains("BuffApply"))
            {
                Add(cues, S.Buff, e.time, .55f, A.Body).scale = .8f;
                if (signal.StartsWith("Skill4")) Add(cues, S.Dust, e.time, .22f, A.Feet).scale = .55f;
            }
            else if (signal == "Desire.Begin") Add(cues, S.Buff, 6f / 12, Mathf.Max(.1f, clip.length - 12f / 12), A.Body, true).scale = .8f;
            else if (signal == "Desire.RestoreHP") Add(cues, S.Buff, e.time, .65f, A.Body).scale = 1.1f;
            // skill3_Follow already has the blade-sized typhoon; never stack the generic spin on it.
        }
    }
    static void Generic(AnimationClip clip, List<BossClipEffects.Cue> cues, BossEffects fx)
    {
        var boss = fx.GetComponent<MidBossController>();
        if (boss == null || boss.data == null) return;
        string name = clip.name.ToLowerInvariant();
        if (name.Contains("buff")) { Add(cues, S.Buff, clip.length * .5f, .5f, A.Body); return; }
        if (Regex.IsMatch(name, "idle|groggy|p2e|p3e|dashprepare|dashend")) return;
        int index = -1;
        var match = Regex.Match(name, @"skill([1-9])");
        if (name.Contains("dskill")) return; // Dedicated desire systems must not invent damage or spawn FX.
        if (match.Success) index = int.Parse(match.Groups[1].Value) - 1;
        else if (name.Contains("combo3")) index = 1;
        index = BossEffects.ClipSkillIndex(fx.stage, fx.mainBoss, clip.name, index);
        BossSkillData skill = index < 0 ? boss.data.basicAttack : index < boss.data.skills.Count ? boss.data.skills[index] : null;
        if (skill == null) return;
        if (name.Contains("dash")) { Add(cues, S.Dash, 0, clip.length, A.Feet, true); return; }
        if (skill.active?.attackSteps == null) return;
        float time = skill.preDelay;
        int ordinal = 0;
        foreach (var step in skill.active.attackSteps)
        {
            if (step == null) continue;
            time += step.preDelay;
            bool motionWindow = BossEffects.TryMovementWindow(clip, step.actionType, out float windowStart, out float windowEnd);
            if (motionWindow) time = windowStart;
            if (step.actionType == BossActionType.DashAttack) Add(cues, S.Dash, time, Mathf.Max(.01f, motionWindow ? windowEnd - windowStart : step.duration), A.Feet, true);
            int count = step.damage > 0 && step.actionType != BossActionType.SpawnPrefab ? step.attackCount : 0;
            for (int pulse = 0; pulse < count; pulse++)
            {
                float impact = time + pulse * step.interval;
                if ((step.actionType == BossActionType.MeleeAttack || step.actionType == BossActionType.Slam)
                    && fx.TryMotionImpact(clip, ordinal + pulse, out float authoredImpact)) impact = authoredImpact;
                var kind = fx.ResolveForPreview(skill, index, step, pulse, out float angle);
                if (kind != BossEffects.Kind.None)
                {
                    S style = kind == BossEffects.Kind.Up ? S.Up : kind == BossEffects.Kind.Horizontal || kind == BossEffects.Kind.Thrust ? S.Thrust :
                        kind == BossEffects.Kind.Spin ? S.Spin : kind == BossEffects.Kind.Punch || kind == BossEffects.Kind.Uppercut ? S.Punch : S.Down;
                    var cue = Add(cues, style, impact, .28f, A.Hitbox); cue.offset = step.hitboxOffset;
                    var rule = fx.overrides?.FirstOrDefault(r => r.skillIndex == index && r.stepIndex == skill.active.attackSteps.IndexOf(step) && r.pulse == pulse);
                    if (rule != null) cue.offset += rule.offset; cue.angle = kind == BossEffects.Kind.Uppercut ? 65 : angle;
                    if (kind == BossEffects.Kind.Horizontal && ((fx.stage == 1 && !fx.mainBoss) || (fx.stage == 2 && fx.mainBoss)))
                    { cue.scale = fx.midBoss1HorizontalScale; cue.offset.y += fx.midBoss1HorizontalHeight + fx.additionalHeavyHorizontalHeight; }
                }
            }
            if (step.actionType == BossActionType.MeleeAttack || step.actionType == BossActionType.Slam
                || (fx.stage == 4 && fx.mainBoss && step.actionType == BossActionType.DashAttack)) ordinal += Mathf.Max(1, step.attackCount);
            if (motionWindow) { time = windowEnd + step.postDelay; continue; }
            float length = step.actionType == BossActionType.MeleeAttack ? Mathf.Max(0, step.attackCount - 1) * step.interval :
                step.actionType == BossActionType.SpawnPrefab ? Mathf.Max(0, step.spawnCount - 1) * step.spawnInterval : step.duration;
            time += length + step.postDelay;
        }
        // Some legacy clips are individual sections rather than an entire data action.
        // Keep the mapping reviewable instead of silently stretching unrelated timing.
        cues.RemoveAll(c => c.time >= clip.length);
    }
    static void Validate(BossClipEffects.Track track)
    {
        foreach (var cue in track.cues)
            if (cue.time < 0 || cue.duration <= 0 || float.IsNaN(cue.time))
                throw new InvalidOperationException("Invalid FX cue: " + track.clip.name);
        if (track.clip.name.Equals("MainBoss5_skill5", StringComparison.OrdinalIgnoreCase))
        {
            var cuts = track.cues.Where(c => c.style == S.Down).ToArray();
            if (cuts.Length != 1 || cuts[0].time > 23f / 12 || track.cues.Count(c => c.style == S.Dash) != 4)
                throw new InvalidOperationException("Skill5 FX timeline does not match the four attacks.");
        }
    }
    public static void ApplyAndVerify()
    {
        ApplyAll();
        var report = new List<string>(); var identities = new HashSet<string>(); int sampled = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null || asset.GetComponentInChildren<BossClipEffects>(true) == null) continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var driver in root.GetComponentsInChildren<BossClipEffects>(true))
                {
                    var fx = root.GetComponentInChildren<BossEffects>(true);
                    identities.Add(Key(fx.stage, fx.mainBoss));
                    int count = 0;
                    foreach (var track in driver.tracks)
                    {
                        Validate(track);
                        foreach (var cue in track.cues)
                        {
                            if (cue.time >= track.clip.length) continue;
                            float time = Mathf.Min(track.clip.length, cue.time + Mathf.Min(.025f, cue.duration * .25f));
                            track.clip.SampleAnimation(driver.gameObject, time);
                            driver.Sample(track.clip, time);
                            if (driver.VisibleCount == 0) throw new InvalidOperationException("FX not visible: " + path + " / " + track.clip.name);
                            count++; sampled++;
                        }
                        // Reverse scrubbing to zero must not retain an effect from the later frame.
                        driver.Sample(track.clip, -1);
                        if (driver.VisibleCount != 0) throw new InvalidOperationException("FX remained after reverse scrub: " + track.clip.name);
                        driver.Release();
                        if (driver.VisibleCount != 0) throw new InvalidOperationException("FX cleanup failed: " + track.clip.name);
                        if (track.cues.Length == 0 && Regex.IsMatch(track.clip.name, "(?i)attack|skill|combo"))
                            report.Add("NO_ADDITIONAL_FX (buff/spawn/dedicated action): " + track.clip.name);
                    }
                    report.Add(path + ": " + driver.tracks.Length + " clips, " + count + " sampled cues");
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        if (identities.Count != 10) throw new InvalidOperationException("Expected all 10 bosses, found " + identities.Count);
        report.Insert(0, "PASS: 10 bosses; " + sampled + " FX cue samples; forward/reverse sampling and cleanup completed.");
        System.IO.File.WriteAllLines("Library/BossFX_R51_Verification.txt", report);
        Debug.Log(string.Join("\n", report));
        AssetDatabase.SaveAssets();
        CaptureMainFive();
    }
    static void CaptureMainFive()
    {
        string path = "Assets/Prefab/5/MainBoss 5.prefab";
        string folder = "Library/BossFX_R51_Previews"; System.IO.Directory.CreateDirectory(folder);
        foreach (var sample in new[] { ("MainBoss5_skill5", 23f / 12), ("MainBoss5_skill1", 13f / 12), ("MainBoss5_skill4", 19f / 12) })
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            Camera camera = null; RenderTexture target = null; Texture2D image = null;
            try
            {
                var driver = root.GetComponentInChildren<BossClipEffects>(true);
                var track = driver.tracks.First(t => t.clip.name == sample.Item1);
                track.clip.SampleAnimation(driver.gameObject, sample.Item2); driver.Sample(track.clip, sample.Item2);
                var renderers = root.GetComponentsInChildren<SpriteRenderer>(true).Where(r => r.enabled && r.sprite != null).ToArray();
                Bounds bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                var obj = new GameObject("FX verification camera") { hideFlags = HideFlags.HideAndDontSave };
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(obj, root.scene);
                camera = obj.AddComponent<Camera>(); camera.scene = root.scene;
                camera.orthographic = true; camera.orthographicSize = Mathf.Max(bounds.extents.y, bounds.extents.x) * 1.4f;
                camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -20);
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.13f, .14f, .16f, 1);
                target = new RenderTexture(640, 640, 24); camera.targetTexture = target; camera.Render();
                var previous = RenderTexture.active;
                try
                {
                    RenderTexture.active = target; image = new Texture2D(640, 640, TextureFormat.RGB24, false);
                    image.ReadPixels(new Rect(0, 0, 640, 640), 0, 0); image.Apply();
                    System.IO.File.WriteAllBytes(folder + "/" + sample.Item1 + ".png", image.EncodeToPNG());
                }
                finally { RenderTexture.active = previous; }
                driver.Release();
            }
            finally
            {
                if (camera != null) UnityEngine.Object.DestroyImmediate(camera.gameObject);
                if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                if (image != null) UnityEngine.Object.DestroyImmediate(image);
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
    static void ClearPreview()
    { if (preview != null) preview.Release(); preview = null; }
    static void UpdatePreview()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !AnimationMode.InAnimationMode()) { ClearPreview(); return; }
        var window = Resources.FindObjectsOfTypeAll<AnimationWindow>().FirstOrDefault(w => w.animationClip != null);
        var selected = Selection.activeGameObject;
        if (window == null || selected == null) { ClearPreview(); return; }
        var candidate = selected.GetComponentInParent<BossClipEffects>() ?? selected.GetComponentInChildren<BossClipEffects>(true);
        if (candidate == null || candidate.Find(window.animationClip) == null) { ClearPreview(); return; }
        string clipName = window.animationClip.name.ToLowerInvariant();
        // Movement is rendered by the same BossEffects factory used in-game.
        if (clipName.Contains("run") || clipName.Contains("walk") ||
            (clipName.Contains("dash") && !clipName.Contains("prepare") && !clipName.Contains("end")))
        { ClearPreview(); return; }
        if (preview != candidate) { ClearPreview(); preview = candidate; }
        bool sharedTrack = candidate.runtimeEnabled && System.Text.RegularExpressions.Regex.IsMatch(window.animationClip.name, @"(?i)mainboss\s*5") && candidate.Find(window.animationClip).runtime;
        preview.Sample(window.animationClip, window.time, false, !sharedTrack);
        SceneView.RepaintAll();
    }
}
#endif
