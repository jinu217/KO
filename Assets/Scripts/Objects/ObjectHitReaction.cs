using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// A short-lived visual snapshot keeps colliders and gameplay timing unchanged.
public sealed class ObjectHitReaction : MonoBehaviour
{
    static readonly Dictionary<GameObject, ObjectHitReaction> active = new Dictionary<GameObject, ObjectHitReaction>();
    readonly List<SpriteRenderer> originals = new List<SpriteRenderer>();
    readonly List<bool> previousHidden = new List<bool>();
    readonly List<SpriteRenderer> copies = new List<SpriteRenderer>();
    readonly List<Color> colors = new List<Color>();
    GameObject source;
    float elapsed, size;
    const float Duration = 0.18f;
    Vector3 origin;
    bool fatal;
    public static void Play(GameObject target, bool finalHit = false)
    {
        if (target == null || !target.activeInHierarchy || !Application.isPlaying) return;
        if (active.TryGetValue(target, out var previous) && previous != null)
        {
            previous.Restore(); previous.gameObject.SetActive(false); Destroy(previous.gameObject);
        }
        var sprites = target.GetComponentsInChildren<SpriteRenderer>();
        if (sprites.Length == 0) return;
        var go = new GameObject("Hit reaction: " + target.name) { hideFlags = HideFlags.DontSave };
        SceneManager.MoveGameObjectToScene(go, target.scene);
        var reaction = go.AddComponent<ObjectHitReaction>();
        reaction.source = target; reaction.fatal = finalHit;
        Bounds bounds = sprites[0].bounds;
        foreach (var sprite in sprites) if (sprite.enabled && sprite.sprite != null) bounds.Encapsulate(sprite.bounds);
        reaction.origin = new Vector3(bounds.center.x, bounds.min.y, target.transform.position.z);
        go.transform.position = reaction.origin;
        reaction.size = Mathf.Clamp(bounds.size.x, 0.3f, 3f);
        foreach (var sprite in sprites)
        {
            if (!sprite.enabled || sprite.sprite == null || sprite.forceRenderingOff) continue;
            var child = new GameObject(sprite.name); child.transform.SetParent(go.transform, false);
            child.transform.position = sprite.transform.position; child.transform.rotation = sprite.transform.rotation;
            child.transform.localScale = sprite.transform.lossyScale;
            var copy = child.AddComponent<SpriteRenderer>();
            copy.sprite = sprite.sprite; copy.sharedMaterial = sprite.sharedMaterial; copy.color = sprite.color;
            copy.flipX = sprite.flipX; copy.flipY = sprite.flipY; copy.drawMode = sprite.drawMode; copy.size = sprite.size;
            copy.sortingLayerID = sprite.sortingLayerID; copy.sortingOrder = sprite.sortingOrder;
            var group = sprite.GetComponentInParent<SortingGroup>();
            if (group != null)
            {
                // Keep the original group order while retaining ordering between its children.
                var visualGroup = go.GetComponent<SortingGroup>() ?? go.AddComponent<SortingGroup>();
                visualGroup.sortingLayerID = group.sortingLayerID; visualGroup.sortingOrder = group.sortingOrder;
            }
            reaction.originals.Add(sprite); reaction.previousHidden.Add(sprite.forceRenderingOff);
            reaction.copies.Add(copy); reaction.colors.Add(sprite.color); sprite.forceRenderingOff = true;
        }
        active[target] = reaction;
    }
    void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / Duration);
        float envelope = 1f - t;
        float pulse = Mathf.Sin(t * Mathf.PI);
        transform.position = origin + Vector3.right * Mathf.Sin(t * Mathf.PI * 4) * envelope * size * 0.035f;
        transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * Mathf.PI * 2) * envelope * 5f);
        transform.localScale = new Vector3(1f + pulse * 0.09f, 1f - pulse * 0.1f, 1f);
        bool disappearing = fatal || source == null || !source.activeInHierarchy;
        for (int i = 0; i < copies.Count; i++)
        {
            Color color = colors[i];
            if (disappearing) color.a *= 1f - t;
            copies[i].color = color;
        }
        if (t >= 1f) { Restore(); Destroy(gameObject); }
    }
    void Restore()
    {
        for (int i = 0; i < originals.Count; i++) if (originals[i] != null) originals[i].forceRenderingOff = previousHidden[i];
        originals.Clear(); previousHidden.Clear();
        if (!ReferenceEquals(source, null) && active.TryGetValue(source, out var current) && current == this) active.Remove(source);
    }
    void OnDisable() { Restore(); }
    void OnDestroy() { Restore(); }
}
