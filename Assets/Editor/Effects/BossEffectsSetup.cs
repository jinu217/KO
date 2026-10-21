#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BossEffectsSetup
{
    [MenuItem("Tools/Effects/Bosses/Apply All Stage 1-5 Prefabs")]
    public static void ApplyAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Texture2D attack, shot, hit, death, chase;
        Sprite[] notice;
        try
        {
            attack = Load("공격/slash_big.png");
            shot = Load("공격/sting.png");
            hit = Load("데미지받을시/impact_big.png");
            death = Load("공격&데미지받을시&사망&대쉬등 다있는곳/death_01.png");
            chase = Load("걸을때 지속.png");
            notice = Enumerable.Range(1, 6).Select(i =>
                AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(Load("대쉬/SM7001_" + i.ToString("00") + ".png")))
                    .OfType<Sprite>().Single()).ToArray();
            if (attack.width % 10 != 0 || shot.width % 10 != 0 || hit.width % 10 != 0 || death.width % 24 != 0 || chase.width % 8 != 0)
                throw new InvalidOperationException("이펙트 시트 크기를 확인하세요.");
        }
        catch (Exception e) { Debug.LogException(e); return; }
        int count = 0;
        for (int stage = 1; stage <= 5; stage++)
        {
            string folder = "Assets/Prefabs/Stage" + stage + "_Enemy";
            if (!AssetDatabase.IsValidFolder(folder)) { Debug.LogWarning("보스 FX 폴더 없음: " + folder); continue; }
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null || asset.GetComponentInChildren<MidBossController>(true) == null) continue;
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (var boss in root.GetComponentsInChildren<MidBossController>(true))
                    {
                        var fx = boss.GetComponent<BossEffects>();
                        bool added = fx == null;
                        if (added) fx = boss.gameObject.AddComponent<BossEffects>();
                        bool fists = stage == 4 && boss is MainBossController;
                        if (fx.attack.sheet == null) fx.attack.sheet = fists ? hit : attack;
                        if (fx.shot.sheet == null) fx.shot.sheet = shot;
                        if (fx.hit.sheet == null) fx.hit.sheet = hit;
                        if (fx.death.sheet == null) fx.death.sheet = death;
                        if (fx.chase.sheet == null) fx.chase.sheet = chase;
                        if (fx.notice.frames == null || fx.notice.frames.Length == 0) fx.notice.frames = notice;
                        if (added)
                        {
                            fx.attack.flipY = !fists;
                            fx.attack.scale = 2f;
                            fx.hit.scale = 1.2f;
                            fx.death.scale = 2f;
                        }
                        EditorUtility.SetDirty(fx);
                        count++;
                    }
                    string backup = "Library/BossEffectsBackups/" + path;
                    if (!File.Exists(backup)) { Directory.CreateDirectory(Path.GetDirectoryName(backup)); File.Copy(path, backup); }
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                catch (Exception e) { Debug.LogError(path + "\n" + e); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("보스 FX 적용 완료: " + count + "개. 연결된 씬 프리팹에도 적용됩니다.");
    }
    static Texture2D Load(string suffix)
    {
        var paths = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(suffix) + " t:Texture2D")
            .Select(AssetDatabase.GUIDToAssetPath).Where(p => p.EndsWith("/사용할 이펙트/" + suffix, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (paths.Length != 1) throw new InvalidOperationException("이펙트를 하나로 찾을 수 없음: " + suffix);
        var importer = (TextureImporter)AssetImporter.GetAtPath(paths[0]);
        importer.textureType = TextureImporterType.Sprite;
        if (importer.spriteImportMode == SpriteImportMode.None) importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.spritePixelsPerUnit = 64;
        importer.maxTextureSize = Mathf.Max(4096, importer.maxTextureSize);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(paths[0]);
    }
}
#endif
