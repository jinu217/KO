using UnityEngine;

// Built-in render pipeline, game camera only. Overlay HUD/menu canvases render afterwards.
// The original static style leaves this component disabled and has no extra render pass.
[DisallowMultipleComponent, RequireComponent(typeof(Camera))]
public sealed class MachineNoisePostProcess : MonoBehaviour
{
    Material material;
    public bool Configure(MachineNoiseStyle style, float strength, float rate)
    {
        if (material == null)
        {
            var shader = Resources.Load<Shader>("MachineNoiseVariants");
            if (shader == null || !shader.isSupported) { enabled = false; return false; }
            material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }
        material.SetFloat("_Style", (float)style);
        material.SetFloat("_Strength", Mathf.Clamp(strength, 0, 1.64f));
        material.SetFloat("_Tick", Mathf.Floor(Time.unscaledTime * rate) % 4096);
        enabled = strength > .001f;
        return enabled;
    }
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (material != null) Graphics.Blit(source, destination, material);
        else Graphics.Blit(source, destination);
    }
    void OnDestroy() { if (material != null) Destroy(material); }
}
