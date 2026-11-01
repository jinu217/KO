using UnityEngine;

// Editor-only movement display. Runtime movement remains owned by Stage5_MainBoss.
[ExecuteAlways, DisallowMultipleComponent]
public sealed class Stage5MainBossPathPreview : MonoBehaviour
{
    public Transform target;
    [HideInInspector] public float active, frame;
#if UNITY_EDITOR
    bool captured;
    Vector3 origin, center, localOrigin, originalScale;
    int lastMode;
    void OnEnable() { UnityEditor.EditorApplication.update += Refresh; }
    void OnDisable() { UnityEditor.EditorApplication.update -= Refresh; Restore(); }
    void OnDestroy() { Restore(); }
    void Restore()
    {
        if (!captured) return;
        if (!Application.isPlaying) { transform.localPosition = localOrigin; transform.localScale = originalScale; }
        captured = false;
    }
    void Refresh()
    {
        if (this == null || Application.isPlaying) return;
        if (!UnityEditor.AnimationMode.InAnimationMode() || active < .5f) { Restore(); return; }
        if (captured && lastMode != Mathf.RoundToInt(active)) Restore();
        if (!captured)
        {
            origin = transform.position; localOrigin = transform.localPosition; originalScale = transform.localScale; lastMode = Mathf.RoundToInt(active);
            Transform player = target;
            if (player == null)
            {
                try { var obj = GameObject.FindGameObjectWithTag("Player"); if (obj != null && obj.scene == gameObject.scene) player = obj.transform; }
                catch (UnityException) { }
            }
            center = player != null ? player.position : origin + Vector3.right * 2.5f;
            center.z = origin.z;
            foreach (string axis in new[] { "x", "y", "z" })
            {
                float value = axis == "x" ? localOrigin.x : axis == "y" ? localOrigin.y : localOrigin.z;
                UnityEditor.AnimationMode.AddPropertyModification(
                    UnityEditor.EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition." + axis),
                    new UnityEditor.PropertyModification
                    {
                        target = transform,
                        propertyPath = "m_LocalPosition." + axis,
                        value = value.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    }, true);
            }
            captured = true;
        }
        Vector3 firstStart = center + new Vector3(-8,-1.8f,0);
        Vector3 up = center + new Vector3(8,1.8f,0);
        Vector3 down = firstStart;
        Vector3 thirdStart = center + new Vector3(8,-1.8f,0);
        Vector3 thirdEnd = center + new Vector3(-8,1.8f,0);
        Vector3 lastStart = center + Vector3.left * 8;
        Vector3 lastEnd = center + Vector3.right * 8;
        Vector3 position;
        int pass = Mathf.Clamp(Mathf.FloorToInt((frame - 8) / 14), 0, 3);
        float uPass = Mathf.Clamp01((frame - (8 + pass * 14)) / 8);
        float progress = Stage5_MainBoss.CrossSlashProgress(uPass);
        if (frame < 8) position = origin;
        else if (pass == 0) position = Vector3.Lerp(firstStart, up, progress);
        else if (pass == 1) position = Vector3.Lerp(up, down, progress);
        else if (pass == 2) position = Vector3.Lerp(thirdStart, thirdEnd, progress);
        else position = Vector3.Lerp(lastStart, lastEnd, progress);
        float direction = center.x >= origin.x ? 1 : -1;
        int mode = Mathf.RoundToInt(active);
        if (mode == 2) position = origin + Vector3.right * direction * 3 * Mathf.Clamp01((frame - 22) / 8);
        else if (mode == 3) position = Vector3.MoveTowards(origin, new Vector3(center.x - direction * .7f, origin.y, origin.z), Mathf.Clamp(frame - 12, 0, 72) / 12 * .8f);
        else if (mode == 4) position = origin + Vector3.up * .32f * Mathf.Clamp01(frame / 3) + Vector3.right * direction * 4 * Mathf.Clamp01((frame - 10) / 11);
        else if (mode == 5) { float t = Mathf.Clamp01(frame / 12); float u = t * t * (3 - 2 * t); position = origin + Vector3.right * direction * 3 * u + Vector3.up * Mathf.Sin(u * Mathf.PI) * 2.1f; }
        else if (mode == 6) position = origin;
        if (mode == 1)
        {
            bool left = frame >= 22 && frame < 50;
            Vector3 scale = originalScale; scale.x = Mathf.Abs(scale.x) * (left ? -1 : 1);
            transform.localScale = scale;
        }
        else transform.localScale = originalScale;
        transform.position = position;
        UnityEditor.SceneView.RepaintAll();
    }
#endif
}
