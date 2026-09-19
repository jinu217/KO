using UnityEngine;

// 프리팹의 이미지와 효과 판정을 묶는 2스테이지 안전 필드 컴포넌트.
[DisallowMultipleComponent]
public sealed class Stage2_SafetyField : MonoBehaviour
{
    [SerializeField, Tooltip("필드 내부 판정에 사용할 2D 충돌체입니다. 비워두면 자신 또는 자식 오브젝트에서 자동으로 찾습니다.")]
    private Collider2D effectCollider;

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
