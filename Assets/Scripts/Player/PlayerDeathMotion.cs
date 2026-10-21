using System.Collections;
using UnityEngine;

// Generates no physics-root motion. The visual Body alone falls onto the floor.
public static class PlayerDeathMotion
{
    public static IEnumerator Play(GameObject player, Animator animator, Rigidbody2D rb, LayerMask groundLayers)
    {
        if (animator == null) animator = player.GetComponentInChildren<Animator>(true);
        if (animator != null) animator.enabled = false;
        var collider = player.GetComponent<Collider2D>();
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, Mathf.Min(0, rb.linearVelocity.y));
            float wait = 0;
            while (rb.simulated && collider != null && wait < 2f)
            {
                var origin = new Vector2(collider.bounds.center.x, collider.bounds.min.y + .06f);
                if (Physics2D.Raycast(origin, Vector2.down, .16f, groundLayers).collider != null) break;
                wait += Time.unscaledDeltaTime;
                yield return null;
            }
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }
        if (animator == null) yield break;
        var clip = Resources.Load<AnimationClip>("PlayerDeath");
        if (clip == null) { Debug.LogWarning("Tools/Player/Death/Generate And Setup를 먼저 실행하세요."); yield break; }
        Transform body = animator.transform.Find("Body");
        if (body == null) yield break;
        var nodes = body.GetComponentsInChildren<Transform>(true);
        var positions = new Vector3[nodes.Length];
        var rotations = new Quaternion[nodes.Length];
        var scales = new Vector3[nodes.Length];
        for (int i = 0; i < nodes.Length; i++)
        { positions[i] = nodes[i].localPosition; rotations[i] = nodes[i].localRotation; scales[i] = nodes[i].localScale; }
        animator.enabled = false;
        float floor = collider != null ? collider.bounds.min.y : Lowest(body);
        if (collider != null)
        {
            var hit = Physics2D.Raycast(collider.bounds.center, Vector2.down, collider.bounds.extents.y + .3f, groundLayers);
            if (hit.collider != null) floor = hit.point.y;
        }
        float elapsed = 0;
        while (elapsed < clip.length)
        {
            clip.SampleAnimation(animator.gameObject, elapsed);
            float blend = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / .12f));
            if (blend < 1)
                for (int i = 0; i < nodes.Length; i++)
                {
                    nodes[i].localPosition = Vector3.Lerp(positions[i], nodes[i].localPosition, blend);
                    nodes[i].localRotation = Quaternion.Slerp(rotations[i], nodes[i].localRotation, blend);
                    nodes[i].localScale = Vector3.Lerp(scales[i], nodes[i].localScale, blend);
                }
            body.position += Vector3.up * ((floor - Lowest(body)) * blend);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        clip.SampleAnimation(animator.gameObject, clip.length);
        body.position += Vector3.up * (floor - Lowest(body));
        yield return new WaitForSecondsRealtime(.25f);
    }
    static float Lowest(Transform body)
    {
        float result = float.PositiveInfinity;
        foreach (var sr in body.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.sprite == null || !sr.enabled || !sr.gameObject.activeInHierarchy || sr.name == "Sword" || sr.name == "Weapon") continue;
            foreach (Vector2 vertex in sr.sprite.vertices)
            {
                Vector3 p = new Vector3(sr.flipX ? -vertex.x : vertex.x, sr.flipY ? -vertex.y : vertex.y, 0);
                result = Mathf.Min(result, sr.transform.TransformPoint(p).y);
            }
        }
        return float.IsPositiveInfinity(result) ? body.position.y : result;
    }
}
