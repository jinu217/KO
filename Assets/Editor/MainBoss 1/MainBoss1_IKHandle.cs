using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class MainBoss1_IKHandle
{
    static bool _active = false;
    static GameObject _cachedRoot = null;
    static GUIStyle _labelStyle = null;

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

    // GripPoint IK: Hand_F를 GripPoint 위치로 자동 맞추기
    const string GRIP_POINT_PATH = "Body/Shoulder_B/Forearm_B/Hand_B/Weapon/GripPoint";

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

        // ── GripPoint 스냅 버튼 ─────────────────────────────────────
        Transform tGrip = _cachedRoot.transform.Find(GRIP_POINT_PATH);
        if (tGrip != null)
        {
            Transform tSF = _cachedRoot.transform.Find("Body/Shoulder_F");
            Transform tFF = _cachedRoot.transform.Find("Body/Shoulder_F/Forearm_F");
            Transform tHF = _cachedRoot.transform.Find("Body/Shoulder_F/Forearm_F/Hand_F");

            Vector3 gripWorld = tGrip.position;
            gripWorld.z = 0f;

            // GripPoint 위치에 마젠타 마커 표시
            Handles.color = Color.magenta;
            Handles.DotHandleCap(0, gripWorld, Quaternion.identity, 0.04f, EventType.Repaint);
            Handles.Label(gripWorld + Vector3.up * 0.14f, "GripPoint", _labelStyle);

            // 버튼: Hand_F를 GripPoint로 스냅 (첫 클릭에 바로 실행)
            float snapSize = 0.06f;
            Rect snapRect = new Rect(
                HandleUtility.WorldToGUIPoint(gripWorld + Vector3.right * 0.18f) - Vector2.one * 20,
                Vector2.one * 40);

            Handles.color = new Color(1f, 0.2f, 0.8f, 0.8f);
            Handles.DrawSolidDisc(gripWorld + Vector3.right * 0.18f, Vector3.forward, snapSize);

            if (Event.current.type == EventType.MouseDown &&
                snapRect.Contains(Event.current.mousePosition))
            {
                if (tSF != null && tFF != null && tHF != null)
                {
                    Undo.RecordObject(tSF, "Snap Hand_F to GripPoint");
                    Undo.RecordObject(tFF, "Snap Hand_F to GripPoint");
                    SolveIK(tSF, tFF, tHF, gripWorld);
                    Event.current.Use();
                }
            }
            Handles.Label(gripWorld + Vector3.right * 0.14f + Vector3.up * 0.08f, "Snap HF→", _labelStyle);
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