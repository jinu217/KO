using UnityEngine;
using UnityEditor;
using System.IO;

public class PrefabStructureConverter : EditorWindow
{
    Object _sourcePrefab;
    Object _targetPrefab;

    [MenuItem("Tools/Prefab/Convert Prefab Structure")]
    public static void ShowWindow()
    {
        var win = GetWindow<PrefabStructureConverter>("Prefab Structure Converter");
        win.minSize = new Vector2(380, 160);
    }

    void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("프리팹 구조 변환 (Joint 제거 + Bottom 추가)", EditorStyles.boldLabel);
        GUILayout.Space(5);
        _sourcePrefab = EditorGUILayout.ObjectField("원본 프리팹 (Joint 있는 것)", _sourcePrefab, typeof(GameObject), false);
        _targetPrefab = EditorGUILayout.ObjectField("대상 프리팹 (탱크몹 구조)", _targetPrefab, typeof(GameObject), false);
        GUILayout.Space(10);
        if (GUILayout.Button("변환 실행", GUILayout.Height(30)))
            Convert();
    }

    void Convert()
    {
        if (_sourcePrefab == null || _targetPrefab == null)
        {
            EditorUtility.DisplayDialog("오류", "원본/대상 프리팹을 모두 지정해주세요.", "확인");
            return;
        }

        string srcPath = AssetDatabase.GetAssetPath(_sourcePrefab);
        string dstPath = AssetDatabase.GetAssetPath(_targetPrefab);

        // 프리팹 인스턴스화
        var src = PrefabUtility.LoadPrefabContents(srcPath);
        var dst = PrefabUtility.LoadPrefabContents(dstPath);

        // ── 헬퍼 ─────────────────────────────────────────────────────
        Transform SrcFind(string path) => src.transform.Find(path);
        Transform DstFind(string path) => dst.transform.Find(path);
        // Joint 있는 경로 못 찾으면 Joint 없는 경로로 폴백
        Transform SrcFindFallback(string withJoint, string withoutJoint) =>
            src.transform.Find(withJoint) ?? src.transform.Find(withoutJoint);

        void CopyPivotAndPos(Transform from, Transform to)
        {
            if (from == null || to == null) return;
            to.localPosition = from.localPosition;
            to.localEulerAngles = from.localEulerAngles;
            // SpriteRenderer pivot 복사
            var srFrom = from.GetComponent<SpriteRenderer>();
            var srTo = to.GetComponent<SpriteRenderer>();
            if (srFrom != null && srTo != null && srFrom.sprite != null)
            {
                // 스프라이트 자체는 대상 것 유지, pivot은 sprite asset에 있어서 직접 복사 불가
                // localPosition으로 위치만 맞춤
            }
        }

        // ── Body ──────────────────────────────────────────────────────
        var srcBody = SrcFind("Body");
        var dstBody = DstFind("Body");
        if (srcBody != null && dstBody != null)
        {
            dstBody.localPosition = srcBody.localPosition;
            dstBody.localEulerAngles = srcBody.localEulerAngles;
        }

        // ── Head ──────────────────────────────────────────────────────
        CopyPivotAndPos(SrcFind("Body/Head"), DstFind("Body/Head"));

        // ── Shoulder_F (ShoulderJoint_F/Shoulder_F → Body/Shoulder_F) ─
        var srcSF = SrcFindFallback("Body/ShoulderJoint_F/Shoulder_F", "Body/Shoulder_F");
        var dstSF = DstFind("Body/Shoulder_F");
        if (srcSF != null && dstSF != null)
        {
            var joint = SrcFind("Body/ShoulderJoint_F");
            dstSF.localPosition = (joint != null ? joint.localPosition : Vector3.zero) + srcSF.localPosition;
            dstSF.localEulerAngles = srcSF.localEulerAngles;
        }

        // ── Hand_F (ArmJoint_F/Hand_F → Shoulder_F/Hand_F) ───────────
        var srcHF = SrcFindFallback("Body/ShoulderJoint_F/Shoulder_F/ArmJoint_F/Hand_F", "Body/Shoulder_F/Hand_F");
        var dstHF = DstFind("Body/Shoulder_F/Hand_F");
        if (srcHF != null && dstHF != null)
        {
            var armJoint = SrcFind("Body/ShoulderJoint_F/Shoulder_F/ArmJoint_F");
            dstHF.localPosition = (armJoint != null ? armJoint.localPosition : Vector3.zero) + srcHF.localPosition;
            dstHF.localEulerAngles = srcHF.localEulerAngles;
        }

        // ── Shoulder_B ────────────────────────────────────────────────
        var srcSB = SrcFindFallback("Body/ShoulderJoint_B/Shoulder_B", "Body/Shoulder_B");
        var dstSB = DstFind("Body/Shoulder_B");
        if (srcSB != null && dstSB != null)
        {
            var joint = SrcFind("Body/ShoulderJoint_B");
            dstSB.localPosition = (joint != null ? joint.localPosition : Vector3.zero) + srcSB.localPosition;
            dstSB.localEulerAngles = srcSB.localEulerAngles;
        }

        // ── Hand_B ────────────────────────────────────────────────────
        var srcHB = SrcFindFallback("Body/ShoulderJoint_B/Shoulder_B/ArmJoint_B/Hand_B", "Body/Shoulder_B/Hand_B");
        var dstHB = DstFind("Body/Shoulder_B/Hand_B");
        if (srcHB != null && dstHB != null)
        {
            var armJoint = SrcFind("Body/ShoulderJoint_B/Shoulder_B/ArmJoint_B");
            dstHB.localPosition = (armJoint != null ? armJoint.localPosition : Vector3.zero) + srcHB.localPosition;
            dstHB.localEulerAngles = srcHB.localEulerAngles;
        }

        // ── Weapon → Club ─────────────────────────────────────────────
        var srcWeapon = SrcFindFallback("Body/ShoulderJoint_B/Shoulder_B/ArmJoint_B/Hand_B/Weapon", "Body/Shoulder_B/Hand_B/Weapon");
        var dstClub = DstFind("Body/Shoulder_B/Hand_B/Club");
        if (srcWeapon != null && dstClub != null)
        {
            dstClub.localPosition = srcWeapon.localPosition;
            dstClub.localEulerAngles = srcWeapon.localEulerAngles;
        }

        // ── 하체: LegJoint → Bottom + Foot (또는 이미 Bottom 구조) ──
        var legJointF = SrcFind("Body/LegJoint_F");
        var legJointB = SrcFind("Body/LegJoint_B");
        var srcBottom = SrcFind("Body/Bottom");
        var srcFootF = SrcFindFallback("Body/LegJoint_F/Foot_F", "Body/Bottom/Foot_F");
        var srcFootB = SrcFindFallback("Body/LegJoint_B/Foot_B", "Body/Bottom/Foot_B");

        var dstBottom = DstFind("Body/Bottom");

        if (legJointF != null)
        {
            // Joint 구조: LegJoint_F Y → Bottom Y
            if (dstBottom != null)
                dstBottom.localPosition = new Vector3(0f, legJointF.localPosition.y, 0f);

            var dstFootF = DstFind("Body/Bottom/Foot_F");
            if (dstFootF != null && srcFootF != null)
            {
                float x = legJointF.localPosition.x + srcFootF.localPosition.x;
                dstFootF.localPosition = new Vector3(x, 0f, 0f);
                dstFootF.localEulerAngles = srcFootF.localEulerAngles;
            }

            var dstFootB = DstFind("Body/Bottom/Foot_B");
            if (dstFootB != null && legJointB != null && srcFootB != null)
            {
                float x = legJointB.localPosition.x + srcFootB.localPosition.x;
                dstFootB.localPosition = new Vector3(x, 0f, 0f);
                dstFootB.localEulerAngles = srcFootB.localEulerAngles;
            }
        }
        else if (srcBottom != null)
        {
            // 이미 Bottom 구조: 그대로 복사
            if (dstBottom != null)
            {
                dstBottom.localPosition = srcBottom.localPosition;
                dstBottom.localEulerAngles = srcBottom.localEulerAngles;
            }

            var dstFootF = DstFind("Body/Bottom/Foot_F");
            if (dstFootF != null && srcFootF != null)
            {
                dstFootF.localPosition = srcFootF.localPosition;
                dstFootF.localEulerAngles = srcFootF.localEulerAngles;
            }

            var dstFootB = DstFind("Body/Bottom/Foot_B");
            if (dstFootB != null && srcFootB != null)
            {
                dstFootB.localPosition = srcFootB.localPosition;
                dstFootB.localEulerAngles = srcFootB.localEulerAngles;
            }
        }

        // ── 저장 ──────────────────────────────────────────────────────
        PrefabUtility.SaveAsPrefabAsset(dst, dstPath);
        PrefabUtility.UnloadPrefabContents(src);
        PrefabUtility.UnloadPrefabContents(dst);
        AssetDatabase.Refresh();

        Debug.Log($"[Converter] 변환 완료: {dstPath}");
        EditorUtility.DisplayDialog("완료", "프리팹 구조 변환 완료", "확인");
    }
}