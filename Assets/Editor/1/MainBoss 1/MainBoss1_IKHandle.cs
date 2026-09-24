using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class MainBoss1_IKHandle
{
    static bool _active = false;
    static GameObject _cachedRoot = null;
    static GUIStyle _labelStyle = null;

    // Weapon/GripPoint = 왼손 검 손잡이 위치
    // Hand_F/GripPoint_H (0,-0.2) = 오른손 피봇 기준점
    const string WEAPON_GRIP_PATH = "Body/Shoulder_B/Forearm_B/Hand_B/Weapon/GripPoint";
    const string HAND_GRIP_H_PATH = "Body/Shoulder_F/Forearm_F/Hand_F/GripPoint_H";

    static readonly (string root, string mid, string tip, Color color, string label)[] CHAINS =
    {
        ("Body/Shoulder_F",
         "Body/Shoulder_F/Forearm_F",
         "Body/Shoulder_F/Forearm_F/Hand_F",
         Color.cyan, "Hand_F"),

        ("Body/Shoulder_B",
         "Body/Shoulder_B/Forearm_B",
         "Body/Shoulder_B/Forearm_B/Hand_B",
         Color.yellow, "Hand_B"),

        ("Body/Bottom/Thigh_F",
         "Body/Bottom/Thigh_F/Calf_F",
         "Body/Bottom/Thigh_F/Calf_F/Foot_F",
         Color.green, "Foot_F"),

        ("Body/Bottom/Thigh_B",
         "Body/Bottom/Thigh_B/Calf_B",
         "Body/Bottom/Thigh_B/Calf_B/Foot_B",
         new Color(1f,0.5f,0f), "Foot_B"),
    };

    static MainBoss1_IKHandle()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    [MenuItem("Tools/MainBoss 1/Toggle IK Handle")]
    static void Toggle()
    {
        _active = !_active;
        _cachedRoot = null;
        SceneView.RepaintAll();
        Debug.Log($"[IK Handle] {(_active ? "ON" : "OFF")}");
    }

    static void OnSceneGUI(SceneView sv)
    {
        if (!_active) return;
        if (_labelStyle == null)
            _labelStyle = new GUIStyle { normal = { textColor = Color.white }, fontSize = 10 };

        if (_cachedRoot == null)
            _cachedRoot = GameObject.Find("MainBoss 1");
        if (_cachedRoot == null) return;

        // ── IK 체인 핸들 ────────────────────────────────────────────
        foreach (var c in CHAINS)
        {
            Transform tRoot = _cachedRoot.transform.Find(c.root);
            Transform tMid = _cachedRoot.transform.Find(c.mid);
            Transform tTip = _cachedRoot.transform.Find(c.tip);
            if (tRoot == null || tMid == null || tTip == null) continue;

            Vector3 tipPos = tTip.position;
            tipPos.z = 0f;

            Handles.color = c.color;
            Handles.Label(tipPos + Vector3.up * 0.12f, c.label, _labelStyle);

            EditorGUI.BeginChangeCheck();
            Vector3 newPos = Handles.FreeMoveHandle(
                tipPos, 0.09f, Vector3.zero, Handles.CircleHandleCap);

            if (EditorGUI.EndChangeCheck())
            {
                newPos.z = 0f;
                Undo.RecordObject(tRoot, "IK");
                Undo.RecordObject(tMid, "IK");
                SolveIK(tRoot, tMid, tTip, newPos);
            }
        }

        // ── GripPoint 스냅 ───────────────────────────────────────────
        Transform tWeaponGrip = _cachedRoot.transform.Find(WEAPON_GRIP_PATH);
        Transform tHandGripH = _cachedRoot.transform.Find(HAND_GRIP_H_PATH);
        Transform tSF = _cachedRoot.transform.Find("Body/Shoulder_F");
        Transform tFF = _cachedRoot.transform.Find("Body/Shoulder_F/Forearm_F");
        Transform tHF = _cachedRoot.transform.Find("Body/Shoulder_F/Forearm_F/Hand_F");

        if (tWeaponGrip != null && tHandGripH != null && tSF != null && tFF != null && tHF != null)
        {
            Vector3 weaponGripWorld = tWeaponGrip.position;
            weaponGripWorld.z = 0f;

            // Weapon/GripPoint 위치 표시 (마젠타)
            Handles.color = Color.magenta;
            Handles.DotHandleCap(0, weaponGripWorld, Quaternion.identity, 0.04f, EventType.Repaint);
            Handles.Label(weaponGripWorld + Vector3.up * 0.14f, "WGrip", _labelStyle);

            // Snap 버튼: GripPoint_H가 Weapon/GripPoint에 닿도록 SF/FF IK
            // 목표 = Weapon/GripPoint 위치에서 GripPoint_H 오프셋 역보정
            // GripPoint_H는 Hand_F 하위 (0,-0.2) → Hand_F 피봇 위치 계산
            // Hand_F pivot → GripPoint_H 오프셋을 역산해서 실제 Hand_F 목표 위치 구함
            Vector3 gripHLocal = tHandGripH.localPosition; // (0, -0.2, 0)
            // Hand_F world rot 고려해서 역오프셋
            // 목표 Hand_F 위치 = weaponGrip - (GripPoint_H 월드 오프셋)
            // GripPoint_H 월드 오프셋 = Hand_F rot으로 회전된 localPos
            // 이를 역산: targetHF = weaponGrip - R(HF_worldRot) * gripHLocal
            float hfWorldRotZ = WorldRotZ(tHF);
            float rad = hfWorldRotZ * Mathf.Deg2Rad;
            Vector3 gripOffset = new Vector3(
                gripHLocal.x * Mathf.Cos(rad) - gripHLocal.y * Mathf.Sin(rad),
                gripHLocal.x * Mathf.Sin(rad) + gripHLocal.y * Mathf.Cos(rad),
                0f);
            Vector3 targetHF = weaponGripWorld - gripOffset;

            // 버튼 위치
            Vector3 btnPos = weaponGripWorld + Vector3.right * 0.20f;

            Handles.color = new Color(1f, 0.2f, 0.8f, 0.9f);
            Handles.DotHandleCap(0, btnPos, Quaternion.identity, 0.04f, EventType.Repaint);
            Handles.Label(btnPos + Vector3.up * 0.08f, "Snap HF", _labelStyle);

            // 클릭 감지
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                Vector2 guiPos = HandleUtility.WorldToGUIPoint(btnPos);
                if (Vector2.Distance(guiPos, Event.current.mousePosition) < 20f)
                {
                    Undo.RecordObject(tSF, "Snap GripPoint_H to WeaponGrip");
                    Undo.RecordObject(tFF, "Snap GripPoint_H to WeaponGrip");
                    SolveIK(tSF, tFF, tHF, targetHF);
                    Event.current.Use();
                    SceneView.RepaintAll();
                }
            }
        }
    }

    static void SolveIK(Transform tRoot, Transform tMid, Transform tTip, Vector3 target)
    {
        float lenA = (tMid.position - tRoot.position).magnitude;
        float lenB = (tTip.position - tMid.position).magnitude;
        if (lenA < 0.0001f || lenB < 0.0001f) return;

        Vector2 rp = new Vector2(tRoot.position.x, tRoot.position.y);
        Vector2 tp = new Vector2(target.x, target.y);

        float dist = Mathf.Clamp(Vector2.Distance(rp, tp),
            Mathf.Abs(lenA - lenB) + 0.001f, lenA + lenB - 0.001f);

        float angleToTarget = Mathf.Atan2(tp.y - rp.y, tp.x - rp.x) * Mathf.Rad2Deg;

        float cosA = (lenA * lenA + dist * dist - lenB * lenB) / (2f * lenA * dist);
        float offsetA = Mathf.Acos(Mathf.Clamp(cosA, -1f, 1f)) * Mathf.Rad2Deg;
        float cosB = (lenA * lenA + lenB * lenB - dist * dist) / (2f * lenA * lenB);
        float angleB = Mathf.Acos(Mathf.Clamp(cosB, -1f, 1f)) * Mathf.Rad2Deg;

        var rsd = new Vector2(tMid.localPosition.x, tMid.localPosition.y).normalized;
        float rsa = Mathf.Atan2(rsd.y, rsd.x) * Mathf.Rad2Deg;
        float rpz = WorldRotZ(tRoot.parent);

        var msd = new Vector2(tTip.localPosition.x, tTip.localPosition.y).normalized;
        float msa = Mathf.Atan2(msd.y, msd.x) * Mathf.Rad2Deg;
        float mpz = WorldRotZ(tMid.parent);

        bool isLeg = tRoot.name.StartsWith("Thigh");
        if (isLeg)
        {
            tRoot.localEulerAngles = new Vector3(0, 0, (angleToTarget + offsetA) - rpz - rsa);
            tMid.localEulerAngles = new Vector3(0, 0, (angleToTarget + offsetA) - (180f - angleB) - mpz - msa);
        }
        else
        {
            tRoot.localEulerAngles = new Vector3(0, 0, (angleToTarget - offsetA) - rpz - rsa);
            tMid.localEulerAngles = new Vector3(0, 0, (angleToTarget - offsetA) + (180f - angleB) - mpz - msa);
        }
    }

    static float WorldRotZ(Transform t)
    {
        float z = 0f;
        int n = 0;
        while (t != null && n++ < 16)
        {
            float lz = t.localEulerAngles.z;
            if (lz > 180f) lz -= 360f;
            z += lz;
            t = t.parent;
        }
        return z;
    }
}