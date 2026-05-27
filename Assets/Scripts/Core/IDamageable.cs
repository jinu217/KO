using UnityEngine;

// 피격 가능한 모든 오브젝트(플레이어/적/보스)가 구현하는 공통 인터페이스
// applyKnockback: 넉백 적용 여부 / applyIFrame: 무적 프레임 적용 여부
public interface IDamageable
{
    void TakeDamage(float damage, Vector2 sourcePos, bool applyKnockback = true, bool applyIFrame = true);
}
