using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.InputSystem;

// A runtime appearance sandbox: uses the game's PlayerAppearance and authored motion clips.
[DefaultExecutionOrder(-100)]
public sealed class EquipmentTestRoom : MonoBehaviour
{
    [Serializable] public class ArmorSet { public string name; public ItemData[] items; }
    public PlayerAppearance player;
    public ItemData[] items;
    public ArmorSet[] armorSets;
    public AnimationClip[] clips;
    readonly EquipmentType[] types = { EquipmentType.Head, EquipmentType.Body, EquipmentType.Hand, EquipmentType.Foot, EquipmentType.Weapon };
    readonly string[] tabs = { "머리", "몸", "손", "발", "무기", "세트" };
    readonly Dictionary<EquipmentType, ItemData> equipped = new Dictionary<EquipmentType, ItemData>();
    readonly Dictionary<string, AnimationClip> motions = new Dictionary<string, AnimationClip>();
    PlayableGraph graph;
    AnimationPlayableOutput output;
    AnimationClipPlayable motion;
    string currentMotion;
    float elapsed, speed = 1f;
    bool paused, automatic = true;
    int tab;
    Vector2 scroll;
    string search = "";
    Camera view;
    GUISkin readableSkin;
    float UiScale => Mathf.Clamp(Screen.height / 720f, .75f, 3f);
    float PanelPixels => Mathf.Min(380f * UiScale, Screen.width * .46f);
    void Awake()
    {
        foreach (var clip in clips)
        {
            if (clip == null || motions.ContainsKey(clip.name)) continue;
            var copy = Instantiate(clip); copy.events = new AnimationEvent[0]; motions[clip.name] = copy;
        }
        graph = PlayableGraph.Create("Equipment test motions"); graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        output = AnimationPlayableOutput.Create(graph, "Player", player.GetComponentInChildren<Animator>());
        view = new GameObject("Equipment Test Camera").AddComponent<Camera>();
        view.orthographic = true; view.orthographicSize = 1.65f; view.transform.position = new Vector3(0, 0, -10);
        view.clearFlags = CameraClearFlags.SolidColor; view.backgroundColor = new Color(.18f,.18f,.2f);
        SetMotion("Player_Idle");
    }
    void Start()
    {
        var first = armorSets.FirstOrDefault(s => s.name.Contains("시작의 기사")) ?? armorSets.FirstOrDefault();
        if (first != null) foreach (var item in first.items) Equip(item);
        Equip(items.FirstOrDefault(i => i != null && i.equipType == EquipmentType.Weapon && i.weaponType == InventoryWeaponType.Sword));
    }
    void Equip(ItemData item)
    {
        if (item == null) return;
        player.Equip(item); equipped[item.equipType] = item;
        if (item.equipType == EquipmentType.Weapon && currentMotion != null && currentMotion.StartsWith("Attack_")) Attack();
    }
    void SetMotion(string name)
    {
        if (!motions.TryGetValue(name, out var clip)) return;
        if (motion.IsValid()) graph.DestroyPlayable(motion);
        motion = AnimationClipPlayable.Create(graph, clip); motion.SetApplyFootIK(false); motion.SetSpeed(0);
        output.SetSourcePlayable(motion); output.SetWeight(1f); currentMotion = name; elapsed = 0; paused = false; graph.Play();
    }
    void Attack()
    {
        var type = equipped.TryGetValue(EquipmentType.Weapon, out var item) ? item.weaponType : InventoryWeaponType.Sword;
        var weapon = type == InventoryWeaponType.Blunt ? WeaponType.Blunt : type == InventoryWeaponType.Scythe ? WeaponType.Scythe
            : type == InventoryWeaponType.Dagger || type == InventoryWeaponType.Spear ? WeaponType.Dagger : WeaponType.Sword;
        SetMotion(PlayerController.WeaponAttackClipName(weapon));
    }
    void Update()
    {
        float panel = PanelPixels;
        view.rect = new Rect(panel / Screen.width, 0, 1 - panel / Screen.width, 1);
        var keyboard = Keyboard.current;
        if (keyboard != null && GUIUtility.keyboardControl == 0)
        {
            float direction = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
            if (direction != 0)
            {
                var scale = player.transform.localScale; scale.x = -direction * Mathf.Abs(scale.x); player.transform.localScale = scale;
                if (automatic && currentMotion == "Player_Idle") SetMotion("Run");
            }
            else if (automatic && currentMotion == "Run") SetMotion("Player_Idle");
            if (keyboard.jKey.wasPressedThisFrame) Attack();
            if (keyboard.kKey.wasPressedThisFrame) SetMotion("Dash");
        }
        if (!motion.IsValid()) return;
        var clip = motions[currentMotion];
        if (!paused) elapsed += Time.unscaledDeltaTime * speed;
        if (elapsed >= clip.length && automatic && (currentMotion.StartsWith("Attack_") || currentMotion == "Dash")) SetMotion("Player_Idle");
    }
    // Evaluate after the Animator, but before PlayerAppearance applies equipment offsets.
    void LateUpdate()
    {
        if (!motion.IsValid()) return;
        var clip = motions[currentMotion];
        motion.SetTime(clip.length > 0 ? elapsed % clip.length : 0); graph.Evaluate(0);
    }
    void OnGUI()
    {
        if (readableSkin == null)
        {
            readableSkin = Instantiate(GUI.skin);
            readableSkin.label.fontSize = 17; readableSkin.label.wordWrap = true;
            readableSkin.button.fontSize = 16; readableSkin.button.fixedHeight = 36;
            readableSkin.toggle.fontSize = 16; readableSkin.toggle.fixedHeight = 30;
            readableSkin.textField.fontSize = 17; readableSkin.textField.fixedHeight = 32;
            readableSkin.verticalScrollbar.fixedWidth = 20;
        }
        var oldSkin = GUI.skin; var oldMatrix = GUI.matrix;
        GUI.skin = readableSkin; GUI.matrix = Matrix4x4.Scale(Vector3.one * UiScale);
        float width = PanelPixels / UiScale;
        GUILayout.BeginArea(new Rect(8, 8, width - 16, Screen.height / UiScale - 16), GUI.skin.box);
        GUILayout.Label("장비 테스트 룸 · Play 모드");
        GUILayout.Label("A/D 방향·달리기 · J 공격 · K 대시");
        GUILayout.Label("외형·모션 전용 테스트 / 세이브 변경 없음");
        int next = GUILayout.SelectionGrid(tab, tabs, 3);
        if (next != tab) { tab = next; search = ""; scroll = Vector2.zero; }
        GUILayout.Label("장비 이름 검색");
        search = GUILayout.TextField(search);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Idle")) { automatic = false; SetMotion("Player_Idle"); }
        if (GUILayout.Button("Run")) { automatic = false; SetMotion("Run"); }
        if (GUILayout.Button("공격")) { automatic = false; Attack(); }
        if (GUILayout.Button("Dash")) { automatic = false; SetMotion("Dash"); }
        GUILayout.EndHorizontal();
        automatic = GUILayout.Toggle(automatic, "키보드 조작 / 공격 후 Idle 복귀");
        paused = GUILayout.Toggle(paused, "모션 일시정지");
        player.applyEquipmentPositionOffsets = GUILayout.Toggle(player.applyEquipmentPositionOffsets, "장비 위치 보정 적용 (비교용)");
        if (currentMotion != null && motions.TryGetValue(currentMotion, out var shownClip))
        {
            GUILayout.Label("모션: " + currentMotion + " · " + (elapsed % shownClip.length).ToString("0.00") + "초");
            if (paused) elapsed = GUILayout.HorizontalSlider(elapsed % shownClip.length, 0, shownClip.length);
        }
        GUILayout.Label("재생 속도 " + speed.ToString("0.00")); speed = GUILayout.HorizontalSlider(speed, .1f, 2f);
        GUILayout.Label("확대 " + view.orthographicSize.ToString("0.00")); view.orthographicSize = GUILayout.HorizontalSlider(view.orthographicSize, .7f, 3f);
        if (GUILayout.Button("좌우 방향 반전")) { var s = player.transform.localScale; s.x = -s.x; player.transform.localScale = s; }
        if (tab < types.Length && equipped.TryGetValue(types[tab], out var chosen)) GUILayout.Label("현재: " + chosen.itemName);
        scroll = GUILayout.BeginScrollView(scroll);
        if (tab == 5)
        {
            foreach (var set in armorSets)
                if (string.IsNullOrEmpty(search) || set.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                    if (GUILayout.Button(set.name, GUILayout.Height(40))) foreach (var item in set.items) Equip(item);
        }
        else
        {
            foreach (var item in items)
            {
                if (item == null || item.equipType != types[tab]) continue;
                string label = item.id + " · " + item.itemName;
                if (!string.IsNullOrEmpty(search) && label.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (GUILayout.Button(label, GUILayout.Height(40))) Equip(item);
            }
        }
        GUILayout.EndScrollView(); GUILayout.EndArea();
        GUI.matrix = oldMatrix; GUI.skin = oldSkin;
    }
    void OnDestroy()
    {
        if (graph.IsValid()) graph.Destroy();
        foreach (var clip in motions.Values) if (clip != null) Destroy(clip);
        if (view != null) Destroy(view.gameObject);
        if (readableSkin != null) Destroy(readableSkin);
    }
}
