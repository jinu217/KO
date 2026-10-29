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
    EquipmentVisualState visualState;
    readonly Dictionary<SpriteRenderer, SpriteRenderer> renderers = new Dictionary<SpriteRenderer, SpriteRenderer>();
    readonly Dictionary<Transform, Transform> transforms = new Dictionary<Transform, Transform>();
    ItemData[] items = new ItemData[0];
    readonly EquipmentType[] slots = { EquipmentType.Head, EquipmentType.Body, EquipmentType.Hand, EquipmentType.Foot, EquipmentType.Weapon };
    readonly string[] labels = { "머리", "몸 / 어깨", "손", "발", "무기" };
    int slot, index;
    [SerializeField] ItemData[] selectedBySlot = new ItemData[5];
    [SerializeField] AnimationClip clip;
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
        if (clip == null)
            clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/1/Player/Player_Idle.anim");
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
            .Where(i => i != null && i.itemType == ItemType.Equipment && i.equipType == slots[slot] && !IsLowGradePreviewItem(i))
            .OrderBy(i => i.id).ThenBy(i => i.name).ToArray();
        if (selectedBySlot == null || selectedBySlot.Length != slots.Length)
            System.Array.Resize(ref selectedBySlot, slots.Length);
        int remembered = System.Array.IndexOf(items, selectedBySlot[slot]);
        index = remembered >= 0 ? remembered : 0;
    }
    static bool IsLowGradePreviewItem(ItemData item)
    {
        var sprites = new[] { item.previewSprite, item.bodySprite, item.shoulderLeftSprite,
            item.shoulderRightSprite, item.handLeftSprite, item.handRightSprite,
            item.footLeftSprite, item.footRightSprite };
        return sprites.Any(sprite =>
        {
            if (sprite == null) return false;
            string path = AssetDatabase.GetAssetPath(sprite);
            return path.Contains("/Item_img/낮은등급장비1(레드)/")
                || path.Contains("/Item_img/낮은등급장비2(구리)/");
        });
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
            if (slots[slot] == EquipmentType.Weapon && GUILayout.Button("이 무기의 공격 모션 선택"))
            {
                Apply();
                var type = items[index].weaponType;
                var weapon = type == InventoryWeaponType.Blunt ? WeaponType.Blunt
                    : type == InventoryWeaponType.Scythe ? WeaponType.Scythe
                    : type == InventoryWeaponType.Spear || type == InventoryWeaponType.Dagger ? WeaponType.Dagger : WeaponType.Sword;
                var motion = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/1/Player/"
                    + PlayerController.WeaponAttackClipName(weapon) + ".anim");
                if (motion != null) { clip = motion; time = 0; RestorePose(); }
                else warning = "이 무기의 공격 모션을 찾지 못했습니다.";
            }
            if (slots[slot] != EquipmentType.Weapon && GUILayout.Button("같은 폴더 장비 세트 한 번에 입히기")) ApplyFolderSet();
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
        using (new EditorGUI.DisabledScope(preview == null || EditorApplication.isPlayingOrWillChangePlaymode))
            if (GUILayout.Button("현재 모습으로 편집용 예시 오브젝트 생성")) CreateEditableExample();
        if (GUILayout.Button("선택한 예시의 위치·정렬을 장비 설정에 저장"))
        {
            var selected = Selection.activeGameObject;
            var exampleSettings = selected != null ? selected.GetComponentInParent<EquipmentExampleSettings>() : null;
            if (exampleSettings != null) EquipmentExampleSettingsEditor.Save(exampleSettings);
            else warning = "이 툴로 새로 만든 장비 예시 오브젝트를 선택하세요.";
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
        visualState = new EquipmentVisualState(preview.transform, preview.GetComponentsInChildren<SpriteRenderer>(true));
        Frame();
    }
    Transform Copy(Transform original, Transform parent)
    {
        var go = new GameObject(original.name) { hideFlags = HideFlags.HideAndDontSave };
        go.transform.SetParent(parent, false);
        go.transform.localPosition = original.localPosition; go.transform.localRotation = original.localRotation; go.transform.localScale = original.localScale;
        transforms[original] = go.transform;
        var sr = original.GetComponent<SpriteRenderer>();
        if (sr != null) { var copy = go.AddComponent<SpriteRenderer>(); EditorUtility.CopySerialized(sr, copy); SetPreviewSorting(copy); renderers[sr] = copy; }
        var group = original.GetComponent<SortingGroup>();
        if (group != null) EditorUtility.CopySerialized(group, go.AddComponent<SortingGroup>());
        foreach (Transform child in original) Copy(child, go.transform);
        go.SetActive(original.gameObject.activeSelf); return go.transform;
    }
    void RestorePose()
    {
        visualState?.Restore();
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
        warning = null;
        ApplyItem(items[index]);
        SceneView.RepaintAll();
    }
    void ApplyItem(ItemData item)
    {
        visualState?.Restore();
        visualState?.SetItem(item);
        switch (item.equipType)
        {
            case EquipmentType.Weapon:
                var weaponRenderer = new SerializedObject(source).FindProperty("weaponRenderer").objectReferenceValue as SpriteRenderer;
                if (weaponRenderer == null)
                    weaponRenderer = source.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r => r.name == "Sword");
                if (item.previewSprite == null) warning = "무기 Preview Sprite가 비어 있어 기존 이미지를 유지합니다.";
                else Set(weaponRenderer, item.previewSprite);
                break;
            case EquipmentType.Head: Set(source.headRenderer, item.previewSprite); break;
            case EquipmentType.Body: Set(source.bodyRenderer, item.bodySprite); Set(source.shoulderLeftRenderer, item.shoulderLeftSprite); Set(source.shoulderRightRenderer, item.shoulderRightSprite); break;
            case EquipmentType.Hand: Set(source.handLeftRenderer, item.handLeftSprite); Set(source.handRightRenderer, item.handRightSprite); break;
            case EquipmentType.Foot: Set(source.footLeftRenderer, item.footLeftSprite); Set(source.footRightRenderer, item.footRightSprite); break;
        }
        SceneView.RepaintAll();
    }
    static string EquipmentFolder(ItemData item)
    {
        Sprite sprite = null;
        switch (item.equipType)
        {
            case EquipmentType.Head: sprite = item.previewSprite; break;
            case EquipmentType.Body: sprite = item.bodySprite; break;
            case EquipmentType.Hand: sprite = item.handLeftSprite != null ? item.handLeftSprite : item.handRightSprite; break;
            case EquipmentType.Foot: sprite = item.footLeftSprite != null ? item.footLeftSprite : item.footRightSprite; break;
        }
        return sprite == null ? null : System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(sprite)).Replace('\\', '/');
    }
    void ApplyFolderSet()
    {
        if (source == null || items.Length == 0 || EditorApplication.isPlayingOrWillChangePlaymode) return;
        string folder = EquipmentFolder(items[index]);
        if (string.IsNullOrEmpty(folder)) { warning = "선택한 장비의 이미지 폴더를 찾지 못했습니다."; return; }
        var candidates = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/Data/Item_List" })
            .Select(g => AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(i => i != null && i.itemType == ItemType.Equipment && i.equipType != EquipmentType.Weapon
                && !IsLowGradePreviewItem(i) && EquipmentFolder(i) == folder).ToArray();
        if (preview == null) Build();
        warning = null;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == EquipmentType.Weapon) continue;
            var matches = candidates.Where(item => item.equipType == slots[i]).ToArray();
            // Shared source art may be referenced by more than one item; never pick an arbitrary one.
            if (matches.Length != 1) { warning += labels[i] + (matches.Length == 0 ? " 장비 없음. " : " 후보가 여러 개라 개별 선택이 필요합니다. "); continue; }
            selectedBySlot[i] = matches[0]; ApplyItem(matches[0]);
        }
        Reload(); Repaint(); SceneView.RepaintAll();
    }
    void Tick()
    {
        double now = EditorApplication.timeSinceStartup; float dt = (float)(now - last); last = now;
        if (preview == null) return;
        if (clip == null) { visualState?.Apply(); return; }
        if (source == null) { Clear(); return; }
        if (playing && clip.length > 0) time = (time + dt) % clip.length;
        var animator = source.GetComponentInChildren<Animator>(true);
        var root = animator != null && transforms.TryGetValue(animator.transform, out var t) ? t : preview.transform;
        visualState?.Restore();
        clip.SampleAnimation(root.gameObject, time);
        foreach (var renderer in renderers.Values) SetPreviewSorting(renderer);
        visualState?.Apply();
        if (playing) Repaint(); SceneView.RepaintAll();
    }
    static void SetPreviewSorting(SpriteRenderer renderer)
    {
        if (renderer == null) return;
        string layer = "Player";
        for (var node = renderer.transform; node != null; node = node.parent)
        {
            if (node.name.EndsWith("_F")) { layer = "Player_F"; break; }
            if (node.name.EndsWith("_B")) { layer = "Player_B"; break; }
        }
        renderer.sortingLayerName = layer;
        if (renderer.name == "Shoulder_F") renderer.sortingOrder = 4;
        else if (renderer.name == "Hand_F") renderer.sortingOrder = 5;
    }
    void CreateEditableExample()
    {
        if (preview == null || EditorApplication.isPlayingOrWillChangePlaymode) return;
        // Copy the sampled pose and equipped sprites, not the source player's defaults.
        visualState?.Apply();
        var example = Instantiate(preview);
        var settings = example.AddComponent<EquipmentExampleSettings>();
        settings.parts = example.GetComponentsInChildren<SpriteRenderer>(true)
            .Where(r => visualState != null && visualState.ItemFor(r.name) != null)
            .Select(r => new EquipmentExampleSettings.Part {
                item = visualState.ItemFor(r.name), renderer = r, part = r.name,
                baseRootPosition = visualState.BasePosition(preview.GetComponentsInChildren<SpriteRenderer>(true).First(p => p.name == r.name))
            }).ToArray();
        example.name = "장비 예시_" + (source != null ? source.name : "Player");
        if (preview.scene.IsValid() && preview.scene.isLoaded && example.scene != preview.scene)
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(example, preview.scene);
        foreach (var node in example.GetComponentsInChildren<Transform>(true))
        {
            node.gameObject.hideFlags = HideFlags.None;
            foreach (var component in node.GetComponents<Component>())
                if (component != null) component.hideFlags = HideFlags.None;
        }
        // Strip transform-only joint helpers from the exported copy, deepest first.
        // Keep the sampled world pose while reconnecting their children.
        foreach (var joint in example.GetComponentsInChildren<Transform>(true).Reverse())
        {
            if (joint == example.transform || joint.name.IndexOf("Joint", System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            if (joint.GetComponents<Component>().Any(c => !(c is Transform))) continue;
            while (joint.childCount > 0)
                joint.GetChild(0).SetParent(joint.parent, true);
            DestroyImmediate(joint.gameObject);
        }
        foreach (var node in example.GetComponentsInChildren<Transform>(true).Reverse())
            if (node != example.transform && node.name == "GroundCheck")
                DestroyImmediate(node.gameObject);
        var parts = example.GetComponentsInChildren<Transform>(true);
        var body = parts.FirstOrDefault(t => t.name == "Body");
        if (body != null)
        {
            body.SetParent(example.transform, true);
            body.SetSiblingIndex(0);
            ArrangeExampleParts(body, parts, "Head", "Shoulder_F", "Shoulder_B", "Foot_F", "Foot_B");
            var front = parts.FirstOrDefault(t => t.name == "Shoulder_F");
            var back = parts.FirstOrDefault(t => t.name == "Shoulder_B");
            var hand = parts.FirstOrDefault(t => t.name == "Hand_B");
            if (front != null) ArrangeExampleParts(front, parts, "Hand_F");
            if (back != null) ArrangeExampleParts(back, parts, "Hand_B");
            if (hand != null) ArrangeExampleParts(hand, parts, "Sword");
        }
        GameObjectUtility.EnsureUniqueNameForSibling(example);
        Undo.RegisterCreatedObjectUndo(example, "장비 예시 오브젝트 생성");
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(example.scene);
        // Remove the overlapping temporary preview; the editable copy stays when this window closes.
        Clear();
        Selection.activeGameObject = example;
        EditorGUIUtility.PingObject(example);
        if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
    }
    static void ArrangeExampleParts(Transform parent, Transform[] parts, params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            var part = parts.FirstOrDefault(t => t.name == names[i]);
            if (part == null) continue;
            part.SetParent(parent, true);
            part.SetSiblingIndex(i);
        }
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
        visualState?.Restore(); visualState = null;
        if (preview != null) DestroyImmediate(preview);
        preview = null; renderers.Clear(); transforms.Clear(); warning = null; SceneView.RepaintAll();
    }
}
#endif
