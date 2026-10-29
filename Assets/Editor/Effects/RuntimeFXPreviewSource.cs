#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
static class RuntimeFXPreviewSource
{
    static Animator actor;
    static GameObject proxy;
    static BossEffects boss;
    static MobEffects mob;
    public static MidBossController BossTemplate { get; private set; }
    public static EnemyController MobTemplate { get; private set; }
    public static string Error { get; private set; }
    public static BossEffects Boss(Animator value) { Ensure(value); return boss; }
    public static MobEffects Mob(Animator value) { Ensure(value); return mob; }
    static void Ensure(Animator value)
    {
        if (actor == value) { if (proxy != null) Sync(value.transform, proxy.transform); return; }
        Clear(); actor = value;
        boss = value.GetComponentInParent<BossEffects>() ?? value.GetComponentInChildren<BossEffects>(true);
        mob = value.GetComponentInParent<MobEffects>() ?? value.GetComponentInChildren<MobEffects>(true);
        if (boss != null || mob != null || value.GetComponentInParent<PlayerController>() != null) return;
        var candidates = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" })
            .Select(id => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(id)))
            .Where(go => go != null && go.GetComponentsInChildren<Animator>(true).Any(a => a.runtimeAnimatorController == value.runtimeAnimatorController)
                && (go.GetComponent<BossEffects>() != null || go.GetComponent<MobEffects>() != null)).ToArray();
        if (candidates.Length != 1) { Error = "인게임 FX 원본 프리팹 연결 필요 (일치: " + candidates.Length + ")"; return; }
        var source = candidates[0];
        proxy = Copy(value.transform, null).gameObject;
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(proxy, value.gameObject.scene);
        var collider = source.GetComponent<Collider2D>();
        if (collider != null) { var copy = (Collider2D)proxy.AddComponent(collider.GetType()); EditorUtility.CopySerialized(collider, copy); copy.isTrigger = true; }
        BossTemplate = source.GetComponent<MidBossController>(); MobTemplate = source.GetComponent<EnemyController>();
        var originalBoss = source.GetComponent<BossEffects>();
        if (originalBoss != null)
        {
            boss = proxy.AddComponent<BossEffects>(); EditorUtility.CopySerialized(originalBoss, boss);
            boss.motionAnimator = value;
            if (originalBoss.weaponAnchor != null) boss.weaponAnchor = value.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == originalBoss.weaponAnchor.name);
        }
        var originalMob = source.GetComponent<MobEffects>();
        if (originalMob != null) { mob = proxy.AddComponent<MobEffects>(); EditorUtility.CopySerialized(originalMob, mob); }
        Sync(value.transform, proxy.transform);
    }
    static Transform Copy(Transform source, Transform parent)
    {
        var go = new GameObject(source.name) { hideFlags = HideFlags.HideAndDontSave };
        go.transform.SetParent(parent, false);
        var renderer = source.GetComponent<SpriteRenderer>();
        if (renderer != null) { var copy = go.AddComponent<SpriteRenderer>(); EditorUtility.CopySerialized(renderer, copy); copy.forceRenderingOff = true; }
        var sorting = source.GetComponent<SortingGroup>();
        if (sorting != null) EditorUtility.CopySerialized(sorting, go.AddComponent<SortingGroup>());
        foreach (Transform child in source) Copy(child, go.transform);
        return go.transform;
    }
    static void Sync(Transform source, Transform target)
    {
        target.localPosition = source.localPosition; target.localRotation = source.localRotation; target.localScale = source.localScale;
        if (target.parent == null) { target.position = source.position; target.rotation = source.rotation; target.localScale = source.lossyScale; }
        for (int i = 0; i < source.childCount; i++) Sync(source.GetChild(i), target.GetChild(i));
    }
    public static void Clear()
    {
        if (proxy != null) Object.DestroyImmediate(proxy);
        proxy = null; actor = null; boss = null; mob = null; BossTemplate = null; MobTemplate = null; Error = null;
    }
}
#endif
