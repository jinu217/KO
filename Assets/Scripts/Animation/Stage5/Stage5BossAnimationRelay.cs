using System;
using System.Collections;
using UnityEngine;

// Animation-only bridge. Gameplay code owns damage, movement, buffs and hit confirmation.
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class Stage5BossAnimationRelay : MonoBehaviour
{
    public event Action<string> Cue;
    [HideInInspector] public float frontWeapon;
    [HideInInspector] public float frontBlade;
    [HideInInspector] public float frontRearArm;
    [HideInInspector] public float npcBlade;
    [HideInInspector] public float hideVisual;
    [SerializeField, HideInInspector] private SpriteRenderer backHand, blade, backForearm;
    [SerializeField, HideInInspector] private int backHandLayer, bladeLayer, frontLayer, backForearmLayer;
    [SerializeField, HideInInspector] private SpriteRenderer[] visualRenderers;
    [SerializeField, HideInInspector] private bool[] rendererEnabled;
    private bool runtimeHidden;
    // Runtime spare sword owns only blade depth; the hand keeps its authored layer.
    private int orbitBladeLayer = -1;
    public void SetOrbitBladeDepth(bool? inFront)
    {
        orbitBladeLayer = inFront.HasValue ? SortingLayer.NameToID(inFront.Value ? "Front" : "Back") : -1;
        ApplyVisuals();
    }

    public void ConfigureVisuals(SpriteRenderer hand, SpriteRenderer weapon, SpriteRenderer[] renderers)
    {
        backHand = hand; blade = weapon; visualRenderers = renderers;
        backHandLayer = hand != null ? hand.sortingLayerID : 0; bladeLayer = weapon != null ? weapon.sortingLayerID : 0;
        backForearm = hand != null && hand.transform.parent != null ? hand.transform.parent.GetComponent<SpriteRenderer>() : null;
        backForearmLayer = backForearm != null ? backForearm.sortingLayerID : 0;
        frontLayer = SortingLayer.NameToID("Front");
        rendererEnabled = new bool[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) rendererEnabled[i] = renderers[i] != null && renderers[i].enabled;
    }
    public void SetRuntimeHidden(bool hidden) { runtimeHidden = hidden; ApplyVisuals(); }
    private void OnDidApplyAnimationProperties() { ApplyVisuals(); }
    private void OnEnable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= RefreshScenePreview;
        UnityEditor.EditorApplication.update += RefreshScenePreview;
#endif
    }
    private void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= RefreshScenePreview;
        previewWasActive = false;
#endif
        RestoreVisuals();
    }
#if UNITY_EDITOR
    private bool previewWasActive;
    // Animation-window scrubbing does not always call MonoBehaviour animation callbacks.
    // Poll sampled flags while previewing, including paused single-frame scrubbing.
    private void RefreshScenePreview()
    {
        if (this == null || !isActiveAndEnabled || Application.isPlaying) return;
        bool preview = UnityEditor.AnimationMode.InAnimationMode();
        if (preview)
        {
            previewWasActive = true;
            ApplyVisuals();
            UnityEditor.SceneView.RepaintAll();
        }
        else if (previewWasActive)
        {
            previewWasActive = false;
            // AnimationMode has restored the registered renderer properties already.
            frontWeapon = 0; frontBlade = 0; frontRearArm = 0; npcBlade = 0; hideVisual = 0; runtimeHidden = false;
            UnityEditor.SceneView.RepaintAll();
        }
    }
#endif
    private void RestoreVisuals()
    { frontWeapon = 0; frontBlade = 0; frontRearArm = 0; npcBlade = 0; hideVisual = 0; runtimeHidden = false; orbitBladeLayer = -1; ApplyVisuals(); }
    private void ApplyVisuals()
    {
        // Resolve the new reference on already-configured scene instances as well.
        if (backForearm == null && backHand != null && backHand.transform.parent != null)
        {
            backForearm = backHand.transform.parent.GetComponent<SpriteRenderer>();
            if (backForearm != null) backForearmLayer = backForearm.sortingLayerID;
        }
        SetLayer(backForearm, frontRearArm > .5f ? frontLayer : backForearmLayer);
        SetLayer(backHand, frontWeapon > .5f || frontRearArm > .5f ? frontLayer : backHandLayer);
        int desiredBlade = frontWeapon > .5f || frontBlade > .5f ? frontLayer : bladeLayer;
        if (npcBlade > .5f)
            foreach (var layer in SortingLayer.layers)
                if (layer.name == "Npc") { desiredBlade = layer.id; break; }
        if (orbitBladeLayer != -1) desiredBlade = orbitBladeLayer;
        SetLayer(blade, desiredBlade);
        if (visualRenderers == null || rendererEnabled == null) return;
        for (int i = 0; i < visualRenderers.Length && i < rendererEnabled.Length; i++)
        {
            SpriteRenderer renderer = visualRenderers[i]; if (renderer == null) continue;
            bool value = rendererEnabled[i] && hideVisual < .5f && !runtimeHidden;
            if (renderer.enabled == value) continue;
#if UNITY_EDITOR
            RecordPreview(renderer, "m_Enabled", renderer.enabled ? "1" : "0");
#endif
            renderer.enabled = value;
        }
    }
    private void SetLayer(SpriteRenderer renderer, int id)
    {
        if (renderer == null || renderer.sortingLayerID == id) return;
#if UNITY_EDITOR
        RecordPreview(renderer, "m_SortingLayerID", renderer.sortingLayerID.ToString());
#endif
        renderer.sortingLayerID = id;
    }
#if UNITY_EDITOR
    private void RecordPreview(SpriteRenderer renderer, string property, string original)
    {
        if (Application.isPlaying || !UnityEditor.AnimationMode.InAnimationMode()) return;
        string path = UnityEditor.AnimationUtility.CalculateTransformPath(renderer.transform, transform);
        UnityEditor.AnimationMode.AddPropertyModification(
            UnityEditor.EditorCurveBinding.DiscreteCurve(path, typeof(SpriteRenderer), property),
            new UnityEditor.PropertyModification { target = renderer, propertyPath = property, value = original }, true);
    }
#endif
    private Animator cachedAnimator;
    private Animator AnimatorComponent
    {
        get
        {
            if (cachedAnimator == null) cachedAnimator = GetComponent<Animator>();
            return cachedAnimator;
        }
    }

    // Called by generated AnimationEvents. No damage or movement is performed here.
    public void OnStage5AnimationCue(string cue) { Cue?.Invoke(cue); }

    public void ConfirmSkill3Hit() { Trigger("Skill3Hit"); }
    public void EndMidBossDash() { Trigger("DashEnd"); }
    public void EnterGroggy() { Trigger("Groggy"); }
    public void RecoverFromGroggy() { Trigger("GroggyEnd"); }
    public void DesireSucceeded() { Trigger("DesireSuccess"); }
    public void PlayAction(string trigger) { Trigger(trigger); }

    public void SetMotion(float speed, bool chasing, bool busy)
    {
        var a = AnimatorComponent; if (a == null || a.runtimeAnimatorController == null) return;
        a.SetFloat("MoveSpeed", speed); a.SetBool("IsChasing", chasing); a.SetBool("IsAttacking", busy);
    }
    public void PlayState(string state, float rate = 1f)
    {
        var a = AnimatorComponent; if (a == null || a.runtimeAnimatorController == null) return;
        if (state == "MainBoss5_Dskill" && transform.localToWorldMatrix.determinant < 0)
        {
            string mirrored = "MainBoss5_Dskill_Mirrored";
            if (a.HasState(0, Animator.StringToHash("Base Layer." + mirrored))) state = mirrored;
        }
        if (state == "MidBoss5_skill3" && transform.localToWorldMatrix.determinant < 0)
        {
            string mirrored = "MidBoss5_skill3_Mirrored";
            if (a.HasState(0, Animator.StringToHash("Base Layer." + mirrored))) state = mirrored;
        }
        // Every cast chooses one complete variant; timings and hit counts are shared.
        if (state == "MainBoss5_skill1" || state == "MidBoss5_skill1")
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string candidate = variant == 0 ? state : state + "_V" + (variant + 1);
            if (a.HasState(0, Animator.StringToHash("Base Layer." + candidate))) state = candidate;
        }
        runtimeHidden = false;
        int hash = Animator.StringToHash("Base Layer." + state);
        if (!a.HasState(0, hash)) { Debug.LogWarning("Stage5 state missing: " + state, this); return; }
        foreach (var p in a.parameters) if (p.type == AnimatorControllerParameterType.Trigger) a.ResetTrigger(p.name);
        a.SetFloat("AttackSpeed", rate); a.SetBool("IsAttacking", true); a.Play(hash, 0, 0);
        // Special actions wait immediately; expose the requested state before
        // WaitForEnd can mistake the previous looping Idle for the new action.
        a.Update(0f);
    }
    public void SeekFrame(string state, float frame, float totalFrames, float rate)
    {
        Animator a = AnimatorComponent;
        if (a == null || a.runtimeAnimatorController == null) return;
        int hash = Animator.StringToHash("Base Layer." + state);
        if (!a.HasState(0, hash)) return;
        a.SetFloat("AttackSpeed", rate); a.SetBool("IsAttacking", true);
        a.Play(hash, 0, Mathf.Clamp01(frame / Mathf.Max(1, totalFrames)));
    }
    public IEnumerator WaitForEnd()
    {
        var a = AnimatorComponent;
        if (a == null || !a.isActiveAndEnabled || a.runtimeAnimatorController == null) yield break;
        yield return null;
        int hash = a.GetCurrentAnimatorStateInfo(0).shortNameHash;
        while (a != null && a.isActiveAndEnabled)
        {
            var state = a.GetCurrentAnimatorStateInfo(0);
            if (state.shortNameHash != hash || state.normalizedTime >= 1 || state.loop) yield break;
            yield return null;
        }
    }

    private void Trigger(string name)
    {
        Animator a = AnimatorComponent;
        if (a == null) return;
        foreach (var parameter in a.parameters)
            if (parameter.name == name && parameter.type == AnimatorControllerParameterType.Trigger)
            {
                a.SetTrigger(name);
                return;
            }
        Debug.LogWarning("Stage5 Animator trigger missing: " + name, this);
    }
}
