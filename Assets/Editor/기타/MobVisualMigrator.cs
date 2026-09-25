using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Assets/Prefab/{스테이지번호}/{몹이름} {스테이지번호}.prefab  (스프라이트/애니메이션이 세팅됨)
///   ↓ 프리팹을 통째로 복사해서(원본과 링크 없는 독립 복사본) 타겟 안에 자식으로 삽입
/// Assets/Prefabs/Stage{스테이지번호}_Enemy/Stage{스테이지번호}_{Elite|Enemy}_{번호}.prefab  (AI 등 기타 설정이 세팅됨)
///
/// 자식을 하나하나 분해해서 복사하는 게 아니라, BasicMob 1 등 소스 오브젝트 자체를
/// (원본과 연결되지 않은) 독립된 복사본으로 타겟의 자식 오브젝트 하나로 넣는다.
/// 타겟 루트의 기존 컴포넌트(AI 스크립트 등)나 GroundCheck 같은 다른 자식은 전혀 건드리지 않는다.
/// </summary>
public class MobVisualMigrator : EditorWindow
{
    string _sourceFolder = "Assets/Prefab/1";
    string _targetFolder = "Assets/Prefabs";
    bool _dryRun = true;

    // 몹 이름 -> 타겟 프리팹 이름 뒷부분 (Stage{N}_{이거}.prefab)
    static readonly (string mobName, string targetSuffix)[] Mapping = new[]
    {
        ("EBasicMob", "Elite_1"),
        ("EArcher",   "Elite_2"),
        ("BasicMob",  "Enemy_1"),
        ("TankMob",   "Enemy_2"),
        ("SpeedMob",  "Enemy_3"),
        ("MidBoss",   "MidBoss"),
        ("MainBoss",  "MainBoss"),
    };

    List<string> _log = new List<string>();
    Vector2 _scroll;

    [MenuItem("Tools/기타/Mob/Migrate Visuals (Prefab -> Prefabs)")]
    public static void ShowWindow()
    {
        var win = GetWindow<MobVisualMigrator>("Mob Visual Migrator");
        win.minSize = new Vector2(480, 400);
    }

    void OnGUI()
    {
        GUILayout.Space(10);

        GUILayout.Label("스프라이트/애니메이션 이전 (Prefab → Prefabs)", EditorStyles.boldLabel);

        _sourceFolder = EditorGUILayout.TextField("소스 폴더 (스프라이트/애니메이션)", _sourceFolder);
        _targetFolder = EditorGUILayout.TextField("타겟 폴더 (AI/기타 설정)", _targetFolder);

        GUILayout.Space(5);

        _dryRun = EditorGUILayout.ToggleLeft(
            "Dry Run (실제로 저장하지 않고 무엇이 처리될지만 확인)",
            _dryRun
        );

        GUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "소스 폴더에 스테이지 폴더를 직접 입력하세요. 예: Assets/Prefab/3\n" +
            "입력한 폴더 안의 프리팹만 처리합니다. 하위 숫자 폴더는 검색하지 않습니다.\n" +
            "매핑: EBasicMob→Elite_1, EArcher→Elite_2, BasicMob→Enemy_1, TankMob→Enemy_2, SpeedMob→Enemy_3, MidBoss→MidBoss, MainBoss→MainBoss",
            MessageType.Info
        );

        GUILayout.Space(10);

        if (GUILayout.Button(_dryRun ? "미리보기 실행" : "실제 실행 (저장됨)", GUILayout.Height(32)))
        {
            // OnGUI(레이아웃 처리 중)에 바로 실행하면 Run() 안의 DisplayDialog(모달)가
            // 레이아웃 스택을 깨서 "EndLayoutGroup: BeginLayoutGroup must be called first" 에러가 남.
            // 다음 에디터 업데이트로 미뤄서 레이아웃이 끝난 뒤에 실행되도록 함.
            EditorApplication.delayCall += Run;
        }

        GUILayout.Space(10);
        GUILayout.Label("로그", EditorStyles.boldLabel);

        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));

        foreach (var line in _log)
        {
            EditorGUILayout.LabelField(line, EditorStyles.wordWrappedLabel);
        }

        EditorGUILayout.EndScrollView();
    }

    void Run()
    {
        _log.Clear();

        if (!AssetDatabase.IsValidFolder(_sourceFolder))
        {
            _log.Add($"[오류] 소스 폴더가 없습니다: {_sourceFolder}");
            return;
        }

        if (!AssetDatabase.IsValidFolder(_targetFolder))
        {
            _log.Add($"[오류] 타겟 폴더가 없습니다: {_targetFolder}");
            return;
        }

        // 소스 폴더 자체의 이름을 스테이지 번호로 사용
        // 예: Assets/Prefab/3 -> stage = 3
        string stageFolderName = Path.GetFileName(_sourceFolder.TrimEnd('/', '\\'));

        if (!int.TryParse(stageFolderName, out int stage))
        {
            _log.Add($"[오류] 소스 폴더의 마지막 이름이 숫자가 아닙니다: {_sourceFolder}");
            _log.Add("예: Assets/Prefab/3");
            return;
        }

        int success = 0, skipped = 0, failed = 0;

        foreach (var (mobName, targetSuffix) in Mapping)
        {
            string sourcePath = $"{_sourceFolder}/{mobName} {stage}.prefab";
            string targetPath =
                $"{_targetFolder}/Stage{stage}_Enemy/Stage{stage}_{targetSuffix}.prefab";

            if (!File.Exists(sourcePath))
            {
                _log.Add($"[건너뜀] 소스 없음: {sourcePath}");
                skipped++;
                continue;
            }

            if (!File.Exists(targetPath))
            {
                _log.Add($"[건너뜀] 타겟 없음: {targetPath}");
                skipped++;
                continue;
            }

            try
            {
                int addedCount = MigrateOne(sourcePath, targetPath, _dryRun);

                _log.Add(
                    $"[{(_dryRun ? "미리보기" : "완료")}] {sourcePath}  →  {targetPath}  " +
                    $"(독립 복사본으로 자식 삽입, {addedCount}개)"
                );

                success++;
            }
            catch (Exception e)
            {
                _log.Add($"[실패] {sourcePath} → {targetPath} : {e.Message}");
                failed++;
                Debug.LogError($"[MobVisualMigrator] {sourcePath} → {targetPath} 실패:\n{e}");
            }
        }

        if (!_dryRun)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        string summary = $"성공 {success}개 / 건너뜀 {skipped}개 / 실패 {failed}개";
        _log.Add("");
        _log.Add(summary);

        EditorUtility.DisplayDialog(
            _dryRun ? "미리보기 완료" : "이전 완료",
            summary + (_dryRun ? "\n\n(Dry Run이라 실제로 저장되지 않았습니다.)" : ""),
            "확인"
        );

        Repaint();
    }

    /// <summary>
    /// 소스 프리팹을 원본과 링크되지 않은 독립 복사본으로 타겟 프리팹 안에 자식으로 넣는다.
    /// 프리팹 연결이 없으므로 나중에 소스 원본을 고쳐도 타겟에는 반영되지 않는다(완전 분리).
    /// 타겟에 이미 있던 GroundCheck/WallCheck 등 다른 자식은 전혀 건드리지 않는다.
    /// 반환값: 새로 추가된 자식 개수(항상 1)
    /// </summary>
    int MigrateOne(string sourcePath, string targetPath, bool dryRun)
    {
        GameObject sourceAsset = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);

        if (sourceAsset == null)
        {
            throw new Exception("소스 프리팹을 로드할 수 없습니다.");
        }

        GameObject targetRoot = PrefabUtility.LoadPrefabContents(targetPath);

        try
        {
            string instanceName = sourceAsset.name; // 예: "BasicMob 1"

            // 이미 이전에 넣어둔 같은 이름의 인스턴스가 있으면 제거(재실행 시 중복 방지)
            Transform existing = targetRoot.transform.Find(instanceName);

            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            // 소스 프리팹을 완전히 독립된 복사본으로 타겟 루트 아래에 생성 (링크 없음)
            GameObject instance = (GameObject)UnityEngine.Object.Instantiate(
                sourceAsset,
                targetRoot.transform
            );

            // Instantiate가 이름 뒤에 "(Clone)"을 붙이므로 원래 이름으로 되돌림
            instance.name = instanceName;

            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            if (!dryRun)
            {
                PrefabUtility.SaveAsPrefabAsset(targetRoot, targetPath);
            }

            return 1;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(targetRoot);
        }
    }
}