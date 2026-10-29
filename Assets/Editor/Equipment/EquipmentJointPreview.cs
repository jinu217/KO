#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class EquipmentJointPreview : EditorWindow
{
    PlayerAppearance source;
    GameObject preview;
    readonly Dictionary<SpriteRenderer, SpriteRenderer> renderers = new Dictionary<SpriteRenderer, SpriteRenderer>();
    readonly Dictionary<Transform, Transform> transforms = new Dictionary<Transform, Transform>();
    ItemData[] items = new ItemData[0];
    readonly EquipmentType[] slots = { EquipmentType.Head, EquipmentType.Body, EquipmentType.Hand, EquipmentType.Foot };
    readonly string[] labels = { "머리", "몸 / 어깨", "손", "발" };
    int slot, index;
    [SerializeField] ItemData[] selectedBySlot = new ItemData[4];
    AnimationClip clip;
    float time;
    bool playing, joints = true;
    double last;
    Vector2 scroll;
    string warning;
    [MenuItem("Tools/Player/장비 관절 미리보기")]
    public static void Open() { GetWindow<EquipmentJointPreview>("장비 관절 확인"); }
    void OnEnable()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += PlayModeChanged;
        AssemblyReloadEvents.beforeAssemblyReload += Clear;
        SceneView.duringSceneGui += DrawJoints;
        last = EditorApplication.timeSinceStartup; Reload();
    }
    void OnDisable()
    {
        Clear(); EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= PlayModeChanged;
        AssemblyReloadEvents.beforeAssemblyReload -= Clear; SceneView.duringSceneGui -= DrawJoints;
    }
    void PlayModeChanged(PlayModeStateChange state) { Clear(); }
    void Reload()
    {
        items = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/Data/Item_List" })
            .Select(g => AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(i => i != null && i.itemType == ItemType.Equipment && i.equipType == slots[slot])
            .OrderBy(i => i.id).ThenBy(i => i.name).ToArray();
        if (selectedBySlot == null || selectedBySlot.Length != slots.Length)
            selectedBySlot = new ItemData[slots.Length];
        int remembered = System.Array.IndexOf(items, selectedBySlot[slot]);
        index = remembered >= 0 ? remembered : 0;
    }
    void RememberSelection()
    {
        if (index >= 0 && index < items.Length) selectedBySlot[slot] = items[index];
    }
    void OnGUI()
    {
        EditorGUILayout.HelpBox("옆에 생성한 임시 외형에 장비를 입힙니다. 원본·인벤토리는 그대로이며 창을 닫으면 제거됩니다. 피봇 변경은 원본 이미지 Import Settings에서 별도로 하세요.", MessageType.Info);
        EditorGUI.BeginChangeCheck();
        var next = (PlayerAppearance)EditorGUILayout.ObjectField("플레이어", source, typeof(PlayerAppearance), true);
        if (EditorGUI.EndChangeCheck()) { Clear(); source = next; }
        if (GUILayout.Button("선택한 플레이어 사용"))
        {
            var go = Selection.activeGameObject;
            var found = go == null ? null : go.GetComponentInParent<PlayerAppearance>() ?? go.GetComponentInChildren<PlayerAppearance>(true);
            if (found != null) { Clear(); source = found; } else warning = "PlayerAppearance가 있는 플레이어를 선택하세요.";
        }
        using (new EditorGUI.DisabledScope(source == null || EditorApplication.isPlayingOrWillChangePlaymode))
            if (GUILayout.Button("미리보기 생성 / 기본 외형으로 초기화")) Build();
        int nextSlot = GUILayout.Toolbar(slot, labels);
        if (nextSlot != slot)
        {
            RememberSelection(); slot = nextSlot; Reload();
        }
        if (GUILayout.Button("장비 목록 새로고침")) { RememberSelection(); Reload(); }
        if (items.Length > 0)
        {
            EditorGUI.BeginChangeCheck();
            index = EditorGUILayout.Popup("장비", index, items.Select(i => i.id + " · " + (string.IsNullOrEmpty(i.itemName) ? i.name : i.itemName)).ToArray());
            if (EditorGUI.EndChangeCheck()) Apply();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("◀ 이전")) { index = (index + items.Length - 1) % items.Length; Apply(); }
                if (GUILayout.Button("입혀보기")) Apply();
                if (GUILayout.Button("다음 ▶")) { index = (index + 1) % items.Length; Apply(); }
            }
            if (GUILayout.Button("이 장비 데이터 찾기")) EditorGUIUtility.PingObject(items[index]);
        }
        joints = EditorGUILayout.Toggle("관절 / 피봇 표시", joints);
        EditorGUI.BeginChangeCheck();
        clip = (AnimationClip)EditorGUILayout.ObjectField("확인할 모션", clip, typeof(AnimationClip), false);
        if (EditorGUI.EndChangeCheck()) { time = 0; RestorePose(); }
        using (new EditorGUI.DisabledScope(preview == null || clip == null))
        {
            playing = GUILayout.Toggle(playing, "모션 재생", "Button");
            time = EditorGUILayout.Slider("모션 시간", time, 0, clip != null ? clip.length : 1);
        }
        if (GUILayout.Button("미리보기 화면에 맞추기")) Frame();
        if (GUILayout.Button("미리보기 제거")) Clear();
        if (!string.IsNullOrEmpty(warning)) EditorGUILayout.HelpBox(warning, MessageType.Warning);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (var pair in renderers)
        {
            if (pair.Value == null || pair.Value.sprite == null) continue;
            var sprite = pair.Value.sprite;
            EditorGUILayout.ObjectField(pair.Key.name, sprite, typeof(Sprite), false);
            EditorGUILayout.LabelField("    Pivot(px): " + sprite.pivot + " / 크기: " + sprite.rect.size);
        }
        EditorGUILayout.EndScrollView();
    }
    void Build()
    {
        Clear(); if (source == null || EditorApplication.isPlayingOrWillChangePlaymode) return;
        preview = Copy(source.transform, null).gameObject;
        preview.name = "Equipment joint preview (temporary)";
        if (source.gameObject.scene.IsValid() && source.gameObject.scene.isLoaded)
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(preview, source.gameObject.scene);
        preview.transform.position = source.transform.position + Vector3.right * 4;
        preview.transform.rotation = source.transform.rotation; preview.transform.localScale = source.transform.lossyScale;
        Frame();
    }
    Transform Copy(Transform original, Transform parent)
    {
        var go = new GameObject(original.name) { hideFlags = HideFlags.HideAndDontSave };
        go.transform.SetParent(parent, false);
        go.transform.localPosition = original.localPosition; go.transform.localRotation = original.localRotation; go.transform.localScale = original.localScale;
        transforms[original] = go.transform;
        var sr = original.GetComponent<SpriteRenderer>();
        if (sr != null) { var copy = go.AddComponent<SpriteRenderer>(); EditorUtility.CopySerialized(sr, copy); renderers[sr] = copy; }
        var group = original.GetComponent<SortingGroup>();
        if (group != null) EditorUtility.CopySerialized(group, go.AddComponent<SortingGroup>());
        foreach (Transform child in original) Copy(child, go.transform);
        go.SetActive(original.gameObject.activeSelf); return go.transform;
    }
    void RestorePose()
    {
        foreach (var pair in transforms)
        {
            if (pair.Key == null || pair.Value == null || pair.Value == preview.transform) continue;
            pair.Value.localPosition = pair.Key.localPosition; pair.Value.localRotation = pair.Key.localRotation; pair.Value.localScale = pair.Key.localScale;
        }
    }
    void Set(SpriteRenderer original, Sprite sprite)
    {
        if (original == null || !renderers.TryGetValue(original, out var target)) { warning += "플레이어의 부위 연결이 없습니다. "; return; }
        target.sprite = sprite;
        if (sprite == null) warning += original.name + "의 장비 이미지가 비어 있습니다. ";
    }
    void Apply()
    {
        RememberSelection();
        if (source == null || items.Length == 0 || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (preview == null) Build();
        warning = null; var item = items[index];
        switch (item.equipType)
        {
            case EquipmentType.Head: Set(source.headRenderer, item.previewSprite); break;
            case EquipmentType.Body: Set(source.bodyRenderer, item.bodySprite); Set(source.shoulderLeftRenderer, item.shoulderLeftSprite); Set(source.shoulderRightRenderer, item.shoulderRightSprite); break;
            case EquipmentType.Hand: Set(source.handLeftRenderer, item.handLeftSprite); Set(source.handRightRenderer, item.handRightSprite); break;
            case EquipmentType.Foot: Set(source.footLeftRenderer, item.footLeftSprite); Set(source.footRightRenderer, item.footRightSprite); break;
        }
        SceneView.RepaintAll();
    }
    void Tick()
    {
        double now = EditorApplication.timeSinceStartup; float dt = (float)(now - last); last = now;
        if (preview == null || clip == null) return;
        if (source == null) { Clear(); return; }
        if (playing && clip.length > 0) time = (time + dt) % clip.length;
        var animator = source.GetComponentInChildren<Animator>(true);
        var root = animator != null && transforms.TryGetValue(animator.transform, out var t) ? t : preview.transform;
        clip.SampleAnimation(root.gameObject, time);
        if (playing) Repaint(); SceneView.RepaintAll();
    }
    void Frame()
    {
        if (preview == null || SceneView.lastActiveSceneView == null) return;
        var visible = renderers.Values.Where(r => r != null && r.sprite != null).ToArray();
        if (visible.Length == 0) return;
        Bounds bounds = visible[0].bounds; foreach (var r in visible) bounds.Encapsulate(r.bounds);
        SceneView.lastActiveSceneView.Frame(bounds, false);
    }
    void DrawJoints(SceneView view)
    {
        if (!joints || preview == null) return;
        foreach (var r in renderers.Values)
        {
            if (r == null || r.sprite == null) continue;
            Handles.color = Color.cyan;
            Handles.DrawWireDisc(r.transform.position, Vector3.forward, HandleUtility.GetHandleSize(r.transform.position) * .025f);
            Handles.Label(r.transform.position, r.name);
            if (r.transform.parent != null) Handles.DrawDottedLine(r.transform.position, r.transform.parent.position, 3);
        }
    }
    void Clear()
    {
        playing = false; time = 0;
        if (preview != null) DestroyImmediate(preview);
        preview = null; renderers.Clear(); transforms.Clear(); warning = null; SceneView.RepaintAll();
    }
}
#endif
