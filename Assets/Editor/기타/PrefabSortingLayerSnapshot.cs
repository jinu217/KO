#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Install in Assets/Editor. Operates on Sorting Layers only, not GameObject.layer.
public static class PrefabSortingLayerSnapshot
{
    private const string Menu = "Tools/기타/Prefab/Sorting Layers/";
    private const string SnapshotPath = "ProjectSettings/PrefabSortingLayers.snapshot.json";
    [Serializable] private sealed class Snapshot
    {
        public int version = 1;
        public string createdUtc;
        public string rootFolder;
        public List<PrefabRecord> prefabs = new List<PrefabRecord>();
    }
    [Serializable] private sealed class PrefabRecord
    {
        public string guid, path;
        public List<Entry> entries = new List<Entry>();
    }
    [Serializable] private sealed class Segment
    {
        public string name;
        public int sameNameIndex, sameNameCount;
    }
    [Serializable] private sealed class Entry
    {
        public List<Segment> hierarchy = new List<Segment>();
        public string componentType;
        public int componentIndex, componentCount;
        public string layerName;
        public int originalLayerId, order;
    }
    private enum Mode { Compare, UnknownOnly, All }

    [MenuItem(Menu + "1. Capture Healthy Branch")]
    public static void Capture()
    {
        if (!Ready()) return;
        string root = FindRoot();
        if (root == null) return;
        var snapshot = new Snapshot { createdUtc = DateTime.UtcNow.ToString("O"), rootFolder = root };
        var errors = new List<string>();
        string[] paths = AssetDatabase.FindAssets("t:Prefab", new[] { root })
            .Select(AssetDatabase.GUIDToAssetPath).Where(p => p.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p, StringComparer.Ordinal).ToArray();
        if (paths.Length == 0) { Debug.LogWarning("대상 프리팹이 없습니다: " + root); return; }
        try
        {
            for (int i = 0; i < paths.Length; i++)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Sorting Layer 저장", paths[i], (float)i / paths.Length)) return;
                GameObject prefab = null;
                try
                {
                    prefab = PrefabUtility.LoadPrefabContents(paths[i]);
                    var record = new PrefabRecord { guid = AssetDatabase.AssetPathToGUID(paths[i]), path = paths[i] };
                    foreach (Component component in Targets(prefab))
                    {
                        int id = LayerId(component);
                        if (!SortingLayer.IsValid(id))
                        {
                            errors.Add(paths[i] + " / " + component.name + " : 미등록 ID " + id);
                            continue;
                        }
                        Component[] peers = component.gameObject.GetComponents(component.GetType());
                        record.entries.Add(new Entry {
                            hierarchy = Hierarchy(prefab.transform, component.transform),
                            componentType = component.GetType().AssemblyQualifiedName,
                            componentIndex = Array.IndexOf(peers, component), componentCount = peers.Length,
                            layerName = SortingLayer.IDToName(id), originalLayerId = id, order = Order(component)
                        });
                    }
                    snapshot.prefabs.Add(record);
                }
                catch (Exception ex) { errors.Add(paths[i] + " : " + ex.Message); }
                finally { if (prefab != null) PrefabUtility.UnloadPrefabContents(prefab); }
            }
        }
        finally { EditorUtility.ClearProgressBar(); }
        // Never replace a healthy baseline with a partial or already broken capture.
        if (errors.Count > 0)
        {
            Report("저장 취소: 오류가 있는 프리팹을 먼저 고쳐주세요. 기존 스냅샷은 유지됩니다.", errors);
            return;
        }
        if (File.Exists(SnapshotPath)) Backup(SnapshotPath, NewBackupFolder("capture"));
        string temp = SnapshotPath + ".tmp";
        File.WriteAllText(temp, JsonUtility.ToJson(snapshot, true), new UTF8Encoding(false));
        if (File.Exists(SnapshotPath)) File.Replace(temp, SnapshotPath, null);
        else File.Move(temp, SnapshotPath);
        Debug.Log("Sorting Layer 저장 완료: " + snapshot.prefabs.Count + "개 프리팹, "
            + snapshot.prefabs.Sum(p => p.entries.Count) + "개 컴포넌트\n" + SnapshotPath
            + "\n이 JSON을 정상 프리팹 및 TagManager.asset과 함께 커밋하세요.");
    }

    [MenuItem(Menu + "2. Compare With Snapshot")]
    public static void Compare() { Process(Mode.Compare); }
    [MenuItem(Menu + "3. Restore Unknown Layers Only")]
    public static void RestoreUnknown() { Process(Mode.UnknownOnly); }
    [MenuItem(Menu + "4. Restore All Saved Settings")]
    public static void RestoreAll() { Process(Mode.All); }

    private static void Process(Mode mode)
    {
        if (!Ready()) return;
        if (!File.Exists(SnapshotPath)) { Debug.LogError("정상 브랜치에서 Capture Healthy Branch를 먼저 실행하세요: " + SnapshotPath); return; }
        Snapshot snapshot;
        try { snapshot = JsonUtility.FromJson<Snapshot>(File.ReadAllText(SnapshotPath)); }
        catch (Exception ex) { Debug.LogError("스냅샷 읽기 실패: " + ex.Message); return; }
        if (snapshot == null || snapshot.version != 1 || snapshot.prefabs == null)
        { Debug.LogError("지원하지 않거나 손상된 스냅샷입니다."); return; }
        var layers = SortingLayer.layers.ToDictionary(l => l.name, l => l.id, StringComparer.Ordinal);
        var log = new List<string>();
        int differences = 0, changedPrefabs = 0, changedComponents = 0, skipped = 0;
        string backupFolder = null;
        // Sources before variants / parents containing nested prefabs.
        var records = snapshot.prefabs.OrderBy(r => {
            string p = AssetDatabase.GUIDToAssetPath(r.guid);
            return string.IsNullOrEmpty(p) ? int.MaxValue : AssetDatabase.GetDependencies(p, true).Length;
        }).ToArray();
        try
        {
            for (int i = 0; i < records.Length; i++)
            {
                PrefabRecord record = records[i];
                string path = AssetDatabase.GUIDToAssetPath(record.guid);
                if (EditorUtility.DisplayCancelableProgressBar("Sorting Layer " + mode, record.path, (float)i / Math.Max(1, records.Length)))
                { log.Add("사용자가 중단했습니다. 이미 저장된 복원은 유지됩니다."); break; }
                if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal)
                    || !path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                { skipped++; log.Add("프리팹 GUID를 찾지 못해 생략: " + record.path); continue; }
                GameObject root = null;
                try
                {
                    root = PrefabUtility.LoadPrefabContents(path);
                    int edited = 0;
                    foreach (Entry entry in record.entries)
                    {
                        string reason;
                        Component c = Resolve(root, entry, out reason);
                        if (c == null) { skipped++; log.Add(path + " / " + Label(entry) + " : " + reason); continue; }
                        int targetId;
                        if (!layers.TryGetValue(entry.layerName, out targetId))
                        { skipped++; log.Add(path + " : 현재 프로젝트에 레이어 없음: " + entry.layerName); continue; }
                        int currentId = LayerId(c), currentOrder = Order(c);
                        if (currentId == targetId && currentOrder == entry.order) continue;
                        differences++;
                        bool unknown = !SortingLayer.IsValid(currentId);
                        log.Add(path + " / " + Label(entry) + " : " + SortingLayer.IDToName(currentId)
                            + " [" + currentId + "] order " + currentOrder + " -> " + entry.layerName
                            + " [" + targetId + "] order " + entry.order);
                        if (mode == Mode.Compare || (mode == Mode.UnknownOnly && !unknown)) continue;
                        SetSorting(c, targetId, entry.order);
                        // Nested/variant modifications stay as overrides in this prefab; never Apply to source.
                        if (PrefabUtility.IsPartOfPrefabInstance(c)) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
                        edited++;
                    }
                    if (edited > 0)
                    {
                        if (backupFolder == null) backupFolder = NewBackupFolder("restore");
                        Backup(path, backupFolder);
                        if (!AssetDatabase.IsOpenForEdit(path)) throw new IOException("파일이 편집 불가 상태입니다: " + path);
                        bool success;
                        PrefabUtility.SaveAsPrefabAsset(root, path, out success);
                        if (!success) throw new IOException("프리팹 저장 실패");
                        changedPrefabs++; changedComponents += edited;
                    }
                }
                catch (Exception ex) { skipped++; log.Add("오류: " + path + " : " + ex.Message); }
                finally { if (root != null) PrefabUtility.UnloadPrefabContents(root); }
            }
        }
        finally { EditorUtility.ClearProgressBar(); }
        string summary = mode + " 완료: 차이 " + differences + "개, 수정 " + changedPrefabs
            + "개 프리팹 / " + changedComponents + "개 컴포넌트, 생략·오류 " + skipped + "개.";
        if (backupFolder != null) summary += "\n복원 전 원본 백업: " + backupFolder;
        Report(summary, log);
    }

    private static IEnumerable<Component> Targets(GameObject root)
    {
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true)) yield return r;
        foreach (SortingGroup g in root.GetComponentsInChildren<SortingGroup>(true)) yield return g;
    }
    private static int LayerId(Component c) { return c is Renderer r ? r.sortingLayerID : ((SortingGroup)c).sortingLayerID; }
    private static int Order(Component c) { return c is Renderer r ? r.sortingOrder : ((SortingGroup)c).sortingOrder; }
    private static void SetSorting(Component c, int layer, int order)
    {
        if (c is Renderer r) { r.sortingLayerID = layer; r.sortingOrder = order; }
        else { var g = (SortingGroup)c; g.sortingLayerID = layer; g.sortingOrder = order; }
        EditorUtility.SetDirty(c);
    }
    // Names + ordinal only among siblings of the same name: other sibling reordering is harmless.
    private static List<Segment> Hierarchy(Transform root, Transform node)
    {
        var result = new List<Segment>();
        while (node != root)
        {
            Transform parent = node.parent;
            var matches = new List<Transform>();
            for (int i = 0; i < parent.childCount; i++) if (parent.GetChild(i).name == node.name) matches.Add(parent.GetChild(i));
            result.Add(new Segment { name = node.name, sameNameIndex = matches.IndexOf(node), sameNameCount = matches.Count });
            node = parent;
        }
        result.Reverse(); return result;
    }
    private static Component Resolve(GameObject root, Entry entry, out string reason)
    {
        reason = null; Transform node = root.transform;
        foreach (Segment part in entry.hierarchy)
        {
            var matches = new List<Transform>();
            for (int i = 0; i < node.childCount; i++) if (node.GetChild(i).name == part.name) matches.Add(node.GetChild(i));
            // Duplicate-name parts are ambiguous after a merge: don't guess their identity.
            if (matches.Count != part.sameNameCount || matches.Count != 1 || part.sameNameIndex != 0)
            { reason = "계층 변경 또는 동명 형제 중복: " + part.name; return null; }
            node = matches[0];
        }
        Type type = Type.GetType(entry.componentType);
        if (type == null || (!typeof(Renderer).IsAssignableFrom(type) && type != typeof(SortingGroup)))
        { reason = "컴포넌트 타입을 찾을 수 없음"; return null; }
        Component[] peers = node.GetComponents(type);
        if (peers.Length != entry.componentCount || peers.Length != 1 || entry.componentIndex != 0)
        { reason = "컴포넌트 변경 또는 동일 타입 중복"; return null; }
        return peers[0];
    }
    private static string Label(Entry e) { return e.hierarchy.Count == 0 ? "(Root)" : string.Join("/", e.hierarchy.Select(s => s.name)); }
    private static string FindRoot()
    {
        string[] folders = AssetDatabase.GetSubFolders("Assets")
            .Where(p => string.Equals(Path.GetFileName(p), "prefab", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (folders.Length == 1) return folders[0];
        Debug.LogError("Assets 바로 아래 Prefab/prefab 폴더가 정확히 하나 필요합니다. 검색 결과: " + folders.Length);
        return null;
    }
    private static bool Ready()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        { Debug.LogWarning("플레이 및 컴파일이 끝난 뒤 실행하세요."); return false; }
        if (UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null)
        { Debug.LogWarning("Prefab Mode에서 저장 후 빠져나와 실행하세요."); return false; }
        return true;
    }
    private static string NewBackupFolder(string operation)
    {
        string folder = "Library/PrefabSortingLayerBackups/" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + "_" + operation;
        Directory.CreateDirectory(folder); return folder;
    }
    private static void Backup(string path, string folder)
    {
        string destination = Path.Combine(folder, path);
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        File.Copy(path, destination, false);
    }
    private static void Report(string summary, List<string> details)
    {
        string folder = "Library/PrefabSortingLayerReports"; Directory.CreateDirectory(folder);
        string path = folder + "/" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + ".txt";
        File.WriteAllText(path, summary + "\n\n" + string.Join("\n", details), new UTF8Encoding(false));
        Debug.Log(summary + "\n보고서: " + path + (details.Count > 0 ? "\n" + string.Join("\n", details.Take(20)) : ""));
    }
}
#endif
