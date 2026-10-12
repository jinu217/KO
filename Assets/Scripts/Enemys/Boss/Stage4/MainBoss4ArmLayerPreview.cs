using UnityEngine;

// Place outside Editor. Supports Animation window sampling in the Scene view.
[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
public sealed class MainBoss4ArmLayerPreview : MonoBehaviour
{
    [SerializeField, HideInInspector] private float forceFront;
    [SerializeField, HideInInspector] private string defaultLayer;
    private SpriteRenderer target;
    public void CaptureDefault()
    {
        if(target == null) target = GetComponent<SpriteRenderer>();
        if(string.IsNullOrEmpty(defaultLayer)) defaultLayer = target.sortingLayerName;
        Apply();
    }
    private void OnEnable() { CaptureDefault(); }
    private void OnDidApplyAnimationProperties() { Apply(); }
    private void LateUpdate() { Apply(); }
    private void OnDisable()
    {
        if(target != null && !string.IsNullOrEmpty(defaultLayer)) target.sortingLayerName = defaultLayer;
    }
    private void Apply()
    {
        if(target == null) target = GetComponent<SpriteRenderer>();
        if(target != null && !string.IsNullOrEmpty(defaultLayer))
            target.sortingLayerName = forceFront > .5f ? "Front" : defaultLayer;
    }
}
