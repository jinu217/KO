#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Assets/Editor/기타/SpritePartsReferenceAlignerR6.cs
public sealed class SpritePartsReferenceAlignerR6 : EditorWindow
{
    sealed class Part
    {
        public SpriteRenderer renderer;
        public string path;
        public bool use;
        public Vector2 origin; // Top-left of the alpha-cropped sprite, reference pixels.
        public RectInt crop;
        public Color32[] pixels;
        public int width, height;
        public float error;
        public bool found;
    }
    GameObject root;
    Texture2D reference;
    SpriteRenderer anchor;
    float imageScale = 4f;
    bool neutralRotation = true;
    bool separateBossLegs = true;
    readonly List<Part> parts = new List<Part>();
    Vector2 scroll;
    string message = "씬 또는 Prefab Mode의 몹 루트와 완성 PNG를 지정하세요.";
    int imageWidth, imageHeight;
    Color32[] referencePixels;
    bool previewApplied;
    int previewUndoGroup;
    [MenuItem("Tools/기타/Sprite Matcher/Reference Part Alignment R6")]
    static void Open() { GetWindow<SpritePartsReferenceAlignerR6>("파츠 자동 배치 R6"); }
    void OnEnable() { titleContent = new GUIContent("파츠 자동 배치 R6"); }
    void OnGUI()
    {
        int removed = parts.RemoveAll(p => p == null || p.renderer == null || p.renderer.sprite == null);
        if (anchor == null) anchor = null;
        if (removed > 0) message = "씬에서 삭제되거나 교체된 파츠 " + removed + "개를 목록에서 정리했습니다. 현재 루트를 지정하고 '1. 파츠 읽기'를 다시 누르세요.";
        GUILayout.Label("Sprite Parts Aligner R6 — 관절 판별 수정판 / 손·발 좌우 분리", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("완성 PNG와 같은 방향·크기의 파츠를 위치 검색합니다. Hand_B 자체는 인식하고, Hand_B 아래의 모든 자손 SpriteRenderer만 건너뜁니다. Bottom과 발은 계속 검색합니다.", MessageType.Info);
        EditorGUI.BeginChangeCheck();
        root = (GameObject)EditorGUILayout.ObjectField("배치할 몹 루트", root, typeof(GameObject), true);
        reference = (Texture2D)EditorGUILayout.ObjectField("완성 이미지 PNG", reference, typeof(Texture2D), false);
        if (EditorGUI.EndChangeCheck()) { parts.Clear(); anchor = null; }
        imageScale = EditorGUILayout.FloatField("완성 이미지 배율", imageScale);
        separateBossLegs = EditorGUILayout.ToggleLeft("허벅지·정강이도 F=그림 왼쪽 / B=오른쪽에서 검색", separateBossLegs);
        EditorGUILayout.HelpBox("Hand_F와 Foot_F는 완성 이미지 왼쪽, Hand_B와 Foot_B는 오른쪽에서 검색합니다. 가려진 픽셀을 임의로 가림 처리하지 않고 원본 색상으로 비교합니다. 다리의 좌우 분리 옵션은 허벅지·정강이에만 적용됩니다.", MessageType.None);
        neutralRotation = EditorGUILayout.ToggleLeft("선택 파츠 회전을 0으로 맞춤 (원본 파츠 모습 기준)", neutralRotation);
        EditorGUILayout.HelpBox("BasicMob 5의 Hand_B에는 기존 90° 회전이 있습니다. 제공된 완성 이미지에 맞추려면 위 옵션을 켜세요. 루트 위치·회전·스케일과 Pivot, Sorting Layer는 유지합니다.", MessageType.None);
        using (new EditorGUI.DisabledScope(previewApplied || root == null || reference == null))
        {
            if (GUILayout.Button("1. 파츠 읽기")) Run(ReadParts);
            if (GUILayout.Button("제공된 BasicMob 5 분석 위치 불러오기")) Run(LoadBasic5);
        }
        if (parts.Count > 0)
        {
            anchor = (SpriteRenderer)EditorGUILayout.ObjectField("고정 기준 파츠", anchor, typeof(SpriteRenderer), true);
            using (new EditorGUI.DisabledScope(previewApplied))
                if (GUILayout.Button("2. 체크된 파츠 자동 검색")) Run(Search);
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(180));
            foreach (var p in parts)
            {
                EditorGUILayout.BeginVertical("box");
                string sideName = PartName(p);
                string sideLabel = sideName == "handf" ? " [F 손 — 이미지 왼쪽만 검색]" :
                    sideName == "handb" ? " [B 손 — 이미지 오른쪽만 검색]" :
                    sideName == "footf" ? " [F 발 — 이미지 왼쪽만 검색]" :
                    sideName == "footb" ? " [B 발 — 이미지 오른쪽만 검색]" : "";
                using (new EditorGUI.DisabledScope(previewApplied))
                {
                    p.use = EditorGUILayout.ToggleLeft(p.path + sideLabel + (p.renderer == anchor ? " [기준 고정]" : ""), p.use);
                    p.origin = EditorGUILayout.Vector2Field("완성 이미지 좌상단 픽셀 위치", p.origin);
                    if (p.use && !p.found && GUILayout.Button("입력 좌표 사용")) p.found = true;
                }
                GUILayout.Label(p.found ? "오차: " + p.error.ToString("F4") + " (낮을수록 유사, 겹침·같은 파츠는 확인 필요)" : "미검색 / 제외");
                if (reference != null && p.found && p.use) DrawCrop(p);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
            using (new EditorGUI.DisabledScope(previewApplied))
                if (GUILayout.Button("3. 씬에 미리 적용 (Undo 가능)")) Run(ApplyPreview);
            using (new EditorGUI.DisabledScope(!previewApplied))
            {
                if (GUILayout.Button("배치 확정 — 씬 / Prefab Mode에서 저장")) { previewApplied = false; message = "배치를 유지합니다. 씬 또는 Prefab Mode를 저장하세요. 씬 인스턴스는 원본 프리팹에 자동 Apply하지 않습니다."; }
                if (GUILayout.Button("미리 적용 되돌리기")) { Undo.RevertAllDownToGroup(previewUndoGroup); previewApplied = false; message = "미리 적용 이전으로 복구했습니다."; }
            }
        }
        EditorGUILayout.HelpBox(message, MessageType.Info);
    }
    void Run(Action action)
    {
        try { action(); }
        catch (Exception e) { message = e.Message; Debug.LogException(e); }
        finally { EditorUtility.ClearProgressBar(); Repaint(); }
    }
    static Texture2D Decode(string path)
    {
        if (!File.Exists(path)) throw new InvalidOperationException("원본 이미지 경로가 없습니다: " + path);
        var t = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
        if (!t.LoadImage(File.ReadAllBytes(path), false)) { DestroyImmediate(t); throw new InvalidOperationException("PNG를 읽을 수 없습니다: " + path); }
        return t;
    }
    void ReadParts()
    {
        if (root == null || reference == null) throw new InvalidOperationException("루트와 완성 PNG를 지정하세요.");
        if (EditorUtility.IsPersistent(root)) throw new InvalidOperationException("Project의 프리팹은 더블클릭해 Prefab Mode로 열거나 씬에 놓고, Hierarchy의 루트를 지정하세요.");
        if (EditorApplication.isPlayingOrWillChangePlaymode || AnimationMode.InAnimationMode()) throw new InvalidOperationException("Play와 Animation Preview를 종료하고 실행하세요.");
        parts.Clear();
        var r = Decode(AssetDatabase.GetAssetPath(reference));
        imageWidth = r.width; imageHeight = r.height; referencePixels = r.GetPixels32(); DestroyImmediate(r);
        foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr == null || sr.sprite == null) continue;
            string path = AnimationUtility.CalculateTransformPath(sr.transform, root.transform);
            if (IsDescendantOfHandB(path)) continue;
            var tex = Decode(AssetDatabase.GetAssetPath(sr.sprite));
            try
            {
                Rect rect = sr.sprite.rect;
                int ox = Mathf.RoundToInt(rect.x), oy = Mathf.RoundToInt(rect.y), w = Mathf.RoundToInt(rect.width), h = Mathf.RoundToInt(rect.height);
                if (ox < 0 || oy < 0 || ox + w > tex.width || oy + h > tex.height) throw new InvalidOperationException("Sprite rect / 원본 크기를 확인하세요: " + sr.name);
                var all = tex.GetPixels32(); var pixels = new Color32[w * h];
                int minX = w, minY = h, maxX = -1, maxY = -1;
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                    {
                        var c = all[(oy + y) * tex.width + ox + x]; pixels[y * w + x] = c;
                        if (c.a > 16) { minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y); maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y); }
                    }
                if (maxX < 0) continue;
                parts.Add(new Part
                {
                    renderer = sr,
                    path = path,
                    pixels = pixels,
                    width = w,
                    height = h,
                    crop = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1),
                    use = true
                });
            }
            finally { DestroyImmediate(tex); }
        }
        anchor = parts.Select(p => p.renderer).FirstOrDefault(s => s.name == "Body");
        message = "파츠 " + parts.Count + "개를 읽었습니다. Hand_B 자체와 Bottom·발은 포함하고 Hand_B 아래 자손만 건너뛰었습니다.";
    }
    void LoadBasic5()
    {
        ReadParts();
        if (imageWidth != 314 || imageHeight != 468) throw new InvalidOperationException("이 분석 위치는 제공된 기본.png (314×468)에만 맞습니다. 다른 이미지는 자동 검색을 사용하세요.");
        imageScale = 4;
        var locations = new Dictionary<string, Vector2> {
            {"Body",new Vector2(54,122)}, {"Head",new Vector2(79,0)},
            {"Shoulder_F",new Vector2(11,103)}, {"Shoulder_B",new Vector2(198,103)},
            {"Hand_F",new Vector2(0,233)}, {"Hand_B",new Vector2(190,229)},
            {"Foot_F",new Vector2(59,364)}, {"Foot_B",new Vector2(166,364)} };
        foreach (var p in parts)
        {
            Vector2 v; p.use = locations.TryGetValue(p.renderer.name, out v);
            if (!p.use) continue;
            p.origin = v; p.found = true; p.error = Score(p, Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), FullSamples(p));
        }
        message = "제공된 이미지의 분석 후보를 불러왔습니다. 좌우 발은 별도 배치했고 도끼는 제외했습니다. 이미지가 겹치는 뒷어깨는 미리 적용 후 확인하세요.";
    }
    struct Sample { public int x, y; public Color32 color; }
    List<Sample> FullSamples(Part p)
    {
        var result = new List<Sample>();
        for (int y = 0; y < p.crop.height; y++) for (int x = 0; x < p.crop.width; x++)
            {
                int sy = p.crop.yMax - 1 - y; var c = p.pixels[sy * p.width + p.crop.x + x];
                if (c.a < 230) continue;
                result.Add(new Sample { x = Mathf.FloorToInt((x + .5f) * imageScale), y = Mathf.FloorToInt((y + .5f) * imageScale), color = c });
            }
        return result;
    }
    float Score(Part p, int x, int y, List<Sample> samples)
    {
        if (samples.Count == 0) return float.PositiveInfinity;
        double total = 0;
        foreach (var s in samples)
        {
            int rx = x + s.x, ry = y + s.y;
            if (rx < 0 || ry < 0 || rx >= imageWidth || ry >= imageHeight) { total += 2; continue; }
            var c = referencePixels[(imageHeight - 1 - ry) * imageWidth + rx];
            float dr = (c.r - s.color.r) / 255f, dg = (c.g - s.color.g) / 255f, db = (c.b - s.color.b) / 255f;
            total += dr * dr + dg * dg + db * db + (1f - c.a / 255f);
        }
        return (float)(total / samples.Count);
    }
    void ValidateParts()
    {
        if (root == null || reference == null || referencePixels == null)
            throw new InvalidOperationException("루트와 이미지를 지정하고 파츠 읽기를 다시 실행하세요.");
        foreach (var p in parts)
        {
            if (p == null || p.renderer == null || p.renderer.sprite == null)
                throw new InvalidOperationException("삭제되거나 스프라이트가 없는 파츠가 있습니다. 파츠를 다시 읽으세요.");
            if (!p.renderer.transform.IsChildOf(root.transform))
                throw new InvalidOperationException("파츠의 루트가 변경되었습니다. 파츠를 다시 읽으세요.");
        }
    }
    void Search()
    {
        ValidateParts();
        if (imageScale <= 0) throw new InvalidOperationException("배율은 0보다 커야 합니다.");
        var active = parts.Where(p => p.use).ToList();
        for (int i = 0; i < active.Count; i++)
        {
            var p = active[i]; var full = FullSamples(p);
            var sparse = new List<Sample>();
            for (int k = 0; k < full.Count; k += Mathf.Max(1, full.Count / 64)) sparse.Add(full[k]);
            int maxX = imageWidth - Mathf.CeilToInt(p.crop.width * imageScale), maxY = imageHeight - Mathf.CeilToInt(p.crop.height * imageScale);
            if (maxX < 0 || maxY < 0) throw new InvalidOperationException("파츠가 완성 이미지보다 큽니다. 배율을 확인하세요: " + p.path);
            var best = new List<Vector3>();
            for (int y = 0; y <= maxY; y += 2)
            {
                if (y % 24 == 0 && EditorUtility.DisplayCancelableProgressBar("파츠 위치 검색", p.path, (i + (float)y / Mathf.Max(1, maxY)) / Mathf.Max(1, active.Count))) throw new OperationCanceledException("검색 취소. 씬은 수정하지 않았습니다.");
                for (int x = 0; x <= maxX; x += 2)
                {
                    if (!CorrectPartSide(p, x)) continue;
                    float error = Score(p, x, y, sparse);
                    if (best.Count < 24 || error < best[best.Count - 1].z)
                    { best.Add(new Vector3(x, y, error)); best.Sort((a, b) => a.z.CompareTo(b.z)); if (best.Count > 24) best.RemoveAt(24); }
                }
            }
            float lowest = float.PositiveInfinity; Vector2 position = Vector2.zero;
            foreach (var candidate in best)
                for (int dy = -2; dy <= 2; dy++) for (int dx = -2; dx <= 2; dx++)
                    {
                        int x = (int)candidate.x + dx, y = (int)candidate.y + dy;
                        if (x < 0 || y < 0 || x > maxX || y > maxY) continue;
                        if (!CorrectPartSide(p, x)) continue;
                        float score = Score(p, x, y, full);
                        if (score < lowest) { lowest = score; position = new Vector2(x, y); }
                    }
            if (float.IsPositiveInfinity(lowest)) throw new InvalidOperationException("해당 방향의 파츠 후보를 찾지 못했습니다: " + p.path);
            p.origin = position; p.error = lowest; p.found = true;
        }
        RefineMatches(active);
        message = "R6 검색 완료. 손과 발은 지정된 이미지 절반에서 원본 색상·알파를 직접 비교했습니다.";
    }
    static string PartName(Part p)
    {
        if (p == null || p.renderer == null || p.renderer.sprite == null) return string.Empty;
        // Prefer this object, then its sprite, then the nearest named ancestor.
        // Walking from the root mislabels Calf and Foot as Thigh.
        string name = NormalizePartName(p.renderer.name);
        if (IsDirectionalPartName(name)) return name;
        string sprite = NormalizePartName(p.renderer.sprite.name);
        if (IsDirectionalPartName(sprite)) return sprite;
        string[] segments = (p.path ?? string.Empty).Split('/');
        for (int i = segments.Length - 1; i >= 0; i--)
        {
            string normalized = NormalizePartName(segments[i]);
            if (IsDirectionalPartName(normalized)) return normalized;
        }
        return name;
    }
    static string NormalizePartName(string value)
    {
        return (value ?? string.Empty).ToLowerInvariant().Replace("_", "").Replace(" ", "").Replace("(clone)", "");
    }
    static bool IsDirectionalPartName(string name)
    {
        return name == "handf" || name == "handb" || IsLegName(name);
    }
    static bool IsLegName(string name)
    {
        return name == "footf" || name == "footb" || name == "thighf" || name == "thighb" || name == "calff" || name == "calfb";
    }
    static bool IsDescendantOfHandB(string path)
    {
        // Exclude descendants only; Hand_B itself and other branches remain.
        string[] segments = (path ?? string.Empty).Split('/');
        for (int i = 0; i < segments.Length - 1; i++)
            if (NormalizePartName(segments[i]) == "handb") return true;
        return false;
    }

    float FineScore(Part p, Vector2 origin, float scale)
    {
        double error = 0; int count = 0;
        // Sample inside each opaque source pixel at four offsets; avoid one-point aliasing.
        for (int y = 0; y < p.crop.height; y++) for (int x = 0; x < p.crop.width; x++)
            {
                Color32 source = p.pixels[(p.crop.yMax - 1 - y) * p.width + p.crop.x + x];
                if (source.a < 230) continue;
                for (int sy = 0; sy < 2; sy++) for (int sx = 0; sx < 2; sx++)
                    {
                        float px = origin.x + (x + .25f + .5f * sx) * scale - .5f;
                        float py = origin.y + (y + .25f + .5f * sy) * scale - .5f;
                        if (px < 0 || py < 0 || px >= imageWidth - 1 || py >= imageHeight - 1) { error += 1; count++; continue; }
                        int ix = (int)px, iy = (int)py; float fx = px - ix, fy = py - iy;
                        Color c = Color.Lerp(Color.Lerp(referencePixels[(imageHeight - 1 - iy) * imageWidth + ix], referencePixels[(imageHeight - 1 - iy) * imageWidth + ix + 1], fx),
                            Color.Lerp(referencePixels[(imageHeight - 2 - iy) * imageWidth + ix], referencePixels[(imageHeight - 2 - iy) * imageWidth + ix + 1], fx), fy);
                        float dr = c.r - source.r / 255f, dg = c.g - source.g / 255f, db = c.b - source.b / 255f;
                        error += dr * dr + dg * dg + db * db + (1 - c.a); count++;
                    }
            }
        return count == 0 ? float.PositiveInfinity : (float)(error / count);
    }
    void RefineMatches(List<Part> active)
    {
        if (active.Count == 0) return;
        float original = imageScale, bestScale = imageScale, bestError = float.PositiveInfinity;
        var initial = active.Select(p => p.origin).ToArray();
        var bestPositions = (Vector2[])initial.Clone();
        // Refine a common scale across all parts instead of stretching individual limbs.
        for (int k = -4; k <= 4; k++)
        {
            if (EditorUtility.DisplayCancelableProgressBar("R6 정밀 보정", "공통 배율 / 파츠 위치", (k + 4) / 9f)) throw new OperationCanceledException("검색 취소. 씬은 수정하지 않았습니다.");
            imageScale = original * (1f + k * .01f);
            var trial = new Vector2[active.Count]; float total = 0;
            for (int i = 0; i < active.Count; i++)
            {
                var p = active[i]; float error = float.PositiveInfinity;
                for (int dy = -4; dy <= 4; dy += 2) for (int dx = -4; dx <= 4; dx += 2)
                    {
                        Vector2 v = initial[i] + new Vector2(dx, dy);
                        if (!CorrectPartSide(p, v.x)) continue;
                        float e = FineScore(p, v, imageScale);
                        if (e < error) { error = e; trial[i] = v; }
                    }
                total += error;
            }
            if (total < bestError) { bestError = total; bestScale = imageScale; bestPositions = trial; }
        }
        imageScale = bestScale;
        for (int i = 0; i < active.Count; i++)
        {
            var p = active[i]; Vector2 center = bestPositions[i]; float error = float.PositiveInfinity;
            for (int dy = -8; dy <= 8; dy++) for (int dx = -8; dx <= 8; dx++)
                {
                    Vector2 v = center + new Vector2(dx * .25f, dy * .25f);
                    if (!CorrectPartSide(p, v.x)) continue;
                    float e = FineScore(p, v, imageScale);
                    if (e < error) { error = e; p.origin = v; }
                }
            p.error = error;
        }
        Debug.Log("[SpriteParts R6] 배율 " + imageScale.ToString("F3") + " / " + string.Join(" / ", active.Select(p => p.renderer.name + ": " + p.origin.ToString("F2"))));
    }
    bool CorrectPartSide(Part p, float x)
    {
        float center = x + p.crop.width * imageScale * .5f;
        string name = PartName(p);
        if (name == "handf" || name == "footf") return center < imageWidth * .5f;
        if (name == "handb" || name == "footb") return center >= imageWidth * .5f;
        if (separateBossLegs && (name == "thighf" || name == "thighb" || name == "calff" || name == "calfb"))
        {
            bool front = name.EndsWith("f", StringComparison.Ordinal);
            return front ? center < imageWidth * .5f : center >= imageWidth * .5f;
        }
        return true;
    }
    static float RoundPosition(float value) => (float)Math.Round(value, 3, MidpointRounding.AwayFromZero);
    Vector2 PivotInImage(Part p)
    {
        Vector2 pivot = p.renderer.sprite.pivot;
        // Image Y is downward; sprite pivot Y is measured upward from sprite rect bottom.
        return p.origin + new Vector2((pivot.x - p.crop.x) * imageScale, (p.crop.yMax - pivot.y) * imageScale);
    }
    void DrawCrop(Part p)
    {
        Rect r = GUILayoutUtility.GetRect(100, 85, GUILayout.ExpandWidth(false));
        Rect uv = new Rect(p.origin.x / imageWidth, 1f - (p.origin.y + p.crop.height * imageScale) / imageHeight, p.crop.width * imageScale / imageWidth, p.crop.height * imageScale / imageHeight);
        GUI.DrawTextureWithTexCoords(r, reference, uv, true);
    }
    void ApplyPreview()
    {
        ValidateParts();
        if (root == null || EditorUtility.IsPersistent(root) || EditorApplication.isPlayingOrWillChangePlaymode || AnimationMode.InAnimationMode()) throw new InvalidOperationException("씬/Prefab Mode에서 Play·Animation Preview를 끄고 실행하세요.");
        var chosen = parts.Where(p => p.use).ToList();
        var body = chosen.FirstOrDefault(p => p.renderer == anchor);
        if (body == null || !body.found || chosen.Any(p => !p.found)) throw new InvalidOperationException("기준 파츠를 포함해 체크된 모든 파츠의 위치를 검색/입력하세요.");
        if (imageScale <= 0) throw new InvalidOperationException("배율을 확인하세요.");
        foreach (var p in chosen)
        {
            if (Mathf.Abs(p.renderer.sprite.pixelsPerUnit - anchor.sprite.pixelsPerUnit) > .001f || p.renderer.flipX || p.renderer.flipY)
                throw new InvalidOperationException("같은 PPU, SpriteRenderer Flip 해제 상태가 필요합니다: " + p.path);
            for (Transform t = p.renderer.transform; t != root.transform; t = t.parent)
            {
                if (t == null) throw new InvalidOperationException("루트가 변경되었습니다. 파츠를 다시 읽으세요.");
                if ((t.localScale - Vector3.one).sqrMagnitude > .00001f) throw new InvalidOperationException("루트 아래 파츠 scale은 (1,1,1)이어야 합니다: " + t.name);
                if (Quaternion.Angle(t.localRotation, Quaternion.identity) > .01f && !(neutralRotation && chosen.Any(q => q.renderer.transform == t)))
                    throw new InvalidOperationException("회전된 파츠가 있습니다. 회전 0 옵션을 켜거나 중간 부모 회전을 정리하세요: " + t.name);
            }
        }
        var frontFoot = chosen.FirstOrDefault(p => PartName(p) == "footf");
        var backFoot = chosen.FirstOrDefault(p => PartName(p) == "footb");
        var frontHand = chosen.FirstOrDefault(p => PartName(p) == "handf");
        var backHand = chosen.FirstOrDefault(p => PartName(p) == "handb");
        if (frontFoot != null && backFoot != null && frontFoot.origin.x + frontFoot.crop.width * imageScale * .5f >= backFoot.origin.x + backFoot.crop.width * imageScale * .5f)
            throw new InvalidOperationException("그림 왼쪽은 Foot_F, 오른쪽은 Foot_B로 좌표를 지정하세요.");
        Vector2 basePixel = PivotInImage(body);
        Vector3 baseLocal = root.transform.InverseTransformPoint(anchor.transform.position);
        var targets = new Dictionary<Transform, Vector3>();
        foreach (var p in chosen)
        {
            Vector2 delta = PivotInImage(p) - basePixel;
            Vector3 old = root.transform.InverseTransformPoint(p.renderer.transform.position);
            targets[p.renderer.transform] = new Vector3(baseLocal.x + delta.x / (imageScale * anchor.sprite.pixelsPerUnit), baseLocal.y - delta.y / (imageScale * anchor.sprite.pixelsPerUnit), old.z);
        }
        Undo.IncrementCurrentGroup(); previewUndoGroup = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Align Sprite Parts To Reference");
        Transform feetBottom = frontFoot != null && backFoot != null ? frontFoot.renderer.transform.parent : null;
        if (feetBottom != null && (feetBottom != backFoot.renderer.transform.parent || feetBottom.name != "Bottom" || feetBottom == root.transform || feetBottom == anchor.transform)) feetBottom = null;
        var undoTargets = chosen.Select(p => (UnityEngine.Object)p.renderer.transform).ToList();
        if (feetBottom != null) undoTargets.Add(feetBottom);
        Undo.RecordObjects(undoTargets.Distinct().ToArray(), "Align Sprite Parts");
        foreach (var p in chosen.OrderBy(p => p.path.Count(c => c == '/')))
        {
            var t = p.renderer.transform;
            if (neutralRotation) t.localRotation = Quaternion.identity;
            t.position = root.transform.TransformPoint(targets[t]);
            if (p.renderer != anchor)
            {
                Vector3 local = t.localPosition;
                t.localPosition = new Vector3(RoundPosition(local.x), RoundPosition(local.y), RoundPosition(local.z));
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(t);
        }
        if (feetBottom != null)
        {
            Transform f = frontFoot.renderer.transform, b = backFoot.renderer.transform;
            // Use the shared height; tiny matching discrepancies are averaged deliberately.
            float sharedY = (f.localPosition.y + b.localPosition.y) * .5f;
            Vector3 bottomPosition = feetBottom.localPosition;
            bottomPosition.y = RoundPosition(bottomPosition.y + sharedY);
            feetBottom.localPosition = bottomPosition;
            Vector3 fp = f.localPosition, bp = b.localPosition;
            fp.y = 0f; bp.y = 0f;
            f.localPosition = fp; b.localPosition = bp;
            PrefabUtility.RecordPrefabInstancePropertyModifications(feetBottom);
            PrefabUtility.RecordPrefabInstancePropertyModifications(f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(b);
        }
        if (frontFoot != null && backFoot != null)
        {
            float fx = root.transform.InverseTransformPoint(frontFoot.renderer.transform.position).x;
            float bx = root.transform.InverseTransformPoint(backFoot.renderer.transform.position).x;
            if (fx >= bx || Mathf.Abs(fx - bx) < .01f)
            {
                Undo.RevertAllDownToGroup(previewUndoGroup);
                throw new InvalidOperationException("R6: 적용 후 양발 분리 검사 실패. 파츠 이름과 부모 구조를 확인하세요.");
            }
        }
        if (frontHand != null && backHand != null)
        {
            float fx = root.transform.InverseTransformPoint(frontHand.renderer.transform.position).x;
            float bx = root.transform.InverseTransformPoint(backHand.renderer.transform.position).x;
            if (fx >= bx || Mathf.Abs(fx - bx) < .01f)
            {
                Undo.RevertAllDownToGroup(previewUndoGroup);
                throw new InvalidOperationException("R6: 적용 후 양손 좌우 분리 검사 실패. 파츠 이름과 부모 구조를 확인하세요.");
            }
        }
        Debug.Log("[SpriteParts R6] 적용 완료: " + string.Join(" / ", chosen.Select(p => p.path + " = " + p.renderer.transform.localPosition.ToString("F3"))));
        Undo.CollapseUndoOperations(previewUndoGroup); previewApplied = true;
        EditorSceneManager.MarkSceneDirty(root.scene); SceneView.RepaintAll();
        message = "씬에 미리 적용했습니다. 루트와 Body 피봇은 고정됩니다. Bottom 직속 양발의 Y는 0, 공통 높이는 Bottom에 적용했습니다. 도끼 등 제외 파츠는 localPosition을 유지하며 부모와 함께 움직입니다. 확인 후 확정하거나 되돌리세요.";
    }
}
#endif
