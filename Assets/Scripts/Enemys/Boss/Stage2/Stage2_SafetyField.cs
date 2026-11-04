using System.Collections;
using UnityEngine;

// 프리팹의 이미지와 효과 판정을 묶는 2스테이지 안전 필드 컴포넌트.
[DisallowMultipleComponent]
public sealed class Stage2_SafetyField : MonoBehaviour
{
    [SerializeField, Tooltip("필드 내부 판정에 사용할 2D 충돌체입니다. 비워두면 자신 또는 자식 오브젝트에서 자동으로 찾습니다.")]
    private Collider2D effectCollider;
    private Transform revealVisual;
    private SpriteRenderer revealRenderer;
    private SpriteMask revealMask;
    private Sprite maskSprite;
    private Texture2D maskTexture;
    private Vector3 maskFullSize;
    private SpriteMaskInteraction originalMaskInteraction;

    public Vector2 Center => effectCollider != null ? effectCollider.bounds.center : transform.position;
    public Bounds EffectBounds => effectCollider != null
        ? effectCollider.bounds
        : new Bounds(transform.position, Vector3.zero);

    public bool Initialize()
    {
        FindEffectCollider();
        if (effectCollider == null) return false;

        effectCollider.isTrigger = true;
        return true;
    }

    public bool Contains(Vector2 worldPosition)
    {
        return effectCollider != null && effectCollider.OverlapPoint(worldPosition);
    }

    public void PrepareReveal()
    {
        SpriteRenderer source = GetComponent<SpriteRenderer>();
        if (source == null || source.sprite == null) return;

        if (revealVisual != null) return;
        var visualObject = new GameObject("RevealMask");
        revealVisual = visualObject.transform;
        revealVisual.SetParent(transform, false);
        Bounds bounds = source.sprite.bounds;
        revealVisual.localPosition = new Vector3(source.flipX ? -bounds.center.x : bounds.center.x,
            source.flipY ? -bounds.center.y : bounds.center.y, 0f);
        maskFullSize = new Vector3(bounds.size.x, bounds.size.y + 0.02f, 1f);
        revealVisual.localScale = new Vector3(0f, maskFullSize.y, 1f);
        maskTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        maskTexture.SetPixel(0, 0, Color.white);
        maskTexture.Apply();
        maskSprite = Sprite.Create(maskTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        revealMask = visualObject.AddComponent<SpriteMask>();
        revealMask.sprite = maskSprite;
        revealMask.isCustomRangeActive = true;
        revealMask.frontSortingLayerID = revealMask.backSortingLayerID = source.sortingLayerID;
        revealMask.frontSortingOrder = source.sortingOrder;
        revealMask.backSortingOrder = source.sortingOrder - 1;
        revealRenderer = source;
        originalMaskInteraction = source.maskInteraction;
        source.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        source.enabled = false;
        if (effectCollider != null) effectCollider.enabled = false;
    }

    public IEnumerator RevealFromCenter(float slowDuration, float fastDuration)
    {
        if (revealVisual != null)
        {
            float elapsed = 0f;
            float duration = slowDuration + fastDuration;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                // 이펙트가 끝날 때 15%, 종료 후 빠른 구간에서 100%까지 연다.
                float width = elapsed < slowDuration
                    ? 0.15f * Mathf.Clamp01(elapsed / slowDuration)
                    : fastDuration > 0f
                        ? Mathf.Lerp(0.15f, 1f, Mathf.Clamp01((elapsed - slowDuration) / fastDuration))
                        : 1f;
                if (revealRenderer != null) revealRenderer.enabled = width > 0f;
                revealVisual.localScale = new Vector3(maskFullSize.x * width, maskFullSize.y, 1f);
                yield return null;
            }
            revealVisual.localScale = maskFullSize;
            if (revealRenderer != null)
            {
                revealRenderer.maskInteraction = originalMaskInteraction;
                revealRenderer.enabled = true;
            }
            if (revealMask != null) revealMask.enabled = false;
        }
        if (effectCollider != null) effectCollider.enabled = true;
    }

    private void OnDestroy()
    {
        if (maskSprite != null) Destroy(maskSprite);
        if (maskTexture != null) Destroy(maskTexture);
    }

    private void Reset()
    {
        FindEffectCollider();
        if (effectCollider != null) effectCollider.isTrigger = true;
    }

    private void OnValidate()
    {
        FindEffectCollider();
        if (effectCollider != null) effectCollider.isTrigger = true;
    }

    private void FindEffectCollider()
    {
        if (effectCollider == null) effectCollider = GetComponent<Collider2D>();
        if (effectCollider == null) effectCollider = GetComponentInChildren<Collider2D>();
    }
}
