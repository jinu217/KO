using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class MB2_IKHandle
{
    static bool _active = false;
    static GameObject _root = null;
    static GUIStyle _style = null;

    const string GRIP1 = "Body/Shoulder_B/Forearm_B/Hand_B/Axe/Grippoint1";
    const string GRIP2 = "Body/Shoulder_F/Forearm_F/Hand_F/Grippoint2";

    static readonly (string root, string mid, string tip, Color col, string label)[] CHAINS =
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
         new Color(1f, 0.5f, 0f), "Foot_B"),
    };

    static MB2_IKHandle()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    [MenuItem("Tools/MainBoss2/Toggle IK Handle")]
    static void Toggle()
    {
        _active = !_active;
        _root = null;
        SceneView.RepaintAll();
        Debug.Log($"[MB2 IK] {(_active ? "ON" : "OFF")}");
    }

    static void OnSceneGUI(SceneView sv)
    {
        if (!_active) return;

        if (_style == null)
            _style = new GUIStyle { normal = { textColor = Color.white }, fontSize = 10 };

        if (_root == null)
            _root = GameObject.Find("MainBoss 2");
        if (_root == null) return;

        Event e = Event.current;

        // ── 4개 팔/다리 IK 핸들 ─────────────────────────────────────
        foreach (var c in CHAINS)
        {
            Transform tR = _root.transform.Find(c.root);
            Transform tM = _root.transform.Find(c.mid);
            Transform tT = _root.transform.Find(c.tip);
            if (tR == null || tM == null || tT == null) continue;

            Vector3 pos = tT.position; pos.z = 0f;

            Handles.color = c.col;
            if (e.type == EventType.Repaint)
                Handles.Label(pos + Vector3.up * 0.14f, c.label, _style);

            EditorGUI.BeginChangeCheck();
            Vector3 next = Handles.FreeMoveHandle(pos, 0.1f, Vector3.zero, Handles.CircleHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                next.z = 0f;
                Undo.RecordObject(tR, "IK MB2");
                Undo.RecordObject(tM, "IK MB2");
                SolveIK(tR, tM, tT, next, c.root.Contains("Thigh"));
            }
        }

        // ── Grippoint 스냅 ───────────────────────────────────────────
        Transform grip1 = _root.transform.Find(GRIP1);
        Transform grip2 = _root.transform.Find(GRIP2);
        Transform sf = _root.transform.Find("Body/Shoulder_F");
        Transform ff = _root.transform.Find("Body/Shoulder_F/Forearm_F");
        Transform hf = _root.transform.Find("Body/Shoulder_F/Forearm_F/Hand_F");

        if (grip1 == null || grip2 == null || sf == null || ff == null || hf == null) return;

        Vector3 g1w = grip1.position; g1w.z = 0f;
        Vector3 g2w = grip2.position; g2w.z = 0f;
        Vector3 btn = g1w + Vector3.right * 0.28f;
        Vector3 targetHF = g1w - (g2w - hf.position);

        if (e.type == EventType.Repaint)
        {
            Handles.color = Color.magenta;
            Handles.DotHandleCap(0, g1w, Quaternion.identity, 0.05f, EventType.Repaint);
            Handles.Label(g1w + Vector3.up * 0.17f, "Grip1 (Axe)", _style);

            Handles.color = Color.green;
            Handles.DotHandleCap(0, g2w, Quaternion.identity, 0.04f, EventType.Repaint);
            Handles.Label(g2w + Vector3.up * 0.15f, "Grip2 (HF)", _style);

            Handles.color = Color.yellow;
            Handles.DrawDottedLine(g1w, g2w, 3f);

            Handles.color = new Color(1f, 0.2f, 0.8f, 0.9f);
            Handles.DotHandleCap(0, btn, Quaternion.identity, 0.05f, EventType.Repaint);
            Handles.Label(btn + Vector3.up * 0.12f, "Snap HF\u2192Grip1", _style);
        }

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            Vector2 gui = HandleUtility.WorldToGUIPoint(btn);
            if (Vector2.Distance(gui, e.mousePosition) < 22f)
            {
                Undo.RecordObject(sf, "Snap Grip2->Grip1");
                Undo.RecordObject(ff, "Snap Grip2->Grip1");
                SolveIK(sf, ff, hf, targetHF, false);
                e.Use();
                SceneView.RepaintAll();
                Debug.Log("[MB2 IK] Snap 완료");
            }
        }
    }

    static void SolveIK(Transform tR, Transform tM, Transform tT, Vector3 target, bool isLeg)
    {
        float lA = (tM.position - tR.position).magnitude;
        float lB = (tT.position - tM.position).magnitude;
        if (lA < 0.0001f || lB < 0.0001f) return;

        Vector2 rp = tR.position;
        Vector2 tp = new Vector2(target.x, target.y);
        float dist = Mathf.Clamp(Vector2.Distance(rp, tp),
            Mathf.Abs(lA - lB) + 0.001f, lA + lB - 0.001f);

        float ang = Mathf.Atan2(tp.y - rp.y, tp.x - rp.x) * Mathf.Rad2Deg;
        float cosA = (lA * lA + dist * dist - lB * lB) / (2f * lA * dist);
        float offA = Mathf.Acos(Mathf.Clamp(cosA, -1f, 1f)) * Mathf.Rad2Deg;
        float cosB = (lA * lA + lB * lB - dist * dist) / (2f * lA * lB);
        float angB = Mathf.Acos(Mathf.Clamp(cosB, -1f, 1f)) * Mathf.Rad2Deg;

        var rsd = new Vector2(tM.localPosition.x, tM.localPosition.y).normalized;
        float rsa = Mathf.Atan2(rsd.y, rsd.x) * Mathf.Rad2Deg;
        float rpz = WorldRotZ(tR.parent);

        var msd = new Vector2(tT.localPosition.x, tT.localPosition.y).normalized;
        float msa = Mathf.Atan2(msd.y, msd.x) * Mathf.Rad2Deg;
        float mpz = WorldRotZ(tM.parent);

        if (isLeg)
        {
            tR.localEulerAngles = new Vector3(0, 0, (ang + offA) - rpz - rsa);
            tM.localEulerAngles = new Vector3(0, 0, (ang + offA) - (180f - angB) - mpz - msa);
        }
        else
        {
            tR.localEulerAngles = new Vector3(0, 0, (ang - offA) - rpz - rsa);
            tM.localEulerAngles = new Vector3(0, 0, (ang - offA) + (180f - angB) - mpz - msa);
        }
    }

    static float WorldRotZ(Transform t)
    {
        float z = 0f; int n = 0;
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