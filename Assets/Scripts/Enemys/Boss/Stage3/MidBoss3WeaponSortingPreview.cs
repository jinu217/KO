using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class MidBoss3WeaponSortingPreview : MonoBehaviour
{
    [Range(0f, 1f)] public float backLayer;
    private SpriteRenderer sprite;
    private bool previewApplied;

    private void OnDidApplyAnimationProperties()
    {
        if (sprite == null) sprite = GetComponent<SpriteRenderer>();
        sprite.sortingLayerName = backLayer >= .5f ? "Back" : "Front";
        previewApplied = !Application.isPlaying;
    }
#if UNITY_EDITOR
    private void Update()
    {
        if (!Application.isPlaying && previewApplied && !AnimationMode.InAnimationMode())
        {
            RestoreFront();
            previewApplied = false;
        }
    }
#endif
    private void OnDisable() { RestoreFront(); }
    private void RestoreFront()
    {
        if (sprite == null) sprite = GetComponent<SpriteRenderer>();
        if (sprite != null) sprite.sortingLayerName = "Front";
    }
}
