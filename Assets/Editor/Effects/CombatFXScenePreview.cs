#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// This window supplies clip time and spawn conditions. Runtime components create the visuals.
[InitializeOnLoad]
static class CombatFXScenePreview
{
    const BindingFlags Methods = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Component movement;
    static PlayerCombatEffects player;
    static MobEffectInstance transient;
    static readonly List<MobEffectInstance> attacks = new List<MobEffectInstance>();
    static Animator owner;
    static AnimationClip activeClip;
    static GameObject pose;
    static string message;
    static double nextUpdate;
    static float previousTime, loopTime;
    static CombatFXScenePreview()
    {
        EditorApplication.update += Update;
        AssemblyReloadEvents.beforeAssemblyReload += Clear;
        EditorApplication.playModeStateChanged += _ => Clear();
        Selection.selectionChanged += Clear;
        EditorApplication.quitting += Clear;
        SceneView.duringSceneGui += DrawStatus;
        EditorApplication.delayCall += RemoveOldPreviews;
    }
    static void RemoveOldPreviews()
    {
        if (Application.isPlaying) return;
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go != null && go.name == "Combat FX Scene Preview" && go.scene.IsValid() && !EditorUtility.IsPersistent(go))
                UnityEngine.Object.DestroyImmediate(go);
    }
    static void Update()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !AnimationMode.InAnimationMode()) { Clear(); return; }
        if (EditorApplication.timeSinceStartup < nextUpdate) return;
        nextUpdate = EditorApplication.timeSinceStartup + 1.0 / 60;
        var window = Resources.FindObjectsOfTypeAll<AnimationWindow>().FirstOrDefault(w => w.animationClip != null);
        var selected = Selection.activeGameObject;
        var animator = selected != null ? selected.GetComponentInParent<Animator>() ?? selected.GetComponentInChildren<Animator>(true) : null;
        if (window == null || animator == null) { Clear(); return; }
        if (owner != animator || activeClip != window.animationClip)
        {
            Clear(); owner = animator; activeClip = window.animationClip;
            player = animator.GetComponentInParent<PlayerCombatEffects>();
            movement = animator.GetComponentsInParent<Component>(true).FirstOrDefault(c => c != null &&
                c.GetType().FullName == "PlayerMovementEffects" && c.GetType().Assembly == typeof(PlayerController).Assembly);
        }
        DestroyTransient();
        string name = activeClip.name.ToLowerInvariant();
        float time = window.time;
        if (window.playing && time < previousTime - 0.05f) loopTime += activeClip.length;
        if (!window.playing) loopTime = 0;
        previousTime = time;
        float movementTime = time + loopTime;
        var controller = animator.GetComponentInParent<PlayerController>();
        float direction = controller != null ? -Mathf.Sign(controller.transform.localScale.x) : Mathf.Sign(animator.transform.lossyScale.x);
        if (direction == 0) direction = 1;
        message = null;
        try
        {
            var boss = RuntimeFXPreviewSource.Boss(animator);
            if (boss != null)
            {
                bool run = name.Contains("run") || (name.Contains("walk") && !(boss.stage == 1 && !boss.mainBoss));
                bool dash = name.Contains("dash") && !name.Contains("prepare") && !name.Contains("end");
                if (run || dash)
                {
                    transient = boss.PreviewMovement(dash, direction, (run ? movementTime : time) - (run ? boss.chaseStartDelay : 0));
                    message = "인게임 이동 FX · 접지/이동 중 조건 · 현재 씬 위치 기준";
                }
                else PreviewBossAttacks(boss, name, time, direction);
                SceneView.RepaintAll(); return;
            }
            if (movement != null && (name.Contains("run") || name.Contains("walk") || name.Contains("dash") || name == "jump"))
            {
                if (player != null) player.PreviewAttack(WeaponType.Sword, Vector2.zero, 1, -1);
                string kind = name.Contains("dash") ? "dash" : name == "jump" ? "jump" : "run";
                var data = new SerializedObject(movement);
                var anchor = data.FindProperty("groundAnchor").objectReferenceValue as Transform;
                if (anchor == null && controller != null) anchor = controller.groundCheck;
                float delay = kind == "run" ? data.FindProperty("walkStartDelay").floatValue + data.FindProperty("additionalWalkStartDelay").floatValue : 0;
                Vector3 origin = anchor != null ? SamplePosition(anchor, kind == "run" ? time : 0) : Feet(animator.transform);
                movement.GetType().GetMethod("PreviewMovement").Invoke(movement, new object[] { kind, origin, direction, (kind == "run" ? movementTime : time) - delay });
                message = "인게임 이동 FX · 접지/이동 중 조건 · 실제 생성 위치와 프레임 사용";
            }
            else if (player != null && controller != null && name.Contains("attack"))
            {
                ClearMovement();
                var weapon = PlayerController.WeaponForAttackClip(activeClip.name, controller.currentWeapon);
                var events = AnimationUtility.GetAnimationEvents(activeClip).Where(e => e.functionName == "Attack" && e.time <= time).ToArray();
                player.PreviewAttack(weapon, Vector2.zero, direction, -1);
                if (events.Length == 0)
                    message = AnimationUtility.GetAnimationEvents(activeClip).Any(e => e.functionName == "Attack") ? "Attack 이벤트 대기" : "이 클립에는 Attack 이벤트가 없어 인게임 공격 FX가 발생하지 않습니다.";
                else
                {
                    float onset = events.Last().time;
                    float offset;
                    var savedWeapon = controller.currentWeapon;
                    try
                    {
                        controller.currentWeapon = weapon;
                        offset = (float)typeof(PlayerController).GetMethod("GetWeaponOffset", Methods).Invoke(controller, null);
                    }
                    finally { controller.currentWeapon = savedWeapon; }
                    Vector3 origin = SamplePosition(controller.transform, onset) + Vector3.right * direction * offset;
                    float? weaponHeight = weapon == WeaponType.Dagger && player.DaggerEffectAnchor != null
                        ? (float?)SamplePosition(player.DaggerEffectAnchor, onset).y : null;
                    player.PreviewAttack(weapon, origin, direction, time - onset, weaponHeight);
                    message = "인게임 공격 FX · 무기: " + weapon + " · 이벤트 " + onset.ToString("0.###") + "초";
                }
            }
            else
            {
                ClearMovement();
                if (player != null) player.PreviewAttack(WeaponType.Sword, Vector2.zero, 1, -1);
                PreviewMob(animator, name, time, direction);
            }
        }
        catch (Exception ex) { Clear(); Debug.LogException(ex); }
        SceneView.RepaintAll();
    }
    static void PreviewBossAttacks(BossEffects boss, string name, float time, float direction)
    {
        var driver = owner.GetComponentInChildren<BossClipEffects>(true);
        if (boss.mainBoss && boss.stage == 5 && driver != null && driver.runtimeEnabled && driver.Find(activeClip)?.runtime == true) return;
        if (!name.Contains("attack") && !name.Contains("combo") && !name.Contains("skill")) return;
        var controller = RuntimeFXPreviewSource.BossTemplate ?? boss.GetComponent<MidBossController>();
        if (controller == null || controller.data == null) return;
        int index = -1;
        var match = System.Text.RegularExpressions.Regex.Match(name, @"skill([1-9])");
        if (name.Contains("dskill")) { message = "욕망 스킬은 전용 실행 상태가 필요합니다."; return; }
        if (match.Success) index = int.Parse(match.Groups[1].Value) - 1;
        else if (name.Contains("combo3")) index = 1;
        BossSkillData skill = index < 0 ? controller.data.basicAttack : index < controller.data.skills.Count ? controller.data.skills[index] : null;
        if (skill?.active?.attackSteps == null) return;
        float onset = skill.preDelay; int ordinal = 0;
        var timing = boss.comboTimings?.FirstOrDefault(t => t.clip == activeClip);
        foreach (var step in skill.active.attackSteps)
        {
            if (step == null) continue;
            onset += step.preDelay;
            if (step.actionType == BossActionType.DashAttack && time >= onset && time < onset + step.duration)
            {
                var trail = boss.PreviewMovement(true, direction, time - onset);
                if (trail != null) attacks.Add(trail);
            }
            int count = step.damage > 0 && step.actionType != BossActionType.SpawnPrefab ? step.attackCount : 0;
            for (int pulse = 0; pulse < count; pulse++, ordinal++)
            {
                float impact = onset + pulse * step.interval;
                if (timing?.impacts != null && ordinal < timing.impacts.Length) impact = Mathf.Max(impact, timing.impacts[ordinal]);
                if (time < impact) continue;
                float reach = controller is Stage1_MidBoss mid && (step.actionType == BossActionType.MeleeAttack || step.actionType == BossActionType.Slam) ? mid.MeleeReachBonus : 0;
                Vector3 center = SamplePosition(owner.transform, impact) + new Vector3(direction * (step.hitboxOffset.x + reach), step.hitboxOffset.y, 0);
                if (step.hitboxSize.x <= 0 || step.hitboxSize.y <= 0) continue;
                var instance = boss.PreviewStep(skill, index, step, pulse, center, direction, time - impact);
                if (instance != null) attacks.Add(instance);
            }
            onset += (step.actionType == BossActionType.MeleeAttack ? Mathf.Max(0, step.attackCount - 1) * step.interval :
                step.actionType == BossActionType.SpawnPrefab ? Mathf.Max(0, step.spawnCount - 1) * step.spawnInterval : step.duration) + step.postDelay;
        }
        message = "인게임 공격 생성 코드 · 스킬 데이터 시간 / 현재 씬 배치 기준 (추적·충돌 조건 제외)";
    }
    static void PreviewMob(Animator animator, string name, float time, float direction)
    {
        var mob = RuntimeFXPreviewSource.Mob(animator);
        if (mob == null) { message = RuntimeFXPreviewSource.Error; return; }
        MobEffects.Effect effect; Vector3 position; float start = 0, speed = 1;
        if (name.Contains("run") || name.Contains("walk")) { effect = mob.chase; position = Feet(mob.transform); }
        else if (name.Contains("death")) { effect = mob.death; position = Center(mob.transform); }
        else if (name.Contains("hit")) { effect = mob.hit; position = Center(mob.transform); }
        else if (name.Contains("attack"))
        {
            var events = AnimationUtility.GetAnimationEvents(activeClip).Where(e => e.functionName == "CheckHit" && e.time <= time).ToArray();
            if (events.Length == 0) { message = "공격 이벤트 대기 / AI·코루틴 타이밍은 게임 상태가 필요합니다."; return; }
            start = events.Last().time; effect = mob.attack; speed = 1.5f;
            var enemy = RuntimeFXPreviewSource.MobTemplate ?? mob.GetComponent<EnemyController>();
            var data = enemy != null ? new SerializedObject(enemy).FindProperty("data") : null;
            if (data == null || data.objectReferenceValue == null) { message = "적 공격 데이터가 없습니다."; return; }
            var config = new SerializedObject(data.objectReferenceValue);
            position = SamplePosition(owner.transform, start) + new Vector3(config.FindProperty("attackOffset").floatValue * direction, config.FindProperty("attackHeightOffset").floatValue, 0);
        }
        else return;
        // Invoke the runtime factory, including its sorting, offset and scale rules.
        transient = (MobEffectInstance)typeof(MobEffects).GetMethod("Play", Methods).Invoke(mob, new object[] { effect, position, direction, speed });
        if (transient != null) transient.PreviewSample(time - start);
    }
    static Vector3 Center(Transform root) { var c = root.GetComponent<Collider2D>(); return c != null ? c.bounds.center : root.position; }
    static Vector3 Feet(Transform root) { var c = root.GetComponent<Collider2D>(); return c != null ? new Vector3(c.bounds.center.x, c.bounds.min.y, root.position.z) : root.position; }
    static Vector3 SamplePosition(Transform target, float time)
    {
        if (target != owner.transform && !target.IsChildOf(owner.transform)) return target.position;
        if (pose == null) pose = CopyTransforms(owner.transform, null).gameObject;
        pose.transform.position = owner.transform.position;
        pose.transform.rotation = owner.transform.rotation;
        pose.transform.localScale = owner.transform.lossyScale;
        activeClip.SampleAnimation(pose, time);
        string path = AnimationUtility.CalculateTransformPath(target, owner.transform);
        var sampled = string.IsNullOrEmpty(path) ? pose.transform : pose.transform.Find(path);
        return sampled != null ? sampled.position : target.position;
    }
    static Transform CopyTransforms(Transform source, Transform parent)
    {
        var go = new GameObject(source.name) { hideFlags = HideFlags.HideAndDontSave };
        go.SetActive(false);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = source.localPosition;
        go.transform.localRotation = source.localRotation;
        go.transform.localScale = source.localScale;
        foreach (Transform child in source) CopyTransforms(child, go.transform);
        return go.transform;
    }
    static void ClearMovement() { if (movement != null) movement.GetType().GetMethod("ClearPreview")?.Invoke(movement, null); }
    static void DestroyTransient()
    {
        if (transient != null) UnityEngine.Object.DestroyImmediate(transient.gameObject); transient = null;
        foreach (var instance in attacks) if (instance != null) UnityEngine.Object.DestroyImmediate(instance.gameObject);
        attacks.Clear();
    }
    static void Clear()
    {
        DestroyTransient(); ClearMovement();
        if (player != null) player.ClearPreview();
        if (pose != null) UnityEngine.Object.DestroyImmediate(pose);
        RuntimeFXPreviewSource.Clear();
        previousTime = 0; loopTime = 0;
        pose = null; owner = null; activeClip = null; movement = null; player = null; message = null;
    }
    static void DrawStatus(SceneView view)
    {
        if (string.IsNullOrEmpty(message)) return;
        Handles.BeginGUI(); GUI.Label(new Rect(50, 36, view.position.width - 62, 42), message, EditorStyles.helpBox); Handles.EndGUI();
    }
}
#endif
